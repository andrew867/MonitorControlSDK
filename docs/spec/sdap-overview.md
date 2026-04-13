# SDAP overview (short)

**SDAP** — UDP **53862**, advertisement layout decoded by [`SdapAdvertisementPacket`](../../src/MonitorControlSDK/Protocol/SdapAdvertisementPacket.cs). Listening helper: [`SdapDiscovery`](../../src/MonitorControlSDK/Transport/SdapDiscovery.cs).

Field offsets (product name, serial, IP octets 50–53, group/unit 120–121, `SONY` community at 4–7) are documented inline on the type.

Cross-checks with public materials: [reference/external-sources.md](../reference/external-sources.md).
