"""
HTTP gateway: forwards JSON to MonitorControl.Web (.NET) for language interop.

Run the .NET API first:
  dotnet run --project src/MonitorControl.Web --urls http://127.0.0.1:5080

Then:
  pip install -r requirements.txt
  uvicorn main:app --host 127.0.0.1 --port 8000
"""

from __future__ import annotations

import os
from contextlib import asynccontextmanager
from pathlib import Path

import httpx
from fastapi import FastAPI, Request
from fastapi.responses import Response, StreamingResponse
from fastapi.staticfiles import StaticFiles

BASE = os.environ.get("MONITOR_CONTROL_API_URL", "http://127.0.0.1:5080").rstrip("/")
STATIC = Path(__file__).resolve().parent / "static"


@asynccontextmanager
async def lifespan(app: FastAPI):
    timeout = httpx.Timeout(120.0, connect=30.0)
    async with httpx.AsyncClient(timeout=timeout) as client:
        app.state.http = client
        yield


app = FastAPI(
    title="MonitorControl Python gateway",
    description=f"Proxies /api/* to {BASE}/api/*",
    lifespan=lifespan,
)


@app.api_route(
    "/api/{full_path:path}",
    methods=["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD"],
)
async def proxy(full_path: str, request: Request) -> Response:
    client: httpx.AsyncClient = request.app.state.http
    url = f"{BASE}/api/{full_path}"
    if request.url.query:
        url = f"{url}?{request.url.query}"
    body = await request.body()
    forward_headers = {}
    for key in ("content-type", "x-firmware-ack"):
        if key in request.headers:
            forward_headers[key] = request.headers[key]

    if request.method == "GET" and full_path.startswith("events/"):
        async with client.stream(
            "GET",
            url,
            headers=forward_headers,
        ) as upstream:
            media = upstream.headers.get("content-type", "text/event-stream")

            async def chunks():
                try:
                    async for block in upstream.aiter_bytes():
                        yield block
                finally:
                    await upstream.aclose()

            return StreamingResponse(
                chunks(),
                status_code=upstream.status_code,
                media_type=media,
            )

    upstream = await client.request(
        request.method,
        url,
        content=body if body else None,
        headers=forward_headers,
    )
    media = upstream.headers.get("content-type", "application/json")
    return Response(content=upstream.content, status_code=upstream.status_code, media_type=media)


app.mount("/", StaticFiles(directory=str(STATIC), html=True), name="ui")
