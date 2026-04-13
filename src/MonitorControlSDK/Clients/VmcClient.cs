using System.Text;
using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Protocol;
using Sony.MonitorControl.Transport;

namespace Sony.MonitorControl.Clients;

/// <summary>VMC (ASCII STATget / STATset) client over SDCP v3 item 0xB000.</summary>
public sealed class VmcClient
{
	private readonly SdcpConnection _connection;
	private readonly object _sync = new();

	public VmcClient(SdcpConnection connection) => _connection = connection;

	/// <summary>Sends a VMC command with one payload segment after the category.</summary>
	public LegacyVmcContainer? Send(string category, string cmd)
	{
		lock (_sync)
		{
			var packet = new SdcpMessageBuffer();
			LegacyVmcContainer vmc = packet.createVmcContainer();
			packet.setupVmcPacketHeader();
			packet.clearContainer();
			vmc.setCommand(category, cmd);
			if (!_connection.sendPacket(packet))
			{
				return null;
			}

			if (!_connection.receivePacket(packet))
			{
				return null;
			}

			return packet.createVmcContainer();
		}
	}

	/// <summary>Sends category + two arguments (e.g. STATset, RGAIN, "500").</summary>
	public LegacyVmcContainer? Send(string category, string cmd, string arg2)
	{
		lock (_sync)
		{
			var packet = new SdcpMessageBuffer();
			LegacyVmcContainer vmc = packet.createVmcContainer();
			packet.setupVmcPacketHeader();
			packet.clearContainer();
			vmc.setCommand(category, cmd, arg2);
			if (!_connection.sendPacket(packet))
			{
				return null;
			}

			if (!_connection.receivePacket(packet))
			{
				return null;
			}

			return packet.createVmcContainer();
		}
	}

	/// <summary>Sends category + three arguments.</summary>
	public LegacyVmcContainer? Send(string category, string cmd, string arg2, string arg3)
	{
		lock (_sync)
		{
			var packet = new SdcpMessageBuffer();
			LegacyVmcContainer vmc = packet.createVmcContainer();
			packet.setupVmcPacketHeader();
			packet.clearContainer();
			vmc.setCommand(category, cmd, arg2, arg3);
			if (!_connection.sendPacket(packet))
			{
				return null;
			}

			if (!_connection.receivePacket(packet))
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
			if (!_connection.sendPacket(packet) || !_connection.receivePacket(packet))
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
