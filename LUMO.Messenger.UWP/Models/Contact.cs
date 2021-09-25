using LUMO.Messenger.UWP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace LUMO.Messenger.Models
{
    public class Contact
    {
        public Contact()
        {
            Messages.CollectionChanged += Messages_CollectionChanged;
        }

        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(Messages != null && Messages.Count != 0)
            {
                LastMessage = Messages.Last();
            }
        }

        public string Nickname { get; set; }
        public ObservableCollection<MessageReceived> Messages { get; set; } = new ObservableCollection<MessageReceived>();
        public MessageReceived LastMessage { get; private set; }
        public Status Status { get; set; } = Status.Unknown;
        public Brush StatusColor
        {
            get
            {
                switch (Status) 
                {
                    case Status.Online:
                        return new SolidColorBrush((Color)Application.Current.Resources["OnlineStatusColor"]);
                    case Status.Offline:
                        return new SolidColorBrush((Color)Application.Current.Resources["OfflineStatusColor"]);
                    case Status.Unknown:
                        return new SolidColorBrush((Color)Application.Current.Resources["UnknownStatusColor"]);
                    default:
                        return null;
                }
            }
        }

        public void SetStatusFromString(string status)
        {
            Status = (Status)Enum.Parse(typeof(Status), status, true);
        }
    }
}
