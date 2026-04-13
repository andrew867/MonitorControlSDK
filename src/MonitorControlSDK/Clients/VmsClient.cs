using Sony.MonitorControl.Protocol;
using Sony.MonitorControl.Transport;

namespace Sony.MonitorControl.Clients;

/// <summary>High-level VMS (SDCP v4 item 0xB900) command client.</summary>
public sealed class VmsClient
{
	private readonly ISdcpTransport _transport;

	private readonly VmsCommandEngine _engine = new();

	/// <summary>Creates a client over an opened <see cref="SdcpConnection"/>.</summary>
	public VmsClient(SdcpConnection connection)
	{
		_transport = connection;
	}

	/// <summary>Full legacy-compatible command surface (pass <see cref="SdcpConnection"/> cast to <see cref="ISdcpTransport"/>).</summary>
	public VmsCommandEngine Engine => _engine;

	/// <inheritdoc cref="VmsCommandEngine.convVmsFloatValue"/>
	public byte[] EncodeVmsFloat(float value, short power) => _engine.convVmsFloatValue(value, power);

	/// <inheritdoc cref="VmsCommandEngine.reconvVmsFloatValue"/>
	public float DecodeVmsFloat(byte[] data) => _engine.reconvVmsFloatValue(data);

	/// <inheritdoc cref="VmsCommandEngine.sendGetProductInformation"/>
	public int SendGetProductInformation(SdcpMessageBuffer buffer) =>
		_engine.sendGetProductInformation(_transport, buffer);

	/// <inheritdoc cref="VmsCommandEngine.sendCommonControlStart"/>
	public int SendCommonControlStart(SdcpMessageBuffer buffer) =>
		_engine.sendCommonControlStart(_transport, buffer);

	/// <inheritdoc cref="VmsCommandEngine.recvVmsPacket"/>
	public int ReceiveVmsPacket(SdcpMessageBuffer buffer) =>
		_engine.recvVmsPacket(_transport, buffer);

	/// <inheritdoc cref="VmsCommandEngine.checkVmsRecvPacketError"/>
	public bool CheckVmsRecvOk(SdcpMessageBuffer buffer) =>
		_engine.checkVmsRecvPacketError(buffer);

	/// <inheritdoc cref="VmsCommandEngine.sendInformationCommonPackagedStatus"/>
	public int SendInformationCommonPackagedStatus(SdcpMessageBuffer buffer) =>
		_engine.sendInformationCommonPackagedStatus(_transport, buffer);

	/// <inheritdoc cref="VmsCommandEngine.sendGetRejionConfig"/>
	public int SendGetRegionConfig(SdcpMessageBuffer buffer) =>
		_engine.sendGetRejionConfig(_transport, buffer);

	/// <inheritdoc cref="VmsCommandEngine.sendGetMonitorNetworkSwitch"/>
	public int SendGetMonitorNetworkSwitch(SdcpMessageBuffer buffer) =>
		_engine.sendGetMonitorNetworkSwitch(_transport, buffer);
}
