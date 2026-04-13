using MonitorControl.Clients;
using MonitorControl.Protocol;
using MonitorControl.Transport;

if (args.Length < 1)
{
	Console.WriteLine("Usage: Sample.Vma <host>");
	return 1;
}

using var tcp = new SdcpConnection(args[0]);
tcp.Open();
var vma = new VmaClient(tcp);
var buf = new SdcpMessageBuffer();
int r = vma.SendGetControlSoftwareVersion(buf);
Console.WriteLine("VMA GetControlSoftwareVersion result={0} payloadLen={1}", r, buf.dataLength);
return 0;
