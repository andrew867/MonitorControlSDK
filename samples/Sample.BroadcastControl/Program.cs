using Sony.MonitorControl.Clients;
using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Repl;
using Sony.MonitorControl.Transport;

if (!TryParseHost(args, out string? host, out string? usageError))
{
	Console.Error.WriteLine(usageError);
	return 1;
}

using var tcp = new SdcpConnection(host!);
try
{
	tcp.Open();
}
catch (Exception ex)
{
	Console.Error.WriteLine("Connect failed: {0}", ex.Message);
	return 2;
}

var vmc = new VmcClient(tcp);
Console.WriteLine("Connected to {0}:{1}. Commands: get, set, help, quit.", host, SdcpConnection.DefaultPort);
Console.WriteLine("Example: get MODEL   |   set BRIGHTNESS 512");

while (true)
{
	Console.Write("broadcast> ");
	string? line = Console.ReadLine();
	if (!BroadcastControlLineParser.TryParse(line, out BroadcastReplCommand cmd, out string? err))
	{
		Console.WriteLine(err);
		continue;
	}

	switch (cmd.Kind)
	{
		case BroadcastReplKind.Empty:
			continue;
		case BroadcastReplKind.Quit:
			return 0;
		case BroadcastReplKind.Help:
			PrintHelp();
			continue;
		case BroadcastReplKind.Get:
		{
			string? s = vmc.GetStatString(cmd.GetField!);
			Console.WriteLine(s ?? "(null)");
			break;
		}
		case BroadcastReplKind.Set:
		{
			LegacyVmcContainer? r = vmc.Send("STATset", cmd.SetSegments!);
			PrintVmcResponse(r);
			break;
		}
	}
}

static void PrintHelp()
{
	Console.WriteLine("get <field>     — STATget (e.g. get MODEL)");
	Console.WriteLine("set <tokens…>   — STATset with remainder as payload (e.g. set BRIGHTNESS 512)");
	Console.WriteLine("help            — this text");
	Console.WriteLine("quit / exit     — disconnect");
}

static void PrintVmcResponse(LegacyVmcContainer? c)
{
	if (c is null)
	{
		Console.WriteLine("(null response)");
		return;
	}

	_ = c.parse(out string[]? args);
	if (args is null || args.Length == 0)
	{
		Console.WriteLine("(empty payload)");
	}
	else
	{
		Console.WriteLine(string.Join(" ", args));
	}
}

static bool TryParseHost(string[] args, out string? host, out string? error)
{
	host = null;
	error = null;
	for (int i = 0; i < args.Length; i++)
	{
		if (args[i] == "--host" && i + 1 < args.Length)
		{
			host = args[++i];
		}
		else if (!args[i].StartsWith("-", StringComparison.Ordinal) && host is null)
		{
			host = args[i];
		}
	}

	if (string.IsNullOrWhiteSpace(host))
	{
		error = "Usage: Sample.BroadcastControl --host <ip>   or   Sample.BroadcastControl <ip>";
		return false;
	}

	return true;
}
