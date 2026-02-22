using System.Collections.Concurrent;
using Airline.Booking.Api.Domain;

namespace Airline.Booking.Api.Stores;

public sealed class BookingStore
{
    private readonly ConcurrentDictionary<Guid, BookingEntity> _bookings = new();

    public BookingEntity Create(Guid flightId, string passengerName)
    {
        var booking = new BookingEntity
        {
            FlightId = flightId,
            PassengerName = passengerName
        };

        _bookings[booking.Id] = booking;
        return booking;
    }

    public BookingEntity? GetById(Guid id) =>
        _bookings.GetValueOrDefault(id);

    public bool TryUpdateStatus(Guid id, BookingStatus status)
    {
        if (!_bookings.TryGetValue(id, out var booking))
            return false;

        booking.Status = status;
        return true;
    }
}
