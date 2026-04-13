# Spec: Broadcast real-time control sample

## Transport

- Single **SDCP TCP** connection to `SdcpConnection.DefaultPort` (53484), opened once at startup, closed on `quit` or Ctrl+C.
- All control in v1 uses **VMC** (`VmcClient`): SDCP **V3** header, item **0xB000**, ASCII payload `STATget …` / `STATset …` as documented in [vmc-string-catalog.md](vmc-string-catalog.md).

## Interactive grammar (REPL)

Commands are line-oriented (stdin), UTF-8, trimmed whitespace.

| Input | Action |
|-------|--------|
| `get <field>` | `STATget <field>` — prints ASCII payload or `(null)` on failure. |
| `set <token> [args...]` | `STATset` with one or more args joined by spaces (e.g. `set BRIGHTNESS 512`, `set FLATFIELDPATTERN OFF`). |
| `help` | Lists commands and examples. |
| `quit` / `exit` | Closes TCP and exits 0. |

**Parsing rules**

- `get` requires exactly one token after `get` (the field name).
- `set` requires at least one token after `set` (the STATset subcommand); additional tokens are passed as a single `STATset …` payload via `VmcClient.Send("STATset", …params)` (any arity supported by the wire string length).

## Safety

- The sample does **not** confirm dangerous operations; operators must follow facility policy.
- Wrong `STATset` on-air can affect picture; use only on paths you control.

## Future extensions

- Optional `--poll-sdap` to print neighbor monitors (UDP 53862) without binding conflicts (short listen window).
- Optional VMS read-only status line (luminance mode, etc.) via `VmsClient` + `dataLengthV4` parsing.
