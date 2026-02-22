using Airline.Booking.Api.Endpoints;
using Airline.Booking.Api.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FlightStore>();
builder.Services.AddSingleton<BookingStore>();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapFlightEndpoints();
app.MapBookingEndpoints();

app.Run();

public partial class Program;
