namespace Airline.Booking.Api.Dtos;

public sealed record BookingResponse(
    Guid Id,
    Guid FlightId,
    string PassengerName,
    string Status);
