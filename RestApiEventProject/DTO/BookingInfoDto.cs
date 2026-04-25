using RestApiEventProject.Models;

namespace RestApiEventProject.DTO;

public class BookingInfoDto
{
    public Guid Id { get; set; }

    public int EventId { get; set; } 
    //Так как guid ломает все прошлые тесты, инициализацию, и потенциально БД, оставляю int,
    //так как не вижу, какой прирост даст переход на guid, помимо геморроя с b-tree базой

    public BookingStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
