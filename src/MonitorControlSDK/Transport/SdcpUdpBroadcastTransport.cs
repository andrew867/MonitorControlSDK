using System.Net;
using System.Net.Sockets;
using Sony.MonitorControl.Protocol;

namespace Sony.MonitorControl.Transport;

/// <summary>
/// Sends SDCP v3 frames over UDP to a broadcast or directed datagram endpoint (default monitor SDCP port <see cref="SdcpConnection.DefaultPort"/>).
/// Intended for Sony manual <strong>Group</strong> / <strong>All</strong> modes where VMC is issued via UDP broadcast; monitors do not return a datagram response in those modes.
/// </summary>
public sealed class SdcpUdpBroadcastTransport : ISdcpTransport, IDisposable
{
	private readonly Socket _socket;

	private readonly EndPoint _remote;

	private bool _disposed;

	/// <param name="remoteEndPoint">Typically <c>new IPEndPoint(IPAddress.Broadcast, 53484)</c> or your subnet’s directed broadcast.</param>
	/// <param name="localBind">Optional local UDP bind (e.g. pick an interface); default is ephemeral port on any address.</param>
	public SdcpUdpBroadcastTransport(EndPoint remoteEndPoint, IPEndPoint? localBind = null)
	{
		ArgumentNullException.ThrowIfNull(remoteEndPoint);
		_remote = remoteEndPoint;
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
		if (localBind != null)
		{
			_socket.Bind(localBind);
		}
	}

	/// <inheritdoc />
	public bool sendPacket(SdcpMessageBuffer packet)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		byte[] wire = packet.packet;
		int len = packet.length;
		if (len <= 0 || len > packet.maxSize)
		{
			return false;
		}

		try
		{
			return _socket.SendTo(wire.AsSpan(0, len), SocketFlags.None, _remote) == len;
		}
		catch (SocketException)
		{
			return false;
		}
	}

	/// <inheritdoc />
	public bool sendPacketV4(SdcpMessageBuffer packet) => false;

	/// <inheritdoc />
	public bool receivePacket(SdcpMessageBuffer packet) => false;

	/// <inheritdoc />
	public bool receivePacketV4(SdcpMessageBuffer packet) => false;

	/// <inheritdoc />
	public void closeTarget() => Dispose();

	/// <inheritdoc />
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		try
		{
			_socket.Shutdown(SocketShutdown.Both);
		}
		catch
		{
			// ignored
		}

		try
		{
			_socket.Dispose();
		}
		catch
		{
			// ignored
		}
	}
}
