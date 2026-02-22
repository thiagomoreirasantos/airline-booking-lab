#!/bin/bash
set -e

. "$HOME/.otel-dotnet-auto/instrument.sh"

exec dotnet Airline.Booking.Api.dll
