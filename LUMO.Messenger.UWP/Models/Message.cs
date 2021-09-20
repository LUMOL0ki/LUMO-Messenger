using System;

namespace LUMO.Messenger.Models
{
    public class Message
    {
        public Contact Sender { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public string GetCreatedString => Created.ToString("HH:mm:ss");
    }
}
