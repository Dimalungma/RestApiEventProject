using System.ComponentModel.DataAnnotations;

namespace RestApiEventProject.DTO;
/// <summary>
/// Запрос для создания очередного объекта
/// </summary>
public record CreateEventRequestDto : IValidatableObject
{
    /// <summary>
    /// Заголовок события
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1, ErrorMessage ="Превышение длины в 200 символов или пустой ввод")]
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Подробное описание (опционально)
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Время начала события
    /// </summary>
    public DateTime StartAt { get; set; }
    /// <summary>
    /// Время окончания события
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Проверка на базовую логику временного континуума
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt == DateTime.MinValue)
        {
            yield return new ValidationResult(
                "StartAt имеет значение по умолчанию или не задан",
                new[] { nameof(StartAt) });
        }
        if (EndAt == DateTime.MinValue)
        {
            yield return new ValidationResult(
                "EndAt имеет значение по умолчанию или не задан",
                new[] { nameof(EndAt) });
        }
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "EndAt должно быть позже StartAt.",
                new[] { nameof(EndAt), nameof(StartAt) });
        }
    }
}