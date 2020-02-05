using System;
using System.Collections.Generic;

namespace API.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }
        public DateTime DateCreated { get; set; }
        public ICollection<Photo> Images { get; set; }
    }
}