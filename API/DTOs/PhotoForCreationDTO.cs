using System;
using Microsoft.AspNetCore.Http;

namespace API.DTOs
{
    public class PhotoForCreationDTO
    {
        public int Id { get; set; } 
        public string FileName { get; set; }
        public int EventId { get; set; }
        public bool IsMain { get; set; }
        public DateTime DateCreated { get; set; }
        public IFormFile File { get; set; }
        public string PublicId { get; set; }

        public PhotoForCreationDTO()
        {
            DateCreated = DateTime.Now;
        }
    }
}