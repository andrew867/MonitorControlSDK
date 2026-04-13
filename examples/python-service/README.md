# Python HTTP gateway (optional)

This is a **thin reverse proxy** plus the same static UI as `MonitorControl.Web`. It lets Python stacks (Django, Celery workers, Jupyter) call the real SDCP implementation over HTTP without embedding .NET in-process.

## Prerequisites

- .NET 8 SDK
- Python 3.10+

## 1. Start the .NET API (required)

From the repository root:

```bash
dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080
```

Open `http://127.0.0.1:5080` for the built-in UI and `http://127.0.0.1:5080/swagger` for OpenAPI.

## 2. Start the Python gateway

```bash
cd examples/python-service
python -m venv .venv
.venv\Scripts\activate   # Windows
# source .venv/bin/activate  # Linux/macOS
pip install -r requirements.txt
set MONITOR_CONTROL_API_URL=http://127.0.0.1:5080
uvicorn main:app --host 127.0.0.1 --port 8000
```

Browse `http://127.0.0.1:8000` — static assets are served locally; `/api/*` is proxied to the .NET process.

## Environment

| Variable | Default | Purpose |
|----------|---------|---------|
| `MONITOR_CONTROL_API_URL` | `http://127.0.0.1:5080` | Upstream .NET base URL |

Firmware POSTs still require the .NET server to allow dangerous firmware (`MonitorControl:AllowDangerousFirmware` or `MONITOR_CONTROL_ALLOW_DANGEROUS_FIRMWARE=1`) and the browser must send `X-Firmware-Ack: CONFIRM` (the copied UI does this when the checkbox is checked).

## Using only Python as HTTP client

You can skip this gateway and call the .NET API directly with `httpx` or `requests` from your application. See [docs/guide/web-api-and-python-gateway.md](../../docs/guide/web-api-and-python-gateway.md).
