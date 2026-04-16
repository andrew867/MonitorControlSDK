using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MonitorControl.Clients;
using MonitorControl.Protocol;
using MonitorControl.Transport;

namespace MonitorControl.Web;

/// <summary>
/// Server-initiated push built on top of periodic SDCP/VMC polls. The monitor does not open an outbound push channel;
/// these endpoints synthesize “live” updates for browsers and automation.
/// </summary>
internal static class MonitorPushEndpoints
{
	internal static void MapMonitorPushEndpoints(this WebApplication app)
	{
		var api = app.MapGroup("/api").WithTags("monitor");

		api.MapGet("/events/monitor", MonitorSseHandler)
			.WithName("MonitorEventsSse")
			.WithDescription("Server-Sent Events: JSON snapshots of STATget fields (polling SDCP on the server).");

		app.Map("/ws/monitor-watch", MonitorWebSocketHandler)
			.WithName("MonitorWatchWebSocket")
			.WithDescription("WebSocket: UTF-8 JSON snapshots on an interval (same poll model as SSE).");
	}

	private static string[] ParseFields(string? fields) =>
		string.IsNullOrWhiteSpace(fields)
			? new[] { "MODEL", "BRIGHTNESS", "CONTRAST" }
			: fields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static async Task MonitorSseHandler(
		HttpContext http,
		IConfiguration config,
		[FromQuery] string host,
		[FromQuery] string? fields,
		[FromQuery] int? intervalMs,
		[FromQuery] int? sdcpUnitId,
		[FromQuery] string? vmcItem,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			http.Response.StatusCode = StatusCodes.Status400BadRequest;
			await http.Response.WriteAsync("Query parameter 'host' (monitor IP) is required.", cancellationToken).ConfigureAwait(false);
			return;
		}

		int interval = Math.Clamp(intervalMs ?? 2000, 250, 60_000);
		int timeout = config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
		string[] fieldList = ParseFields(fields);

		http.Response.Headers.CacheControl = "no-cache";
		http.Response.Headers.Append("Content-Type", "text/event-stream");
		await http.Response.StartAsync(cancellationToken).ConfigureAwait(false);

		while (!cancellationToken.IsCancellationRequested)
		{
			await WriteSnapshotLineAsync(http.Response, host, timeout, fieldList, sdcpUnitId, vmcItem, cancellationToken)
				.ConfigureAwait(false);
			try
			{
				await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}
	}

	private static async Task MonitorWebSocketHandler(
		HttpContext http,
		IConfiguration config,
		CancellationToken cancellationToken)
	{
		if (!http.WebSockets.IsWebSocketRequest)
		{
			http.Response.StatusCode = StatusCodes.Status400BadRequest;
			await http.Response.WriteAsync("Expected WebSocket upgrade (ws:// or wss://).", cancellationToken).ConfigureAwait(false);
			return;
		}

		string host = http.Request.Query["host"].ToString();
		if (string.IsNullOrWhiteSpace(host))
		{
			http.Response.StatusCode = StatusCodes.Status400BadRequest;
			await http.Response.WriteAsync("Query parameter 'host' is required.", cancellationToken).ConfigureAwait(false);
			return;
		}

		int interval = Math.Clamp(
			int.TryParse(http.Request.Query["intervalMs"], out int im) ? im : 2000,
			250,
			60_000);
		int timeout = config.GetValue("MonitorControl:DefaultSdcpTimeoutMs", 10_000);
		string[] fieldList = ParseFields(http.Request.Query["fields"].ToString());
		int? sdcpUnitId = int.TryParse(http.Request.Query["sdcpUnitId"], out int su) ? su : null;
		string? vmcItem = http.Request.Query["vmcItem"].ToString();
		if (string.IsNullOrWhiteSpace(vmcItem))
		{
			vmcItem = null;
		}

		using WebSocket ws = await http.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);

		while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
		{
			Dictionary<string, string?> dict =
				await PollFieldsAsync(host, timeout, fieldList, sdcpUnitId, vmcItem, cancellationToken).ConfigureAwait(false);
			byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(dict);
			await ws.SendAsync(utf8, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);

			try
			{
				await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
		}

		if (ws.State == WebSocketState.Open)
		{
			await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).ConfigureAwait(false);
		}
	}

	private static async Task WriteSnapshotLineAsync(
		HttpResponse response,
		string host,
		int timeoutMs,
		string[] fields,
		int? sdcpUnitId,
		string? vmcItem,
		CancellationToken cancellationToken)
	{
		Dictionary<string, string?> dict =
			await PollFieldsAsync(host, timeoutMs, fields, sdcpUnitId, vmcItem, cancellationToken).ConfigureAwait(false);
		if (dict.TryGetValue("_error", out string? err) && err is not null)
		{
			string escaped = JsonSerializer.Serialize(err);
			await response.WriteAsync($"event: fault\ndata: {escaped}\n\n", cancellationToken).ConfigureAwait(false);
		}
		else
		{
			string json = JsonSerializer.Serialize(dict);
			await response.WriteAsync($"data: {json}\n\n", cancellationToken).ConfigureAwait(false);
		}

		await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
	}

	private static async Task<Dictionary<string, string?>> PollFieldsAsync(
		string host,
		int timeoutMs,
		string[] fields,
		int? sdcpUnitId,
		string? vmcItem,
		CancellationToken cancellationToken)
	{
		await Task.Yield();
		var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
		try
		{
			using var tcp = new SdcpConnection(host.Trim())
			{
				ReceiveTimeoutMs = timeoutMs,
				SendTimeoutMs = timeoutMs,
			};
			tcp.Open();
			var vmc = new VmcClient(tcp)
			{
				VmcItemNumber = SdcpMessageBuffer.ParseVmcItemSpecifier(vmcItem),
			};
			if (sdcpUnitId is >= 0 and <= 255)
			{
				vmc.TcpSingleUnitId = (byte)sdcpUnitId.Value;
			}

			foreach (string f in fields)
			{
				cancellationToken.ThrowIfCancellationRequested();
				dict[f.Trim()] = vmc.GetStatString(f.Trim());
			}
		}
		catch (Exception ex)
		{
			dict["_error"] = ex.Message;
		}

		return dict;
	}
}
