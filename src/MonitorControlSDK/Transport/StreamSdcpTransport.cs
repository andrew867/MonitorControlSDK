using System.IO;
using Sony.MonitorControl.Protocol;

namespace Sony.MonitorControl.Transport;

/// <summary>SDCP framing over an arbitrary <see cref="Stream"/> (tests, proxies). Matches <see cref="SdcpConnection"/> read/write sizes.</summary>
public sealed class StreamSdcpTransport : ISdcpTransport, IDisposable
{
	private readonly Stream _stream;
	private readonly bool _ownsStream;

	public StreamSdcpTransport(Stream stream, bool ownsStream = false)
	{
		_stream = stream ?? throw new ArgumentNullException(nameof(stream));
		_ownsStream = ownsStream;
	}

	public bool sendPacket(SdcpMessageBuffer packet)
	{
		try
		{
			_stream.Write(packet.packet, 0, packet.length);
			_stream.Flush();
		}
		catch
		{
			return false;
		}

		return true;
	}

	public bool sendPacketV4(SdcpMessageBuffer packet)
	{
		try
		{
			_stream.Write(packet.packetV4, 0, packet.lengthV4);
			_stream.Flush();
		}
		catch
		{
			return false;
		}

		return true;
	}

	public bool receivePacket(SdcpMessageBuffer packet)
	{
		try
		{
			var array = new byte[packet.maxSize];
			_ = _stream.Read(array, 0, packet.maxSize);
			packet.packet = array;
		}
		catch
		{
			return false;
		}

		return true;
	}

	public bool receivePacketV4(SdcpMessageBuffer packet)
	{
		try
		{
			var array = new byte[packet.maxSize];
			_ = _stream.Read(array, 0, packet.maxSize);
			packet.packetV4 = array;
		}
		catch
		{
			return false;
		}

		return true;
	}

	public void closeTarget()
	{
		if (_ownsStream)
		{
			try
			{
				_stream.Dispose();
			}
			catch
			{
				// ignored
			}
		}
	}

	public void Dispose() => closeTarget();
}
