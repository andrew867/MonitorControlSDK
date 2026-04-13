# OpenAPI → C client (and similar generators)

`MonitorControl.Web` exposes **OpenAPI 3** at `/swagger/v1/swagger.json` when the app is running. The repository also **commits** the same document as [`openapi/monitorcontrol.openapi.json`](../../openapi/monitorcontrol.openapi.json) so CI, reviewers, and codegen pipelines can consume a **frozen** spec without starting ASP.NET. See [`openapi/README.md`](../../openapi/README.md).

You can feed that document to **OpenAPI Generator** to emit a **libcurl-based C client**, **Java**, **Python**, **TypeScript**, etc., instead of hand-rolling JSON bodies (as in the [Arduino HTTP knob example](../../examples/arduino-knobs-brightness-contrast/)).

## 1. Use the committed spec (preferred)

For local experiments, open the JSON directly:

```bash
openapi-generator-cli generate \
  -i openapi/monitorcontrol.openapi.json \
  -g c \
  -o generated/openapi-c
```

## 1b. Refresh the spec from a running host

When you change routes or DTOs, regenerate the committed file from the repository root (bash):

```bash
bash scripts/fetch-openapi.sh
```

This starts the web host briefly, overwrites `openapi/monitorcontrol.openapi.json`, then exits. **Commit** the updated JSON with your API change.

Override the ephemeral port if needed:

```bash
PORT=56000 bash scripts/fetch-openapi.sh
```

## 2. Generate a small C client (Docker)

Requires [Docker](https://docs.docker.com/get-docker/):

```bash
bash scripts/generate-c-client.sh
```

Output defaults to `generated/openapi-c/` (gitignored). The **C** generator emits libcurl calls, structs, and JSON helpers suitable for desktop or embedded Linux — not for AVR. For **ESP32**, generated C is often too heavy; prefer **keeping JSON minimal** on the MCU or calling the HTTP API from a co-processor.

## 3. Generator without Docker

Install [OpenAPI Generator CLI](https://openapi-generator.tech/docs/installation) locally, then:

```bash
openapi-generator-cli generate \
  -i openapi/monitorcontrol.openapi.json \
  -g c \
  -o generated/openapi-c
```

## 4. Arduino / “arduino-rest-api style”

There is no first-party vendor generator for Wiring. Practical options:

- **HTTP + hand-built JSON** (this repo’s sketches) — smallest flash footprint.
- **ArduinoJson** + thin wrappers around `POST /api/vmc/set` bodies shaped like the OpenAPI `VmcSetBody` schema (derive field names from Swagger).
- **OpenAPI Generator `cpp-qt5-client`** or **`java`** for an Android sidecar — if the product already ships a richer runtime.

Re-run **`fetch-openapi.sh`** after API changes so downstream codegen stays in sync.

## Related

- [Web API guide](web-api-and-python-gateway.md) — route list including SSE and WebSocket push.
- Live spec: `http://<host>:<port>/swagger/v1/swagger.json`
