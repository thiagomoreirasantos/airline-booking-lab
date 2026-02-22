namespace Airline.Booking.Api.Domain;

public sealed class Flight
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Origin { get; init; }
    public required string Destination { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal Price { get; init; }
}
