namespace RestApiEventProject.DTO;
/// <summary>
/// Ответ на запрос мероприятия
/// </summary>
public record EventResponseDto
{
    /// <summary>
    /// Номер мероприятия
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Название мероприятия
    /// </summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Расширенное текстовое описание
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Дата начала
    /// </summary>
    public DateTime StartAt { get; set; }
    /// <summary>
    /// Дата конца
    /// </summary>
    public DateTime EndAt { get; set; }
    /// <summary>
    /// Все места на мероприятии
    /// </summary>
    public int TotalSeats { get; set; }
    /// <summary>
    /// Доступное число мест
    /// </summary>
    public int AvailableSeats { get; set; }
}
