using MonitorControl.Protocol;
using Xunit;

namespace MonitorControl.Tests;

/// <summary>Header + payload layout for UDP group/all VMC (no I/O).</summary>
public sealed class VmcUdpBroadcastWireTests
{
	[Fact]
	public void AllMonitorsScope_SetsGroupAndUnitIdToFF()
	{
		var packet = new SdcpMessageBuffer();
		packet.setAllConnection();
		_ = packet.createVmcContainer();
		packet.setupVmcPacketHeader();
		packet.clearContainer();
		Assert.Equal(0xFF, packet.packet[6]);
		Assert.Equal(0xFF, packet.packet[7]);
	}

	[Fact]
	public void GroupScope_SetsGroupIdAndZeroUnit()
	{
		var packet = new SdcpMessageBuffer();
		packet.setGroupConnection(7);
		_ = packet.createVmcContainer();
		packet.setupVmcPacketHeader();
		packet.clearContainer();
		Assert.Equal(7, packet.packet[6]);
		Assert.Equal(0, packet.packet[7]);
	}
}
