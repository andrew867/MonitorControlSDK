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

	/// <summary>Builds ASCII payload: <c>category</c> plus optional space-separated segments (e.g. <c>STATget MODEL</c>, <c>STATset BRIGHTNESS 512</c>).</summary>
	public void setCommand(string category, params string[] segments)
	{
		if (segments is null || segments.Length == 0)
		{
			stringToData(category);
			return;
		}

		string text = category + " " + string.Join(" ", segments);
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
