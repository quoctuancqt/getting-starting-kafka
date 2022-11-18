using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    var random = new Random();
    string key = "";

    while ((key = Console.ReadLine()) != "q")
    {
        string payload = $$"""
                {
                    "temperature": {{random.NextDouble()}},
                    "humidity": {{random.Next(30,50)}}
                  }
            """;

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
