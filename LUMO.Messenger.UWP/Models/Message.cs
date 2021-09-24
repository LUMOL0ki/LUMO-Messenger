using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LUMO.Messenger.Models
{
    public class Message
    {
        public Contact Sender { get; set; }
        public virtual string Content { get; set; }

        public override string ToString()
        {
            return $"{Sender}: {Content}";
        }
    }
}
