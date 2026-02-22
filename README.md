# Airline Ticket Booking (.NET) + OpenTelemetry (zero-code) + Kubernetes + Grafana

A simple airline ticket booking API built with .NET, featuring **zero-code OpenTelemetry instrumentation**, Kubernetes deployment, and full observability with Grafana, Loki, Tempo, and Prometheus.

## Architecture

```
[Client/Loadgen]
      |
      v
[booking-api (.NET)]
  |   (OTLP gRPC/HTTP)
  v
[otel-collector]
  |--> logs  -> Loki
  |--> traces-> Tempo
  |--> metrics-> Prometheus
                 |
                 v
               Grafana
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Liveness probe |
| GET | `/ready` | Readiness probe |
| GET | `/api/flights/search?from=OPO&to=LIS&date=2026-03-01` | Search flights |
| POST | `/api/bookings` | Create booking |
| GET | `/api/bookings/{id}` | Get booking |
| POST | `/api/bookings/{id}/confirm` | Confirm booking |
| POST | `/api/bookings/{id}/cancel` | Cancel booking |

## Quick Start - Docker Compose (Local Dev)

### 1. Start the full observability stack

```bash
cd observability
docker compose up -d
```

This starts: Grafana, Loki, Tempo, Prometheus, OpenTelemetry Collector, and the booking API.

### 2. Test the API

```bash
# Health check
curl http://localhost:8080/health

# Search flights
curl "http://localhost:8080/api/flights/search?from=OPO&to=LIS&date=2026-03-01"

# Create a booking (use a flightId from the search results)
curl -X POST http://localhost:8080/api/bookings \
  -H "Content-Type: application/json" \
  -d '{"flightId":"<FLIGHT_ID>","passengerName":"John Doe"}'

# Confirm a booking
curl -X POST http://localhost:8080/api/bookings/<BOOKING_ID>/confirm
```

### 3. Run the load generator

```bash
chmod +x scripts/loadgen.sh
./scripts/loadgen.sh
```

Options:

| Variable | Default | Description |
|----------|---------|-------------|
| `BASE_URL` | `http://localhost:8080` | API base URL |
| `CONCURRENCY` | `5` | Parallel requests |
| `ITERATIONS` | `50` | Total booking flows |
| `DELAY` | `0.1` | Delay between steps (seconds) |

Example:

```bash
CONCURRENCY=10 ITERATIONS=100 ./scripts/loadgen.sh
```

### 4. Access Grafana

Open [http://localhost:3000](http://localhost:3000) (admin/admin).

## Quick Start - Kubernetes (kind)

### 1. Create the cluster and deploy

```bash
chmod +x scripts/kind-up.sh
./scripts/kind-up.sh
```

### 2. Run load test against the cluster

```bash
BASE_URL=http://localhost:8080 ./scripts/loadgen.sh
```

### Using Helm

```bash
helm install airline-booking deploy/helm/airline-booking \
  -n airline-observability --create-namespace
```

Override OTEL settings per environment:

```bash
helm install airline-booking deploy/helm/airline-booking \
  -n airline-observability --create-namespace \
  --set otel.exporterEndpoint=http://my-collector:4318 \
  --set otel.resourceAttributes="deployment.environment=staging,service.version=0.2.0"
```

## Observability - Validating in Grafana

### Logs (Loki)

1. Go to **Explore** > select **Loki** datasource
2. Query: `{service_name="airline-booking-api"}`
3. You should see structured JSON logs with `bookingId`, `flightId`, `status`

### Traces (Tempo)

1. Go to **Explore** > select **Tempo** datasource
2. Search by service name: `airline-booking-api`
3. Click on a trace to see the full span waterfall

### Metrics (Prometheus)

1. Go to **Explore** > select **Prometheus** datasource
2. Query: `http_server_request_duration_seconds_bucket{service_name="airline-booking-api"}`

## Troubleshooting

### API not sending telemetry

1. Check OTEL env vars are set:
   ```bash
   docker exec booking-api env | grep OTEL
   ```
2. Verify collector connectivity:
   ```bash
   docker exec booking-api curl -s http://otel-collector:4318/v1/traces
   ```

### No data in Grafana

1. Check collector logs:
   ```bash
   docker logs otel-collector
   ```
2. Verify Loki is receiving data:
   ```bash
   curl -s http://localhost:3100/ready
   ```
3. Check Tempo health:
   ```bash
   curl -s http://localhost:3200/ready
   ```

### Kubernetes pods not starting

```bash
kubectl -n airline-observability get pods
kubectl -n airline-observability describe pod <pod-name>
kubectl -n airline-observability logs <pod-name>
```

## Project Structure

```
.
├─ src/
│  └─ Airline.Booking.Api/
│     ├─ Airline.Booking.Api.csproj
│     ├─ Program.cs
│     ├─ Domain/         # Flight, BookingEntity, BookingStatus
│     ├─ Dtos/           # Record-based DTOs
│     ├─ Endpoints/      # Minimal API endpoint groups
│     ├─ Logging/        # High-performance LoggerMessage source gen
│     ├─ Stores/         # In-memory data stores
│     └─ appsettings.json
├─ deploy/
│  ├─ k8s/              # Kubernetes manifests
│  └─ helm/             # Helm chart with OTEL values
├─ observability/
│  ├─ docker-compose.yaml
│  ├─ otel-collector.yaml
│  ├─ loki-config.yaml
│  ├─ tempo.yaml
│  ├─ prometheus.yaml
│  └─ grafana/provisioning/
├─ scripts/
│  ├─ loadgen.sh        # Load test script
│  └─ kind-up.sh        # Kind cluster setup
├─ Dockerfile
├─ entrypoint.sh
└─ README.md
```
