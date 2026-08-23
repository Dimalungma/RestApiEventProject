using System.Text.Json;
using Confluent.Kafka;
using EventsService.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestApiEventProject.Contracts;

namespace EventsService.Infrastructure.Messaging;

public sealed class KafkaBookingLifecycleConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaBookingLifecycleConsumer> _logger;

    public KafkaBookingLifecycleConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaBookingLifecycleConsumer> logger)
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
            KafkaTopics.BookingCreated,
            KafkaTopics.BookingCancelled
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
            scope.ServiceProvider.GetRequiredService<IBookingLifecycleHandler>();

        switch (consumeResult.Topic)
        {
            case KafkaTopics.BookingCreated:
                {
                    var @event =
                        JsonSerializer.Deserialize<BookingCreated>(
                            consumeResult.Message.Value)
                        ?? throw new JsonException(
                            "BookingCreated имеет пустое тело");

                    await handler.HandleBookingCreatedAsync(
                        @event.BookingId,
                        @event.EventId,
                        @event.SeatsCount,
                        cancellationToken);

                    break;
                }

            case KafkaTopics.BookingCancelled:
                {
                    var @event =
                        JsonSerializer.Deserialize<BookingCancelled>(
                            consumeResult.Message.Value)
                        ?? throw new JsonException(
                            "BookingCancelled имеет пустое тело");

                    await handler.HandleBookingCancelledAsync(
                        @event.BookingId,
                        @event.EventId,
                        @event.SeatsCount,
                        cancellationToken);

                    break;
                }
        }
    }
}