using System.Diagnostics.CodeAnalysis;

namespace RestApiEventProject.Models;

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
    public List<Booking> Bookings { get; set; } = [];

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats += count;

        if (AvailableSeats > TotalSeats)
        {
            AvailableSeats = TotalSeats;
        }
    }

    public bool TryChangeTotalSeats(int newTotalSeats)
    {
        if (newTotalSeats <= 0)
        {
            return false;
        }

        var reservedSeats = TotalSeats - AvailableSeats;

        if (newTotalSeats < reservedSeats)
        {
            return false;
        }

        TotalSeats = newTotalSeats;
        AvailableSeats = newTotalSeats - reservedSeats;

        return true;
    }
}
