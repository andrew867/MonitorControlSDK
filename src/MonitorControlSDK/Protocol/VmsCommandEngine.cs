using System;
using System.Linq;
using Sony.MonitorControl.Internal;
using Sony.MonitorControl.Transport;

namespace Sony.MonitorControl.Protocol;

/// <summary>Binary VMS command engine (complete port of legacy <c>VmsCommand</c>).</summary>
public sealed class VmsCommandEngine
{
	public const int MAX_MONITOR_LENGTH = 12;

	public const int INDEX_MONITOR_NAME = 42;

	public const int MAX_SERIAL_LENGTH = 7;

	public const int INDEX_SERIAL_NAME = 54;

	public const int INDEX_LOW_LATENCY_HDMI_MODE = 3;

	public const int NO_ERROR = 32;

	public const int SEND_ERROR = 33;

	public const int RECV_ERROR = 34;

	public const int CONNECT_OTHER_TOOL = 35;

	public const int LOW_LATENCY_ENABLE = 0;

	public const int LOW_LATENCY_DISABLE = 1;

	private const int PC_OPERATECONDITION_AREA_SETTING = 1;

	public byte[] conv_endian_to_big(byte[] data)
	{
		if (BitConverter.IsLittleEndian)
		{
			return data.Reverse().ToArray();
		}
		return data;
	}

	public byte[] conv_endian_to_little(byte[] data)
	{
		if (!BitConverter.IsLittleEndian)
		{
			return data.Reverse().ToArray();
		}
		return data;
	}

	public byte[] conv_endian(byte[] data)
	{
		return data.Reverse().ToArray();
	}

	public byte[] convVmsFloatValue(float data, short power)
	{
		byte[] array = new byte[8];
		float num = data;
		byte b = 0;
		uint num2 = 0u;
		byte b2 = 0;
		ushort num3 = 0;
		if (num < 0f)
		{
			b = 1;
			num *= -1f;
		}
		if (power > 0)
		{
			b2 = 1;
			num3 = (ushort)power;
			num2 = (uint)((double)num * Math.Pow(10.0, (int)num3));
		}
		else
		{
			b2 = 0;
			num3 = (ushort)(-1 * power);
			num2 = (uint)((double)num / Math.Pow(10.0, (int)num3));
		}
		array[0] = b;
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(num2)), 0, array, 1, 4);
		array[5] = b2;
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(num3)), 0, array, 6, 2);
		return array;
	}

	public float reconvVmsFloatValue(byte[] data)
	{
		float num = 0f;
		byte b = data[0];
		byte[] array = new byte[4];
		Array.Copy(data, 1, array, 0, 4);
		uint num2 = BitConverter.ToUInt32(conv_endian(array), 0);
		byte b2 = data[5];
		byte[] array2 = new byte[2];
		Array.Copy(data, 6, array2, 0, 2);
		uint num3 = BitConverter.ToUInt16(conv_endian(array2), 0);
		num = ((b2 != 0) ? ((float)num2 / (float)Math.Pow(10.0, num3)) : ((float)num2 * (float)Math.Pow(10.0, num3)));
		if (b != 0)
		{
			num *= -1f;
		}
		return num;
	}

	public byte[] convVmsRgbFloatStructure(float r_data, float g_data, float b_data, short power)
	{
		byte[] sourceArray = convVmsFloatValue(r_data, power);
		byte[] sourceArray2 = convVmsFloatValue(g_data, power);
		byte[] sourceArray3 = convVmsFloatValue(b_data, power);
		byte[] array = new byte[24];
		Array.Copy(sourceArray, 0, array, 0, 8);
		Array.Copy(sourceArray2, 0, array, 8, 8);
		Array.Copy(sourceArray3, 0, array, 16, 8);
		return array;
	}

	private static bool sendVmsPacket(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		sdcpPacket.setSdcpV4PacketHeader();
		sdcpPacket.setupVmsPacketHeader();
		return sdcpTcp.sendPacketV4(sdcpPacket);
	}

	public int recvVmsPacket(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		if (!sdcpTcp.receivePacketV4(sdcpPacket))
		{
			return 34;
		}
		return 32;
	}

	public bool checkVmsRecvPacketError(SdcpMessageBuffer sdcpPacket)
	{
		if (sdcpPacket.data[0] != 0)
		{
			return false;
		}
		return true;
	}

	public int sendGetProductInformation(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.systemGetPruductInformation();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendCommonControlStart(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.systemSetStartControl();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendRestoreFactorySetAll(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.systemConfigurationRestoreFactorySetAll();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		sdcpTcp.closeTarget();
		return 32;
	}

	public int sendCommonColorTemperatureAdjustmentPasswordLockStatus(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentCommonColorTemperatureAdjustmentPasswordLockStatus();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendWBCorrectionValueClear(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentWBCorrectionValueClear();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutBVM_EF(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionBvmEF(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutPVMA_EF(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionPvmaEF(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutLMDA_EF(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionLmdaEF(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignal(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionBvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionPvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionBvmhx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutPvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionPvmxxx00(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustExecutBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustmentExecutionBvmhxxx10(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignalPvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignalPvmaLmda(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignalBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignalBvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignalPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignalPvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignalBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignalBvmhx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignalPvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignalPvmxxx00(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjInternalSignalBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjInternalSignalBvmhxxx10(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustPannelCorrectGainVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, float r_data, float g_data, float b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] vmsRgbStructure = convVmsRgbFloatStructure(r_data, g_data, b_data, 6);
		vmsContainer.adjustmentAutoColorTempAdjPanelCorrectGainVal(isStatusSence: false, vmsRgbStructure);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustPannelCorrectGainVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjPanelCorrectGainVal(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustPannelCorrectBiasVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, short r_data, short g_data, short b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[6];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 4, 2);
		vmsContainer.adjustmentAutoColorTempAdjPanelCorrectBiasVal(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustPannelCorrectBiasVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjPanelCorrectBiasVal(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainVal(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainVal(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainValPvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValPvmaLmda(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainValBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValBvmx(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainValPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValPvmx(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainValBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValBvmhx(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainValPvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValPvmxxx00(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbGainValBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValBvmhxxx10(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjBacklightValLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, short l_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[2];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(l_data)), 0, array, 0, 2);
		vmsContainer.adjustmentAutoColorTempAdjBacklightValLmda(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainValPvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValPvmaLmda(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainValBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValBvmx(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainValPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValPvmx(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainValBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValBvmhx(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainValPvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValPvmxxx00(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbGainValBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbGainValBvmhxxx10(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbBiasVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasVal(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasVal(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasVal(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbBiasValPvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValPvmaLmda(isStatusSence: false, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasValPvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValPvmaLmda(isStatusSence: true, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbBiasValBvmx(VmsBiasSettingMode type, ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValBvmx(isStatusSence: false, type, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasValBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValBvmx(isStatusSence: true, VmsBiasSettingMode.NO_USE, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustUserWbBiasValPvmx(VmsBiasSettingMode type, ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, int r_data, int g_data, int b_data)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[12];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(r_data)), 0, array, 0, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(g_data)), 0, array, 4, 4);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(b_data)), 0, array, 8, 4);
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValPvmx(isStatusSence: false, type, array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasValPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValPvmx(isStatusSence: true, VmsBiasSettingMode.NO_USE, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasValBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValBvmhx(isStatusSence: true, VmsBiasSettingMode.NO_USE, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasValPvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValPvmxxx00(isStatusSence: true, VmsBiasSettingMode.NO_USE, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustUserWbBiasValBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjUserWbBiasValBvmhxxx10(isStatusSence: true, VmsBiasSettingMode.NO_USE, null);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetAutoColorTempAdjustTargetLuminamceMode(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjTargetLumiMode();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValue(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetVal(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValuePvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetValPvmaLmda(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValueBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetValBvmx(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValuePvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetValPvmx(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValueBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetValBvmhx(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValuePvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetValPvmxxx00(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustTargetValueBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, ushort target_x, ushort target_y, ushort target_highlight, ushort target_lowlight)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		byte[] array = new byte[8];
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_x)), 0, array, 0, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_y)), 0, array, 2, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_highlight)), 0, array, 4, 2);
		Array.Copy(conv_endian_to_big(BitConverter.GetBytes(target_lowlight)), 0, array, 6, 2);
		vmsContainer.adjustmentAutoColorTempAdjTargetValBvmhxxx10(array);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustContrastBrightHold(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjContBrightHoldMode(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustContrastBrightHoldPvmaLmda(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjContBrightHoldModePvmaLmda(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustContrastBrightHoldBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjContBrightHoldModeBvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTempAdjustContrastBrightHoldPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTempAdjContBrightHoldModePvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustYAdjustmentMode(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustYAdjustmentMode(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustYAdjustmentModeBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, bool mode)
	{
		byte mode2 = 1;
		if (mode)
		{
			mode2 = 2;
		}
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustYAdjustmentModeBvmx(mode2);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustYAdjustmentModePvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, bool mode)
	{
		byte mode2 = 1;
		if (mode)
		{
			mode2 = 2;
		}
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustYAdjustmentModePvmx(mode2);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustYAdjustmentModeBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, bool mode)
	{
		byte mode2 = 1;
		if (mode)
		{
			mode2 = 2;
		}
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustYAdjustmentModeBvmhx(mode2);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustYAdjustmentModePvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, bool mode)
	{
		byte mode2 = 1;
		if (mode)
		{
			mode2 = 2;
		}
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustYAdjustmentModePvmxxx00(mode2);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustYAdjustmentModeBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, bool mode)
	{
		byte mode2 = 1;
		if (mode)
		{
			mode2 = 2;
		}
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustYAdjustmentModeBvmhxxx10(mode2);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustContrastBrightYAdjustmentExecution(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustContrastBrightYAdjustmentExecution(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionBvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionPvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionBvmhx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionBvmhx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionPvmxxx00(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionPvmxxx00(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionBvmhxxx10(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustContrastBrightYAdjustmentExecutionBvmhxxx10(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustInternalSignalLevelCalcExecution(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustInternalSignalLevelCalcExecution(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustInternalSignalLevelCalcExecutionBvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustInternalSignalLevelCalcExecutionBvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetAutoColorTemperatureAdjustInternalSignalLevelCalcExecutionPvmx(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentAutoColorTemperatureAdjustInternalSignalLevelCalcExecutionPvmx(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendSetLuminanceSensorCalibrationExecution(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, byte mode)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentLumiSensorCalibrationExecution(mode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetLuminanceSensorCalibrationExecutionStatus(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.adjustmentLumiSensorCalibrationExecutionStatus();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetMonitorNetworkSwitch(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.informationGetMonitorNetworkSwitch();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendLowLetencyModeSet(int mode, ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		bool lowLatencyMode = false;
		if (mode == 0)
		{
			lowLatencyMode = true;
		}
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.universalFunctionDisplayFunctionLowLatencyModeSet(lowLatencyMode);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendLowLetencyModeSence(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.universalFunctionDisplayFunctionLowLatencyModeSence();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendColorCustomizingColorTemp(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket, bool isStatusSence, byte userPreset, byte colorTemp)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.colorCustomizingColorTemp(isStatusSence, userPreset, colorTemp);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendBackupExecutionStart(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.copySystemBackupSystemExecution(mode: true);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendBackupExecutionEnd(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.copySystemBackupSystemExecution(mode: false);
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendCreateBackupFileExecution(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.copySystemBackupSystemCreateBackupFileExecution();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendLoadBackupFileExecution(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.copySystemBackupSystemLoadExecution();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendInformationCommonPackagedStatus(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.packagedStatusInformationCommonPackagedStatus();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendGetRejionConfig(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		bool flag = false;
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.packagedStatusPackagedCommonStatus();
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}

	public int sendMainOperationConfigurationPackegedStatusSense(ISdcpTransport sdcpTcp, SdcpMessageBuffer sdcpPacket)
	{
		LegacyVmsContainer vmsContainer = sdcpPacket.createVmsContainer();
		vmsContainer.packagedStatusMainOperationConfigurationPackegedStatusSense();
		bool flag = false;
		if (!sendVmsPacket(sdcpTcp, sdcpPacket))
		{
			return 33;
		}
		return 32;
	}
}
