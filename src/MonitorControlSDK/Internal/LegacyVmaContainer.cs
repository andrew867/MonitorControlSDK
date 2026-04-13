namespace Sony.MonitorControl.Internal;

public sealed class LegacyVmaContainer : ILegacySdcpContainer
{
	public class ParamAdjMode
	{
		private const byte NORMAL = 0;

		private const byte ADJUST_GAMMA = 1;

		private const byte ADJUST_BL_COLORTEMP_ROUGH = 2;

		private const byte ADJUST_BL_UNIFORMITY = 3;

		private const byte ADJUST_BL_COLORTEMP = 4;

		private const byte ADJUST_BL_PRIMARYCOLOR = 5;

		private const byte ADJUST_UNIFORMITY = 6;

		private const byte ADJUST_NOUSE = 7;

		private const byte ADJUST_3DLUT = 8;
	}

	public class ParamServiceAdjMode
	{
		public const byte NORMAL = 0;

		public const byte ADJUST_UNIFORMITY = 1;
	}

	public class ParamUfAdjMode
	{
		public const byte COMPLETE = 0;

		public const byte ABORT = 1;

		public const byte MATRIX_METHOD = 2;

		public const byte PWM_METHOD = 3;
	}

	public class ParamUfProbeSense
	{
		public const byte SUPPRESS = 0;

		public const byte SHOW = 1;
	}

	private byte[] data;

	private ushort length;

	private const byte CMD_ADJUSTMENT = 0;

	private const byte CMD_SERVICE = 1;

	private const byte CMD_DIRECTBL = 2;

	private const byte CMD_ADJUSTMENT_ADJ_MODE = 0;

	private const byte CMD_ADJUSTMENT_WINDOW_LEVEL_RED = 1;

	private const byte CMD_ADJUSTMENT_WINDOW_LEVEL_GREEN = 2;

	private const byte CMD_ADJUSTMENT_WINDOW_LEVEL_BLUE = 3;

	private const byte CMD_ADJUSTMENT_ADJ_CHECK_MODE = 4;

	private const byte CMD_ADJUSTMENT_UFCURSOR = 5;

	private const byte CMD_ADJUSTMENT_UFCURSOR_X = 6;

	private const byte CMD_ADJUSTMENT_UFCURSOR_Y = 7;

	private const byte CMD_ADJUSTMENT_UF = 8;

	private const byte CMD_ADJUSTMENT_PANEL_CENTER_CURSOR = 9;

	private const byte CMD_ADJUSTMENT_AGING_MODE = 10;

	private const byte CMD_ADJUSTMENT_WINDOW_LEVEL_WHITE = 14;

	private const byte CMD_ADJUSTMENT_PANEL_DRIVE = 15;

	private const byte CMD_ADJUSTMENT_ADJ_LOAD_ADJDATA = 16;

	private const byte CMD_ADJUSTMENT_EMU = 17;

	private const byte CMD_ADJUSTMENT_COLORTEMP_XYY = 18;

	private const byte CMD_ADJUSTMENT_CHECK_MTX2 = 19;

	private const byte CMD_ADJUSTMENT_SEL_PLANE = 20;

	private const byte CMD_ADJUSTMENT_OPBOARD_WRITE_NVM = 21;

	private const byte CMD_ADJUSTMENT_OPBOARD_SELECT_SDI = 22;

	private const byte CMD_ADJUSTMENT_OPBOARD_SELECT_AUDIO = 23;

	private const byte CMD_ADJUSTMENT_OPBOARD_ADJ_F0 = 24;

	private const byte CMD_ADJUSTMENT_OPBOARD_SELECT_CLOSEDCAPTION = 25;

	private const byte CMD_ADJUSTMENT_SELECT_USER_LUT = 26;

	private const byte CMD_ADJUSTMENT_REGIST_USER_LUT = 27;

	private const byte CMD_ADJUSTMENT_REMOVE_USER_LUT = 28;

	private const byte CMD_ADJUSTMENT_SERVICE_ADJ_MODE = 29;

	private const byte CMD_ADJUSTMENT_UF_ADJ_MODE = 30;

	private const byte CMD_ADJUSTMENT_UF_TARGET = 31;

	private const byte CMD_ADJUSTMENT_UF_MEAS = 32;

	private const byte CMD_ADJUSTMENT_UF_POS = 33;

	private const byte CMD_ADJUSTMENT_UF_PROBESENSE_SIZE = 34;

	private const byte CMD_ADJUSTMENT_UF_PROBESENSE_POS = 35;

	private const byte CMD_ADJUSTMENT_UF_PROBESENSE = 36;

	private const byte CMD_ADJUSTMENT_UF_CALC_MATRIX = 37;

	private const byte CMD_ADJUSTMENT_GET_PROBE_OFFSET = 38;

	private const byte CMD_SERVICE_SET_OPERATION_TIME = 1;

	private const byte CMD_SERVICE_GET_OPERATION_TIME = 2;

	private const byte CMD_SERVICE_BACKLIGHT_RESET = 3;

	private const byte CMD_SERVICE_RESTORE_FACTORY = 4;

	private const byte CMD_SERVICE_EDID_WP = 5;

	private const byte CMD_SERVICE_SET_RTC = 6;

	private const byte CMD_SERVICE_GET_RTC = 7;

	private const byte CMD_SERVICE_UPGRADE_CHUNK = 8;

	private const byte CMD_SERVICE_UPGRADE_KERNEL = 9;

	private const byte CMD_SERVICE_UPGRADE_FPGA = 10;

	private const byte CMD_SERVICE_UPGRADE_RESTART = 11;

	private const byte CMD_SERVICE_GET_SOFTWARE_VERSION = 12;

	private const byte CMD_SERVICE_GET_KERNEL_VERSION = 13;

	private const byte CMD_SERVICE_GET_FPGA_VERSION = 14;

	private const byte CMD_DIRECTBL_GET_BLM_SERIAL = 0;

	private const byte CMD_DIRECTBL_GET_BLM_VERSION = 1;

	private const byte CMD_DIRECTBL_GET_NVM_VERSION = 2;

	private const byte CMD_DIRECTBL_GET_PWM = 3;

	private const byte CMD_DIRECTBL_GET_COLOR_SENSOR = 4;

	private const byte CMD_DIRECTBL_GET_BL_POW = 5;

	private const byte CMD_DIRECTBL_GET_ROOM_TEMP = 6;

	private const byte CMD_DIRECTBL_GET_PANEL_TEMP = 7;

	private const byte CMD_DIRECTBL_GET_BLM_TEMP = 8;

	private const byte CMD_DIRECTBL_GET_FAN_STATUS = 9;

	private const byte CMD_DIRECTBL_READ_ECS = 10;

	private const byte CMD_DIRECTBL_READ_REG = 11;

	private const byte CMD_DIRECTBL_LED_OPEN_CHECK = 12;

	public ushort dataLength
	{
		get
		{
			return length;
		}
		set
		{
		}
	}

	public LegacyVmaContainer(ref byte[] dt, ushort len)
	{
		data = dt;
		length = len;
	}

	public bool isAckOk()
	{
		if (data[2] == 0)
		{
			return true;
		}
		return false;
	}

	public void jigAdjMode(byte md)
	{
		data[0] = 0;
		data[1] = 0;
		data[2] = md;
		length = 3;
	}

	public void jigColorTemp_xyY(int x, int y, int Y)
	{
		data[0] = 0;
		data[1] = 18;
		data[2] = (byte)(x / 256);
		data[3] = (byte)(x % 256);
		data[4] = (byte)(y / 256);
		data[5] = (byte)(y % 256);
		data[6] = (byte)(Y / 256);
		data[7] = (byte)(Y % 256);
		length = 8;
	}

	public void jigWhiteLevel(int level)
	{
		data[0] = 0;
		data[1] = 14;
		data[2] = (byte)(level / 256);
		data[3] = (byte)(level % 256);
		length = 4;
	}

	public void jigServiceAdjMode(byte md)
	{
		data[0] = 0;
		data[1] = 29;
		data[2] = md;
		length = 3;
	}

	public void jigUfAdjMode(byte md)
	{
		data[0] = 0;
		data[1] = 30;
		data[2] = md;
		length = 3;
	}

	public void jigUfTarget(int x, int y, int Y)
	{
		data[0] = 0;
		data[1] = 31;
		data[2] = (byte)(x / 256);
		data[3] = (byte)(x % 256);
		data[4] = (byte)(y / 256);
		data[5] = (byte)(y % 256);
		data[6] = (byte)(Y / 256);
		data[7] = (byte)(Y % 256);
		length = 8;
	}

	public void jigUfMeas(int x, int y, int Y)
	{
		data[0] = 0;
		data[1] = 32;
		data[2] = (byte)(x / 256);
		data[3] = (byte)(x % 256);
		data[4] = (byte)(y / 256);
		data[5] = (byte)(y % 256);
		data[6] = (byte)(Y / 256);
		data[7] = (byte)(Y % 256);
		length = 8;
	}

	public void jigUfPos(int h, int v)
	{
		data[0] = 0;
		data[1] = 33;
		data[2] = (byte)h;
		data[3] = (byte)v;
		length = 4;
	}

	public void jigUfProbeSenseSize(int w, int h)
	{
		data[0] = 0;
		data[1] = 34;
		data[2] = (byte)(w / 256);
		data[3] = (byte)(w % 256);
		data[4] = (byte)(h / 256);
		data[5] = (byte)(h % 256);
		length = 6;
	}

	public void jigUfProbeSensePos(int x, int y)
	{
		data[0] = 0;
		data[1] = 35;
		data[2] = (byte)(x / 256);
		data[3] = (byte)(x % 256);
		data[4] = (byte)(y / 256);
		data[5] = (byte)(y % 256);
		length = 6;
	}

	public void jigUfProbeSense(byte md)
	{
		data[0] = 0;
		data[1] = 36;
		data[2] = md;
		length = 3;
	}

	public void jigUfCalcMatrix()
	{
		data[0] = 0;
		data[1] = 37;
		length = 2;
	}

	public void jigGetProbeOffset()
	{
		data[0] = 0;
		data[1] = 38;
		length = 2;
	}

	public void jigGetProbeOffset(out int x, out int y, out int Y)
	{
		x = (short)((data[2] << 8) | data[3]);
		y = (short)((data[4] << 8) | data[5]);
		Y = (short)((data[6] << 8) | data[7]);
	}

	public void serviceUpgradeChunk(int n)
	{
		data[0] = 1;
		data[1] = 8;
		data[2] = (byte)n;
		length = 3;
	}

	public void serviceUpgradeKernel(int size)
	{
		data[0] = 1;
		data[1] = 9;
		data[2] = (byte)(size >> 24);
		data[3] = (byte)(size >> 16);
		data[4] = (byte)(size >> 8);
		data[5] = (byte)size;
		length = 6;
	}

	public void serviceUpgradeFPGA(int size)
	{
		data[0] = 1;
		data[1] = 10;
		data[2] = (byte)(size >> 24);
		data[3] = (byte)(size >> 16);
		data[4] = (byte)(size >> 8);
		data[5] = (byte)size;
		length = 6;
	}

	public void serviceUpgradeRestart()
	{
		data[0] = 1;
		data[1] = 11;
		length = 2;
	}

	public void serviceGetRTC()
	{
		data[0] = 1;
		data[1] = 7;
		length = 2;
	}

	public void GetControlSoftwareVersion()
	{
		data[0] = 1;
		data[1] = 12;
		length = 2;
	}

	public void GetKernelVersion()
	{
		data[0] = 1;
		data[1] = 13;
		length = 2;
	}

	public void GetFPGA1Version()
	{
		data[0] = 1;
		data[1] = 14;
		data[2] = 0;
		length = 3;
	}

	public void GetFPGA2Version()
	{
		data[0] = 1;
		data[1] = 14;
		data[2] = 1;
		length = 3;
	}

	public void GetFPGACoreVersion()
	{
		data[0] = 1;
		data[1] = 14;
		data[2] = 2;
		length = 3;
	}

	public void directBLGetBlmSerial()
	{
		data[0] = 2;
		data[1] = 0;
		length = 2;
	}

	public void directBLGetBlmVersion()
	{
		data[0] = 2;
		data[1] = 1;
		length = 2;
	}

	public void directBLGetNvmVersion()
	{
		data[0] = 2;
		data[1] = 2;
		length = 2;
	}

	public void directBLGetPwm()
	{
		data[0] = 2;
		data[1] = 3;
		length = 2;
	}

	public void directBLGetColorSensor()
	{
		data[0] = 2;
		data[1] = 4;
		length = 2;
	}

	public void directBLGetBlPow()
	{
		data[0] = 2;
		data[1] = 5;
		length = 2;
	}

	public void directBLGetRoomTemp()
	{
		data[0] = 2;
		data[1] = 6;
		length = 2;
	}

	public void directBLGetPanelTemp()
	{
		data[0] = 2;
		data[1] = 7;
		length = 2;
	}

	public void directBLGetBlmTemp()
	{
		data[0] = 2;
		data[1] = 8;
		length = 2;
	}

	public void directBLGetFanStatus()
	{
		data[0] = 2;
		data[1] = 9;
		length = 2;
	}

	public void directBLReadEcs(uint type, uint id, ushort num)
	{
		data[0] = 2;
		data[1] = 10;
		data[2] = (byte)((type >> 24) & 0xFF);
		data[3] = (byte)((type >> 16) & 0xFF);
		data[4] = (byte)((type >> 8) & 0xFF);
		data[5] = (byte)(type & 0xFF);
		data[6] = (byte)((id >> 24) & 0xFF);
		data[7] = (byte)((id >> 16) & 0xFF);
		data[8] = (byte)((id >> 8) & 0xFF);
		data[9] = (byte)(id & 0xFF);
		data[10] = (byte)((num >> 8) & 0xFF);
		data[11] = (byte)(num & 0xFF);
		length = 12;
	}

	public void directBLReadReg(byte suba, byte num)
	{
		data[0] = 2;
		data[1] = 11;
		data[2] = suba;
		data[3] = num;
		length = 4;
	}

	public void directBLLedOpenCheck()
	{
		data[0] = 2;
		data[1] = 12;
		length = 2;
	}
}
