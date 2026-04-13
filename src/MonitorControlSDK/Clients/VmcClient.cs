using System.Text;
using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Protocol;
using Sony.MonitorControl.Transport;

namespace Sony.MonitorControl.Clients;

/// <summary>VMC (ASCII STATget / STATset) client over SDCP v3 item 0xB000.</summary>
public sealed class VmcClient
{
	private readonly ISdcpTransport _transport;
	private readonly object _sync = new();

	public VmcClient(ISdcpTransport transport) => _transport = transport;

	/// <summary>Sends a VMC command: <c>category</c> plus optional segments (e.g. <c>Send("STATset", "RGAIN", "500")</c>).</summary>
	public LegacyVmcContainer? Send(string category, params string[] segments)
	{
		lock (_sync)
		{
			var packet = new SdcpMessageBuffer();
			LegacyVmcContainer vmc = packet.createVmcContainer();
			packet.setupVmcPacketHeader();
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
			packet.setupVmcPacketHeader();
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

			int num = wire[12];
			var array = new byte[num];
			Array.Copy(packet.data, array, num);
			return num == 2 ? null : Encoding.ASCII.GetString(array);
		}
	}
}
