namespace RestApiEventProject.Models;
/// <summary>
/// Уточняющий запрос с фильтрами для GET Events
/// </summary>
public class GetEventsQuery
{
    /// <summary>
    /// Имя мепроприятия
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// Дата, после которой должно быть мероприятие
    /// </summary>
    public DateTime? From { get; set; }
    /// <summary>
    /// Дата, до которой должно быть мероприятие
    /// </summary>
    public DateTime? To { get; set; }
}
