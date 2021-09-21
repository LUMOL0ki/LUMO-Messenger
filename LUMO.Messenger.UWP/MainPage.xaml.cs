using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
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
        private ObservableCollection<MessageReceived> CurrentMessages;
        private ObservableCollection<Contact> Contacts;
        private ObservableCollection<Group> Groups;
        private readonly Queue<MessageSend> MessageQueue = new Queue<MessageSend>();

        private Contact user;
        private IMqttClient mqttClient;
        private string currentTopic = "all";

        public MainPage()
        {
            this.InitializeComponent();
            user = new Contact
            {
                Nickname = "MOR0157",
                Status = ContatStatus.Online
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            mqttClient = ((App)Application.Current).mqttClient;

            /*CurrentMessages = new ObservableCollection<MessageReceived>()
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
            */
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
                    Name = "all"
                }
            };

            CurrentMessages = Groups[0].Messages;
            
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
            mqttClient.UseApplicationMessageReceivedHandler(async amr =>
            {
                string topic = amr.ApplicationMessage.Topic;
                switch (topic)
                {
                    case string message when message.Contains("/all/") || message.Contains("/user/"):
                        await ReceiveMessageAsync(topic, amr.ApplicationMessage.Payload);
                        break;
                    case string status when status.Contains("/status/"):
                        await UpdateStatusAsync(topic, amr.ApplicationMessage.Payload);
                        break;
                }
            });
            /*
            try
            {
                await mqttClient.ConnectAsync(mqttOptions);
                await SetStatusAsync(ContatStatus.Online);
            }
            catch (Exception)
            {
            }
            */
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
        }

        private async Task ReceiveMessageAsync(string topic, byte[] payload)
        {
            try
            {
                string sender = topic.Split("/")[3];
                string[] payloadParts = new string[2];
                int index = 0;

                foreach (string part in Encoding.UTF8.GetString(payload).Split("Aktuální čas: "))
                {
                    payloadParts[index] = part;
                    index++;
                }

                MessageReceived newMessage = new MessageReceived
                {
                    Sender = new Contact { Nickname = sender },
                    Content = payloadParts[0],
                    Created = payloadParts[1] != null ? DateTime.Parse(payloadParts[1]) : DateTime.Now,
                    Orientation = user.Nickname.Equals(sender) ? MessageOrientation.Right : MessageOrientation.Left
                };

                Debug.WriteLine(newMessage.ToString());

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
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
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task UpdateStatusAsync(string topic, byte[] payload)
        {
            string[] topicArray = topic.Split("/");
            string payloadText = Encoding.UTF8.GetString(payload);
            string[] payloadParts = payloadText.Split(" ");

            Contact contactToUpdate = Contacts.FirstOrDefault(c => c.Nickname.Equals(topicArray[3]));
            if(contactToUpdate != null)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    contactToUpdate.SetStatus(payloadParts.Count() > 1 ? payloadParts[1] : payloadParts[0]);
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Contacts.Add(new Contact()
                    {
                        Nickname = topicArray[3],
                        Status = (ContatStatus)Enum.Parse(typeof(ContatStatus), payloadParts.Count() > 1 ? payloadParts[1] : payloadParts[0], true)
                    });
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
