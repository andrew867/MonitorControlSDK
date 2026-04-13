using System.Net;
using MonitorControl.Internal;
using MonitorControl.Protocol;
using MonitorControl.Transport;

namespace MonitorControl.Clients;

/// <summary>Which monitors receive a UDP SDCP VMC datagram (Group / All broadcast modes per programmer manual).</summary>
public enum VmcUdpBroadcastScope
{
	/// <summary>Group ID and unit ID both <c>0xFF</c> in the SDCP header.</summary>
	AllMonitors,

	/// <summary>Group ID set to <see cref="VmcUdpBroadcastClient.TrySend"/> / <see cref="Send"/> argument (1–99).</summary>
	Group,
}

/// <summary>
/// Fire-and-forget VMC over UDP SDCP (same wire as TCP, different transport). Use <see cref="SdapDiscovery"/> on port 53862 for device advertisement discovery;
/// this class is for <strong>group shading / multi-monitor control</strong> per the PVM programmer manual.
/// </summary>
public sealed class VmcUdpBroadcastClient : IDisposable
{
	private readonly SdcpUdpBroadcastTransport _transport;

	/// <param name="destination">UDP destination; default is IPv4 <see cref="IPAddress.Broadcast"/> on <see cref="SdcpConnection.DefaultPort"/>.</param>
	/// <param name="localBind">Optional local bind when multiple NICs exist.</param>
	public VmcUdpBroadcastClient(IPEndPoint? destination = null, IPEndPoint? localBind = null)
	{
		var dest = destination ?? new IPEndPoint(IPAddress.Broadcast, SdcpConnection.DefaultPort);
		_transport = new SdcpUdpBroadcastTransport(dest, localBind);
	}

	/// <summary>Sends one VMC command with the given scope. Does not wait for a reply (none expected for Group/All UDP).</summary>
	/// <returns><see langword="true"/> if the datagram was accepted by the host UDP stack for transmission.</returns>
	public bool TrySend(VmcUdpBroadcastScope scope, byte groupId1To99, string category, params string[] segments)
	{
		ArgumentException.ThrowIfNullOrEmpty(category);
		if (scope == VmcUdpBroadcastScope.Group && (groupId1To99 < 1 || groupId1To99 > 99))
		{
			throw new ArgumentOutOfRangeException(nameof(groupId1To99), "Group ID must be 1–99 for Group scope.");
		}

		var packet = new SdcpMessageBuffer();
		if (scope == VmcUdpBroadcastScope.AllMonitors)
		{
			packet.setAllConnection();
		}
		else
		{
			packet.setGroupConnection(groupId1To99);
		}

		LegacyVmcContainer vmc = packet.createVmcContainer();
		packet.setupVmcPacketHeader();
		packet.clearContainer();
		vmc.setCommand(category, segments);
		return _transport.sendPacket(packet);
	}

	/// <inheritdoc />
	public void Dispose() => _transport.Dispose();
}
