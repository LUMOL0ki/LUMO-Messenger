namespace LUMO.Messenger.Client.Models
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
        public ContatStatus Status { get; set; } = ContatStatus.Unknown;
    }
}
