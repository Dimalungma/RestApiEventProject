using BookingsService.Application;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestApiEventProject.Contracts;
using System.Text.Json;

namespace BookingsService.Infrastructure.Messaging;

public sealed class KafkaBookingEventPublisher : IBookingEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaBookingEventPublisher> _logger;

    public KafkaBookingEventPublisher(
        IOptions<KafkaOptions> options,
        ILogger<KafkaBookingEventPublisher> logger)
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

    public Task PublishBookingCreatedAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var @event = new BookingCreated(
            bookingId,
            eventId,
            seatsCount,
            createdAtUtc);

        return PublishAsync(
            KafkaTopics.BookingCreated,
            eventId,
            @event,
            cancellationToken);
    }

    public Task PublishBookingConfirmedAsync(
        long bookingId,
        int eventId,
        long userId,
        int seatsCount,
        DateTime confirmedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var @event = new BookingConfirmed(
            bookingId,
            eventId,
            userId,
            seatsCount,
            confirmedAtUtc);

        return PublishAsync(
            KafkaTopics.BookingConfirmed,
            eventId,
            @event,
            cancellationToken);
    }

    public Task PublishBookingRejectedAsync(
        long bookingId,
        int eventId,
        long userId,
        string reason,
        DateTime rejectedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var @event = new BookingRejected(
            bookingId,
            eventId,
            userId,
            reason,
            rejectedAtUtc);

        return PublishAsync(
            KafkaTopics.BookingRejected,
            eventId,
            @event,
            cancellationToken);
    }

    public Task PublishBookingCancelledAsync(
        long bookingId,
        int eventId,
        int seatsCount,
        DateTime cancelledAtUtc,
        CancellationToken cancellationToken = default)
    {
        var @event = new BookingCancelled(
            bookingId,
            eventId,
            seatsCount,
            cancelledAtUtc);

        return PublishAsync(
            KafkaTopics.BookingCancelled,
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

        var result = await _producer.ProduceAsync(topic, message, cancellationToken);

        _logger.LogInformation($"Kafka событие {typeof(T).Name} опубликовано в {result.TopicPartitionOffset}");
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}