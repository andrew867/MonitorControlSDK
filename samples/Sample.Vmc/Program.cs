using MonitorControl.Clients;
using MonitorControl.Protocol;
using MonitorControl.Transport;

if (args.Length < 2)
{
	Console.WriteLine("Usage: Sample.Vmc <host> <STATget-field> [--sdcp-unit <0-255>] [--vmc-item B000|B001|monitor|builtIn]");
	return 1;
}

using var tcp = new SdcpConnection(args[0]);
tcp.Open();
var vmc = new VmcClient(tcp);
for (int i = 2; i < args.Length; i++)
{
	if (string.Equals(args[i], "--sdcp-unit", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
	{
		if (byte.TryParse(args[++i], out byte uid))
		{
			vmc.TcpSingleUnitId = uid;
		}
	}
	else if (string.Equals(args[i], "--vmc-item", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
	{
		vmc.VmcItemNumber = SdcpMessageBuffer.ParseVmcItemSpecifier(args[++i]);
	}
}

Console.WriteLine(vmc.GetStatString(args[1]) ?? "(null)");
return 0;
