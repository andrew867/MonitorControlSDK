using MonitorControl.Transport;

Console.WriteLine("SDAP discovery sample — listening 5s on UDP " + SdapDiscovery.DefaultPort);
using var d = new SdapDiscovery();
d.StartListen();
var deadline = DateTime.UtcNow.AddSeconds(5);
while (DateTime.UtcNow < deadline)
{
	if (d.TryRead(null, out var p, out _))
	{
		Console.WriteLine(
			"packetIp={0}\ttcpHost={1}\t{2}\t{3}",
			p.ConnectionIp,
			p.RecommendedControlIPv4 ?? "?",
			p.ProductName,
			p.SerialNumber);
	}
	else
	{
		Thread.Sleep(50);
	}
}
