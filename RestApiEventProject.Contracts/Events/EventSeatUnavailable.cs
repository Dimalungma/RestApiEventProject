namespace RestApiEventProject.Contracts;

public sealed record EventSeatUnavailable(
    long BookingId,
    string Reason,
    DateTime RejectedAtUtc);