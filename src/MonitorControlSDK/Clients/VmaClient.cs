using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Protocol;
using Sony.MonitorControl.Transport;

namespace Sony.MonitorControl.Clients;

/// <summary>VMA service and adjustment commands (SDCP item 0xF000 / v3 VMA header).</summary>
public sealed class VmaClient
{
	private readonly ISdcpTransport _transport;

	public VmaClient(ISdcpTransport transport) => _transport = transport;

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

	/// <summary>VMA service command 8: firmware transfer chunk index (dangerous; requires correct device state).</summary>
	public int SendFirmwareUpgradeChunk(int chunkIndex, SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.serviceUpgradeChunk(chunkIndex);
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>VMA service command 9: declare kernel image size before streaming (dangerous).</summary>
	public int SendFirmwareUpgradeKernel(int byteSize, SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.serviceUpgradeKernel(byteSize);
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>VMA service command 10: declare FPGA image size before streaming (dangerous).</summary>
	public int SendFirmwareUpgradeFpga(int byteSize, SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.serviceUpgradeFPGA(byteSize);
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>VMA service command 11: reboot after upgrade payload applied (dangerous).</summary>
	public int SendFirmwareUpgradeRestart(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.serviceUpgradeRestart();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>VMA service command 14 / sub 0: FPGA #1 version query.</summary>
	public int SendGetFpga1Version(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.GetFPGA1Version();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>VMA service command 14 / sub 1: FPGA #2 version query.</summary>
	public int SendGetFpga2Version(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.GetFPGA2Version();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}

	/// <summary>VMA service command 14 / sub 2: FPGA core version query.</summary>
	public int SendGetFpgaCoreVersion(SdcpMessageBuffer packet)
	{
		packet.setupVma();
		packet.clearContainer();
		LegacyVmaContainer vma = packet.createVmaContainer();
		vma.GetFPGACoreVersion();
		if (!_transport.sendPacket(packet))
		{
			return MonitorProtocolCodes.SendError;
		}

		return !_transport.receivePacket(packet) ? MonitorProtocolCodes.RecvError : MonitorProtocolCodes.Ok;
	}
}
