#!/bin/bash
set -euo pipefail

CLUSTER_NAME="${CLUSTER_NAME:-airline-lab}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
K8S_DIR="${ROOT_DIR}/deploy/k8s"

GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $1"; }
log_step() { echo -e "${BLUE}[STEP]${NC} $1"; }

# Check prerequisites
for cmd in kind kubectl docker; do
    if ! command -v "$cmd" &>/dev/null; then
        echo "Error: $cmd is required but not installed."
        exit 1
    fi
done

# Create kind cluster
log_step "Creating kind cluster '${CLUSTER_NAME}'..."
if kind get clusters 2>/dev/null | grep -q "^${CLUSTER_NAME}$"; then
    log_info "Cluster '${CLUSTER_NAME}' already exists, skipping creation"
else
    kind create cluster --name "$CLUSTER_NAME"
    log_info "Cluster '${CLUSTER_NAME}' created"
fi

# Build and load the API image
log_step "Building booking-api Docker image..."
docker build -t airline-booking-api:latest "$ROOT_DIR"
log_info "Image built"

log_step "Loading image into kind cluster..."
kind load docker-image airline-booking-api:latest --name "$CLUSTER_NAME"
log_info "Image loaded"

# Apply Kubernetes manifests
log_step "Applying Kubernetes manifests..."
kubectl apply -f "${K8S_DIR}/namespace.yaml"
kubectl apply -f "${K8S_DIR}/otel-collector.configmap.yaml"
kubectl apply -f "${K8S_DIR}/otel-collector.deployment.yaml"
kubectl apply -f "${K8S_DIR}/otel-collector.service.yaml"
kubectl apply -f "${K8S_DIR}/booking-api.deployment.yaml"
kubectl apply -f "${K8S_DIR}/booking-api.service.yaml"
log_info "Manifests applied"

# Wait for pods
log_step "Waiting for pods to be ready..."
kubectl -n airline-observability wait --for=condition=ready pod -l app=otel-collector --timeout=120s 2>/dev/null || true
kubectl -n airline-observability wait --for=condition=ready pod -l app=booking-api --timeout=120s 2>/dev/null || true
log_info "Pods ready"

# Port-forward
log_step "Setting up port-forward for booking-api on port 8080..."
kubectl -n airline-observability port-forward svc/booking-api 8080:80 &
PF_PID=$!
log_info "Port-forward active (PID: $PF_PID). API available at http://localhost:8080"

echo ""
echo "============================================"
echo "  Cluster is ready!"
echo "  API: http://localhost:8080"
echo "  Stop port-forward: kill $PF_PID"
echo "============================================"

wait $PF_PID
