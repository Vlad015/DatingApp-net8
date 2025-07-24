namespace API.Dto
{
    public class DeletePhotoWithReasonDto
    {
        public string Username { get; set; }
        public int PhotoId { get; set; }
        public string Reason { get; set; }
    }
}
