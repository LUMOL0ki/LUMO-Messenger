using System;

namespace LUMO.Messenger.Client.Models
{
    public class Message
    {
        public Contact Sender { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
    }
}
