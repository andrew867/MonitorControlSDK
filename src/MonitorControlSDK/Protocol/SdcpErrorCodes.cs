namespace Sony.MonitorControl.Protocol;

/// <summary>SDCP error codes from legacy <c>SdcpPacket</c> (subset; see full list in source).</summary>
public static class SdcpErrorCodes
{
	public const ushort InvalidItem = 257;
	public const ushort InvalidItemRequest = 258;
	public const ushort InvalidLength = 259;
	public const ushort InvalidData = 260;
	public const ushort ShortData = 273;
	public const ushort InvalidSubCommand = 288;
	public const ushort InvalidSubCommandData = 289;
	public const ushort PasswordLocked = 290;
	public const ushort PasswordAuthenticationError = 291;
	public const ushort OperateConditionError = 292;
	public const ushort CannotControl = 295;
	public const ushort InvalidVersion = 4097;
	public const ushort InvalidCategory = 4098;
	public const ushort InvalidRequest = 4099;
	public const ushort NetworkTimeout = 8193;
	public const ushort InvalidPacket = ushort.MaxValue;
}
