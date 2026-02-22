using Airline.Booking.Api.Domain;

namespace Airline.Booking.Api.Stores;

public sealed class FlightStore
{
    private readonly List<Flight> _flights =
    [
        new() { Origin = "OPO", Destination = "LIS", Date = new DateOnly(2026, 3, 1), Price = 49.99m },
        new() { Origin = "OPO", Destination = "LIS", Date = new DateOnly(2026, 3, 2), Price = 59.99m },
        new() { Origin = "LIS", Destination = "OPO", Date = new DateOnly(2026, 3, 1), Price = 45.00m },
        new() { Origin = "LIS", Destination = "FAO", Date = new DateOnly(2026, 3, 1), Price = 39.99m },
        new() { Origin = "OPO", Destination = "FAO", Date = new DateOnly(2026, 3, 3), Price = 69.99m },
        new() { Origin = "FAO", Destination = "LIS", Date = new DateOnly(2026, 3, 1), Price = 35.00m },
        new() { Origin = "LIS", Destination = "MAD", Date = new DateOnly(2026, 3, 1), Price = 89.99m },
        new() { Origin = "OPO", Destination = "MAD", Date = new DateOnly(2026, 3, 2), Price = 99.99m },
    ];

    public IReadOnlyList<Flight> Search(string from, string to, DateOnly date) =>
        _flights
            .Where(f => f.Origin.Equals(from, StringComparison.OrdinalIgnoreCase)
                     && f.Destination.Equals(to, StringComparison.OrdinalIgnoreCase)
                     && f.Date == date)
            .ToList();

    public Flight? GetById(Guid id) =>
        _flights.FirstOrDefault(f => f.Id == id);
}
