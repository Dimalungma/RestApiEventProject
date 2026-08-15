namespace EventsService.Domain;
public enum ChangeTotalSeatsResult //Отдельный enum, чтобы хранились только операции связанные с TryChangeSeats
{
    Success,
    InvalidTotalSeats,
    TotalSeatsLessThanReservedSeats
}
