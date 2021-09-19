using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LUMO.Messenger.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected string host = "pcfeib425t.vsb.cz";
        protected int port = 1883;
        protected string clientId = "MOR0157";
        protected string username = "mobilni";
        protected string password = "Systemy";

        protected IMqttClient mqttClient = new MqttFactory().CreateMqttClient();

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

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
        }

        protected async Task SendMessage(string message)
        {
            await mqttClient.PublishAsync($"/mschat/all/{clientId}", $"{message} Aktuální čas: {DateTime.Now:HH:mm:ss}");
        }

        protected void ReceiveMessage(string topic, byte[] payload)
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
    }
}
