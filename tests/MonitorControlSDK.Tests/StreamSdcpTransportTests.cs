using System.IO;
using MonitorControl.Internal;
using MonitorControl.Protocol;
using MonitorControl.Transport;
using Xunit;

namespace MonitorControl.Tests;

public sealed class StreamSdcpTransportTests
{
	[Fact]
	public void SendPacket_writes_wire_bytes()
	{
		var ms = new MemoryStream();
		var p = new SdcpMessageBuffer();
		p.setupVmcPacketHeader();
		p.clearContainer();
		LegacyVmcContainer vmc = p.createVmcContainer();
		vmc.setCommand("STATget", "MODEL");
		using (var tr = new StreamSdcpTransport(ms, ownsStream: false))
		{
			Assert.True(tr.sendPacket(p));
		}

		byte[] written = ms.ToArray();
		// Header + payload length is synced when the wire buffer is materialized (see SdcpMessageBuffer.packet getter).
		Assert.Equal(p.packet.Length, written.Length);
	}

	[Fact]
	public void ReceivePacket_reads_fixed_buffer_size()
	{
		var incoming = new byte[973];
		incoming[0] = 3;
		incoming[1] = 11;
		// Payload length 960 bytes so total SDCP v3 frame is 13 + 960 = 973 (matches maxSize read).
		incoming[11] = 3;
		incoming[12] = 192;
		var ms = new MemoryStream(incoming);
		using var tr = new StreamSdcpTransport(ms, ownsStream: false);
		var p = new SdcpMessageBuffer();
		Assert.True(tr.receivePacket(p));
		Assert.Equal(973, p.packet.Length);
		Assert.Equal(3, p.packet[0]);
	}

	[Fact]
	public void ReceivePacket_reassembles_v3_frame_when_reads_are_fragmented()
	{
		var incoming = new byte[973];
		incoming[0] = 3;
		incoming[1] = 11;
		incoming[11] = 3;
		incoming[12] = 192;
		using var ms = new ChunkedMemoryStream(incoming, readChunk: 17);
		using var tr = new StreamSdcpTransport(ms, ownsStream: false);
		var p = new SdcpMessageBuffer();
		Assert.True(tr.receivePacket(p));
		Assert.Equal(973, p.packet.Length);
	}

	[Fact]
	public void ReceivePacketV4_reassembles_when_reads_are_fragmented()
	{
		int payload = 100;
		var incoming = new byte[37 + payload];
		incoming[0] = 4;
		incoming[1] = 11;
		incoming[35] = (byte)(payload >> 8);
		incoming[36] = (byte)payload;
		using var ms = new ChunkedMemoryStream(incoming, readChunk: 5);
		using var tr = new StreamSdcpTransport(ms, ownsStream: false);
		var p = new SdcpMessageBuffer();
		Assert.True(tr.receivePacketV4(p));
		Assert.Equal(37 + payload, p.packetV4.Length);
	}
}
