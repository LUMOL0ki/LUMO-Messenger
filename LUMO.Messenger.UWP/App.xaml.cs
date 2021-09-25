using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Clients;
using LUMO.Messenger.UWP.Factories;
using LUMO.Messenger.UWP.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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

namespace LUMO.Messenger.UWP
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private readonly string host = "pcfeib425t.vsb.cz";
        private readonly int port = 1883;
        private readonly string clientId = "MOR0157";
        private readonly string username = "mobilni";
        private readonly string password = "Systemy";

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        public MessengerClient MessengerClient { get; private set; }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            MessengerClient = new MessengerClient
            {
                Host = host,
                Port = port,
                ClientId = clientId,
                User = new Account
                {
                    Nickname = clientId,
                    Status = Status.Online
                },
                Username = username,
                Password = password
            };

            MessengerClient.UseApplicationMessageReceivedHandler(async amr =>
            {
                string topic = amr.ApplicationMessage.Topic;
                switch (topic)
                {
                    case string message when message.Contains("/all/") || message.Contains("/user/"):
                        await MessageReceivedAsync(topic, amr.ApplicationMessage.Payload);
                        break;
                    case string status when status.Contains("/status/"):
                        await StatusUpdateAsync(topic, amr.ApplicationMessage.Payload);
                        break;
                }
            });

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(SignInPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private async Task MessageReceivedAsync(string topic, byte[] payload)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MessengerClient.OnMessageReceived(topic, payload);
            });
        }

        private async Task StatusUpdateAsync(string topic, byte[] payload)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MessengerClient.OnStatusUpdate(topic, payload);
            });
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            try
            {
                await MessengerClient.DisconnectAsync(MqttClientDisconnectReason.NormalDisconnection);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
