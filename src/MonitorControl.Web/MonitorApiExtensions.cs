using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using MonitorControl.Clients;
using MonitorControl.Internal;
using MonitorControl.Protocol;
using MonitorControl.Transport;

namespace MonitorControl.Web;

internal static class MonitorApiExtensions
{
	internal static void MapMonitorControlApi(this WebApplication app)
	{
		var api = app.MapGroup("/api").WithTags("monitor");

		api.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MonitorControl.Web" }))
			.WithName("Health");

		api.MapGet("/sdap/discover", async (
			IConfiguration config,
			int? durationMs,
			string? bind,
			CancellationToken cancellationToken) =>
		{
			int ms = Math.Clamp(durationMs ?? 3000, 200, 60_000);
			using var discovery = new SdapDiscovery();
			try
			{
				if (!string.IsNullOrWhiteSpace(bind) && IPAddress.TryParse(bind, out var bindIp))
				{
					discovery.StartListen(bindIp);
				}
				else
				{
					discovery.StartListen();
				}
			}
			catch (SocketException ex)
			{
				return Results.Problem(
					title: "SDAP bind failed",
					detail: ex.Message,
					statusCode: StatusCodes.Status409Conflict);
			}

			var deadline = DateTime.UtcNow.AddMilliseconds(ms);
			var items = new List<DiscoverItem>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
			{
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero)
				{
					break;
				}

				using var packetCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				packetCts.CancelAfter(remaining);
				try
				{
					(SdapAdvertisementPacket packet, string? matched)? read =
						await discovery.ReadAsync(null, packetCts.Token).ConfigureAwait(false);
					if (read is null)
					{
						continue;
					}

					SdapAdvertisementPacket p = read.Value.packet;
					string key = $"{p.ConnectionIp}|{p.SerialNumber}";
					if (seen.Add(key))
					{
						items.Add(new DiscoverItem(
							p.SourceIp?.ToString(),
							p.ProductName,
							p.SerialNumber,
							p.ConnectionIp,
							p.GroupId,
							p.UnitId,
							p.Version,
							p.Category));
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}

			return Results.Ok(new DiscoverResponse(ms, items));
		})
			.WithName("SdapDiscover");

		api.MapPost("/vmc/get", (IConfiguration config, [FromBody] VmcGetBody body) =>
		{
			if (string.IsNullOrWhiteSpace(body.Host) || string.IsNullOrWhiteSpace(body.Field))
			{
				return Results.BadRequest(new { error = "host and field are required." });
			}

			int timeout = body.TimeoutMs ?? config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
			try
			{
				using var tcp = new SdcpConnection(body.Host.Trim())
				{
					ReceiveTimeoutMs = timeout,
					SendTimeoutMs = timeout,
				};
				tcp.Open();
				var vmc = new VmcClient(tcp);
				string? value = vmc.GetStatString(body.Field.Trim());
				return Results.Ok(new VmcGetResponse(value));
			}
			catch (Exception ex)
			{
				return Results.Problem(title: "SDCP failed", detail: ex.Message, statusCode: 502);
			}
		})
			.WithName("VmcGet");

		api.MapPost("/vmc/set", (IConfiguration config, [FromBody] VmcSetBody body) =>
		{
			if (string.IsNullOrWhiteSpace(body.Host) || body.Args is null || body.Args.Count == 0)
			{
				return Results.BadRequest(new { error = "host and args (STATset tail) are required." });
			}

			int timeout = body.TimeoutMs ?? config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
			try
			{
				using var tcp = new SdcpConnection(body.Host.Trim())
				{
					ReceiveTimeoutMs = timeout,
					SendTimeoutMs = timeout,
				};
				tcp.Open();
				var vmc = new VmcClient(tcp);
				LegacyVmcContainer? r = vmc.Send("STATset", body.Args.ToArray());
				if (r is null)
				{
					return Results.Ok(new VmcSetResponse(null, "no response container"));
				}

				_ = r.parse(out string[]? tokens);
				return Results.Ok(new VmcSetResponse(tokens, null));
			}
			catch (Exception ex)
			{
				return Results.Problem(title: "SDCP failed", detail: ex.Message, statusCode: 502);
			}
		})
			.WithName("VmcSet");

		api.MapPost("/vmc/broadcast", ([FromBody] VmcBroadcastBody body) =>
		{
			if (body.Tokens is null || body.Tokens.Count < 1)
			{
				return Results.BadRequest(new { error = "tokens must include at least the VMC category (e.g. STATset)." });
			}

			IPAddress destIp = IPAddress.Broadcast;
			if (!string.IsNullOrWhiteSpace(body.BroadcastAddress))
			{
				if (!IPAddress.TryParse(body.BroadcastAddress.Trim(), out IPAddress? parsed))
				{
					return Results.BadRequest(new { error = "invalid broadcastAddress." });
				}

				destIp = parsed;
			}

			int port = body.Port is > 0 and < 65536 ? body.Port.Value : SdcpConnection.DefaultPort;
			IPEndPoint? localEp = null;
			if (!string.IsNullOrWhiteSpace(body.LocalBind) && IPAddress.TryParse(body.LocalBind.Trim(), out IPAddress? lb))
			{
				localEp = new IPEndPoint(lb, 0);
			}

			var dest = new IPEndPoint(destIp, port);
			var scope = string.Equals(body.Scope, "group", StringComparison.OrdinalIgnoreCase)
				? VmcUdpBroadcastScope.Group
				: VmcUdpBroadcastScope.AllMonitors;
			int gid = Math.Clamp(body.GroupId ?? 1, 1, 99);
			try
			{
				using var client = new VmcUdpBroadcastClient(dest, localEp);
				string category = body.Tokens[0];
				string[] tail = body.Tokens.Count > 1
					? body.Tokens.GetRange(1, body.Tokens.Count - 1).ToArray()
					: Array.Empty<string>();
				bool ok = client.TrySend(scope, (byte)gid, category, tail);
				return Results.Ok(new VmcBroadcastResponse(ok, destIp.ToString(), port, body.Scope));
			}
			catch (Exception ex)
			{
				return Results.Problem(title: "UDP VMC broadcast failed", detail: ex.Message, statusCode: 502);
			}
		})
			.WithName("VmcBroadcast");

		api.MapPost("/vms/product-info", (IConfiguration config, [FromBody] HostBody body) =>
		{
			if (string.IsNullOrWhiteSpace(body.Host))
			{
				return Results.BadRequest(new { error = "host is required." });
			}

			int timeout = body.TimeoutMs ?? config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
			try
			{
				using var tcp = new SdcpConnection(body.Host.Trim())
				{
					ReceiveTimeoutMs = timeout,
					SendTimeoutMs = timeout,
				};
				tcp.Open();
				var vms = new VmsClient(tcp);
				var buf = new SdcpMessageBuffer();
				int send = vms.SendGetProductInformation(buf);
				if (send != MonitorProtocolCodes.Ok)
				{
					return Results.Ok(new VmsProductInfoResponse(false, send, 0, null, "send failed"));
				}

				int recv = vms.ReceiveVmsPacket(buf);
				if (recv != MonitorProtocolCodes.Ok)
				{
					return Results.Ok(new VmsProductInfoResponse(false, recv, 0, null, "receive failed"));
				}

				if (!vms.CheckVmsRecvOk(buf))
				{
					return Results.Ok(new VmsProductInfoResponse(false, recv, buf.dataLengthV4, null, "device NAK in payload"));
				}

				int len = Math.Min(buf.dataLengthV4, buf.data.Length);
				string hex = WireFormat.ToHex(buf.data.AsSpan(0, len));
				return Results.Ok(new VmsProductInfoResponse(true, MonitorProtocolCodes.Ok, buf.dataLengthV4, hex, null));
			}
			catch (Exception ex)
			{
				return Results.Problem(title: "VMS failed", detail: ex.Message, statusCode: 502);
			}
		})
			.WithName("VmsProductInfo");

		api.MapPost("/vma/control-software-version", (IConfiguration config, [FromBody] HostBody body) =>
			VmaRead(config, body, static (v, p) => v.SendGetControlSoftwareVersion(p)));

		api.MapPost("/vma/kernel-version", (IConfiguration config, [FromBody] HostBody body) =>
			VmaRead(config, body, static (v, p) => v.SendGetKernelVersion(p)));

		api.MapPost("/vma/rtc", (IConfiguration config, [FromBody] HostBody body) =>
			VmaRead(config, body, static (v, p) => v.SendGetRtc(p)));

		api.MapPost("/vma/fpga1-version", (IConfiguration config, [FromBody] HostBody body) =>
			VmaRead(config, body, static (v, p) => v.SendGetFpga1Version(p)));

		api.MapPost("/vma/fpga2-version", (IConfiguration config, [FromBody] HostBody body) =>
			VmaRead(config, body, static (v, p) => v.SendGetFpga2Version(p)));

		api.MapPost("/vma/fpga-core-version", (IConfiguration config, [FromBody] HostBody body) =>
			VmaRead(config, body, static (v, p) => v.SendGetFpgaCoreVersion(p)));

		api.MapPost("/vma/firmware/upgrade-kernel-size", (IConfiguration config, HttpContext http, [FromBody] FirmwareSizeBody body) =>
			FirmwareAct(config, http, body, static (v, p, b) => v.SendFirmwareUpgradeKernel(b.SizeBytes, p)));

		api.MapPost("/vma/firmware/upgrade-fpga-size", (IConfiguration config, HttpContext http, [FromBody] FirmwareSizeBody body) =>
			FirmwareAct(config, http, body, static (v, p, b) => v.SendFirmwareUpgradeFpga(b.SizeBytes, p)));

		api.MapPost("/vma/firmware/upgrade-chunk", (IConfiguration config, HttpContext http, [FromBody] FirmwareChunkBody body) =>
			FirmwareAct(config, http, body, static (v, p, b) => v.SendFirmwareUpgradeChunk(b.ChunkIndex, p)));

		api.MapPost("/vma/firmware/upgrade-restart", (IConfiguration config, HttpContext http, [FromBody] HostBody body) =>
			FirmwareAct(config, http, body, static (v, p, _) => v.SendFirmwareUpgradeRestart(p)));
	}

	private static IResult VmaRead(
		IConfiguration config,
		HostBody body,
		Func<VmaClient, SdcpMessageBuffer, int> act)
	{
		if (string.IsNullOrWhiteSpace(body.Host))
		{
			return Results.BadRequest(new { error = "host is required." });
		}

		int timeout = body.TimeoutMs ?? config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
		try
		{
			using var tcp = new SdcpConnection(body.Host.Trim())
			{
				ReceiveTimeoutMs = timeout,
				SendTimeoutMs = timeout,
			};
			tcp.Open();
			var vma = new VmaClient(tcp);
			var packet = new SdcpMessageBuffer();
			int code = act(vma, packet);
			_ = packet.packet;
			int len = Math.Min(packet.dataLength, packet.data.Length);
			string hex = WireFormat.ToHex(packet.data.AsSpan(0, len), maxBytes: 512);
			return Results.Ok(new VmaReadResponse(code == MonitorProtocolCodes.Ok, code, packet.dataLength, hex));
		}
		catch (Exception ex)
		{
			return Results.Problem(title: "VMA failed", detail: ex.Message, statusCode: 502);
		}
	}

	private static IResult FirmwareAct<TBody>(
		IConfiguration config,
		HttpContext http,
		TBody body,
		Func<VmaClient, SdcpMessageBuffer, TBody, int> act)
		where TBody : HostBody
	{
		if (!WireFormat.FirmwareGate(config, http.Request.Headers))
		{
			return Results.Problem(
				title: "Firmware actions disabled",
				detail: "Set MonitorControl:AllowDangerousFirmware=true or MONITOR_CONTROL_ALLOW_DANGEROUS_FIRMWARE=1 and send header X-Firmware-Ack: CONFIRM",
				statusCode: StatusCodes.Status403Forbidden);
		}

		if (string.IsNullOrWhiteSpace(body.Host))
		{
			return Results.BadRequest(new { error = "host is required." });
		}

		int timeout = body.TimeoutMs ?? config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
		try
		{
			using var tcp = new SdcpConnection(body.Host.Trim())
			{
				ReceiveTimeoutMs = timeout,
				SendTimeoutMs = timeout,
			};
			tcp.Open();
			var vma = new VmaClient(tcp);
			var packet = new SdcpMessageBuffer();
			int code = act(vma, packet, body);
			_ = packet.packet;
			int len = Math.Min(packet.dataLength, packet.data.Length);
			string hex = WireFormat.ToHex(packet.data.AsSpan(0, len), maxBytes: 512);
			return Results.Ok(new VmaReadResponse(code == MonitorProtocolCodes.Ok, code, packet.dataLength, hex));
		}
		catch (Exception ex)
		{
			return Results.Problem(title: "VMA firmware failed", detail: ex.Message, statusCode: 502);
		}
	}
}

internal sealed record DiscoverItem(
	string? SourceIp,
	string ProductName,
	string SerialNumber,
	string ConnectionIp,
	byte GroupId,
	byte UnitId,
	byte Version,
	byte Category);

internal sealed record DiscoverResponse(int ListenDurationMs, IReadOnlyList<DiscoverItem> Items);

internal record HostBody(string Host, int? TimeoutMs);

internal sealed record VmcGetBody(string Host, string Field, int? TimeoutMs);

internal sealed record VmcGetResponse(string? Value);

internal sealed record VmcSetBody(string Host, List<string> Args, int? TimeoutMs);

internal sealed record VmcSetResponse(string[]? ResponseTokens, string? Error);

internal sealed record VmcBroadcastBody(string? Scope, int? GroupId, string? BroadcastAddress, int? Port, string? LocalBind, List<string>? Tokens);

internal sealed record VmcBroadcastResponse(bool Ok, string DestinationIp, int Port, string? Scope);

internal sealed record VmsProductInfoResponse(bool Ok, int ProtocolCode, int DataLengthV4, string? PayloadHex, string? Message);

internal sealed record VmaReadResponse(bool Ok, int ProtocolCode, int DataLength, string PayloadHex);

internal sealed record FirmwareSizeBody(string Host, int SizeBytes, int? TimeoutMs) : HostBody(Host, TimeoutMs);

internal sealed record FirmwareChunkBody(string Host, int ChunkIndex, int? TimeoutMs) : HostBody(Host, TimeoutMs);
