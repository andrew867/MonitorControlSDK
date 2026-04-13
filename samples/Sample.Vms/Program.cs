using MonitorControl.Clients;
using MonitorControl.Protocol;
using MonitorControl.Transport;

if (args.Length < 1)
{
	Console.WriteLine("Usage: Sample.Vms <host>");
	return 1;
}

using var tcp = new SdcpConnection(args[0]);
tcp.Open();
var vms = new VmsClient(tcp);
var buf = new SdcpMessageBuffer();
if (vms.SendGetProductInformation(buf) != MonitorProtocolCodes.Ok ||
    vms.ReceiveVmsPacket(buf) != MonitorProtocolCodes.Ok)
{
	Console.Error.WriteLine("VMS exchange failed.");
	return 2;
}

Console.WriteLine("Payload bytes (V4): {0}", buf.dataLengthV4);
return 0;
