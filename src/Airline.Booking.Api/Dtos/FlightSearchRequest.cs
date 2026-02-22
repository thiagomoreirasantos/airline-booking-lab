namespace Airline.Booking.Api.Dtos;

public sealed record FlightSearchRequest(string From, string To, string Date);
