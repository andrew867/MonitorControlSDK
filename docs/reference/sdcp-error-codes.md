# SDCP error and status codes

These **ushort** values appear in legacy tooling and in device negative acknowledgements. The canonical definitions live in [`SdcpMessageBuffer`](../../src/MonitorControlSDK/Protocol/SdcpMessageBuffer.cs) (`SDCP_ERR_*` constants). A smaller curated set is duplicated in [`SdcpErrorCodes`](../../src/MonitorControlSDK/Protocol/SdcpErrorCodes.cs).

The **PVM-740 Interface Manual for Programmers** excerpt (ManualsLib) states that on an **NG** response the payload carries the **original item number** plus a **1-byte category** and **1-byte error code**, and mentions a **communication** family (e.g. checksum / `F0**h`-style encodings in the excerpt). Map captured bytes to the `SDCP_ERR_*` / `0xF010` checksum constant and neighbors in `SdcpMessageBuffer` — see also [pvm-740-programmer-manual-synthesis.md](pvm-740-programmer-manual-synthesis.md) § SDCP error response.

| Decimal | Hex | Name in source |
|--------:|-----|------------------|
| 257 | 0x0101 | `SDCP_ERR_INVALID_ITEM` |
| 258 | 0x0102 | `SDCP_ERR_INVALID_ITEMREQ` |
| 259 | 0x0103 | `SDCP_ERR_INVALID_LENGTH` |
| 260 | 0x0104 | `SDCP_ERR_INVALID_DATA` |
| 273 | 0x0111 | `SDCP_ERR_SHORT_DATA` |
| 288 | 0x0120 | `SDCP_ERR_INVALID_SUB_CMD` |
| 289 | 0x0121 | `SDCP_ERR_INVALID_SUB_CMD_DATA` |
| 290 | 0x0122 | `SDCP_ERR_PASSWORD_LOCKED` |
| 291 | 0x0123 | `SDCP_ERR_PASSWORD_AUTHENTICATION_ERROR` |
| 292 | 0x0124 | `SDCP_ERR_OPERATE_CONDITION_ERROR` |
| 293 | 0x0125 | `SDCP_ERR_COPY_EXECUTION_ERROR` |
| 294 | 0x0126 | `SDCP_ERR_USER_LUT_EXECUTION_ERROR` |
| 295 | 0x0127 | `SDCP_ERR_CAN_NOT_CONTROL` |
| 384 | 0x0180 | `SDCP_ERR_NOTAPPLICABLE` |
| 400 | 0x0190 | `SDCP_ERR_NOTEXECUTED_ORDER_SETTING` |
| 513 | 0x0201 | `SDCP_ERR_DIFFCOMMUNITY` |
| 4097 | 0x1001 | `SDCP_ERR_INVALID_VERSION` |
| 4098 | 0x1002 | `SDCP_ERR_INVALID_CATEGORY` |
| 4099 | 0x1003 | `SDCP_ERR_INVALID_REQUEST` |
| 4113 | 0x1011 | `SDCP_ERR_SHORT_HEADER` |
| 4114 | 0x1012 | `SDCP_ERR_SHORT_COMMUNITY` |
| 4115 | 0x1013 | `SDCP_ERR_SHORT_COMMAND_V3` |
| 4116 | 0x1014 | `SDCP_ERR_SHORT_ID_V3` |
| 4117 | 0x1015 | `SDCP_ERR_SHORT_MONITORNAME_V4` |
| 4118 | 0x1016 | `SDCP_ERR_SHORT_ID_V4` |
| 4119 | 0x1017 | `SDCP_ERR_SHORT_COMMAND_V4` |
| 4128 | 0x1020 | `SDCP_ERR_ACCESS_DENIED` |
| 4129 | 0x1021 | `SDCP_ERR_INVALID_ID` |
| 4130 | 0x1022 | `SDCP_ERR_NAME_DIFFER` |
| 4131 | 0x1023 | `SDCP_ERR_CANNOT_EXECUTE_SET_OPERATION` |
| 8193 | 0x2001 | `SDCP_ERR_NETWORK_TIMEOUT` |
| 61441 | 0xF001 | `SDCP_ERR_COMM_TIMEOUT` |
| 61456 | 0xF010 | `SDCP_ERR_COMM_CHECKSUM` |
| 61472 | 0xF020 | `SDCP_ERR_COMM_FRAMING` |
| 61488 | 0xF030 | `SDCP_ERR_COMM_PARITY` |
| 61504 | 0xF040 | `SDCP_ERR_COMM_OVERRUN` |
| 61520 | 0xF050 | `SDCP_ERR_COMM_OTHER` |
| 61680 | 0xF0F0 | `SDCP_ERR_COMM_UNKNOWN` |
| 61712 | 0xF110 | `SDCP_ERR_NVRAM_READ` |
| 61728 | 0xF120 | `SDCP_ERR_NVRAM_WRITE` |
| 65531 | 0xFFFB | `SDCP_ERR_INTERNAL_ASSERT` |
| 65532 | 0xFFFC | `SDCP_CLOSE_NETWORK` |
| 65533 | 0xFFFD | `SDCP_ERR_INTERNAL_NETWORK` |
| 65534 | 0xFFFE | `SDCP_ERR_INTERNAL_ERROR` |
| 65535 | 0xFFFF | `SDCP_ERR_INVALID_PACKET` |

**Return codes** used by higher-level helpers (`VmsCommandEngine`, `VmaClient`) are **not** the same list: see [`MonitorProtocolCodes`](../../src/MonitorControlSDK/Protocol/MonitorProtocolCodes.cs) (`Ok = 32`, send/recv errors).
