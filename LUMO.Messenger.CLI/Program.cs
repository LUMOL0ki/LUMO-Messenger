// See https://aka.ms/new-console-template for more information
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using System.Text;

string host = "pcfeib425t.vsb.cz";
int port = 1883;
string clientId = "MOR0157";
string username = "mobilni";
string password = "Systemy";

IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
IMqttClientOptions mqttOptions = new MqttClientOptionsBuilder()
                                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                                    .WithTcpServer(host, port)
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
    ReceiveMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload);
});
await mqttClient.ConnectAsync(mqttOptions);
await SendMessage("Test");
//mqttClient.PublishAsync(new MqttApplicationMessage() { Topic = "/mschat/all/anon",  })

while (true)
{

}

//await mqttClient.PublishAsync("/mschat/all/#", "test");

async Task SendMessage(string message)
{
    await mqttClient.PublishAsync($"/mschat/all/{clientId}", $"{message} Aktuální čas: {DateTime.Now:HH:mm:ss}");
}

void ReceiveMessage(string topic, byte[] payload)
{
    string[] formating = new string[2] { "undefined", "undefined" };
    int index = 0;

    foreach (string part in Encoding.UTF8.GetString(payload).Split("Aktuální čas: "))
    {
        formating[index] = part;
        index++;
    }

    string user = topic.Split("/")[3];

    Console.WriteLine($"{user} {formating[1]}: {formating[0]}");
}