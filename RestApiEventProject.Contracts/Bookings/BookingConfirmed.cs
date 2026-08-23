namespace RestApiEventProject.Contracts;

public sealed record BookingConfirmed(
    long BookingId,
    int EventId,
    long UserId,
    int SeatsCount,
    DateTime ConfirmedAtUtc);