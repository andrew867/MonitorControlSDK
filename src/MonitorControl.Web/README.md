# MonitorControl.Web

ASP.NET Core host: JSON REST API (`/api/*`), **SSE** (`/api/events/monitor`), **WebSocket** (`/ws/monitor-watch`), Swagger UI / **OpenAPI 3** at `/swagger/v1/swagger.json`, and static browser demo under `wwwroot/`.

Run: `dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080`

Documentation:

- [docs/guide/web-api-and-python-gateway.md](../../docs/guide/web-api-and-python-gateway.md) — routes, bodies, firmware gate, push polling model.
- [docs/guide/openapi-codegen.md](../../docs/guide/openapi-codegen.md) — committed spec at [openapi/monitorcontrol.openapi.json](../../openapi/monitorcontrol.openapi.json), codegen.
- [docs/handbook.md](../../docs/handbook.md) — how HTTP fits in the full stack.
