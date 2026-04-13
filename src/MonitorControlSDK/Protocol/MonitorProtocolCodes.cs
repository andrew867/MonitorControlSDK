namespace Sony.MonitorControl.Protocol;

/// <summary>Return codes used by legacy command helpers (32 = success).</summary>
public static class MonitorProtocolCodes
{
	public const int Ok = 32;

	public const int SendError = 33;

	public const int RecvError = 34;

	public const int ConnectOtherTool = 35;
}
