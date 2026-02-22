# Airline Ticket Booking (.NET) + OpenTelemetry (zero-code) + Kubernetes + Grafana

## 1) Objetivo do projeto
Construir um sistema simples de **reserva de passagens aéreas** em **.NET**, com foco em:

- **OpenTelemetry “zero-code”** (sem adicionar SDK/Exporter no código).
- **Kubernetes** (deploy da API + OpenTelemetry Collector).
- **Observabilidade no Grafana**:
  - **Logs** → Loki
  - **Traces** → Tempo
  - **Metrics** → Prometheus
- **Pipeline**: App → OTLP → **OpenTelemetry Collector** → Loki/Tempo/Prometheus → **Grafana**

> Nota importante (para evitar confusão): o Grafana **visualiza** dados.  
> Para logs/traces/metrics você precisa de backends como **Loki/Tempo/Prometheus**.  
> Este projeto sobe esses backends via Docker Compose (local dev) e usa o Collector como “hub”.

---

## 2) Regras obrigatórias (zero-code de verdade)
1. **Não instalar pacotes OpenTelemetry no código** (sem `OpenTelemetry.*` no `.csproj`).
2. O código deve usar apenas:
   - ASP.NET Core (Minimal API ou Controllers)
   - `ILogger<T>` para logs
   - `HttpClient` padrão (se necessário)
3. Telemetria será habilitada via:
   - **OpenTelemetry .NET Automatic Instrumentation** dentro do container (instalação via script).
   - **Variáveis de ambiente** (`OTEL_*`) definidas no Kubernetes/Helm.

---

## 3) Funcionalidades do domínio (escopo mínimo)
A API representa um “Airline Ticket Booking”.

### Endpoints mínimos
- `GET /health` (liveness)
- `GET /ready` (readiness)
- `GET /api/flights/search?from=OPO&to=LIS&date=2026-03-01`
- `POST /api/bookings`
- `GET /api/bookings/{id}`
- `POST /api/bookings/{id}/confirm`
- `POST /api/bookings/{id}/cancel`

### Modelos (simples)
- `Flight` (origem, destino, data, preço)
- `Booking` (id, flightId, passengerName, status)
- `BookingStatus` (Pending, Confirmed, Canceled)

### Logs (para gerar sinal)
- Logar início/fim de cada operação (info).
- Logar falhas (error).
- Incluir `bookingId`, `flightId` e `status` como campos estruturados quando possível.

---

## 4) Arquitetura (alto nível)

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
  |--> metrics-> Prometheus (scrape do collector)
                 |
                 v
               Grafana
```

---

## 5) Estrutura de pastas (entregável)
Crie exatamente esta estrutura:

```
.
├─ src/
│  └─ Airline.Booking.Api/
│     ├─ Airline.Booking.Api.csproj
│     ├─ Program.cs
│     ├─ Domain/
│     ├─ Endpoints/
│     └─ appsettings.json (sem OTEL!)
├─ deploy/
│  ├─ k8s/
│  │  ├─ namespace.yaml
│  │  ├─ booking-api.deployment.yaml
│  │  ├─ booking-api.service.yaml
│  │  ├─ otel-collector.configmap.yaml
│  │  ├─ otel-collector.deployment.yaml
│  │  └─ otel-collector.service.yaml
│  └─ helm/
│     └─ airline-booking/
│        ├─ Chart.yaml
│        ├─ values.yaml
│        └─ templates/
│           ├─ deployment.yaml
│           ├─ service.yaml
│           ├─ configmap-otel-env.yaml
│           └─ _helpers.tpl
├─ observability/
│  ├─ docker-compose.yaml
│  ├─ otel-collector.yaml
│  ├─ loki-config.yaml
│  ├─ tempo.yaml
│  ├─ prometheus.yaml
│  └─ grafana/
│     └─ provisioning/
│        ├─ datasources/
│        │  └─ datasources.yaml
│        └─ dashboards/
│           └─ dashboards.yaml
├─ scripts/
│  ├─ loadgen.sh
│  └─ kind-up.sh
├─ Dockerfile
├─ entrypoint.sh
└─ README.md
```

---

## 6) Plano de execução (SAR)
### 6.1 Situação
Você precisa de uma API .NET instrumentada “zero-code”, rodando em container e no Kubernetes, com dados chegando no Grafana.

### 6.2 Ação
Você vai:
1. Criar a API (`src/Airline.Booking.Api`).
2. Criar container com **auto-instrumentation** (via Dockerfile + entrypoint).
3. Subir stack local (Grafana + Loki + Tempo + Prometheus + Collector) via Docker Compose.
4. Criar manifests K8s (API + Collector) e um Helm chart da API.
5. Adicionar scripts de automação (kind + load generator).

### 6.3 Resultado
O usuário consegue:
- Ver **logs** no Grafana (via Loki).
- Ver **traces** no Grafana (via Tempo).
- Ver **metrics** no Grafana (via Prometheus).
Tudo sem tocar no código para OpenTelemetry.

---

## 7) Implementação da API (.NET)
### Requisitos
- .NET LTS (use `mcr.microsoft.com/dotnet/aspnet` e `sdk` correspondentes)
- Minimal API é suficiente.

### Padrões
- Endpoints separados por arquivos em `Endpoints/`.
- Nada de OpenTelemetry no código.
- Logging com `ILogger`.

---

## 8) Dockerfile (build + instalar OpenTelemetry auto-instrumentation)
### Regras
- Multi-stage build.
- Instalar a auto-instrumentation com o script oficial (download do release “latest”).
- Rodar a aplicação sempre passando pelo `instrument.sh` (via `entrypoint.sh`).

### Entregáveis
- `Dockerfile` na raiz
- `entrypoint.sh` na raiz (executável)

**entrypoint.sh (comportamento esperado):**
1. Executa `. $HOME/.otel-dotnet-auto/instrument.sh`
2. Executa `dotnet Airline.Booking.Api.dll`

---

## 9) Observability stack local (Docker Compose)
Arquivo: `observability/docker-compose.yaml`

Subir:
- Grafana (porta 3000)
- Loki (porta 3100)
- Tempo (porta 3200)
- Prometheus (porta 9090)
- OpenTelemetry Collector
- (Opcional) booking-api também via compose para teste rápido

### OpenTelemetry Collector (local)
Arquivo: `observability/otel-collector.yaml`

Pipelines mínimas:
- Receber OTLP (`grpc:4317`, `http:4318`)
- Exportar:
  - logs -> Loki via `otlphttp` para `http://loki:3100/otlp`
  - traces -> Tempo (OTLP)
  - metrics -> Prometheus exporter (Prometheus vai “scrapar”)

### Provisionamento do Grafana
Arquivo: `observability/grafana/provisioning/datasources/datasources.yaml`
- Datasource Loki
- Datasource Tempo
- Datasource Prometheus

---

## 10) Kubernetes (manifests)
Pasta: `deploy/k8s`

### 10.1 Namespace
- `airline-observability`

### 10.2 OpenTelemetry Collector (no cluster)
- `ConfigMap` com config do collector
- `Deployment` do collector
- `Service` expondo:
  - 4317 (OTLP gRPC)
  - 4318 (OTLP HTTP)

### 10.3 booking-api (no cluster)
- `Deployment` com:
  - env vars `OTEL_*` apontando para o service do collector
  - probes /health e /ready
  - resource requests/limits simples
- `Service` ClusterIP na porta da API

---

## 11) Helm chart da API (deployment + values com OTEL)
Pasta: `deploy/helm/airline-booking`

### Exigência do usuário
Ter um `values.yaml` com as variáveis OTEL e um template de `Deployment` que injeta:

Exemplos de variáveis (ajustar conforme necessário):
- `OTEL_SERVICE_NAME=airline-booking-api`
- `OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector.airline-observability.svc.cluster.local:4317`
- `OTEL_EXPORTER_OTLP_PROTOCOL=grpc`
- `OTEL_RESOURCE_ATTRIBUTES=deployment.environment=dev,service.version=0.1.0`
- `OTEL_TRACES_EXPORTER=otlp`
- `OTEL_METRICS_EXPORTER=otlp`
- `OTEL_LOGS_EXPORTER=otlp`

> Observação: manter isso em `values.yaml`, e permitir override por ambiente.

---

## 12) Scripts
### 12.1 scripts/kind-up.sh
- Cria cluster kind
- Aplica namespace + collector + api
- (Opcional) Port-forward da API

### 12.2 scripts/loadgen.sh
- Loop chamando endpoints para gerar tráfego
- Exemplo:
  - search flights
  - create booking
  - confirm booking
  - cancel booking
- Deve aceitar `BASE_URL` como variável

---

## 13) README.md (runbook)
Deve conter:
1. Como rodar local com Docker Compose
2. Como rodar no Kubernetes com kind
3. Onde acessar:
   - Grafana: `http://localhost:3000`
4. Como validar:
   - Logs no Grafana Explore (Loki)
   - Traces no Grafana (Tempo)
   - Metrics no Grafana (Prometheus)
5. Troubleshooting rápido:
   - conferir env vars `OTEL_*`
   - conferir conectividade `booking-api -> otel-collector`

---

## 14) Critérios de aceitação
- [ ] API roda local e responde endpoints.
- [ ] Container builda e sobe com auto-instrumentation.
- [ ] Docker Compose sobe Grafana + Loki + Tempo + Prometheus + Collector.
- [ ] Logs aparecem no Grafana (Loki).
- [ ] Traces aparecem no Grafana (Tempo).
- [ ] Metrics aparecem no Grafana (Prometheus).
- [ ] No Kubernetes: API envia OTLP para o Collector.
- [ ] Helm chart existe e injeta `OTEL_*` via `values.yaml`.

---

## 15) Referências (links)
- OpenTelemetry .NET zero-code: https://opentelemetry.io/docs/zero-code/dotnet/
- Configuração (env vars `OTEL_*`): https://opentelemetry.io/docs/zero-code/dotnet/configuration/
- OpenTelemetry Collector (config): https://opentelemetry.io/docs/collector/configuration/
- Kubernetes + Collector components (filelog, kubeletstats, k8sattributes): https://opentelemetry.io/docs/platforms/kubernetes/collector/components/
- Loki ingest via OTEL Collector (otlphttp + /otlp): https://grafana.com/docs/loki/latest/send-data/otel/
- Getting started (OTEL Collector + Loki): https://grafana.com/docs/loki/latest/send-data/otel/otel-collector-getting-started/
- Grafana + OpenTelemetry Collector overview: https://grafana.com/docs/opentelemetry/collector/opentelemetry-collector/