using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Persistence
{
    public class AppDBContext: DbContext
    {
        public AppDBContext(DbContextOptions<AppDBContext> options) : base(options) {}

        public DbSet<Event> Events { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Photo> Images { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
    }
}