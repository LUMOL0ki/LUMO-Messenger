using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Clients;
using LUMO.Messenger.UWP.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
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
        private MessengerClient messengerClient;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            messengerClient = ((App)Application.Current).MessengerClient;
            messengerClient.OnConnected += MessengerClient_OnConnected;
            messengerClient.OnDisconnected += MessengerClient_OnDisconnected;
            try
            {
                await messengerClient.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"When connecting exception was invoked: {ex.Message}");
            }
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            messengerClient.OnDisconnected -= MessengerClient_OnConnected;
            messengerClient.OnDisconnected -= MessengerClient_OnDisconnected;
            if (messengerClient.IsConnected)
            {
                await messengerClient.DisconnectAsync(MqttClientDisconnectReason.NormalDisconnection);
            }
            base.OnNavigatedFrom(e);
        }

        private async void MessengerClient_OnConnected()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                messengerClient.Dispose();
                LoadingGrid.Visibility = Visibility.Collapsed;
                Loading.IsActive = false;
            });
        }

        private async void MessengerClient_OnDisconnected()
        {
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    LoadingGrid.Visibility = Visibility.Visible;
                    Loading.IsActive = true;
                });
                await messengerClient.ReconnectAsync();
            }
            catch (Exception)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    messengerClient.Dispose();
                    GoBack();
                });
            }
        }

        private async Task MessageSendAsync(string message)
        {
            if (SendText.Text != "")
            {
                await messengerClient.SendMessageAsync(message);
                SendText.Text = "";
            }
        }

        private void GoBack()
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await MessageSendAsync(SendText.Text);
        }

        private void GroupList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Group clickedGroup = e.ClickedItem as Group;
            contactList.SelectedIndex = -1;
            messengerClient.CurrentMessages = clickedGroup.Messages;
            messageReceiveList.ItemsSource = messengerClient.CurrentMessages;
            messengerClient.CurrentTopic = clickedGroup.Name;
        }

        private void ContactList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Contact clickedContact = e.ClickedItem as Contact;
            groupList.SelectedIndex = -1;
            messengerClient.CurrentMessages = clickedContact.Messages;
            messageReceiveList.ItemsSource = messengerClient.CurrentMessages;
            messengerClient.CurrentTopic = $"user/{clickedContact.Nickname}";
        }

        private async void SendText_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == VirtualKey.Enter)
            {
                await MessageSendAsync(SendText.Text);
            }
        }

        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchText.Text))
            {
                groupList.ItemsSource = messengerClient.Groups;
                contactList.ItemsSource = messengerClient.Contacts;
            }
            else
            {
                groupList.ItemsSource = messengerClient.Groups.Where(g => g.Name.Contains(SearchText.Text, StringComparison.CurrentCultureIgnoreCase));
                contactList.ItemsSource = messengerClient.Contacts.Where(c => c.Nickname.Contains(SearchText.Text, StringComparison.CurrentCultureIgnoreCase));
            }
        }
    }
}
