namespace Airline.Booking.Api.Logging;

public static partial class BookingLogMessages
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message = "Creating booking for passenger {PassengerName} on flight {FlightId}")]
    public static partial void CreatingBooking(
        ILogger logger, string passengerName, Guid flightId);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Booking {BookingId} created with status {Status} for flight {FlightId}")]
    public static partial void BookingCreated(
        ILogger logger, Guid bookingId, string status, Guid flightId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Retrieving booking {BookingId}")]
    public static partial void RetrievingBooking(
        ILogger logger, Guid bookingId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Booking {BookingId} not found")]
    public static partial void BookingNotFound(
        ILogger logger, Guid bookingId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Confirming booking {BookingId}")]
    public static partial void ConfirmingBooking(
        ILogger logger, Guid bookingId);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Information,
        Message = "Booking {BookingId} confirmed successfully")]
    public static partial void BookingConfirmed(
        ILogger logger, Guid bookingId);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Information,
        Message = "Canceling booking {BookingId}")]
    public static partial void CancelingBooking(
        ILogger logger, Guid bookingId);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Information,
        Message = "Booking {BookingId} canceled successfully")]
    public static partial void BookingCanceled(
        ILogger logger, Guid bookingId);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Error,
        Message = "Error creating booking for passenger {PassengerName} on flight {FlightId}")]
    public static partial void BookingCreationFailed(
        ILogger logger, string passengerName, Guid flightId, Exception exception);

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Error,
        Message = "Error processing booking {BookingId}")]
    public static partial void BookingOperationFailed(
        ILogger logger, Guid bookingId, Exception exception);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Error,
        Message = "Flight {FlightId} not found when creating booking for passenger {PassengerName}")]
    public static partial void FlightNotFoundForBooking(
        ILogger logger, Guid flightId, string passengerName);

    [LoggerMessage(
        EventId = 2011,
        Level = LogLevel.Error,
        Message = "Booking {BookingId} cannot be {Operation} because current status is {Status}")]
    public static partial void InvalidBookingStatusTransition(
        ILogger logger, Guid bookingId, string operation, string status);

    [LoggerMessage(
        EventId = 2012,
        Level = LogLevel.Error,
        Message = "Simulated processing failure for booking {BookingId}")]
    public static partial void SimulatedFailure(
        ILogger logger, Guid bookingId, Exception exception);
}
