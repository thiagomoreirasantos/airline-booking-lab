using Airline.Booking.Api.Domain;
using Airline.Booking.Api.Dtos;
using Airline.Booking.Api.Logging;
using Airline.Booking.Api.Stores;

namespace Airline.Booking.Api.Endpoints;

public static class BookingEndpoints
{
    private static readonly Random s_random = new();

    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings");

        group.MapPost("/", CreateBooking).WithName("CreateBooking");
        group.MapGet("/{id:guid}", GetBooking).WithName("GetBooking");
        group.MapPost("/{id:guid}/confirm", ConfirmBooking).WithName("ConfirmBooking");
        group.MapPost("/{id:guid}/cancel", CancelBooking).WithName("CancelBooking");
    }

    private static void SimulateRandomFailure(Guid bookingId, ILogger logger)
    {
        if (s_random.Next(100) < 10) // 10% chance of failure
        {
            var ex = new InvalidOperationException("Payment gateway timeout");
            BookingLogMessages.SimulatedFailure(logger, bookingId, ex);
            throw ex;
        }
    }

    private static IResult CreateBooking(
        CreateBookingRequest request,
        BookingStore bookingStore,
        FlightStore flightStore,
        ILogger<Program> logger)
    {
        try
        {
            BookingLogMessages.CreatingBooking(logger, request.PassengerName, request.FlightId);

            var flight = flightStore.GetById(request.FlightId);
            if (flight is null)
            {
                BookingLogMessages.FlightNotFoundForBooking(logger, request.FlightId, request.PassengerName);
                return Results.NotFound(new { error = "Flight not found." });
            }

            var booking = bookingStore.Create(request.FlightId, request.PassengerName);

            BookingLogMessages.BookingCreated(
                logger, booking.Id, booking.Status.ToString(), booking.FlightId);

            return Results.Created(
                $"/api/bookings/{booking.Id}",
                ToResponse(booking));
        }
        catch (Exception ex)
        {
            BookingLogMessages.BookingCreationFailed(logger, request.PassengerName, request.FlightId, ex);
            return Results.StatusCode(500);
        }
    }

    private static IResult GetBooking(
        Guid id,
        BookingStore bookingStore,
        ILogger<Program> logger)
    {
        BookingLogMessages.RetrievingBooking(logger, id);

        var booking = bookingStore.GetById(id);
        if (booking is null)
        {
            BookingLogMessages.BookingNotFound(logger, id);
            return Results.NotFound(new { error = "Booking not found." });
        }

        return Results.Ok(ToResponse(booking));
    }

    private static IResult ConfirmBooking(
        Guid id,
        BookingStore bookingStore,
        ILogger<Program> logger)
    {
        try
        {
            BookingLogMessages.ConfirmingBooking(logger, id);

            var booking = bookingStore.GetById(id);
            if (booking is null)
            {
                BookingLogMessages.BookingNotFound(logger, id);
                return Results.NotFound(new { error = "Booking not found." });
            }

            if (booking.Status != BookingStatus.Pending)
            {
                BookingLogMessages.InvalidBookingStatusTransition(
                    logger, id, "confirmed", booking.Status.ToString());
                return Results.Conflict(new { error = $"Booking cannot be confirmed. Current status: {booking.Status}" });
            }

            SimulateRandomFailure(id, logger);

            bookingStore.TryUpdateStatus(id, BookingStatus.Confirmed);
            BookingLogMessages.BookingConfirmed(logger, id);

            return Results.Ok(ToResponse(booking));
        }
        catch (Exception ex)
        {
            BookingLogMessages.BookingOperationFailed(logger, id, ex);
            return Results.StatusCode(500);
        }
    }

    private static IResult CancelBooking(
        Guid id,
        BookingStore bookingStore,
        ILogger<Program> logger)
    {
        try
        {
            BookingLogMessages.CancelingBooking(logger, id);

            var booking = bookingStore.GetById(id);
            if (booking is null)
            {
                BookingLogMessages.BookingNotFound(logger, id);
                return Results.NotFound(new { error = "Booking not found." });
            }

            if (booking.Status == BookingStatus.Canceled)
            {
                BookingLogMessages.InvalidBookingStatusTransition(
                    logger, id, "canceled", booking.Status.ToString());
                return Results.Conflict(new { error = "Booking is already canceled." });
            }

            SimulateRandomFailure(id, logger);

            bookingStore.TryUpdateStatus(id, BookingStatus.Canceled);
            BookingLogMessages.BookingCanceled(logger, id);

            return Results.Ok(ToResponse(booking));
        }
        catch (Exception ex)
        {
            BookingLogMessages.BookingOperationFailed(logger, id, ex);
            return Results.StatusCode(500);
        }
    }

    private static BookingResponse ToResponse(BookingEntity booking) =>
        new(booking.Id, booking.FlightId, booking.PassengerName, booking.Status.ToString());
}
