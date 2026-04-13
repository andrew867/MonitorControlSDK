using Sony.MonitorControl.Protocol;
using Xunit;

namespace Sony.MonitorControl.Tests;

public sealed class VmsFloatCodecTests
{
	[Fact]
	public void VmsFloat_RoundTrip_MatchesLegacyEngine()
	{
		var engine = new VmsCommandEngine();
		byte[] enc = engine.convVmsFloatValue(1.234f, 3);
		float dec = engine.reconvVmsFloatValue(enc);
		Assert.InRange(dec, 1.233f, 1.235f);
	}
}
