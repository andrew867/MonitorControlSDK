using System.Net.Sockets;
using MonitorControl.Protocol;

namespace MonitorControl.Transport;

/// <summary>TCP connection to monitor SDCP port (default 53484), matching legacy <c>SdcpTcp</c> framing behavior.</summary>
public sealed class SdcpConnection : ISdcpTransport, IAsyncDisposable, IDisposable
{
	public const int DefaultPort = 53484;

	private TcpClient? _client;

	public string Host { get; }

	public int Port { get; }

	public int ReceiveTimeoutMs { get; set; } = 10_000;

	public int SendTimeoutMs { get; set; } = 5_000;

	public SdcpConnection(string host, int port = DefaultPort)
	{
		Host = host;
		Port = port;
	}

	/// <summary>Opens TCP connection (synchronous; matches legacy tools).</summary>
	public void Open()
	{
		_client?.Close();
		_client = new TcpClient(Host, Port)
		{
			ReceiveTimeout = ReceiveTimeoutMs,
			SendTimeout = SendTimeoutMs,
		};
		_client.LingerState = new LingerOption(true, 0);
	}

	public NetworkStream GetStream()
	{
		if (_client?.Connected != true)
		{
			throw new InvalidOperationException("Not connected. Call Open() first.");
		}

		return _client.GetStream();
	}

	public async Task OpenAsync(CancellationToken cancellationToken = default)
	{
		_client?.Close();
		var c = new TcpClient { ReceiveTimeout = ReceiveTimeoutMs, SendTimeout = SendTimeoutMs };
		c.LingerState = new LingerOption(true, 0);
		await c.ConnectAsync(Host, Port, cancellationToken).ConfigureAwait(false);
		_client = c;
	}

	public bool sendPacket(SdcpMessageBuffer packet)
	{
		try
		{
			NetworkStream stream = GetStream();
			byte[] wire = packet.packet;
			stream.Write(wire, 0, packet.length);
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
			NetworkStream stream = GetStream();
			byte[] wire = packet.packetV4;
			stream.Write(wire, 0, packet.lengthV4);
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
			NetworkStream stream = GetStream();
			var array = new byte[packet.maxSize];
			_ = stream.Read(array, 0, packet.maxSize);
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
			NetworkStream stream = GetStream();
			var array = new byte[packet.maxSize];
			_ = stream.Read(array, 0, packet.maxSize);
			packet.packetV4 = array;
		}
		catch
		{
			return false;
		}

		return true;
	}

	public async Task<bool> ReceivePacketV4Async(SdcpMessageBuffer packet, CancellationToken cancellationToken = default)
	{
		try
		{
			NetworkStream stream = GetStream();
			var array = new byte[packet.maxSize];
			int read = await stream.ReadAsync(array.AsMemory(0, packet.maxSize), cancellationToken).ConfigureAwait(false);
			if (read <= 0)
			{
				return false;
			}

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
		try
		{
			_client?.Close();
		}
		catch
		{
			// ignored
		}

		_client = null;
	}

	public void Dispose() => closeTarget();

	public ValueTask DisposeAsync()
	{
		closeTarget();
		return ValueTask.CompletedTask;
	}
}
