using System.Collections.Generic;
using API.Models;

namespace API.DTOs
{
    public class EventDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }
        public ICollection<Photo> Images { get; set; }
    }
}