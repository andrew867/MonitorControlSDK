using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Protocol;
using Sony.MonitorControl.Transport;

namespace Sony.MonitorControl.Clients;

/// <summary>VMA service and adjustment commands (SDCP item 0xF000 / v3 VMA header).</summary>
public sealed class VmaClient
{
	private readonly ISdcpTransport _transport;

	public VmaClient(SdcpConnection connection) => _transport = connection;

	/// <summary>Requests control software version (VMA service command 12).</summary>
	public int SendGetControlSoftwareVersion(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.GetControlSoftwareVersion();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>Requests kernel version (VMA service command 13).</summary>
	public int SendGetKernelVersion(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.GetKernelVersion();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>Reads RTC from device (VMA service command 7).</summary>
	public int SendGetRtc(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.serviceGetRTC();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>Sends factory adjustment mode byte (VMA adjustment command 0).</summary>
	public int SendAdjustmentMode(byte mode, SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.jigAdjMode(mode);
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}
}
