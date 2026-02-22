namespace Airline.Booking.Api.Domain;

public sealed class BookingEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid FlightId { get; init; }
    public required string PassengerName { get; init; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
}
