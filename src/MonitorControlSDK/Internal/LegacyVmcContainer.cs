using System.Linq;
using System.Text;

namespace Sony.MonitorControl.Internal;

public sealed class LegacyVmcContainer : ILegacySdcpContainer
{
	private byte[] data;

	private ushort length;

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

	public LegacyVmcContainer(ref byte[] dt, ushort len)
	{
		data = dt;
		length = len;
	}

	private void stringToData(string str)
	{
		length = (ushort)str.Length;
		for (ushort num = 0; num < length; num++)
		{
			data[num] = (byte)str[num];
		}
	}

	private string dataToString()
	{
		return Encoding.ASCII.GetString(data);
	}

	public void setCommand(string category, string cmd)
	{
		string text = category;
		text += " ";
		text += cmd;
		stringToData(text);
	}

	public void setCommand(string category, string cmd, string cmd2)
	{
		string text = category;
		text += " ";
		text += cmd;
		text += " ";
		text += cmd2;
		stringToData(text);
	}

	public void setCommand(string category, string cmd, string cmd2, string cmd3)
	{
		string text = category;
		text += " ";
		text += cmd;
		text += " ";
		text += cmd2;
		text += " ";
		text += cmd3;
		stringToData(text);
	}

	public int parse(out string[]? argList)
	{
		string s = dataToString();
		try
		{
			argList = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			return argList.Length;
		}
		catch
		{
			argList = null;
			return 0;
		}
	}
}
