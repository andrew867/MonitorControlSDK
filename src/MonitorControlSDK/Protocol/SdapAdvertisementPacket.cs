using System.Net;
using System.Text;

namespace MonitorControl.Protocol;

/// <summary>Decoded SDAP v4/v5 advertisement (legacy <c>SdapPacket</c> fields).</summary>
/// <remarks>Field meanings and SDAP v4 header rules (<c>DA</c>, version <c>04h</c>, category <c>0Bh</c>/<c>0Ch</c>) are summarized alongside the PVM-740 programmer manual excerpt in <c>docs/reference/pvm-740-programmer-manual-synthesis.md</c>.</remarks>
public sealed class SdapAdvertisementPacket
{
	public const int MaxPacketSize = 144;

	public byte[] Raw { get; } = new byte[MaxPacketSize];

	public IPAddress? SourceIp { get; set; }

	public byte Version => Raw[2];

	public byte Category => Raw[3];

	public string ProductName
	{
		get
		{
			var sb = new StringBuilder();
			for (uint i = 8; i < 20 && Raw[i] != 0; i++)
			{
				sb.Append((char)Raw[i]);
			}

			return sb.ToString();
		}
	}

	public string SerialNumber
	{
		get
		{
			uint num = 0;
			for (uint i = 20; i < 24; i++)
			{
				num <<= 8;
				num |= Raw[i];
			}

			return num.ToString();
		}
	}

	/// <summary>Monitor primary IP octets from packet (indices 50–53).</summary>
	public string ConnectionIp =>
		$"{Raw[50]}.{Raw[51]}.{Raw[52]}.{Raw[53]}";

	/// <summary>IPv4 string to use for SDCP TCP when <see cref="ConnectionIp"/> is unset (<c>0.0.0.0</c> per manual); otherwise <see cref="ConnectionIp"/>.</summary>
	public string? RecommendedControlIPv4
	{
		get
		{
			if (ConnectionIp != "0.0.0.0")
			{
				return ConnectionIp;
			}

			return SourceIp?.ToString();
		}
	}

	public byte GroupId => Raw[120];

	public byte UnitId => Raw[121];

	public static SdapAdvertisementPacket FromBuffer(byte[] buffer, int length)
	{
		var p = new SdapAdvertisementPacket();
		Array.Copy(buffer, p.Raw, Math.Min(length, MaxPacketSize));
		return p;
	}

	public bool IsHeaderOk() => Raw[0] == 68 && Raw[1] == 65;

	public bool IsCommunityOk() => Raw[4] == 83 && Raw[5] == 79 && Raw[6] == 78 && Raw[7] == 89;

	public bool IsPeerToPeer() => GroupId == 0 && UnitId == 0;
}
