namespace RestApiEventProject.Contracts;

public sealed record BookingCreated(
    long BookingId,
    int EventId,
    int SeatsCount,
    DateTime CreatedAtUtc);