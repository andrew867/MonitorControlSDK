namespace MonitorControl.Internal;

/// <summary>VMC/VMS/VMA payload views share this length contract with the SDCP message buffer.</summary>
public interface ILegacySdcpContainer
{
	ushort dataLength { get; set; }
}
