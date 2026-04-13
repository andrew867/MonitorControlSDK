using System.Globalization;

namespace MonitorControl.Web;

internal static class WireFormat
{
	internal static string ToHex(ReadOnlySpan<byte> data, int maxBytes = 512)
	{
		int n = Math.Min(data.Length, maxBytes);
		if (n == 0)
		{
			return string.Empty;
		}

		return Convert.ToHexString(data[..n]);
	}

	internal static bool FirmwareGate(IConfiguration config, IHeaderDictionary headers)
	{
		string? env = Environment.GetEnvironmentVariable("MONITOR_CONTROL_ALLOW_DANGEROUS_FIRMWARE");
		bool envOn = env is not null && (env.Equals("1", StringComparison.OrdinalIgnoreCase)
			|| env.Equals("true", StringComparison.OrdinalIgnoreCase)
			|| env.Equals("yes", StringComparison.OrdinalIgnoreCase));
		bool cfg = config.GetValue("MonitorControl:AllowDangerousFirmware", false);
		if (!envOn && !cfg)
		{
			return false;
		}

		if (!headers.TryGetValue("X-Firmware-Ack", out var ack))
		{
			return false;
		}

		return string.Equals(ack.ToString(), "CONFIRM", StringComparison.Ordinal);
	}
}
