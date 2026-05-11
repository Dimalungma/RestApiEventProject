namespace RestApiEventProject.Models;

public class Event
{
    public  int Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public required DateTime StartAt { get; set; }

    public required DateTime EndAt { get; set; }

    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

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
}
