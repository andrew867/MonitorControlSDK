using MonitorControl.Internal;
using Xunit;

namespace MonitorControl.Tests;

public sealed class LegacyVmcContainerTests
{
	[Fact]
	public void Parse_uses_payload_length_not_entire_buffer()
	{
		var buf = new byte[960];
		for (int i = 0; i < buf.Length; i++)
		{
			buf[i] = (byte)'X';
		}

		var c = new LegacyVmcContainer(ref buf, 0);
		c.setCommand("STATret", "OK");
		int n = c.parse(out string[]? args);
		Assert.True(n > 0);
		Assert.NotNull(args);
		Assert.Equal(2, args!.Length);
		Assert.Equal("STATret", args[0]);
		Assert.Equal("OK", args[1]);
	}
}
