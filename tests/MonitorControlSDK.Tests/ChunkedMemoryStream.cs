namespace MonitorControl.Tests;

/// <summary>Exposes an in-memory byte sequence with reads capped to a small count to simulate TCP partial reads (array and span paths).</summary>
internal sealed class ChunkedMemoryStream : MemoryStream
{
	private readonly int _readChunk;

	public ChunkedMemoryStream(byte[] buffer, int readChunk)
		: base(buffer, writable: false)
	{
		_readChunk = readChunk <= 0 ? 1 : readChunk;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int n = Math.Min(count, _readChunk);
		return base.Read(buffer, offset, n);
	}

	public override int Read(Span<byte> buffer)
	{
		if (buffer.IsEmpty)
		{
			return 0;
		}

		int n = Math.Min(buffer.Length, _readChunk);
		return base.Read(buffer[..n]);
	}
}
