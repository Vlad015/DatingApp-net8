namespace API.Dto
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public DateTime DateSent { get; set; }
        public string? RecipientUsername { get; set; }
    }
}
