namespace API.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime DateSent { get; set; }
        public int AppUserId { get; set; } 
        public AppUser? Recipient { get; set; }
        public string? RecipientUsername { get; set; }
        public string? SenderUsername { get; set; }
    }
}
