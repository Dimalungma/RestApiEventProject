namespace RestApiEventProject.Contracts;

public sealed record EventSeatReserved(
    long BookingId,
    DateTime ReservedAtUtc);