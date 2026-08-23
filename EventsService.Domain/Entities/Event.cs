using System.Diagnostics.CodeAnalysis;

namespace EventsService.Domain;

public class Event
{
    private Event()
    {
        Title = null!;
    }
    [SetsRequiredMembers]
    public Event( //Конструктор для маппера
    string title,
    string? description,
    DateTime startAt,
    DateTime endAt,
    int totalSeats)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    public  int Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public required DateTime StartAt { get; set; }

    public required DateTime EndAt { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public ReserveSeatsResult TryReserveSeats(int count = 1)
    {
        if (count <= 0)
        {
            return ReserveSeatsResult.InvalidSeatsCount;
        }

        if (StartAt <= DateTime.UtcNow)
        {
            return ReserveSeatsResult.EventAlreadyStarted; //Теперь о дате начала знает только EventService
        }

        if (AvailableSeats < count)
        {
            return ReserveSeatsResult.NoAvailableSeats;
        }

        AvailableSeats -= count;

        return ReserveSeatsResult.Success;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;

        if (AvailableSeats > TotalSeats)
        {
            AvailableSeats = TotalSeats;
        }
    }

    public ChangeTotalSeatsResult TryChangeTotalSeats(int newTotalSeats)
    {
        if (newTotalSeats <= 0)
        {
            return ChangeTotalSeatsResult.InvalidTotalSeats;
        }

        var reservedSeats = TotalSeats - AvailableSeats;

        if (newTotalSeats < reservedSeats)
        {
            return ChangeTotalSeatsResult.TotalSeatsLessThanReservedSeats;
        }

        TotalSeats = newTotalSeats;
        AvailableSeats = newTotalSeats - reservedSeats;

        return ChangeTotalSeatsResult.Success;
    }
}
