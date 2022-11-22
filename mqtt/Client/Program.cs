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

    while ((key = Console.ReadLine()!) != "q")
    {
        string payload = "";

        if (key == "1") // Send telemetry
        {
            var data = new KafkaMessageModel
            {
                Data = new List<DeviceData>{
                    new DeviceData
                    {
                        ModelNo = "SRMD01",
                        SerialNo = "SRDE01",
                        OnOff = "On",
                        BatteryPercentage = 100,
                        SpeedSetting = "LOW"
                    }
                },
                Type = "telemetry"
            };

            payload = JsonConvert.SerializeObject(data, settings);
        }

        if (key == "2") // Send register device
        {
            var data = new KafkaMessageModel
            {
                Data = new List<DeviceData>{
                    new DeviceData
                    {
                        ModelNo = "SRMD01",
                        SerialNo = "SRDE03"
                    }
                },
                Type = "init"
            };

            payload = JsonConvert.SerializeObject(data, settings);
        }

        Console.WriteLine($"Payload:{payload}");

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("MQTT_TOPIC")
            .WithPayload(payload)
            .WithContentType("application/json")
            .Build();

        await mqttClient.PublishAsync(message);
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



class KafkaMessageModel
{
    public string GatewayNo { get; set; } = "GW01";
    public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
    public string Type { get; set; } = "init";
    public List<DeviceData> Data { get; set; } = new();
}

class DeviceData
{
    public string ModelNo { get; set; } = "";
    public string SerialNo { get; set; } = "";
    public string OnOff { get; set; } = "";
    public int BatteryPercentage { get; set; } = 0;
    public string SpeedSetting { get; set; } = "";
}