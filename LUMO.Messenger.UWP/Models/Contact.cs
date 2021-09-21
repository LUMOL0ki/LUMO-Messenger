using LUMO.Messenger.UWP.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LUMO.Messenger.Models
{
    public enum ContatStatus
    {
        Online,
        Offline,
        Unknown
    }

    public class Contact
    {
        public string Nickname { get; set; }
        public ICollection<MessageReceived> Messages { get; set; } = new ObservableCollection<MessageReceived>();
        public MessageReceived LastMessage => null; // Messages != null || Messages.Count != 0 ? Messages.Last() : null;
        public ContatStatus Status { get; set; } = ContatStatus.Unknown;
    }
}
