namespace Airline.Booking.Api.Logging;

public static partial class FlightLogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Searching flights from {Origin} to {Destination} on {Date}")]
    public static partial void SearchingFlights(
        ILogger logger, string origin, string destination, string date);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Found {Count} flights from {Origin} to {Destination} on {Date}")]
    public static partial void FlightsFound(
        ILogger logger, int count, string origin, string destination, string date);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "No flights found from {Origin} to {Destination} on {Date}")]
    public static partial void NoFlightsFound(
        ILogger logger, string origin, string destination, string date);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Invalid date format '{Date}' for flight search from {Origin} to {Destination}")]
    public static partial void InvalidDateFormat(
        ILogger logger, string origin, string destination, string date);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "Error searching flights from {Origin} to {Destination} on {Date}")]
    public static partial void FlightSearchFailed(
        ILogger logger, string origin, string destination, string date, Exception exception);
}
