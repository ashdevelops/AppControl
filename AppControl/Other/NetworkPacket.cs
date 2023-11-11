namespace AppControl.Other;

public class NetworkPacket
{
    public string Name { get; set; }
    public Dictionary<string, object> Data { get; set; }

    public NetworkPacket(string name, Dictionary<string, object> data)
    {
        Name = name;
        Data = data;
    }
}