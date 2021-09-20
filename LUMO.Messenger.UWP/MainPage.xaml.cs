using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Models;
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
        private readonly string host = "pcfeib425t.vsb.cz";
        private readonly int port = 1883;
        private readonly string clientId = "MOR0157";
        private readonly string username = "mobilni";
        private readonly string password = "Systemy";

        private ObservableCollection<Message> Messages;
        private ObservableCollection<Contact> Contacts;
        private ObservableCollection<Group> Groups;
        
        private Contact user;
        private IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
        private string currentTopic;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            user = new Contact
            {
                Nickname = clientId,
                Status = ContatStatus.Online
            };

            Messages = new ObservableCollection<Message>()
            {
                new Message
                {
                    Sender = new Contact
                    {
                        Nickname = "anon"
                    },
                    Content = "Hello",
                    Created = DateTime.Parse(DateTime.Now.ToString("HH:mm:ss"))
                }
            };
            Contacts = new ObservableCollection<Contact>()
            {
                new Contact
                {
                    Nickname = "franta",
                    Status = ContatStatus.Online
                },
                new Contact
                {
                    Nickname = "pepa",
                    Status = ContatStatus.Offline
                },
                new Contact
                {
                    Nickname = "lukas",
                    Status = ContatStatus.Unknown
                }
            };
            Groups = new ObservableCollection<Group>()
            {
                new Group
                {
                    Name = "All"
                }
            };
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
            }
            catch (Exception)
            {

            }
        }

        private async Task SendMessage(string message)
        {
            MessageSend messageSend = new MessageSend
            {
                Content = message
            };
            await mqttClient.PublishAsync($"/mschat/all/{user.Nickname}", $"{messageSend.Content} Aktuální čas: {messageSend.Created:HH:mm:ss}");
            Messages.Add(new Message
            {
                Sender = user,
                Content = messageSend.Content,
                Created = messageSend.Created
            });
        }

        private void ReceiveMessage(string topic, byte[] payload)
        {
            string undefined = "undefined";
            string[] formating = new string[2] { undefined, undefined };
            int index = 0;

            foreach (string part in Encoding.UTF8.GetString(payload).Split("Aktuální čas: "))
            {
                formating[index] = part;
                index++;
            }
            /*
            Messages.Add(new Message
            {
                Sender = new Contact { Nickname = topic.Split("/")[3] },
                Content = formating[0],
                Created = formating[1] != undefined ? DateTime.Parse(formating[1]) : DateTime.Now
            });
            */
        }
    }
}
