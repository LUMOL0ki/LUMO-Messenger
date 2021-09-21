using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        private ObservableCollection<MessageReceived> CurrentMessages;
        private ObservableCollection<Contact> Contacts;
        private ObservableCollection<Group> Groups;
        private Queue<MessageSend> MessageQueue = new Queue<MessageSend>();

        private Contact user;
        private IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
        private string currentTopic = "all";

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

            CurrentMessages = new ObservableCollection<MessageReceived>()
            {
                new MessageReceived
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
                                                .WithCommunicationTimeout(TimeSpan.FromSeconds(6))
                                                .WithClientId(clientId)
                                                .WithCredentials(username, password)
                                                .Build();

            mqttClient.UseDisconnectedHandler(cd =>
            {
                Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} disconnected");
            });
            mqttClient.UseConnectedHandler(async cc =>
            {
                Debug.WriteLine(cc.AuthenticateResult.ResultCode);
                Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} connected.");
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("/mschat/#").Build());
            });
            mqttClient.UseApplicationMessageReceivedHandler(amr =>
            {
                string topic = amr.ApplicationMessage.Topic;
                switch (topic)
                {
                    case string message when message.Contains("/all") || message.Contains("/user"):
                        ReceiveMessage(topic, amr.ApplicationMessage.Payload);
                        break;
                    case string status when status.Contains("/status"):
                        UpdateStatus(topic, amr.ApplicationMessage.Payload);
                        break;
                }
            });
            try
            {
                await mqttClient.ConnectAsync(mqttOptions);
            }
            catch (Exception)
            {
            }
        }

        private async Task SendMessageAsync(string message)
        {
            MessageSend messageSend = new MessageSend
            {
                Topic = $"/mschat/{currentTopic}/{user.Nickname}",
                Content = message
            };

            try
            {
                foreach(MessageSend messageInQueue in MessageQueue)
                {
                    await SendMessageAsync(messageInQueue);
                }
                MessageQueue.Clear();

                await SendMessageAsync(messageSend);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                MessageQueue.Enqueue(messageSend);
            }
        }

        private async Task SendMessageAsync(MessageSend message)
        {
            await mqttClient.PublishAsync(message.Topic, message.ToString(), true);
            CurrentMessages.Add(new MessageReceived
            {
                Sender = user,
                Content = message.Content,
                Created = message.Created,
                Orientation = "Right"
            });
        }

        private void ReceiveMessage(string topic, byte[] payload)
        {
            try
            {
                string undefined = "undefined";
                string[] formating = new string[2] { undefined, undefined };
                int index = 0;

                foreach (string part in Encoding.UTF8.GetString(payload).Split("Aktuální čas: "))
                {
                    formating[index] = part;
                    index++;
                }
                MessageReceived newMessage = new MessageReceived
                {
                    Sender = new Contact { Nickname = topic.Split("/")[3] },
                    Content = formating[0],
                    Created = formating[1] != undefined ? DateTime.Parse(formating[1]) : DateTime.Now
                };
                Debug.WriteLine(newMessage.ToString());

                if (topic.Contains("/all/"))
                {
                    Groups.FirstOrDefault(g => topic.Contains(g.Name)).Messages.Add(newMessage);
                }
                else if (topic.Contains("/user/"))
                {
                    Contacts.FirstOrDefault(c => topic.Contains(c.Nickname)).Messages.Add(newMessage);
                }

                /*
                Messages.Add(newMessage);
                */
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void UpdateStatus(string topic, byte[] payload)
        {
            string[] topicArray = topic.Split("/");
            Contact contactToUpdate = Contacts.FirstOrDefault(c => c.Nickname.Equals(topicArray[3]));
            if(contactToUpdate != null)
            {
                contactToUpdate.Status = (ContatStatus)Enum.Parse(typeof(ContatStatus), Encoding.UTF8.GetString(payload), true);
            }
            else
            {
                Contacts.Add(new Contact()
                {
                    Nickname = topicArray[3],
                    Status = (ContatStatus)Enum.Parse(typeof(ContatStatus), Encoding.UTF8.GetString(payload), true)
                });
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync(SendText.Text);
            SendText.Text = "";
        }
    }
}
