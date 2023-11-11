using System.Text;

namespace AppControl;

public class FramingProtocol
{
    public static byte[] WrapMessage(byte[] message)
    {
        var lengthPrefix = BitConverter.GetBytes(message.Length);
        var all = new byte[lengthPrefix.Length + message.Length];

        lengthPrefix.CopyTo(all, 0);
        message.CopyTo(all, lengthPrefix.Length);

        return all;
    }
}