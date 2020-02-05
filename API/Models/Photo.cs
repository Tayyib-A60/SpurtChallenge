using System;

namespace API.Models
{
    public class Photo
    {
        public int Id { get; set; } 
        public string FileName { get; set; }
        public bool IsMain { get; set; }
        public DateTime DateCreated { get; set; }
        public string PublicId { get; set; }
        public Event Event { get; set; }
        public int EventId { get; set; }
    }
}