using LUMO.Messenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Models
{
    public class MessageSend : Message
    {
        private string content;

        public MessageSend()
        {
            Timestamp = DateTime.Now;
        }

        public string Topic { get; set; }
        public override string Content
        {
            get
            {
                return content;
            }

            set
            {
                content = $"{Timestamp:dd.MM.yyyy HH:mm:ss}: {value}";
            }
        }
        public DateTime Timestamp { get; }

        public override string ToString()
        {
            return $"{Timestamp:dd.MM.yyyy HH:mm:ss} {Topic}: {Content}";
        }
    }
}
