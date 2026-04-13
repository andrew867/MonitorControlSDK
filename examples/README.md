# Examples (non-.NET and gateways)

These folders are **optional** integration patterns around the same SDAP/SDCP behavior as the core library. They are maintained with the solution so teams can adopt the stack from their preferred runtime.

| Path | Role |
|------|------|
| [python-service/](python-service/) | `uvicorn` + `httpx` reverse proxy to `MonitorControl.Web`; serves a small static UI on port **8000** by default. |
| [arduino-knobs-brightness-contrast/](arduino-knobs-brightness-contrast/) | ESP32 / ESP8266: ADC pots → `POST /api/vmc/set` on the HTTP API. |
| [esp32-sdcp-vmc/](esp32-sdcp-vmc/) | ESP32: native TCP SDCP / VMC (no HTTP gateway). |

**HTTP route reference:** [docs/guide/web-api-and-python-gateway.md](../docs/guide/web-api-and-python-gateway.md)  
**OpenAPI snapshot:** [openapi/monitorcontrol.openapi.json](../openapi/monitorcontrol.openapi.json)
