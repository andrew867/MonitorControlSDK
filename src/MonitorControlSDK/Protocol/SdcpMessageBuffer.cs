using System;
using MonitorControl.Internal;

namespace MonitorControl.Protocol;

/// <summary>SDCP wire buffer (V3 header + payload, optional V4 extended header) matching legacy <c>SdcpPacket</c> layout.</summary>
public sealed class SdcpMessageBuffer
{
	private enum SdcpPacketNetworkType
	{
		SDCP_PACKET_NETWORK_TYPE_P2P,
		SDCP_PACKET_NETWORK_TYPE_SINGLE,
		SDCP_PACKET_NETWORK_TYPE_GROUP,
		SDCP_PACKET_NETWORK_TYPE_ALL,
		SDCP_PACKET_NETWORK_TYPE_UNKNOWN
	}

	private const byte SDCP_VER3 = 3;

	private const byte SDCP_VER4 = 4;

	private const byte SDCP_CATEGORY = 11;

	public const byte SDCP_COMMAND_REQUEST = 0;

	public const byte SDCP_COMMAND_RESPONSE_OK = 1;

	public const byte SDCP_COMMAND_RESPONSE_NG = 0;

	/// <summary>SDCP version 3 item number for VMC: ASCII commands in the Data field (<c>B000h</c> per PVM-740 programmer manual excerpt).</summary>
	public const ushort SdcpV3ItemVideoMonitorControl = 0xB000;

	/// <summary>SDCP v3 item number for VMC on monitors with built-in controllers (<c>B001h</c> per same excerpt). <see cref="Clients.VmcClient"/> uses <see cref="SdcpV3ItemVideoMonitorControl"/> by default.</summary>
	public const ushort SdcpV3ItemVideoMonitorControlBuiltIn = 0xB001;

	private const ushort SDCP_COMMAND_ITEM_SYSTEM_SETTING = 47104;

	private const ushort SDCP_COMMAND_ITEM_SERV_ADJ = 61440;

	private const ushort SDCP_COMMAND_ITEM_V4_PC_APPLICATION_4K = 47360;

	private const ushort SDCP_COMMAND_ITEM_V4_CONTROLLER = 47364;

	public const ushort SDCP_ERR_INVALID_ITEM = 257;

	public const ushort SDCP_ERR_INVALID_ITEMREQ = 258;

	public const ushort SDCP_ERR_INVALID_LENGTH = 259;

	public const ushort SDCP_ERR_INVALID_DATA = 260;

	public const ushort SDCP_ERR_SHORT_DATA = 273;

	public const ushort SDCP_ERR_INVALID_SUB_CMD = 288;

	public const ushort SDCP_ERR_INVALID_SUB_CMD_DATA = 289;

	public const ushort SDCP_ERR_PASSWORD_LOCKED = 290;

	public const ushort SDCP_ERR_PASSWORD_AUTHENTICATION_ERROR = 291;

	public const ushort SDCP_ERR_OPERATE_CONDITION_ERROR = 292;

	public const ushort SDCP_ERR_COPY_EXECUTION_ERROR = 293;

	public const ushort SDCP_ERR_USER_LUT_EXECUTION_ERROR = 294;

	public const ushort SDCP_ERR_CAN_NOT_CONTROL = 295;

	public const ushort SDCP_ERR_NOTAPPLICABLE = 384;

	public const ushort SDCP_ERR_NOTEXECUTED_ORDER_SETTING = 400;

	public const ushort SDCP_ERR_DIFFCOMMUNITY = 513;

	public const ushort SDCP_ERR_INVALID_VERSION = 4097;

	public const ushort SDCP_ERR_INVALID_CATEGORY = 4098;

	public const ushort SDCP_ERR_INVALID_REQUEST = 4099;

	public const ushort SDCP_ERR_SHORT_HEADER = 4113;

	public const ushort SDCP_ERR_SHORT_COMMUNITY = 4114;

	public const ushort SDCP_ERR_SHORT_COMMAND_V3 = 4115;

	public const ushort SDCP_ERR_SHORT_ID_V3 = 4116;

	public const ushort SDCP_ERR_SHORT_MONITORNAME_V4 = 4117;

	public const ushort SDCP_ERR_SHORT_ID_V4 = 4118;

	public const ushort SDCP_ERR_SHORT_COMMAND_V4 = 4119;

	public const ushort SDCP_ERR_ACCESS_DENIED = 4128;

	public const ushort SDCP_ERR_INVALID_ID = 4129;

	public const ushort SDCP_ERR_NAME_DIFFER = 4130;

	public const ushort SDCP_ERR_CANNOT_EXECUTE_SET_OPERATION = 4131;

	public const ushort SDCP_ERR_NETWORK_TIMEOUT = 8193;

	public const ushort SDCP_ERR_COMM_TIMEOUT = 61441;

	public const ushort SDCP_ERR_COMM_CHECKSUM = 61456;

	public const ushort SDCP_ERR_COMM_FRAMING = 61472;

	public const ushort SDCP_ERR_COMM_PARITY = 61488;

	public const ushort SDCP_ERR_COMM_OVERRUN = 61504;

	public const ushort SDCP_ERR_COMM_OTHER = 61520;

	public const ushort SDCP_ERR_COMM_UNKNOWN = 61680;

	public const ushort SDCP_ERR_NVRAM_READ = 61712;

	public const ushort SDCP_ERR_NVRAM_WRITE = 61728;

	public const ushort SDCP_ERR_INTERNAL_ASSERT = 65531;

	public const ushort SDCP_CLOSE_NETWORK = 65532;

	public const ushort SDCP_ERR_INTERNAL_NETWORK = 65533;

	public const ushort SDCP_ERR_INTERNAL_ERROR = 65534;

	public const ushort SDCP_ERR_INVALID_PACKET = ushort.MaxValue;

	private const byte SDCP_SUPPORT_VERSION_3 = 0;

	private const byte SDCP_SUPPORT_VERSION_4 = 1;

	private const byte SDCP_SUPPORT_VERSION_3_4 = 2;

	private const int SDCP_VMC_MAX_DATA_LEN = 499;

	private const int SDCP_VMS_MAX_DATA_SIZE = 499;

	private const int SDCP_VMA_MAX_DATA_SIZE = 960;

	private const int SDCP_DATA_SIZE = 960;

	private const int SDCP_HEADER_SIZE = 13;

	private const int SDCP_PACKET_SIZE = 973;

	private const int SDCP_HEADER_NAME_SIZE = 24;

	private const int SDCP_V4_HEADER_SIZE = 37;

	private const uint INDEX_VERSION = 0u;

	private const uint INDEX_CATEGORY = 1u;

	private const uint INDEX_COMMUNITY = 2u;

	private const uint INDEX_GROUPID = 6u;

	private const uint INDEX_UNITID = 7u;

	private const uint INDEX_REQUEST_RESPONSE = 8u;

	private const uint INDEX_ITEM_NO = 9u;

	private const uint INDEX_DATA_LENGTH = 11u;

	private const uint INDEX_DATA = 13u;

	private const uint INDEX_GROUPID_V4 = 30u;

	private const uint INDEX_UNITID_V4 = 31u;

	private const uint INDEX_REQUEST_RESPONSE_V4 = 32u;

	private const uint INDEX_ITEM_NO_V4 = 33u;

	private const uint INDEX_DATA_LENGTH_V4 = 35u;

	private const uint INDEX_DATA_V4 = 37u;

	private SdcpPacketNetworkType connectType;

	private byte unitId;

	private byte groupId;

	private byte[] packetHeader;

	private byte[] packetData;

	private ILegacySdcpContainer? containerMani;

	public byte[] packet
	{
		get
		{
			if (containerMani != null)
			{
				dataLength = containerMani.dataLength;
			}

			byte[] array = new byte[length];
			Array.Copy(packetHeader, array, 13);
			Array.Copy(packetData, 0L, array, 13L, dataLength);
			return array;
		}
		set
		{
			Array.Copy(value, packetHeader, 13);
			Array.Copy(value, 13L, packetData, 0L, dataLength);
		}
	}

	public byte[] packetV4
	{
		get
		{
			if (containerMani != null)
			{
				dataLengthV4 = containerMani.dataLength;
			}

			byte[] array = new byte[lengthV4];
			Array.Copy(packetHeader, array, 37);
			Array.Copy(packetData, 0L, array, 37L, dataLengthV4);
			return array;
		}
		set
		{
			Array.Copy(value, packetHeader, 37);
			Array.Copy(value, 37L, packetData, 0L, dataLengthV4);
		}
	}

	public int maxSize
	{
		get
		{
			return 973;
		}
		set
		{
		}
	}

	public int length
	{
		get
		{
			return headerLength + dataLength;
		}
		set
		{
		}
	}

	public int headerLength
	{
		get
		{
			return 13;
		}
		set
		{
		}
	}

	public int lengthV4
	{
		get
		{
			return headerLengthV4 + dataLengthV4;
		}
		set
		{
		}
	}

	public int headerLengthV4
	{
		get
		{
			return 37;
		}
		set
		{
		}
	}

	public int dataLength
	{
		get
		{
			return packetHeader[11] * 256 + packetHeader[12];
		}
		set
		{
			packetHeader[11] = (byte)(value / 256);
			packetHeader[12] = (byte)(value % 256);
		}
	}

	public int dataLengthV4
	{
		get
		{
			return packetHeader[35] * 256 + packetHeader[36];
		}
		set
		{
			packetHeader[35] = (byte)(value / 256);
			packetHeader[36] = (byte)(value % 256);
		}
	}

	public byte[] data
	{
		get
		{
			return packetData;
		}
		set
		{
			packetData = value;
		}
	}

	public int sdcpUdpPortNumber => 53484;

	public SdcpMessageBuffer()
	{
		connectType = SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_P2P;
		unitId = 1;
		groupId = 1;
		packetHeader = new byte[13];
		packetData = new byte[960];
		setupVma();
		clearContainer();
		containerMani = null;
	}

	public void setSdcpV4PacketHeader()
	{
		packetHeader = new byte[37];
	}

	public void setSingleConnection(byte uid)
	{
		connectType = SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_SINGLE;
		unitId = uid;
		setIDsByConnectType();
	}

	public void setGroupConnection(byte gid)
	{
		connectType = SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_GROUP;
		groupId = gid;
		setIDsByConnectType();
	}

	public void setAllConnection()
	{
		connectType = SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_ALL;
		setIDsByConnectType();
	}

	public void setP2pConnection()
	{
		connectType = SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_P2P;
		groupId = 0;
		unitId = 0;
		setIDsByConnectType();
	}

	private void setIDsByConnectType()
	{
		switch (connectType)
		{
		case SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_SINGLE:
			packetHeader[6] = 0;
			packetHeader[7] = unitId;
			break;
		case SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_GROUP:
			packetHeader[6] = groupId;
			packetHeader[7] = 0;
			break;
		case SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_ALL:
			packetHeader[6] = byte.MaxValue;
			packetHeader[7] = byte.MaxValue;
			break;
		default:
			packetHeader[6] = 0;
			packetHeader[7] = 0;
			break;
		}
	}

	private void setIDsByConnectTypeV4()
	{
		switch (connectType)
		{
		case SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_SINGLE:
			packetHeader[30] = 0;
			packetHeader[31] = unitId;
			break;
		case SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_GROUP:
			packetHeader[30] = groupId;
			packetHeader[31] = 0;
			break;
		case SdcpPacketNetworkType.SDCP_PACKET_NETWORK_TYPE_ALL:
			packetHeader[30] = byte.MaxValue;
			packetHeader[31] = byte.MaxValue;
			break;
		default:
			packetHeader[30] = 0;
			packetHeader[31] = 0;
			break;
		}
	}

	public void setupVma()
	{
		packetHeader[0] = 3;
		packetHeader[1] = 11;
		packetHeader[2] = 83;
		packetHeader[3] = 79;
		packetHeader[4] = 78;
		packetHeader[5] = 89;
		setIDsByConnectType();
		packetHeader[8] = 0;
		packetHeader[9] = 240;
		packetHeader[10] = 0;
	}

	public void setupVmcPacketHeader()
	{
		packetHeader[0] = 3;
		packetHeader[1] = 11;
		packetHeader[2] = 83;
		packetHeader[3] = 79;
		packetHeader[4] = 78;
		packetHeader[5] = 89;
		setIDsByConnectType();
		packetHeader[8] = 0;
		packetHeader[9] = (byte)(SdcpV3ItemVideoMonitorControl >> 8);
		packetHeader[10] = (byte)(SdcpV3ItemVideoMonitorControl & 0xFF);
	}

	public void setupVmsPacketHeader()
	{
		packetHeader[0] = 4;
		packetHeader[1] = 11;
		packetHeader[2] = 83;
		packetHeader[3] = 79;
		packetHeader[4] = 78;
		packetHeader[5] = 89;
		setIDsByConnectTypeV4();
		packetHeader[32] = 0;
		packetHeader[33] = 185;
		packetHeader[34] = 0;
	}

	public void clearContainer()
	{
		dataLength = 0;
	}

	internal LegacyVmaContainer createVmaContainer()
	{
		containerMani = new LegacyVmaContainer(ref packetData, (ushort)dataLength);
		return (LegacyVmaContainer)containerMani;
	}

	internal LegacyVmcContainer createVmcContainer()
	{
		containerMani = new LegacyVmcContainer(ref packetData, (ushort)dataLength);
		return (LegacyVmcContainer)containerMani;
	}

	internal LegacyVmsContainer createVmsContainer()
	{
		containerMani = new LegacyVmsContainer(ref packetData, (ushort)dataLength);
		return (LegacyVmsContainer)containerMani;
	}

	public bool checkAckError()
	{
		if (packetHeader[8] == 1)
		{
			return true;
		}
		return false;
	}
}
