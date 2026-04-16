using System.Text;
using MonitorControl.Internal;
using MonitorControl.Protocol;
using MonitorControl.Transport;

namespace MonitorControl.Clients;

/// <summary>VMC (ASCII STATget / STATset) client over SDCP v3 item 0xB000.</summary>
public sealed class VmcClient
{
	private readonly ISdcpTransport _transport;
	private readonly object _sync = new();

	public VmcClient(ISdcpTransport transport) => _transport = transport;

	/// <summary>
	/// When set (typically 1–99), SDCP v3 uses <see cref="SdcpMessageBuffer.setSingleConnection"/> (group 0, this unit) instead of P2P (0,0).
	/// Use when the monitor’s SDAP advertisement shows a non-zero unit and TCP commands become unreliable.
	/// </summary>
	public byte? TcpSingleUnitId { get; set; }

	/// <summary>Sends a VMC command: <c>category</c> plus optional segments (e.g. <c>Send("STATset", "RGAIN", "500")</c>).</summary>
	public LegacyVmcContainer? Send(string category, params string[] segments)
	{
		lock (_sync)
		{
			var packet = new SdcpMessageBuffer();
			LegacyVmcContainer vmc = packet.createVmcContainer();
			PrepareVmcHeader(packet);
			packet.clearContainer();
			vmc.setCommand(category, segments);
			if (!_transport.sendPacket(packet))
			{
				return null;
			}

			if (!_transport.receivePacket(packet))
			{
				return null;
			}

			return packet.createVmcContainer();
		}
	}

	/// <summary>Reads STATget payload as ASCII string (legacy <c>VmcCommand.getSTATgetMessage</c> behavior).</summary>
	public string? GetStatString(string command)
	{
		lock (_sync)
		{
			var packet = new SdcpMessageBuffer();
			LegacyVmcContainer vmc = packet.createVmcContainer();
			PrepareVmcHeader(packet);
			packet.clearContainer();
			vmc.setCommand("STATget", command);
			if (!_transport.sendPacket(packet) || !_transport.receivePacket(packet))
			{
				return null;
			}

			byte[] wire = packet.packet;
			if (wire.Length < 13)
			{
				return null;
			}

			int num = (wire[11] << 8) | wire[12];
			if (num < 0 || num > packet.data.Length)
			{
				return null;
			}

			var array = new byte[num];
			Array.Copy(packet.data, array, num);
			return num == 2 ? null : Encoding.ASCII.GetString(array);
		}
	}

	private void PrepareVmcHeader(SdcpMessageBuffer packet)
	{
		packet.setupVmcPacketHeader();
		if (TcpSingleUnitId is { } uid)
		{
			packet.setSingleConnection(uid);
		}
	}
}
