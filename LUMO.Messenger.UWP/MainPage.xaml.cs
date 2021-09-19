using LUMO.Messenger.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LUMO.Messenger.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Message> Messages;

        protected string host = "pcfeib425t.vsb.cz";
        protected int port = 1883;
        protected string clientId = "MOR0157";
        protected string username = "mobilni";
        protected string password = "Systemy";

        protected IMqttClient mqttClient = new MqttFactory().CreateMqttClient();


        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Messages = new ObservableCollection<Message>();
            IMqttClientOptions mqttOptions = new MqttClientOptionsBuilder()
                                                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                                                .WithTcpServer(host, port)
                                                .WithClientId(clientId)
                                                .WithCredentials(username, password)
                                                .Build();

            mqttClient.UseDisconnectedHandler(cd =>
            {
                Console.WriteLine($"{DateTime.Now.ToShortTimeString()} disconnected");
            });
            mqttClient.UseConnectedHandler(async cc =>
            {
                Console.WriteLine(cc.AuthenticateResult.ResultCode);
                Console.WriteLine($"{DateTime.Now.ToShortTimeString()} connected.");
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("/mschat/all/#").Build());
            });
            mqttClient.UseApplicationMessageReceivedHandler(amr =>
            {
                ReceiveMessage(amr.ApplicationMessage.Topic, amr.ApplicationMessage.Payload);
            });
            try
            {
                await mqttClient.ConnectAsync(mqttOptions);
                await SendMessage("Test");
            }
            catch (Exception)
            {

            }
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
            Message message = new Message
            {
                Sender = new Contact { Nickname = user },
                Content = formating[0],
                Created = DateTime.Parse(formating[1])
            };
            Console.WriteLine($"{message.Sender.Nickname} {message.Created}: {message.Content}");
        }
    }
}
