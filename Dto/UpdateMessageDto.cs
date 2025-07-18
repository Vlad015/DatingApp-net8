namespace API.Dto
{
    public class UpdateMessageDto
    {
        public int MessageId { get; set; }
        public string NewContent { get; set; }=string.Empty;
        public DateTime Date {  get; set; }
        public bool Edited {  get; set; }
        public DateTime UpdatedDate { get; set; }

    }
}
