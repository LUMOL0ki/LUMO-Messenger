using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Models
{
    public class MessageSend
    {
        public MessageSend()
        {
            Created = DateTime.Now;
        }

        public string Content {  get; set; }
        public DateTime Created { get; }
    }
}
