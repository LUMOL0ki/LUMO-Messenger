using LUMO.Messenger.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace LUMO.Messenger.UWP.Models
{
    public enum MessageOrientation
    {
        Left,
        Right
    }

    public class MessageReceived
    {
        public Contact Sender { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public string CreatedText => Created.ToString("HH:mm:ss");
        public MessageOrientation Orientation { get; set; } = MessageOrientation.Left;
        public string OrientationText => Orientation.ToString();
        public Brush Background
        {
            get
            {
                switch (Orientation)
                {
                    case MessageOrientation.Left:
                        return new SolidColorBrush((Color)Application.Current.Resources["SecondaryBackgroundColor"]);
                    case MessageOrientation.Right:
                        return new SolidColorBrush((Color)Application.Current.Resources["PrimaryButtonColor"]);
                    default:
                        return null;
                }
            }
        }

        public override string ToString()
        {
            return $"{Sender} {CreatedText}: {Content}";
        }
    }
}
