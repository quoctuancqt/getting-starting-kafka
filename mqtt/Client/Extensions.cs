using System.Text;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

public static class Extensions
{
    public static TObject DumpToConsole<TObject>(this TObject @object)
    {
        var output = "NULL";

        if (@object != null)
        {
            output = JsonConvert.SerializeObject(@object);
        }

        Console.WriteLine($"[{@object?.GetType().Name}]:\r\n{output}");

        return @object;
    }

    public static void DumpToConsole(this MqttApplicationMessageReceivedEventArgs e)
    {
        var payload = e.ApplicationMessage.Payload.Parse();

        Console.WriteLine("Payload:\r\n{Payload}", e.ApplicationMessage.ConvertPayloadToString());
    }

    public static Dictionary<string, object> Parse(this byte[] bytes)
    {
        if (bytes is null) return null;

        string jsonStr = Encoding.UTF8.GetString(bytes);

        return JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonStr);
    }
}