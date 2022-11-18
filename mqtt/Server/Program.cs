using System.Text;
using MQTTnet;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(o =>
{
    // This will allow MQTT connections based on TCP port 1883.
    o.ListenAnyIP(1883, l => l.UseMqtt());

    // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:5000/mqtt"
    // See code below for URI configuration.
    o.ListenAnyIP(5000); // Default HTTP pipeline
});

var services = builder.Services;

services.AddHostedMqttServer(optionsBuilder =>
{
    optionsBuilder.WithDefaultEndpoint();
});

services.AddMqttConnectionHandler();
services.AddConnections();

services.AddSingleton<MqttController>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapConnectionHandler<MqttConnectionHandler>("/mqtt", httpConnectionDispatcherOptions
        => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector = protocolList => protocolList.FirstOrDefault() ?? string.Empty);
});

var mqttController = app.Services.GetService<MqttController>()!;

app.UseMqttServer(server =>
{
    /*
        * Attach event handlers etc. if required.
        */
    server.ValidatingConnectionAsync += mqttController.ValidateConnection;
    server.ClientConnectedAsync += mqttController.OnClientConnected;
    server.InterceptingPublishAsync += async args =>
    {
        // Do some work with the message...
        if (!args.ClientId.Equals("ServerSenderClient"))
        {
            var topic = args.ApplicationMessage.Topic;

            if (topic.StartsWith("MQTTnet.RPC/"))
            {
                var responseTopic = $"{topic}/response";

                var applicationMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(responseTopic)
                    .WithPayload("pong")
                    .Build();

                await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(applicationMessage)
                {
                    SenderClientId = "ServerSenderClient"
                });
            }
            else
            {
                Console.WriteLine(string.Format("Payload {0}", Encoding.UTF8.GetString(args.ApplicationMessage.Payload)));
            }
        }
    };
});

app.Run();

sealed class MqttController
{
    public MqttController()
    {
        // Inject other services via constructor.
    }

    public Task OnClientConnected(ClientConnectedEventArgs eventArgs)
    {
        Console.WriteLine($"Client '{eventArgs.ClientId}' connected.");
        return Task.CompletedTask;
    }


    public Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
    {
        Console.WriteLine($"Client '{eventArgs.ClientId}' wants to connect. Accepting!");
        return Task.CompletedTask;
    }
}