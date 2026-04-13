using MonitorControl.Transport;

Console.WriteLine("SDAP discovery sample — listening 5s on UDP " + SdapDiscovery.DefaultPort);
using var d = new SdapDiscovery();
d.StartListen();
var deadline = DateTime.UtcNow.AddSeconds(5);
while (DateTime.UtcNow < deadline)
{
	if (d.TryRead(null, out var p, out _))
	{
		Console.WriteLine("{0}\t{1}\t{2}", p.ConnectionIp, p.ProductName, p.SerialNumber);
	}
	else
	{
		Thread.Sleep(50);
	}
}
