using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Factories
{
    internal static class MessageFactory
    {
        internal static MqttApplicationMessage CreateWillMessage(string nickname)
        {
            return new MqttApplicationMessage
            {
                Topic = $"/mschat/status/{nickname}",
                Payload = Encoding.UTF8.GetBytes("offline"),
                Retain = true
            };
        }
    }
}
