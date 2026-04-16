using System.IO;

namespace MonitorControl.Transport;

/// <summary>Reads SDCP v3/v4 frames from a stream using length-prefixed headers (matches legacy wire layout in <see cref="Protocol.SdcpMessageBuffer"/>).</summary>
/// <remarks>
/// A single <see cref="Stream.Read(byte[], int, int)"/> is not guaranteed to return a full SDCP frame; partial reads desynchronize the byte stream and cause cascading parse failures until reconnect.
/// </remarks>
internal static class SdcpFrameReader
{
	public const int V3HeaderLength = 13;

	public const int V3MaxPayload = 960;

	public const int V4HeaderLength = 37;

	public static int V4MaxPayloadIn973Buffer => 973 - V4HeaderLength;

	private static bool TryReadAll(Stream stream, Span<byte> buffer)
	{
		try
		{
			int offset = 0;
			while (offset < buffer.Length)
			{
				int n = stream.Read(buffer.Slice(offset));
				if (n <= 0)
				{
					return false;
				}

				offset += n;
			}
		}
		catch (IOException)
		{
			return false;
		}

		return true;
	}

	public static bool TryReadV3(Stream stream, Span<byte> buffer, int bufferCapacity)
	{
		if (bufferCapacity < V3HeaderLength)
		{
			return false;
		}

		if (!TryReadAll(stream, buffer[..V3HeaderLength]))
		{
			return false;
		}

		int dataLen = (buffer[11] << 8) | buffer[12];
		if (dataLen < 0 || dataLen > V3MaxPayload)
		{
			return false;
		}

		int total = V3HeaderLength + dataLen;
		if (total > bufferCapacity)
		{
			return false;
		}

		if (dataLen > 0 && !TryReadAll(stream, buffer.Slice(V3HeaderLength, dataLen)))
		{
			return false;
		}

		return true;
	}

	public static bool TryReadV4(Stream stream, Span<byte> buffer, int bufferCapacity)
	{
		if (bufferCapacity < V4HeaderLength)
		{
			return false;
		}

		if (!TryReadAll(stream, buffer[..V4HeaderLength]))
		{
			return false;
		}

		int dataLen = (buffer[35] << 8) | buffer[36];
		if (dataLen < 0 || dataLen > V4MaxPayloadIn973Buffer)
		{
			return false;
		}

		int total = V4HeaderLength + dataLen;
		if (total > bufferCapacity)
		{
			return false;
		}

		if (dataLen > 0 && !TryReadAll(stream, buffer.Slice(V4HeaderLength, dataLen)))
		{
			return false;
		}

		return true;
	}

	public static async Task<bool> TryReadAllAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		try
		{
			int offset = 0;
			while (offset < buffer.Length)
			{
				int n = await stream
					.ReadAsync(buffer.Slice(offset), cancellationToken)
					.ConfigureAwait(false);
				if (n <= 0)
				{
					return false;
				}

				offset += n;
			}
		}
		catch (IOException)
		{
			return false;
		}

		return true;
	}
}
