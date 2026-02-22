FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Airline.Booking.Api/Airline.Booking.Api.csproj ./Airline.Booking.Api/
RUN dotnet restore ./Airline.Booking.Api/Airline.Booking.Api.csproj

COPY src/Airline.Booking.Api/ ./Airline.Booking.Api/
RUN dotnet publish ./Airline.Booking.Api/Airline.Booking.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    unzip \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSfL https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/releases/latest/download/otel-dotnet-auto-install.sh -o otel-install.sh \
    && sh otel-install.sh \
    && rm otel-install.sh

COPY --from=build /app/publish .
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

EXPOSE 8080
ENTRYPOINT ["./entrypoint.sh"]
