# VMC command surface (`STATget` / `STATset`)

VMC uses SDCP **V3**, item **0xB000**, with an ASCII payload: `CATEGORY arg1 arg2 …` packed into the data area — see [`LegacyVmcContainer.setCommand`](../../src/MonitorControlSDK/Internal/LegacyVmcContainer.cs) and [`VmcClient`](../../src/MonitorControlSDK/Clients/VmcClient.cs).

## Categories

| Category | Direction | Example |
|-----------|-----------|---------|
| `STATget` | Host → monitor → ASCII answer | `STATget MODEL` |
| `STATset` | Host → monitor → status in response container | `STATset BRIGHTNESS 512` |

## Tokens commonly used in Sony tooling

These strings appear across interoperating apps and forks; **support is model-specific**. Treat unknown tokens as “try and read the response / error”.

| Token / pattern | Typical meaning |
|-----------------|-----------------|
| `RGAIN`, `GGAIN`, `BGAIN` | RGB gain |
| `RBIAS`, `GBIAS`, `BBIAS` | RGB bias |
| `CONTRAST`, `BRIGHTNESS` | Picture |
| `ALLCONTRAST`, `ALLBRIGHTNESS` | Global picture |
| `FLATFIELDPATTERN` + `ON` / `OFF` | Flat field |
| `WBSEL USER` | User white balance |
| `COLORR`, `COLORG`, `COLORB`, `COLORW` + value | Chroma / white drive |
| `MODEL`, `SHOWID`, `SHOWIPADDRESS`, `MENUOFF`, `ENTER` | Identity / UI helpers |

## SDK usage

```csharp
vmc.GetStatString("MODEL");
vmc.Send("STATset", "BRIGHTNESS", "512");
```

## Extending the catalog

Search this repository’s **own** sources for additional literals:

```bash
rg "STAT(get|set)" src samples --glob "*.cs"
```

Document any new tokens you confirm on hardware in a PR to this file.
