using Sony.MonitorControl.Protocol;

namespace Sony.MonitorControl.Transport;

/// <summary>SDCP send/receive surface (legacy-compatible method names for parity with reference implementation).</summary>
public interface ISdcpTransport
{
	bool sendPacket(SdcpMessageBuffer packet);

	bool sendPacketV4(SdcpMessageBuffer packet);

	bool receivePacket(SdcpMessageBuffer packet);

	bool receivePacketV4(SdcpMessageBuffer packet);

	void closeTarget();
}
