using System.Net;
using System.Net.Sockets;
using Sony.MonitorControl.Protocol;

namespace Sony.MonitorControl.Transport;

/// <summary>Listens for SDAP monitor advertisements on UDP port 53862 (legacy <c>SdapUdp</c>).</summary>
public sealed class SdapDiscovery : IDisposable
{
	public const int DefaultPort = 53862;

	private Socket? _socket;

	/// <summary>Binds to <see cref="IPAddress.Any"/> on the SDAP port.</summary>
	public void StartListen()
	{
		_socket?.Close();
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		_socket.Bind(new IPEndPoint(IPAddress.Any, DefaultPort));
	}

	/// <summary>Binds to a specific local adapter address.</summary>
	public void StartListen(IPAddress localAddress)
	{
		_socket?.Close();
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		_socket.Bind(new IPEndPoint(localAddress, DefaultPort));
	}

	public void Dispose()
	{
		try
		{
			_socket?.Shutdown(SocketShutdown.Both);
		}
		catch
		{
			// ignored
		}

		_socket?.Close();
		_socket = null;
	}

	/// <summary>Non-blocking read: returns false if no datagram is available.</summary>
	public bool TryRead(IReadOnlyList<string>? productNameFilter, out SdapAdvertisementPacket packet, out string? matchedFilter)
	{
		packet = new SdapAdvertisementPacket();
		matchedFilter = null;
		if (_socket == null || _socket.Available == 0)
		{
			return false;
		}

		EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
		int len = _socket.ReceiveFrom(packet.Raw, ref remote);
		if (remote is IPEndPoint ip)
		{
			packet.SourceIp = ip.Address;
		}

		_ = len;
		if (!packet.IsHeaderOk() || !packet.IsCommunityOk())
		{
			return false;
		}

		if (productNameFilter == null)
		{
			return true;
		}

		foreach (string item in productNameFilter)
		{
			int length = item.Length;
			if (packet.ProductName.Length > 0 && length <= packet.ProductName.Length)
			{
				string prefix = packet.ProductName[..length];
				if (item == prefix)
				{
					matchedFilter = item;
					return true;
				}
			}
		}

		return false;
	}

	public async Task<(SdapAdvertisementPacket packet, string? matched)?> ReadAsync(
		IReadOnlyList<string>? productNameFilter,
		CancellationToken cancellationToken = default)
	{
		if (_socket == null)
		{
			throw new InvalidOperationException("Call StartListen first.");
		}

		var buffer = new byte[SdapAdvertisementPacket.MaxPacketSize];
		SocketReceiveFromResult result = await _socket
			.ReceiveFromAsync(buffer.AsMemory(), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0), cancellationToken)
			.ConfigureAwait(false);
		var packet = SdapAdvertisementPacket.FromBuffer(buffer, result.ReceivedBytes);
		if (result.RemoteEndPoint is IPEndPoint ip)
		{
			packet.SourceIp = ip.Address;
		}

		if (!packet.IsHeaderOk() || !packet.IsCommunityOk())
		{
			return null;
		}

		if (productNameFilter == null)
		{
			return (packet, null);
		}

		foreach (string item in productNameFilter)
		{
			int length = item.Length;
			if (packet.ProductName.Length > 0 && length <= packet.ProductName.Length)
			{
				string prefix = packet.ProductName[..length];
				if (item == prefix)
				{
					return (packet, item);
				}
			}
		}

		return null;
	}
}
