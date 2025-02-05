namespace API.Errors
{
    public class ApiException(int statusCode, string message, string details)
    {
        int StatusCodeCode { get; set; } = statusCode;
        public string Message { get; set;} = message;
        public string Details { get; set;}= details;
    }
    
}
