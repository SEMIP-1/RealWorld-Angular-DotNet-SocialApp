namespace api.Interfaces
{
    public class MessageInterface
    {
        public string content { get; set; } = null!;
        public string senderId { get; set; } = null!;
        public string receiverId { get; set; } = null!;
    }
}
