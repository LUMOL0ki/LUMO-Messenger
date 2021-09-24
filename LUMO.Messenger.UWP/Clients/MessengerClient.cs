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
    public class MessengerClient
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
            //CurrentMessages = Groups.FirstOrDefault().Messages;
        }

        public Contact User { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 1883;
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string CurrentTopic { get; set; }

        public ObservableCollection<Contact> Contacts;
        public ObservableCollection<Group> Groups;
        public ObservableCollection<MessageReceived> CurrentMessages;

        private async void OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            Debug.WriteLine(args.AuthenticateResult.ResultCode);
            Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} connected.");
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("/mschat/#").Build());
        }

        private async void OnDisconectedAsync(MqttClientDisconnectedEventArgs args)
        {
            if(args.Reason != MqttClientDisconnectReason.NormalDisconnection)
            {
                await ReconnectAsync();
            }
            Debug.WriteLine($"{DateTime.Now.ToShortTimeString()} disconnected.");
        }

        private async Task SetStatusAsync(ContatStatus status)
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
                Debug.WriteLine(ex.Message);
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
                                                   .WithWillDelayInterval(60)
                                                   .WithWillMessage(MessageFactory.CreateWillMessage(User.Nickname))
                                                   .Build();

            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnectedAsync);
            mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconectedAsync);
            

            await mqttClient.ConnectAsync(mqttClientOptions);
            await SetStatusAsync(ContatStatus.Online);
        }

        public async Task ReconnectAsync()
        {
            try
            {
                await mqttClient.ReconnectAsync();
                await SetStatusAsync(ContatStatus.Online);
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("/mschat/#").Build());

                foreach (MessageSend messageInQueue in messageQueue)
                {
                    await SendMessageAsync(messageInQueue);
                }
                messageQueue.Clear();
            }
            catch (Exception)
            {
                await ReconnectAsync();
            }
        }

        public IMqttClient UseDisconnectedHandler(Func<MqttClientDisconnectedEventArgs, Task> handler)
        {
            return mqttClient.UseDisconnectedHandler(handler);
        }

        public async Task DisconnectAsync(MqttClientDisconnectReason reason)
        {
            await SetStatusAsync(ContatStatus.Offline);
            await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions()
            {
                ReasonCode = reason,
            }, System.Threading.CancellationToken.None);
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
                await mqttClient.PublishAsync(message.Topic, message.Content, true);
                if (message.Topic.Contains("user") && !message.Topic.Contains($"user/{User.Nickname}"))
                {
                    CurrentMessages.Add(MessageFactory.CreateMessageReceived(User, message, MessageOrientation.Right));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                messageQueue.Enqueue(message);
            }

        }

        public void UseApplicationMessageReceivedHandler(Func<MqttApplicationMessageReceivedEventArgs, Task> func)
        {
            mqttClient.UseApplicationMessageReceivedHandler(func);
        }

        public ContatStatus GetStatus(byte[] statusPayload)
        {
            string payloadText = Encoding.UTF8.GetString(statusPayload);
            string[] payloadParts = payloadText.Split(" ");
            string status = payloadParts.Count() > 1 ? payloadParts.Last() : payloadParts[0];
            try
            {
                return (ContatStatus)Enum.Parse(typeof(ContatStatus), status, true);
            }
            catch(Exception)
            {
                return ContatStatus.Unknown;
            }
        }
    }
}
