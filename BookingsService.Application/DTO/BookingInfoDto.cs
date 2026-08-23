using BookingsService.Domain;

namespace BookingsService.Application;
/// <summary>
/// DTO информации о бронированиях
/// </summary>
public class BookingInfoDto
{
    /// <summary>
    /// Уникальный номер бронирования
    /// </summary>
    public long Id { get; set; }
    //Планируется подключение к БД, и я не видел чтобы в 5 спринте как то добавляли "приватный" id для сохранения
    /// <summary>
    /// Id мероприятия, на которое бронируется билет
    /// </summary>
    public int EventId { get; set; } 
    //Так как guid ломает все прошлые тесты, инициализацию, и потенциально БД, оставляю int,
    //так как не вижу, какой прирост даст переход на guid, помимо геморроя с b-tree базой
    /// <summary>
    /// Статус бронирования - в процессе, завершен, отклонен
    /// </summary>
    public BookingStatus Status { get; set; }
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Дата фактического бронирования
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
