using System.Text.Json;
using Confluent.Kafka;
using EventsService.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestApiEventProject.Contracts;

namespace EventsService.Infrastructure.Messaging;

public sealed class KafkaEventSeatEventPublisher : IEventSeatEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventSeatEventPublisher> _logger;

    public KafkaEventSeatEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaEventSeatEventPublisher> logger)
    {
        _logger = logger;

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public Task PublishEventSeatReservedAsync(
        long bookingId,
        int eventId,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var @event = new EventSeatReserved(
            bookingId,
            reservedAtUtc);

        return PublishAsync(
            KafkaTopics.EventSeatReserved,
            eventId,
            @event,
            cancellationToken);
    }

    public Task PublishEventSeatUnavailableAsync(
        long bookingId,
        int eventId,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var @event = new EventSeatUnavailable(
            bookingId,
            reason,
            rejectedAtUtc);

        return PublishAsync(
            KafkaTopics.EventSeatUnavailable,
            eventId,
            @event,
            cancellationToken);
    }

    private async Task PublishAsync<T>(
        string topic,
        int eventId,
        T @event,
        CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = eventId.ToString(),
            Value = JsonSerializer.Serialize(@event)
        };

        var result = await _producer.ProduceAsync(
            topic,
            message,
            cancellationToken);

        _logger.LogInformation(
            $"Kafka событие {typeof(T).Name} опубликовано в {result.TopicPartitionOffset}");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}