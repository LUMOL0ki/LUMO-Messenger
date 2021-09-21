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
using Windows.UI;
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
        private ObservableCollection<MessageReceived> currentMessages;
        private ObservableCollection<Contact> contacts;
        private ObservableCollection<Group> groups;
        private readonly Queue<MessageSend> messageQueue = new Queue<MessageSend>();

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
            contacts = new ObservableCollection<Contact>();
            groups = new ObservableCollection<Group>()
            {
                new Group
                {
                    Name = "all"
                }
            };

            currentMessages = groups[0].Messages;
            
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
                foreach(MessageSend messageInQueue in messageQueue)
                {
                    await SendMessageAsync(messageInQueue);
                }
                messageQueue.Clear();

                await SendMessageAsync(messageSend);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                messageQueue.Enqueue(messageSend);
            }
        }

        private async Task SendMessageAsync(MessageSend message)
        {
            await mqttClient.PublishAsync(message.Topic, message.ToString(), true);
            if (message.Topic.Contains("user") && !message.Topic.Contains($"user/{user.Nickname}"))
            {
                currentMessages.Add(new MessageReceived
                {
                    Sender = user,
                    Content = message.Content,
                    Created = message.Created,
                    Orientation = MessageOrientation.Right
                });
            }
        }

        private async Task ReceiveMessageAsync(string topic, byte[] payload)
        {
            string sender = topic.Split("/").Last();
            string[] payloadParts = new string[2];
            int index = 0;

            foreach (string part in Encoding.UTF8.GetString(payload).Split("Aktuální čas: "))
            {
                payloadParts[index] = part;
                index++;
            }

            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    MessageReceived newMessage = new MessageReceived
                    {
                        Sender = new Contact { Nickname = sender },
                        Content = payloadParts[0],
                        Created = payloadParts[1] != null ? DateTime.Parse(payloadParts[1]) : DateTime.Now,
                        Orientation = user.Nickname.Equals(sender) ? MessageOrientation.Right : MessageOrientation.Left
                    };

                    

                    Debug.WriteLine(newMessage.ToString());

                    if (topic.Contains("/all/"))
                    {
                        groups.FirstOrDefault(g => topic.Contains(g.Name)).Messages.Add(newMessage);
                    }
                    else if (topic.Contains("/user/"))
                    {
                        Debug.WriteLine(topic);
                        Contact contact = contacts.FirstOrDefault(c => sender.Equals(c.Nickname));
                        if(contact == null)
                        {
                            contacts.Add(new Contact
                            {
                                Nickname = sender
                            });
                            contact = contacts.Last();
                        }
                        contact.Messages.Add(newMessage);
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

            Contact contactToUpdate = contacts.FirstOrDefault(c => c.Nickname.Equals(topicArray[3]));

            if(contactToUpdate != null)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    contactToUpdate.SetStatus(payloadParts.Count() > 1 ? payloadParts.Last() : payloadParts[0]);
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    contactToUpdate = new Contact()
                    {
                        Nickname = topicArray[3]
                    };
                    contactToUpdate.SetStatus(payloadParts.Count() > 1 ? payloadParts.Last() : payloadParts[0]);
                    contacts.Add(contactToUpdate);
                });
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if(SendText.Text != "")
            {
                await SendMessageAsync(SendText.Text);
                SendText.Text = "";
            }
        }

        private void groupList_ItemClick(object sender, ItemClickEventArgs e)
        {
            //(contactList.ContainerFromItem(e.ClickedItem) as ListViewItem).IsSelected = false;
            Group clickedGroup = e.ClickedItem as Group;
            currentMessages = clickedGroup.Messages;
            messageReceiveList.ItemsSource = currentMessages;
            currentTopic = clickedGroup.Name;
        }

        private void contactList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Contact clickedContact = e.ClickedItem as Contact;
            currentMessages = clickedContact.Messages;
            messageReceiveList.ItemsSource = currentMessages;
            currentTopic = $"user/{clickedContact.Nickname}";
            Debug.WriteLine(currentTopic);
        }
    }
}
