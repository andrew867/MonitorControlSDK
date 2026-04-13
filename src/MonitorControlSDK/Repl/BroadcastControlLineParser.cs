namespace MonitorControl.Repl;

/// <summary>Parses REPL lines for broadcast-style monitor control (see docs/spec/broadcast-realtime-control.md).</summary>
public static class BroadcastControlLineParser
{
	public static bool TryParse(string? line, out BroadcastReplCommand command, out string? error)
	{
		command = BroadcastReplCommand.Empty;
		error = null;
		if (line is null)
		{
			command = BroadcastReplCommand.Quit;
			return true;
		}

		line = line.Trim();
		if (line.Length == 0)
		{
			return true;
		}

		string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return true;
		}

		string verb = parts[0];
		if (verb.Equals("quit", StringComparison.OrdinalIgnoreCase) || verb.Equals("exit", StringComparison.OrdinalIgnoreCase))
		{
			command = BroadcastReplCommand.Quit;
			return true;
		}

		if (verb.Equals("help", StringComparison.OrdinalIgnoreCase) || verb.Equals("?", StringComparison.OrdinalIgnoreCase))
		{
			command = BroadcastReplCommand.Help;
			return true;
		}

		if (verb.Equals("get", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length != 2)
			{
				error = "get requires exactly one field name (e.g. get MODEL).";
				return false;
			}

			command = BroadcastReplCommand.ForGet(parts[1]);
			return true;
		}

		if (verb.Equals("set", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2)
			{
				error = "set requires at least one STATset token (e.g. set BRIGHTNESS 512).";
				return false;
			}

			var tail = new string[parts.Length - 1];
			Array.Copy(parts, 1, tail, 0, tail.Length);
			command = BroadcastReplCommand.ForSet(tail);
			return true;
		}

		error = "Unknown command. Type help.";
		return false;
	}
}

public readonly struct BroadcastReplCommand
{
	public BroadcastReplKind Kind { get; private init; }

	public string? GetField { get; private init; }

	public string[]? SetSegments { get; private init; }

	public static BroadcastReplCommand Empty => new() { Kind = BroadcastReplKind.Empty };

	public static BroadcastReplCommand Quit => new() { Kind = BroadcastReplKind.Quit };

	public static BroadcastReplCommand Help => new() { Kind = BroadcastReplKind.Help };

	public static BroadcastReplCommand ForGet(string field) => new()
	{
		Kind = BroadcastReplKind.Get,
		GetField = field,
	};

	public static BroadcastReplCommand ForSet(string[] statSetSegments) => new()
	{
		Kind = BroadcastReplKind.Set,
		SetSegments = statSetSegments,
	};
}

public enum BroadcastReplKind
{
	Empty,
	Get,
	Set,
	Help,
	Quit,
}
