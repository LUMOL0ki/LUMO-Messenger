using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Helpers
{
    public static class MessageHelper
    {
        public static DateTime GetDateTime(string datetime)
        {
            if (!DateTime.TryParse(datetime, out DateTime timestamp))
            {
                return DateTime.Now;
            }
            return timestamp;
        }

        public static MessageReceived Parse(string topic, byte[] payload)
        {

            Contact sender = new Contact { Nickname = topic.Split("/").Last() };
            string content;
            DateTime timestamp;
            string payloadText = Encoding.UTF8.GetString(payload);
            ICollection<string> payloadParts;

            if (payloadText.Contains("Aktuální čas: "))
            {
                payloadParts = payloadText.Split("Aktuální čas: ");
                content = payloadParts.First();
                timestamp = GetDateTime(payloadParts.Last());
            }
            else
            {
                payloadParts = payloadText.Split(": ");
                content = payloadParts.Last();
                timestamp = GetDateTime(payloadParts.First());
            }

            return new MessageReceived
            {
                Sender = sender,
                Content = content,
                Timestamp = timestamp
            };
        }

    }
}
