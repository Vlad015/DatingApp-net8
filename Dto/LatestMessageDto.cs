namespace API.Dto
{
    public class LatestMessageDto
    {
        public string? Username { get; set; }
        public string? PhotoUrl { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTime? LastMessageSent { get; set; }
    }
}
