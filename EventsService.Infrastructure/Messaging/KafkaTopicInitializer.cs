using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestApiEventProject.Contracts;

namespace EventsService.Infrastructure.Messaging;

public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(
        IOptions<KafkaOptions> options,
        ILogger<KafkaTopicInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var adminClient =
            new AdminClientBuilder(
                new AdminClientConfig
                {
                    BootstrapServers = _options.BootstrapServers
                })
                .Build();

        var topicNames = new[]
        {
            KafkaTopics.BookingCreated,
            KafkaTopics.BookingCancelled,
            KafkaTopics.EventSeatReserved,
            KafkaTopics.EventSeatUnavailable,
            KafkaTopics.BookingConfirmed,
            KafkaTopics.BookingRejected
        };

        var topicSpecifications = topicNames
            .Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = 3,
                ReplicationFactor = 1
            })
            .ToArray();

        try
        {
            await adminClient.CreateTopicsAsync(topicSpecifications);

            _logger.LogInformation(
                "Kafka топики успешно созданы");
        }
        catch (CreateTopicsException exception)
        {
            foreach (var result in exception.Results)
            {
                if (result.Error.Code == ErrorCode.TopicAlreadyExists)
                {
                    _logger.LogInformation(
                        $"Kafka топик {result.Topic} уже существует");

                    continue;
                }

                _logger.LogError(
                    $"Не удалось создать Kafka топик {result.Topic}: {result.Error.Reason}");
            }
        }
        catch (Exception exception)
        {
            //По требованиям спринта ошибка создания топика
            //не должна валить весь EventsService, хотя непонятно зачем он тогда работает
            _logger.LogError(
                exception,
                "Не удалось выполнить инициализацию Kafka топиков");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}