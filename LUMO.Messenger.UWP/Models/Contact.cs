using LUMO.Messenger.UWP.Models;
using System;
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
        public ObservableCollection<MessageReceived> Messages { get; set; } = new ObservableCollection<MessageReceived>();
        public MessageReceived LastMessage => null; // Messages != null || Messages.Count != 0 ? Messages.Last() : null;
        public ContatStatus Status { get; set; } = ContatStatus.Unknown;

        public void SetStatus(string status)
        {
            Status = (ContatStatus)Enum.Parse(typeof(ContatStatus), status, true);
        }
    }
}
