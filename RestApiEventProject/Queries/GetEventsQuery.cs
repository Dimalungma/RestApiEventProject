using System.ComponentModel.DataAnnotations;

namespace RestApiEventProject.Queries;
/// <summary>
/// Уточняющий запрос с фильтрами для GET Events
/// </summary>
public class GetEventsQuery : IValidatableObject
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
    /// <summary>
    /// Страница данных, которая будет выгружена (после фильтрации)
    /// </summary>
    public int Page { get; set; } = 1;
    /// <summary>
    /// Количество событий на 1 странице
    /// </summary>
    public int PageSize { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Page <= 0)
        {
            yield return new ValidationResult(
                "Page должно быть больше 0.",
                new[] { nameof(Page) });
        }

        if (PageSize <= 0)
        {
            yield return new ValidationResult(
                "PageSize должно быть больше 0.",
                new[] { nameof(PageSize) });
        }

        if (From.HasValue && To.HasValue && From > To)
        {
            yield return new ValidationResult(
                "Параметр From не может быть больше To.",
                new[] { nameof(From), nameof(To) });
        }
    }
}
