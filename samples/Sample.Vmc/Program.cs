using MonitorControl.Clients;
using MonitorControl.Transport;

if (args.Length < 2)
{
	Console.WriteLine("Usage: Sample.Vmc <host> <STATget-field>");
	return 1;
}

using var tcp = new SdcpConnection(args[0]);
tcp.Open();
var vmc = new VmcClient(tcp);
Console.WriteLine(vmc.GetStatString(args[1]) ?? "(null)");
return 0;
