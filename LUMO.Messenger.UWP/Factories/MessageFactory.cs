﻿using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Helpers;
using LUMO.Messenger.UWP.Models;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Factories
{
    public static class MessageFactory
    {
        public static MqttApplicationMessage CreateWillMessage(string nickname)
        {
            return new MqttApplicationMessage
            {
                Topic = $"/mschat/status/{nickname}",
                Payload = Encoding.UTF8.GetBytes("offline"),
                Retain = true
            };
        }

        public static MessageReceived CreateMessageReceived(Account user, string topic, byte[] payload)
        {
            MessageReceived message = MessageHelper.Parse(topic, payload);
            message.Orientation = user.Nickname.Equals(message.Sender.Nickname) ? MessageOrientation.Right : MessageOrientation.Left;
            return message;
        }

        public static MessageReceived CreateMessageReceived(Account user, MessageSend message, MessageOrientation orientation)
        {
            return new MessageReceived
            {
                Sender = new Contact { Nickname = user.Nickname, Status = user.Status },
                Content = message.Content.Split(": ").Last(),
                Timestamp = message.Timestamp,
                Orientation = orientation
        };
        }
    }
}
