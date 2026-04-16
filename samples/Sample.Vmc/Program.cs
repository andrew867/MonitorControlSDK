using MonitorControl.Clients;
using MonitorControl.Transport;

if (args.Length < 2)
{
	Console.WriteLine("Usage: Sample.Vmc <host> <STATget-field> [--sdcp-unit <0-255>]");
	return 1;
}

using var tcp = new SdcpConnection(args[0]);
tcp.Open();
var vmc = new VmcClient(tcp);
for (int i = 2; i < args.Length - 1; i++)
{
	if (string.Equals(args[i], "--sdcp-unit", StringComparison.OrdinalIgnoreCase) &&
	    byte.TryParse(args[i + 1], out byte uid))
	{
		vmc.TcpSingleUnitId = uid;
		break;
	}
}

Console.WriteLine(vmc.GetStatString(args[1]) ?? "(null)");
return 0;
