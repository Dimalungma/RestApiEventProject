namespace RestApiEventProject.Application;

public enum BookingCancelError 
{
    BookingNotFound, //Опять-таки, я против использования исключений для бизнес логики, поэтому идем через error'ы
    Forbidden
}
