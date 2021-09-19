using LUMO.Messenger.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System;
using System.Net;
using System.Threading.Tasks;

string host = "pcfeib425t.vsb.cz:9999/ws";
int port = 1883;
string clientId = "MOR0157";
string username = "mobilni";
string password = "Systemy";


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
IMqttClientOptions mqttOptions = new MqttClientOptionsBuilder()
                                    .WithWebSocketServer(host)
                                    .WithClientId(clientId)
                                    .WithCredentials(username, password)
                                    .Build();

mqttClient.UseDisconnectedHandler(e =>
{
    Console.WriteLine($"{DateTime.Now.ToShortTimeString()} disconnected");
});
mqttClient.UseConnectedHandler(async e =>
{
    Console.WriteLine(e.AuthenticateResult.ResultCode);
    Console.WriteLine($"{DateTime.Now.ToShortTimeString()} connected.");
    await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("/mschat/all/#").Build());
});
mqttClient.UseApplicationMessageReceivedHandler(e =>
{
    Console.WriteLine("Meassage received");
    Console.WriteLine(e.ApplicationMessage.Topic);
    Console.WriteLine(System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
});

await mqttClient.ConnectAsync(mqttOptions);

await builder.Build().RunAsync();

//await mqttClient.SubscribeAsync("/mschat/all/#", MqttQualityOfServiceLevel.AtMostOnce);
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
