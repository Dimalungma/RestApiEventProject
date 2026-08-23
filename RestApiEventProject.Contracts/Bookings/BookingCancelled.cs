namespace RestApiEventProject.Contracts;

public sealed record BookingCancelled(
    long BookingId,
    int EventId,
    int SeatsCount,
    DateTime CancelledAtUtc);