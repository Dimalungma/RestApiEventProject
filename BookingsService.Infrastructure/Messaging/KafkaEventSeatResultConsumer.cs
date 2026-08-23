using System.Text.Json;
using BookingsService.Application;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestApiEventProject.Contracts;

namespace BookingsService.Infrastructure.Messaging;

public sealed class KafkaEventSeatResultConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaEventSeatResultConsumer> _logger;

    public KafkaEventSeatResultConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaEventSeatResultConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {

        //Чтобы не блокировался запуск host до первого Consume().
        await Task.Yield();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer =
            new ConsumerBuilder<string, string>(consumerConfig).Build();

        consumer.Subscribe(
        [
            KafkaTopics.EventSeatReserved,
            KafkaTopics.EventSeatUnavailable
        ]);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> consumeResult;

                try
                {
                    consumeResult = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(
                        exception,
                        "Ошибка чтения сообщения Kafka");

                    await Task.Delay(
                        TimeSpan.FromSeconds(1),
                        stoppingToken);

                    continue;
                }

                try
                {
                    await HandleMessageAsync(
                        consumeResult,
                        stoppingToken);

                    consumer.Commit(consumeResult);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(
                        exception,
                        $"Некорректное сообщение в топике {consumeResult.Topic}, пропускаю");

                    consumer.Commit(consumeResult);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        $"Ошибка обработки сообщения {consumeResult.TopicPartitionOffset}");

                    //Возвращаем consumer на проблемное сообщение.
                    consumer.Seek(consumeResult.TopicPartitionOffset);

                    await Task.Delay(
                        TimeSpan.FromSeconds(1),
                        stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleMessageAsync(
        ConsumeResult<string, string> consumeResult,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var handler =
            scope.ServiceProvider.GetRequiredService<IEventSeatResultHandler>();

        switch (consumeResult.Topic)
        {
            case KafkaTopics.EventSeatReserved:
                {
                    var @event =
                        JsonSerializer.Deserialize<EventSeatReserved>(
                            consumeResult.Message.Value)
                        ?? throw new JsonException(
                            "EventSeatReserved имеет пустое тело");

                    await handler.HandleSeatReservedAsync(
                        @event.BookingId,
                        cancellationToken);

                    break;
                }

            case KafkaTopics.EventSeatUnavailable:
                {
                    var @event =
                        JsonSerializer.Deserialize<EventSeatUnavailable>(
                            consumeResult.Message.Value)
                        ?? throw new JsonException(
                            "EventSeatUnavailable имеет пустое тело");

                    await handler.HandleSeatUnavailableAsync(
                        @event.BookingId,
                        @event.Reason,
                        cancellationToken);

                    break;
                }
        }
    }
}