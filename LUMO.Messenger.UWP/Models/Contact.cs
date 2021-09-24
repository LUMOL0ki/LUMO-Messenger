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
        public MessageReceived LastMessage => Messages != null && Messages.Count != 0 ? Messages.Last() : null;
        public ContatStatus Status { get; set; } = ContatStatus.Unknown;
        public Brush StatusColor
        {
            get
            {
                switch (Status) 
                {
                    /*
                    case ContatStatus.Online:
                        return new SolidColorBrush((Color)Application.Current.Resources["OnlineStatusColor"]);
                    case ContatStatus.Offline:
                        return new SolidColorBrush((Color)Application.Current.Resources["OfflineStatusColor"]);
                    case ContatStatus.Unknown:
                        return new SolidColorBrush((Color)Application.Current.Resources["UnknownStatusColor"]);
                    */
                    default:
                        return null;
                }
            }
        }

        public void SetStatusFromString(string status)
        {
            Status = (ContatStatus)Enum.Parse(typeof(ContatStatus), status, true);
        }
    }
}
