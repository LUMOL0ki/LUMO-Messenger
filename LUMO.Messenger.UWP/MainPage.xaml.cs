using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Clients;
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
using Windows.System;
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
        private MessengerClient messengerClient;
        private string currentTopic = "all";

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            user = ((App)Application.Current).user;
            messengerClient = ((App)Application.Current).messengerClient;

            contacts = messengerClient.Contacts;
            groups = messengerClient.Groups;

            currentMessages = messengerClient.CurrentMessages;
            
            messengerClient.UseApplicationMessageReceivedHandler(async amr =>
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

            try
            {
                await messengerClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task SendMessageAsync(string message)
        {

            if (SendText.Text == "")
            {
                return;
            }

            MessageSend messageSend = new MessageSend
            {
                Topic = $"/mschat/{currentTopic}/{user.Nickname}",
                Content = message
            };

            try
            {
                foreach(MessageSend messageInQueue in messageQueue)
                {
                    await messengerClient.SendMessageAsync(messageInQueue);
                }
                messageQueue.Clear();

                await messengerClient.SendMessageAsync(messageSend);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                messageQueue.Enqueue(messageSend);
            }
        }

        private async Task ReceiveMessageAsync(string topic, byte[] payload)
        {
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    MessageReceived newMessage = messengerClient.GetMessageReceived(topic, payload);

                    Debug.WriteLine(newMessage.ToString());

                    if (topic.Contains("/all/"))
                    {
                        groups.FirstOrDefault(g => topic.Contains(g.Name)).Messages.Add(newMessage);
                    }
                    else if (topic.Contains("/user/"))
                    {
                        Debug.WriteLine(topic);
                        Contact contact = contacts.FirstOrDefault(c => newMessage.Sender.Nickname.Equals(c.Nickname));
                        if(contact == null)
                        {
                            contacts.Add(new Contact
                            {
                                Nickname = newMessage.Sender.Nickname
                            });
                            contact = contacts.Last();
                        }
                        contact.Messages.Add(newMessage);
                    }
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

            Contact contactToUpdate = contacts.FirstOrDefault(c => c.Nickname.Equals(topicArray[3]));

            if(contactToUpdate == null)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    contactToUpdate = new Contact()
                    {
                        Nickname = topicArray[3]
                    };
                    contactToUpdate.Status = messengerClient.GetStatus(payload);
                    contacts.Add(contactToUpdate);
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    contactToUpdate.Status = messengerClient.GetStatus(payload);
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
            messengerClient.CurrentMessages = clickedGroup.Messages;
            currentMessages = messengerClient.CurrentMessages;
            messageReceiveList.ItemsSource = currentMessages;
            currentTopic = clickedGroup.Name;
        }

        private void contactList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Contact clickedContact = e.ClickedItem as Contact;
            messengerClient.CurrentMessages = clickedContact.Messages;
            currentMessages = messengerClient.CurrentMessages;
            messageReceiveList.ItemsSource = currentMessages;
            currentTopic = $"user/{clickedContact.Nickname}";
            Debug.WriteLine(currentTopic);
        }

        private async void RefreshConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            await messengerClient.ReconnectAsync();
        }

        private async void SendText_KeyDown(object sender, KeyRoutedEventArgs e)
        {

            if (SendText.Text != "" && e.Key.Equals(VirtualKey.Enter))
            {
                await SendMessageAsync(SendText.Text);
                SendText.Text = "";
            }
        }
    }
}
