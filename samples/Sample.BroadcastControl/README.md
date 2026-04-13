# Sample: broadcast real-time control

Interactive REPL over one long-lived SDCP TCP session. Spec: [docs/spec/broadcast-realtime-control.md](../../docs/spec/broadcast-realtime-control.md).

```bash
dotnet run --project samples/Sample.BroadcastControl -- 192.168.0.10
# or
dotnet run --project samples/Sample.BroadcastControl -- --host 192.168.0.10
```

Use only on paths you control; `STATset` can change picture while on air.
