# Optional third-party sources (cross-check only)

**You do not need these links to implement or operate against this repository.** All SDAP/SDCP/VMC rules exercised by the shipped code are documented under [`docs/reference/`](.) and [`docs/spec/`](../spec/), including the in-repo [**PVM-740 programmer manual synthesis**](pvm-740-programmer-manual-synthesis.md).

Use the table below only to sanity-check headers and ports against public materials. **This repository’s C# remains authoritative** for behavior we ship; public PDFs may describe **projectors** or **older monitors** with small differences.

For **in-repo** supplementary reference code under [`references/`](../../references/), see [**references-parity.md**](references-parity.md) (maps every subtree to `src/` or marks UI-only / out-of-scope items).

| Resource | URL | Relevance |
|----------|-----|-----------|
| PVM-740 Interface Manual for Programmers (HTML excerpt on ManualsLib) | [ManualsLib — programmer manual](http://www.manualslib.com/manual/1270703/Sony-Pvm-740.html) (e.g. [page 6 — SDCP packets](http://www.manualslib.com/manual/1270703/Sony-Pvm-740.html?page=6)) | Ports **53862/53484/21**, SDAP v4 **`DA`**, SDCP v3 **`03h`/`0Bh`**, items **`B000h`/`B001h`**, VMC rules — **fully synthesized in-repo:** [pvm-740-programmer-manual-synthesis.md](pvm-740-programmer-manual-synthesis.md) + [appendices/pvm-740-vmc-catalog-from-manual.txt](appendices/pvm-740-vmc-catalog-from-manual.txt) |
| Projector protocol manual (multi-protocol overview) | [BPJ Protocol Manual PDF](https://pro.sony/s3/2018/07/05125823/Sony_Protocol-Manual_1st-Edition.pdf) | SDAP on **53862**; **ADCP** on **53595** for projectors (not the same as monitor SDCP **53484**) |
| pySDCP (Python, projectors) | [Galala7/pySDCP](https://github.com/Galala7/pySDCP) | Community SDCP + SDAP patterns |
| sony-sdcp-com (Node, projectors) | [vokkim/sony-sdcp-com](https://github.com/vokkim/sony-sdcp-com) | Community command shapes |
| BRAVIA Simple IP Control | [BRAVIA knowledge center](https://pro-bravia.sony.net/remote-display-control/simple-ip-control/) | **Different** stack (TCP **20060**, 24-byte records) — do not confuse with SDCP |

When public vendor PDFs disagree with on-wire captures from **your** chassis, trust **captures** and file an issue with hex evidence.
