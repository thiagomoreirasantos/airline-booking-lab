using Airline.Booking.Api.Dtos;
using Airline.Booking.Api.Logging;
using Airline.Booking.Api.Stores;

namespace Airline.Booking.Api.Endpoints;

public static class FlightEndpoints
{
    public static void MapFlightEndpoints(this WebApplication app)
    {
        app.MapGet("/api/flights/search", SearchFlights)
           .WithName("SearchFlights")
           .WithTags("Flights");
    }

    private static IResult SearchFlights(
        string from,
        string to,
        string date,
        FlightStore flightStore,
        ILogger<Program> logger)
    {
        try
        {
            FlightLogMessages.SearchingFlights(logger, from, to, date);

            if (!DateOnly.TryParse(date, out var parsedDate))
            {
                FlightLogMessages.InvalidDateFormat(logger, from, to, date);
                return Results.BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });
            }

            var flights = flightStore.Search(from, to, parsedDate);

            if (flights.Count == 0)
            {
                FlightLogMessages.NoFlightsFound(logger, from, to, date);
                return Results.Ok(Array.Empty<FlightResponse>());
            }

            FlightLogMessages.FlightsFound(logger, flights.Count, from, to, date);

            var response = flights.Select(f => new FlightResponse(
                f.Id, f.Origin, f.Destination, f.Date.ToString("yyyy-MM-dd"), f.Price));

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            FlightLogMessages.FlightSearchFailed(logger, from, to, date, ex);
            return Results.StatusCode(500);
        }
    }
}
