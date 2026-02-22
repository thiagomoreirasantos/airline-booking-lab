namespace Airline.Booking.Api.Dtos;

public sealed record CreateBookingRequest(Guid FlightId, string PassengerName);
