namespace API.Models
{
    public class Message
    {
        public string FromName { get; set; }
        public string FromEmail { get; set; }
        public string ToName { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string PlainContent { get; set; }
        public string HtmlContent { get; set; }
    }
}