# External sources (cross-check only)

Use these to sanity-check headers and ports. **This repository’s C# remains authoritative** for behavior we ship; public PDFs may describe **projectors** or **older monitors** with small differences.

| Resource | URL | Relevance |
|----------|-----|-----------|
| PVM-740 Interface Manual for Programmers (SDCP packet overview) | [ManualsLib — SDCP packets](http://www.manualslib.com/manual/1270703/Sony-Pvm-740.html?page=6) | V3 header: version `03h`, category `0Bh`, `SONY` community, group/unit |
| Sony Protocol Manual (projectors, multi-protocol overview) | [BPJ Protocol Manual PDF](https://pro.sony/s3/2018/07/05125823/Sony_Protocol-Manual_1st-Edition.pdf) | SDAP on **53862**; **ADCP** on **53595** for projectors (not the same as monitor SDCP **53484**) |
| pySDCP (Python, projectors) | [Galala7/pySDCP](https://github.com/Galala7/pySDCP) | Community SDCP + SDAP patterns |
| sony-sdcp-com (Node, projectors) | [vokkim/sony-sdcp-com](https://github.com/vokkim/sony-sdcp-com) | Community command shapes |
| BRAVIA Simple IP Control | [Sony Pro BRAVIA Knowledge Center](https://pro-bravia.sony.net/remote-display-control/simple-ip-control/) | **Different** stack (TCP **20060**, 24-byte records) — do not confuse with SDCP |

When public Sony PDFs disagree with on-wire captures from **your** chassis, trust **captures** and file an issue with hex evidence.
