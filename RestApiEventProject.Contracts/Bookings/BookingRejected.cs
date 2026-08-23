namespace RestApiEventProject.Contracts;

public sealed record BookingRejected(
    long BookingId,
    int EventId,
    long UserId,
    string Reason,
    DateTime RejectedAtUtc);