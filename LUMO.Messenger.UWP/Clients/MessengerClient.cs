using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Factories;
using LUMO.Messenger.UWP.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Clients
{
    public class MessengerClient : IDisposable
    {
        private readonly Queue<MessageSend> messageQueue = new Queue<MessageSend>();
        private readonly IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
        private IMqttClientOptions mqttClientOptions;

        public MessengerClient()
        {
            Contacts = new ObservableCollection<Contact>();
            Groups = new ObservableCollection<Group>()
            {
                new Group
                {
                    Name = "all"
                }
            };
        }

        public Account User { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 1883;
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string CurrentTopic { get; set; }

        public ObservableCollection<Contact> Contacts { get; }
        public ObservableCollection<Group> Groups { get; }
        public ObservableCollection<MessageReceived> CurrentMessages { get; set; }

        public delegate void OnConnectedHandler();
        public event OnConnectedHandler OnConnected;

        public delegate void OnConnectionFailedHandler();
        public event OnConnectionFailedHandler OnConnectionFailed;

        public delegate void OnDisconnectedHandler();
        public event OnDisconnectedHandler OnDisconnected;


        private async void OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            OnConnected?.Invoke();
            Debug.WriteLine(args.AuthenticateResult.ResultCode);
            Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} connected.");
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("/mschat/#").WithExactlyOnceQoS().Build());
            await SetStatusAsync(Status.Online);
        }

        private async void OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} disconnected {args.Reason}.");
            if (mqttClient.IsConnected)
            {
                await SetStatusAsync(Status.Offline);
            }
            OnDisconnected?.Invoke();
        }

        private async Task SetStatusAsync(Status status)
        {
            await mqttClient.PublishAsync($"/mschat/status/{User.Nickname}", status.ToString().ToLower(), true);
        }

        private MessageReceived GetMessageReceived(string topic, byte[] payload)
        {
            return MessageFactory.CreateMessageReceived(User, topic, payload);
        }

        public void OnMessageReceived(string topic, byte[] payload)
        {
            try
            {
                MessageReceived newMessage = GetMessageReceived(topic, payload);

                Debug.WriteLine(newMessage.ToString());

                if (topic.Contains("/all/"))
                {
                    Groups.FirstOrDefault(g => topic.Contains(g.Name)).Messages.Add(newMessage);
                }
                else if (topic.Contains("/user/"))
                {
                    Debug.WriteLine(topic);
                    Contact contact = Contacts.FirstOrDefault(c => newMessage.Sender.Nickname.Equals(c.Nickname));
                    if (contact == null)
                    {
                        Contacts.Add(new Contact
                        {
                            Nickname = newMessage.Sender.Nickname
                        });
                        contact = Contacts.Last();
                    }
                    contact.Messages.Add(newMessage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"When receiving message exception was invoked: {ex.Message}");
            }
        }

        public void OnStatusUpdate(string topic, byte[] payload)
        {
            string[] topicArray = topic.Split("/");

            Contact contactToUpdate = Contacts.FirstOrDefault(c => c.Nickname.Equals(topicArray.Last()));

            if (contactToUpdate == null)
            {
                contactToUpdate = new Contact()
                {
                    Nickname = topicArray.Last()
                };
                contactToUpdate.Status = GetStatus(payload);
                Contacts.Add(contactToUpdate);
            }
            else
            {
                contactToUpdate.Status = GetStatus(payload);
            }
        }

        public async Task ConnectAsync()
        {
            mqttClientOptions = new MqttClientOptionsBuilder()
                                                   .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                                                   .WithTcpServer(Host, Port)
                                                   .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                                                   .WithCommunicationTimeout(TimeSpan.FromSeconds(3))
                                                   .WithClientId(ClientId)
                                                   .WithCredentials(Username, Password)
                                                   .WithCleanSession(false)
                                                   .WithWillDelayInterval(60)
                                                   .WithWillMessage(MessageFactory.CreateWillMessage(User.Nickname))
                                                   .Build();

            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnectedAsync);
            mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnectedAsync);

            Dispose();

            await mqttClient.ConnectAsync(mqttClientOptions);
        }

        public async Task ReconnectAsync()
        {
            try
            {
                await mqttClient.ReconnectAsync();

                foreach (MessageSend messageInQueue in messageQueue)
                {
                    await SendMessageAsync(messageInQueue);
                }
                messageQueue.Clear();
            }
            catch (Exception)
            {
                OnConnectionFailed?.Invoke();
            }
        }

        public IMqttClient UseDisconnectedHandler(Func<MqttClientDisconnectedEventArgs, Task> handler)
        {
            return mqttClient.UseDisconnectedHandler(handler);
        }

        public async Task DisconnectAsync(MqttClientDisconnectReason reason)
        {
            try
            {
                Dispose();
                await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions()
                {
                    ReasonCode = reason,
                }, System.Threading.CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"When disconnecting exception was invoked: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(string message)
        {
            await SendMessageAsync(new MessageSend
            {
                Topic = $"/mschat/{CurrentTopic}/{User.Nickname}",
                Content = message
            });
        }

        public async Task SendMessageAsync(MessageSend message)
        {
            if (string.IsNullOrEmpty(message.Content))
            {
                return;
            }

            try
            {
                await mqttClient.PublishAsync(message.Topic, message.Content, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce, false);
                if (message.Topic.Contains("user") && !message.Topic.Contains($"user/{User.Nickname}"))
                {
                    CurrentMessages.Add(MessageFactory.CreateMessageReceived(User, message, MessageOrientation.Right));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"When sending message exception was invoked: {ex.Message}");
                messageQueue.Enqueue(message);
            }

        }

        public void UseApplicationMessageReceivedHandler(Func<MqttApplicationMessageReceivedEventArgs, Task> func)
        {
            mqttClient.UseApplicationMessageReceivedHandler(func);
        }

        public Status GetStatus(byte[] statusPayload)
        {
            string payloadText = Encoding.UTF8.GetString(statusPayload);
            string[] payloadParts = payloadText.Split(" ");
            string status = payloadParts.Count() > 1 ? payloadParts.Last() : payloadParts[0];
            try
            {
                return (Status)Enum.Parse(typeof(Status), status, true);
            }
            catch(Exception)
            {
                return Status.Unknown;
            }
        }

        public void Dispose()
        {
            foreach (Group group in Groups)
            {
                group.Messages.Clear();
            }
            Contacts.Clear();
            CurrentMessages = null;
        }
    }
}
