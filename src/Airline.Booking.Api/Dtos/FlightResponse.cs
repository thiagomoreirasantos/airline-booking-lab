namespace Airline.Booking.Api.Dtos;

public sealed record FlightResponse(
    Guid Id,
    string Origin,
    string Destination,
    string Date,
    decimal Price);
