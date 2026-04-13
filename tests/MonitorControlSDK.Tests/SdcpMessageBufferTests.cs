using Sony.MonitorControl.Protocol;
using Xunit;

namespace Sony.MonitorControl.Tests;

public sealed class SdcpMessageBufferTests
{
	[Fact]
	public void VmcHeader_DefaultP2P_MatchesLegacyConstants()
	{
		var p = new SdcpMessageBuffer();
		p.setupVmcPacketHeader();
		p.clearContainer();
		Assert.Equal(3, p.packet[0]);
		Assert.Equal(11, p.packet[1]);
		Assert.Equal((byte)'S', p.packet[2]);
		Assert.Equal((byte)'O', p.packet[3]);
		Assert.Equal((byte)'N', p.packet[4]);
		Assert.Equal((byte)'Y', p.packet[5]);
		Assert.Equal(0xB000, p.packet[9] * 256 + p.packet[10]);
	}

	[Fact]
	public void VmsV4Header_ItemNumber_Is0xB900()
	{
		var p = new SdcpMessageBuffer();
		p.setSdcpV4PacketHeader();
		p.setSingleConnection(2);
		p.setupVmsPacketHeader();
		p.clearContainer();
		Assert.Equal(4, p.packetV4[0]);
		Assert.Equal(185, p.packetV4[33]);
		Assert.Equal(0, p.packetV4[34]);
		Assert.Equal(2, p.packetV4[31]);
	}

	[Fact]
	public void VmaHeader_Item_Is0xF000()
	{
		var p = new SdcpMessageBuffer();
		p.setupVma();
		Assert.Equal(240, p.packet[9]);
		Assert.Equal(0, p.packet[10]);
	}
}
