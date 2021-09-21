using LUMO.Messenger.UWP.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LUMO.Messenger.Models
{
    public class Group
    {
        public string Name { get; set; }
        public ObservableCollection<MessageReceived> Messages { get; set; } = new ObservableCollection<MessageReceived>();
    }
}
