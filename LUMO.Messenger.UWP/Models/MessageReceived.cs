using LUMO.Messenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Models
{
    public class MessageReceived
    {
        public Contact Sender { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public string GetCreatedString => Created.ToString("HH:mm:ss");
        public string Orientation { get; set; } = "Left";
    }
}
