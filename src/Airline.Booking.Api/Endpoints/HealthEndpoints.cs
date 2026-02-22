namespace Airline.Booking.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
           .WithName("Health")
           .WithTags("Health");

        app.MapGet("/ready", () => Results.Ok(new { status = "Ready" }))
           .WithName("Ready")
           .WithTags("Health");
    }
}
