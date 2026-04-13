# OpenAPI (MonitorControl.Web)

This folder holds the **committed** OpenAPI 3 document for the HTTP host, so integrators can diff schemas, run codegen, or import into API gateways **without** starting the ASP.NET process.

| File | Description |
|------|-------------|
| `monitorcontrol.openapi.json` | Snapshot of `GET /swagger/v1/swagger.json` from `MonitorControl.Web` (Swashbuckle). |

## Regenerate

From the repository root (requires .NET 8 SDK and `curl`):

```bash
bash scripts/fetch-openapi.sh
```

Optional: `PORT=56000 bash scripts/fetch-openapi.sh` if the default ephemeral port is busy.

Run this **whenever** minimal API routes, request/response DTOs, or Swagger metadata change, then commit the updated JSON with the code change.

## Related documentation

- [docs/guide/openapi-codegen.md](../docs/guide/openapi-codegen.md) — C client via Docker, alternatives.
- [docs/guide/web-api-and-python-gateway.md](../docs/guide/web-api-and-python-gateway.md) — REST + SSE + WebSocket semantics (push is server-side polling).
