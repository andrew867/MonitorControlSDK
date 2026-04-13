using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Protocol;
using Xunit;

namespace Sony.MonitorControl.Tests;

public sealed class FirmwareWireEncodingTests
{
	[Fact]
	public void Vma_service_upgrade_restart_payload_bytes()
	{
		var p = new SdcpMessageBuffer();
		p.setupVma();
		p.clearContainer();
		LegacyVmaContainer vma = p.createVmaContainer();
		vma.serviceUpgradeRestart();
		_ = p.packet; // sync header dataLength from container
		Assert.Equal(1, p.data[0]);
		Assert.Equal(11, p.data[1]);
		Assert.Equal(2, p.dataLength);
	}

	[Fact]
	public void Vma_service_upgrade_kernel_payload_includes_be_size()
	{
		var p = new SdcpMessageBuffer();
		p.setupVma();
		p.clearContainer();
		LegacyVmaContainer vma = p.createVmaContainer();
		vma.serviceUpgradeKernel(0x01020304);
		_ = p.packet;
		Assert.Equal(1, p.data[0]);
		Assert.Equal(9, p.data[1]);
		Assert.Equal(1, p.data[2]);
		Assert.Equal(2, p.data[3]);
		Assert.Equal(3, p.data[4]);
		Assert.Equal(4, p.data[5]);
		Assert.Equal(6, p.dataLength);
	}
}
