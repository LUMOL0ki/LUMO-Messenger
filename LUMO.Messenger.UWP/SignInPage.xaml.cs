using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Clients;
using LUMO.Messenger.UWP.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace LUMO.Messenger.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SignInPage : Page
    {
        private MessengerClient messengerClient;

        public SignInPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            messengerClient = ((App)Application.Current).MessengerClient;
            messengerClient.OnConnected += MessengerClient_OnConnected;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            messengerClient.OnConnected -= MessengerClient_OnConnected;
            ErrorText.Text = "";
        }

        private async void MessengerClient_OnConnected()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Frame.Navigate(typeof(MainPage));
            });
        }

        private void NewAccountButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SignUpPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
        }

        private async Task SignIn()
        {
            messengerClient.ClientId = nicknameText.Text;
            messengerClient.Username = nicknameText.Text;
            messengerClient.Password = passwordText.Password;
            messengerClient.User = new Account
            {
                Nickname = messengerClient.ClientId,
                Status = Status.Online
            };
            signInGrid.Visibility = Visibility.Collapsed;
            loading.IsActive = true;
            try
            {
                await messengerClient.ConnectAsync();
            }
            catch (Exception)
            {
                signInGrid.Visibility = Visibility.Visible;
                loading.IsActive = false;
                ErrorText.Text = "Sign in failed";
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            await SignIn();
        }

        private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                await SignIn();
            }
        }
    }
}
