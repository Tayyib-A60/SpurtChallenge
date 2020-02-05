using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Models;

namespace API.Core
{
    public interface ISpurtRepository
    {
        void Add<T>(T entity) where T : class;
        void Update<T>(T entity) where T : class;
        void Delete<T>(T entity) where T : class;
        Task<bool> EntityExists<T>(T entityName) where T: class;
        Task<bool> SaveAllChanges();
        Task<IEnumerable<Event>> GetEvents();
        Task<Event> GetEvent(int eventId);
        Task<Photo> GetPhoto(int id);
        Task<Photo> GetMainPhoto(int id);
        Task<User> CreateUser (UserDTO user);
        string CreateToken (User user);
        User Authenticate(string email, string password);
        void DeleteUser(User user);
        Task<bool> ForgotPassword(User user);
    }
}