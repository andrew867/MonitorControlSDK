# VMC string catalog

VMC sends ASCII payloads: `category` + space + `cmd` [+ args], over SDCP **V3** item **0xB000**.

## Categories

Legacy tools primarily use:

- **`STATget`** — query a single field name (second token).
- **`STATset`** — set channel; second token is command name; optional third/fourth tokens are values.

## Commands observed in repository apps

The following tokens appear in `sendCommand("STATset", …)` / `STATget` across `LMD_AutoWhiteBalance`, `Monitor_AutoWhiteAdjustment`, and `Monitor_Update` (non-exhaustive grep snapshot; extend by re-running search).

| Command / pattern | Typical use |
|-------------------|-------------|
| `RGAIN`, `GGAIN`, `BGAIN` | RGB gain |
| `RBIAS`, `GBIAS`, `BBIAS` | RGB bias |
| `CONTRAST`, `BRIGHTNESS` | Picture |
| `ALLCONTRAST`, `ALLBRIGHTNESS` | Global picture |
| `FLATFIELDPATTERN` + `ON`/`OFF` or `FLATFIELDPATTERN ON` single token variant | Flat field |
| `WBSEL USER` | User white balance |
| `COLORR`, `COLORG`, `COLORB`, `COLORW` + numeric | Chroma / white drive |
| `MODEL`, `SHOWID`, `SHOWIPADDRESS`, `MENUOFF`, `ENTER`, … | UI / identity (`ControlVmcCommand` in VerUpTool) |

## SDK mapping

Use `VmcClient.Send("STATset", "RGAIN", "500")` or `GetStatString("MODEL")` etc.

## Regenerate raw list

```bash
rg "sendCommand\\(" --glob "*.cs" Monitor_Update LMD_AutoWhiteBalance Monitor_AutoWhiteAdjustment
```
