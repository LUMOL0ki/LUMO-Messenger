using LUMO.Messenger.Models;
using LUMO.Messenger.UWP.Helpers;
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

    public class MessageReceived : Message
    {
        public virtual DateTime Timestamp { get; set; }
        public string TimestampAsFormattedText => Timestamp.ToString("HH:mm:ss");
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
        public Brush Foreground
        {
            get
            {
                switch (Orientation)
                {
                    case MessageOrientation.Left:
                        return new SolidColorBrush((Color)Application.Current.Resources["PrimaryTextColor"]);
                    case MessageOrientation.Right:
                        return new SolidColorBrush((Color)Application.Current.Resources["AlternativeTextColor"]);
                    default:
                        return null;
                }
            }
        }

        public static MessageReceived Parse(string topic, byte[] payload)
        {
            return MessageHelper.Parse(topic, payload);
        }

        public override string ToString()
        {
            return $"{TimestampAsFormattedText} {Sender}: {Content}";
        }
    }
}
