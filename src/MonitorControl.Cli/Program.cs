using System.CommandLine;
using System.Net;
using MonitorControl.Clients;
using MonitorControl.Protocol;
using MonitorControl.Transport;

var hostOption = new Option<string>("--host", "Monitor IP address") { IsRequired = true };
var bindOption = new Option<string?>("--bind", "Local IP to bind for SDAP (default: any)");
var filterOption = new Option<string?>("--filter", "Product name prefix filter");
var timeoutOption = new Option<int>("--timeout-ms", () => 10_000, "SDCP receive timeout");
var bcastIpOption = new Option<string?>("--broadcast", "UDP destination IP (default 255.255.255.255)");
var bcastPortOption = new Option<int>("--port", () => SdcpConnection.DefaultPort, "UDP destination port");
var bcastBindOption = new Option<string?>("--local-bind", "Optional local bind address for UDP socket");
var scopeOption = new Option<string>("--scope", () => "all", "all | group");
var groupOption = new Option<int>("--group-id", () => 1, "Group ID 1–99 when scope=group");
var tokensArgument = new Argument<string[]>("tokens", "VMC tokens, e.g. STATset BRIGHTNESS 512")
{
	Arity = ArgumentArity.OneOrMore,
};

var discoverCmd = new Command("discover", "Listen for SDAP advertisements (UDP 53862)");
discoverCmd.AddOption(bindOption);
discoverCmd.AddOption(filterOption);
discoverCmd.SetHandler(RunDiscover, bindOption, filterOption);

var vmcBcastCmd = new Command("vmc-broadcast", "Send VMC over UDP SDCP (Group/All; no response)");
vmcBcastCmd.AddOption(scopeOption);
vmcBcastCmd.AddOption(groupOption);
vmcBcastCmd.AddOption(bcastIpOption);
vmcBcastCmd.AddOption(bcastPortOption);
vmcBcastCmd.AddOption(bcastBindOption);
vmcBcastCmd.AddArgument(tokensArgument);
vmcBcastCmd.SetHandler(RunVmcBroadcast, scopeOption, groupOption, bcastIpOption, bcastPortOption, bcastBindOption, tokensArgument);

var vmsCmd = new Command("vms-info", "VMS: product info + common packaged status (requires SDCP)");
vmsCmd.AddOption(hostOption);
vmsCmd.AddOption(timeoutOption);
vmsCmd.SetHandler(RunVmsInfo, hostOption, timeoutOption);

var statArgument = new Argument<string>("stat", "STATget field name e.g. MODEL");
var vmcCmd = new Command("vmc", "VMC: send STATget line (e.g. MODEL)");
vmcCmd.AddOption(hostOption);
vmcCmd.AddArgument(statArgument);
vmcCmd.SetHandler(RunVmcGet, hostOption, statArgument);

var vmaCmd = new Command("vma-version", "VMA: read control software version string");
vmaCmd.AddOption(hostOption);
vmaCmd.SetHandler(RunVmaVersion, hostOption);

var root = new RootCommand("SDAP/SDCP monitor control CLI (MonitorControlSDK)");
root.AddCommand(discoverCmd);
root.AddCommand(vmcBcastCmd);
root.AddCommand(vmsCmd);
root.AddCommand(vmcCmd);
root.AddCommand(vmaCmd);

return await root.InvokeAsync(args);

static void RunVmcBroadcast(string scope, int groupId, string? broadcastIp, int port, string? localBind, string[] tokens)
{
	if (tokens.Length < 1)
	{
		Console.Error.WriteLine("At least one token is required (e.g. STATset BRIGHTNESS 512).");
		return;
	}

	IPAddress destIp = IPAddress.Broadcast;
	if (!string.IsNullOrWhiteSpace(broadcastIp))
	{
		if (!IPAddress.TryParse(broadcastIp, out IPAddress? parsed))
		{
			Console.Error.WriteLine("Invalid --broadcast address.");
			return;
		}

		destIp = parsed;
	}

	IPEndPoint? localEp = null;
	if (!string.IsNullOrWhiteSpace(localBind) && IPAddress.TryParse(localBind, out var lb))
	{
		localEp = new IPEndPoint(lb, 0);
	}

	var dest = new IPEndPoint(destIp, port);
	var vmcScope = string.Equals(scope, "group", StringComparison.OrdinalIgnoreCase)
		? VmcUdpBroadcastScope.Group
		: VmcUdpBroadcastScope.AllMonitors;
	byte gid = (byte)Math.Clamp(groupId, 1, 99);
	using var client = new VmcUdpBroadcastClient(dest, localEp);
	string category = tokens[0];
	string[] tail = tokens.Length > 1 ? tokens[1..] : Array.Empty<string>();
	bool ok = client.TrySend(vmcScope, gid, category, tail);
	Console.WriteLine(ok ? "UDP VMC send ok." : "UDP VMC send failed.");
}

static void RunDiscover(string? bind, string? filter)
{
	IReadOnlyList<string>? filters = filter != null ? new[] { filter } : null;
	using var d = new SdapDiscovery();
	if (bind != null && IPAddress.TryParse(bind, out var ip))
	{
		d.StartListen(ip);
	}
	else
	{
		d.StartListen();
	}

	Console.WriteLine("Listening for SDAP on port {0}… (Ctrl+C to stop)", SdapDiscovery.DefaultPort);
	while (true)
	{
		if (!d.TryRead(filters, out var pack, out var matched))
		{
			Thread.Sleep(50);
			continue;
		}

		Console.WriteLine("{0}  product={1}  serial={2}  group={3} unit={4}  ip={5}",
			pack.SourceIp,
			pack.ProductName,
			pack.SerialNumber,
			pack.GroupId,
			pack.UnitId,
			pack.ConnectionIp);
		if (matched != null)
		{
			Console.WriteLine("  matched filter: {0}", matched);
		}
	}
}

static void RunVmsInfo(string host, int timeoutMs)
{
	using var tcp = new SdcpConnection(host) { ReceiveTimeoutMs = timeoutMs, SendTimeoutMs = timeoutMs };
	tcp.Open();
	var vms = new VmsClient(tcp);
	var buf = new SdcpMessageBuffer();
	int r = vms.SendGetProductInformation(buf);
	if (r != MonitorProtocolCodes.Ok)
	{
		Console.Error.WriteLine("Send failed: {0}", r);
		return;
	}

	r = vms.ReceiveVmsPacket(buf);
	if (r != MonitorProtocolCodes.Ok)
	{
		Console.Error.WriteLine("Recv failed: {0}", r);
		return;
	}

	if (!vms.CheckVmsRecvOk(buf))
	{
		Console.Error.WriteLine("Device returned error status in VMS payload.");
		return;
	}

	Console.WriteLine("VMS payload length (V4): {0}", buf.dataLengthV4);
	Console.WriteLine(HexDump(buf.data.AsSpan(0, Math.Min(buf.dataLengthV4, 64))));
	_ = vms.SendInformationCommonPackagedStatus(buf);
	_ = vms.ReceiveVmsPacket(buf);
	Console.WriteLine("Common packaged status (first 64 bytes):");
	Console.WriteLine(HexDump(buf.data.AsSpan(0, Math.Min(buf.dataLengthV4, 64))));
}

static void RunVmcGet(string host, string stat)
{
	using var tcp = new SdcpConnection(host);
	tcp.Open();
	var vmc = new VmcClient(tcp);
	string? s = vmc.GetStatString(stat);
	Console.WriteLine(s ?? "(null)");
}

static void RunVmaVersion(string host)
{
	using var tcp = new SdcpConnection(host);
	tcp.Open();
	var vma = new VmaClient(tcp);
	var buf = new SdcpMessageBuffer();
	int r = vma.SendGetControlSoftwareVersion(buf);
	Console.WriteLine("result={0}", r);
	if (r == MonitorProtocolCodes.Ok)
	{
		Console.WriteLine(HexDump(buf.data.AsSpan(0, Math.Min(buf.dataLength, 64))));
	}
}

static string HexDump(ReadOnlySpan<byte> span)
{
	var sb = new System.Text.StringBuilder();
	for (int i = 0; i < span.Length; i++)
	{
		sb.Append(span[i].ToString("X2"));
		sb.Append(i % 16 == 15 ? Environment.NewLine : " ");
	}

	return sb.ToString();
}
