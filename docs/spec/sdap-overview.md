# SDAP overview

**SDAP** is UDP-based monitor advertisement and discovery on port **53862** (`SdapDiscovery.DefaultPort`).

## Packet layout

Legacy constants live on `SdapPacket` in `MonitorNetwork`; the SDK exposes a read model `SdapAdvertisementPacket` with the same offsets for product name, serial, IP octets, group/unit IDs, and community check (`SONY` at bytes 4–7).

## Listening

`SdapDiscovery.TryRead` mirrors legacy `SdapUdp.read`: optional product-name prefix filter list; returns when header and community are valid.

## Correct IP string

The legacy `SdapPacket.connectionIP` property concatenation was incorrect in reference code; `SdapAdvertisementPacket.ConnectionIp` uses proper `"a.b.c.d"` formatting from bytes 50–53.

## Reference

[MonitorNetwork/MonitorNetwork/SdapUdp.cs](../../MonitorNetwork/MonitorNetwork/SdapUdp.cs), [SdapPacket.cs](../../MonitorNetwork/MonitorNetwork/SdapPacket.cs).
