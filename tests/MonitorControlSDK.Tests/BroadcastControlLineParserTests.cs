using Sony.MonitorControl.Repl;
using Xunit;

namespace Sony.MonitorControl.Tests;

public sealed class BroadcastControlLineParserTests
{
	[Fact]
	public void Get_parses_single_field()
	{
		Assert.True(BroadcastControlLineParser.TryParse("  get MODEL ", out var cmd, out var err));
		Assert.Null(err);
		Assert.Equal(BroadcastReplKind.Get, cmd.Kind);
		Assert.Equal("MODEL", cmd.GetField);
	}

	[Fact]
	public void Set_parses_multiple_segments()
	{
		Assert.True(BroadcastControlLineParser.TryParse("set BRIGHTNESS 512", out var cmd, out var err));
		Assert.Null(err);
		Assert.Equal(BroadcastReplKind.Set, cmd.Kind);
		Assert.NotNull(cmd.SetSegments);
		Assert.Equal(new[] { "BRIGHTNESS", "512" }, cmd.SetSegments);
	}

	[Fact]
	public void Get_wrong_arity_fails()
	{
		Assert.False(BroadcastControlLineParser.TryParse("get", out _, out var err));
		Assert.NotNull(err);
	}

	[Fact]
	public void Help_and_quit()
	{
		Assert.True(BroadcastControlLineParser.TryParse("HELP", out var h, out _));
		Assert.Equal(BroadcastReplKind.Help, h.Kind);
		Assert.True(BroadcastControlLineParser.TryParse("exit", out var q, out _));
		Assert.Equal(BroadcastReplKind.Quit, q.Kind);
	}
}
