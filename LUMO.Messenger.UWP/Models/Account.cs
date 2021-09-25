using LUMO.Messenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LUMO.Messenger.UWP.Models
{
    public class Account
    {
        public string Nickname { get; set; }
        public string Password { get; set; }
        public Status Status { get; set; } = Status.Unknown;
    }
}
