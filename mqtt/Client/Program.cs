using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

var factory = new MqttFactory();

var mqttOptions = new MqttClientOptionsBuilder()
   .WithTcpServer("localhost")
   .Build();

var mqttClient = factory.CreateMqttClient();

await Setup(mqttClient);

await PublishTelemetry(mqttClient);

async Task Setup(IMqttClient mqttClient)
{
    // Setup message handling before connecting so that queued messages
    // are also handled properly. When there is no event handler attached all
    // received messages get lost.
    mqttClient.ApplicationMessageReceivedAsync += HandleMesasgeReceived;

    mqttClient.DisconnectedAsync += HandleDisconnect;

    var response = await mqttClient.ConnectAsync(mqttOptions);

    Console.WriteLine("The MQTT client is connected.");

    response.DumpToConsole();
}

async Task PublishTelemetry(IMqttClient mqttClient)
{
    var settings = new JsonSerializerSettings
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    var random = new Random();
    string key = "";

    while ((key = Console.ReadLine()) != "q")
    {
        // string payload = $$"""
        //         {
        //             "temperature": {{random.NextDouble()}},
        //             "humidity": {{random.Next(30,50)}}
        //           }
        //     """;

        var data = new List<PingMessageModel>();
        data.Add(new PingMessageModel
        {
            Type = "init",
            Data = new object[] {
                new { ModelNo = "SRMD01", SerialNo = "SRDE01" }
            }
        });
        data.Add(new PingMessageModel
        {
            Type = "init",
            Data = new object[] {
                new { ModelNo = "SRMD01", SerialNo = "SRDE02" }
            }
        });

        var payload = JsonConvert.SerializeObject(data, settings);

        Console.WriteLine($"Payload:{payload}");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("temperature")
            .WithPayload(payload)
            .WithContentType("application/json")
            .Build();

        await mqttClient.PublishAsync(message);

        Thread.Sleep(1000);
    }

    var mqttClientDisconnectOptions = factory.CreateClientDisconnectOptionsBuilder().Build();

    await mqttClient.DisconnectAsync(mqttClientDisconnectOptions, CancellationToken.None);

    Console.ReadLine();
}


async Task HandleMesasgeReceived(MqttApplicationMessageReceivedEventArgs e)
{
    await Task.CompletedTask;
    var payload = e.ApplicationMessage.Payload.Parse();
    var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
    var jObject = JObject.Parse(json);
    var data = jObject.SelectToken("data");
    var id = data?.SelectToken("id")?.Value<int>();

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Receive message {JsonConvert.SerializeObject(payload)}");
    Console.ResetColor();
}

async Task HandleDisconnect(MqttClientDisconnectedEventArgs e)
{
    Console.WriteLine("Disconnected");

    try
    {
        await mqttClient.ConnectAsync(mqttOptions);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Reconnect failed {0}:", ex.Message);
    };
}

class PingMessageModel
{
    public string SerialNumber { get; set; } = "GW01";
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
    public string Type { get; set; } = "init";
    public dynamic[] Data { get; set; }
}