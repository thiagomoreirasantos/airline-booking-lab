#!/bin/bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
CONCURRENCY="${CONCURRENCY:-5}"
ITERATIONS="${ITERATIONS:-50}"
DELAY="${DELAY:-0.1}"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

TOTAL_REQUESTS=0
FAILED_REQUESTS=0
START_TIME=$(date +%s%N)

log_info()  { echo -e "${GREEN}[INFO]${NC}  $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }
log_step()  { echo -e "${BLUE}[STEP]${NC}  $1"; }

track_request() {
    TOTAL_REQUESTS=$((TOTAL_REQUESTS + 1))
    local status_code=$1
    local endpoint=$2
    if [ "$status_code" -ge 400 ] 2>/dev/null; then
        FAILED_REQUESTS=$((FAILED_REQUESTS + 1))
        log_error "HTTP $status_code - $endpoint"
    fi
}

print_summary() {
    local end_time=$(date +%s%N)
    local elapsed_ms=$(( (end_time - START_TIME) / 1000000 ))
    local elapsed_s=$(echo "scale=2; $elapsed_ms / 1000" | bc)
    local success=$((TOTAL_REQUESTS - FAILED_REQUESTS))

    echo ""
    echo "============================================"
    echo "  LOAD TEST SUMMARY"
    echo "============================================"
    echo "  Total requests:    $TOTAL_REQUESTS"
    echo "  Successful:        $success"
    echo "  Failed:            $FAILED_REQUESTS"
    echo "  Duration:          ${elapsed_s}s"
    if [ "$TOTAL_REQUESTS" -gt 0 ]; then
        local rps=$(echo "scale=2; $TOTAL_REQUESTS / $elapsed_s" | bc 2>/dev/null || echo "N/A")
        echo "  Requests/sec:      $rps"
    fi
    echo "============================================"
}

run_booking_flow() {
    local flow_id=$1

    # 1. Search flights
    local search_status
    search_status=$(curl -s -o /dev/null -w "%{http_code}" \
        "${BASE_URL}/api/flights/search?from=OPO&to=LIS&date=2026-03-01")
    track_request "$search_status" "GET /api/flights/search (flow $flow_id)"

    # Get a real flight ID from search results
    local flights_response
    flights_response=$(curl -s "${BASE_URL}/api/flights/search?from=OPO&to=LIS&date=2026-03-01")
    local flight_id
    flight_id=$(echo "$flights_response" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    if [ -z "$flight_id" ]; then
        log_warn "No flights found, skipping booking flow $flow_id"
        return
    fi

    sleep "$DELAY"

    # 2. Create booking
    local booking_response
    booking_response=$(curl -s -w "\n%{http_code}" \
        -X POST "${BASE_URL}/api/bookings" \
        -H "Content-Type: application/json" \
        -d "{\"flightId\":\"${flight_id}\",\"passengerName\":\"Passenger-${flow_id}\"}")
    local booking_status
    booking_status=$(echo "$booking_response" | tail -1)
    local booking_body
    booking_body=$(echo "$booking_response" | sed '$d')
    track_request "$booking_status" "POST /api/bookings (flow $flow_id)"

    local booking_id
    booking_id=$(echo "$booking_body" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    if [ -z "$booking_id" ]; then
        log_warn "Failed to create booking in flow $flow_id"
        return
    fi

    sleep "$DELAY"

    # 3. Get booking
    local get_status
    get_status=$(curl -s -o /dev/null -w "%{http_code}" \
        "${BASE_URL}/api/bookings/${booking_id}")
    track_request "$get_status" "GET /api/bookings/$booking_id (flow $flow_id)"

    sleep "$DELAY"

    # 4. Confirm or cancel (alternate)
    if [ $((flow_id % 2)) -eq 0 ]; then
        local confirm_status
        confirm_status=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST "${BASE_URL}/api/bookings/${booking_id}/confirm")
        track_request "$confirm_status" "POST /api/bookings/$booking_id/confirm (flow $flow_id)"
    else
        local cancel_status
        cancel_status=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST "${BASE_URL}/api/bookings/${booking_id}/cancel")
        track_request "$cancel_status" "POST /api/bookings/$booking_id/cancel (flow $flow_id)"
    fi

    sleep "$DELAY"

    # 5. Try invalid operations to generate warn/error logs
    if [ $((flow_id % 5)) -eq 0 ]; then
        # Search non-existent route (generates Warning log)
        local not_found_status
        not_found_status=$(curl -s -o /dev/null -w "%{http_code}" \
            "${BASE_URL}/api/flights/search?from=XXX&to=YYY&date=2026-03-01")
        track_request "$not_found_status" "GET /api/flights/search (no results, flow $flow_id)"

        # Search with invalid date format (generates Error log)
        local bad_date_status
        bad_date_status=$(curl -s -o /dev/null -w "%{http_code}" \
            "${BASE_URL}/api/flights/search?from=OPO&to=LIS&date=invalid-date")
        track_request "$bad_date_status" "GET /api/flights/search (bad date, flow $flow_id)"

        # Create booking with non-existent flightId (generates Error log)
        local fake_flight_id="00000000-0000-0000-0000-000000000099"
        local bad_booking_status
        bad_booking_status=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST "${BASE_URL}/api/bookings" \
            -H "Content-Type: application/json" \
            -d "{\"flightId\":\"${fake_flight_id}\",\"passengerName\":\"Ghost-${flow_id}\"}")
        track_request "$bad_booking_status" "POST /api/bookings (bad flight, flow $flow_id)"

        # Get non-existent booking (generates Warning log)
        local fake_id="00000000-0000-0000-0000-000000000000"
        local fake_status
        fake_status=$(curl -s -o /dev/null -w "%{http_code}" \
            "${BASE_URL}/api/bookings/${fake_id}")
        track_request "$fake_status" "GET /api/bookings/$fake_id (not found, flow $flow_id)"

        # Try to confirm an already confirmed/canceled booking (generates Error log)
        local double_status
        double_status=$(curl -s -o /dev/null -w "%{http_code}" \
            -X POST "${BASE_URL}/api/bookings/${booking_id}/confirm")
        track_request "$double_status" "POST /api/bookings/$booking_id/confirm (duplicate, flow $flow_id)"
    fi
}

echo "============================================"
echo "  AIRLINE BOOKING - LOAD TEST"
echo "============================================"
echo "  Base URL:     $BASE_URL"
echo "  Concurrency:  $CONCURRENCY"
echo "  Iterations:   $ITERATIONS"
echo "  Delay:        ${DELAY}s"
echo "============================================"
echo ""

# Health check
log_step "Checking API health..."
health_status=$(curl -s -o /dev/null -w "%{http_code}" "${BASE_URL}/health" 2>/dev/null || echo "000")
if [ "$health_status" != "200" ]; then
    log_error "API is not healthy (HTTP $health_status). Is it running at $BASE_URL?"
    exit 1
fi
log_info "API is healthy"

# Run load test
log_step "Starting load test with $ITERATIONS iterations..."

for i in $(seq 1 "$ITERATIONS"); do
    # Run flows in parallel up to CONCURRENCY
    run_booking_flow "$i" &

    # Control concurrency
    if [ $((i % CONCURRENCY)) -eq 0 ]; then
        wait
        log_info "Completed batch $((i / CONCURRENCY)) ($i/$ITERATIONS iterations)"
    fi
done

# Wait for remaining background jobs
wait

print_summary
