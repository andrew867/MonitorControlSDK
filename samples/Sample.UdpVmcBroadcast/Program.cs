using System.Net;
using MonitorControl.Clients;
using MonitorControl.Transport;

// One-shot UDP SDCP VMC (Group / All). Discovery advertisements stay on SDAP UDP 53862 (see Sample.Discovery).
// Replace broadcast with your subnet directed address (e.g. 192.168.1.255) if global broadcast is filtered.

IPEndPoint? dest = null;
if (args.Length >= 1 && IPAddress.TryParse(args[0], out var ip))
{
	dest = new IPEndPoint(ip, SdcpConnection.DefaultPort);
}

using var client = new VmcUdpBroadcastClient(dest);
bool ok = client.TrySend(VmcUdpBroadcastScope.AllMonitors, 0, "STATset", "BRIGHTNESS", "512");
Console.WriteLine(ok ? "UDP send queued (All monitors, STATset BRIGHTNESS 512)." : "UDP send failed.");
