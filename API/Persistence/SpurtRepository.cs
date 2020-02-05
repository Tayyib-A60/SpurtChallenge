using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.Core;
using API.DTOs;
using API.Extension;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Persistence
{
    public class SpurtRepository : ISpurtRepository
    {
        private AppDBContext _context { get; }
        private IConfiguration _configuration { get; }
        private AppSettings _appSettings { get; }
        public SpurtRepository (AppDBContext context, IOptions<AppSettings> appSettings, IConfiguration configuration)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _configuration = configuration;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Added;
        }
        public void Update<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Modified;
        }
        public void Delete<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Deleted;
        }
        public async Task<IEnumerable<Event>> GetEvents()
        {
            return await _context.Events
                            .Include(e => e.Images)
                            .ToListAsync();
        }
        public async Task<Event> GetEvent(int eventId)
        {
            var singleEvent = await _context.Events
                            .Include(e => e.Images)
                            .FirstOrDefaultAsync(e => e.Id == eventId);
            return singleEvent;
        }
        public User Authenticate (string email, string password) {
            if (string.IsNullOrEmpty (email) || string.IsNullOrEmpty (password))
                return null;
            var user = _context.Users.SingleOrDefault (u => u.Email == email);
            if (user == null)
                return null;
            if (!VerifyPasswordHash (password, user.PasswordHash, user.PasswordSalt))
                return null;
            return user;
        }
        public async Task<User> CreateUser (UserDTO userDTO)
        {
            var user = new User{
                Name = userDTO.Name,
                Email = userDTO.Email,
                DateRegistered = DateTime.Now,
                Role = Role.Admin
            };

            if (user == null)
                throw new NullReferenceException ("User cannot be null");
            if (string.IsNullOrWhiteSpace (userDTO.Password))
                throw new ArgumentNullException ("Password is Required");
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash (userDTO.Password, out passwordHash, out passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _context.Users.Add(user);
            await _context.SaveChangesAsync ();

            return user;
        }
        public async Task<bool> EntityExists<T>(T entityName) where T: class
        {
            if(entityName is User) {
                var user = entityName as User;
                if(await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower())) {
                    return true;
                }
                return false;
            } else if(entityName is Subscriber) {
                var sub = entityName as Subscriber;
                if(await _context.Subscribers.AnyAsync(u => u.Email == sub.Email)) {
                    return true;
                }
                return false;
            }
            return false;
        }

        public string CreateToken (User user) {
            var tokenHandler = new JwtSecurityTokenHandler ();
            var key = Encoding.ASCII.GetBytes (_appSettings.Secret);
            var sub = new ClaimsIdentity ();
            var tokenDescriptor = new SecurityTokenDescriptor {
                Subject = new ClaimsIdentity (new Claim[] {
                new Claim (ClaimTypes.NameIdentifier, user.Email),
                new Claim (ClaimTypes.Name, user.Name),
                new Claim (ClaimTypes.Role, user.Role.ToString ()),
                new Claim (ClaimTypes.GroupSid, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes (120),
                SigningCredentials = new SigningCredentials (new SymmetricSecurityKey (key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken (tokenDescriptor);
            var tokenString = tokenHandler.WriteToken (token);
            return tokenString;
        }
        private static void CreatePasswordHash (string password, out byte[] passwordHash, out byte[] passwordSalt) {
            if (password == null) throw new ArgumentNullException ("password");
            if (string.IsNullOrWhiteSpace (password)) throw new ArgumentException ("value cannot be empty or whitespace, on string is allowed ", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512 ()) {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash (System.Text.Encoding.UTF8.GetBytes (password));
            }
        }
        private static bool VerifyPasswordHash (string password, byte[] storedHash, byte[] storedSalt) {
            if (password == null) throw new ArgumentNullException ("password");
            if (string.IsNullOrWhiteSpace (password)) throw new ArgumentException ("value cannot be empty or whitespace, only string is allowed ", "password");
            if (storedHash.Length != 64) throw new ArgumentException ("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException ("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512 (storedSalt)) {
                var computedHash = hmac.ComputeHash (System.Text.Encoding.UTF8.GetBytes (password));
                for (int i = 0; i < computedHash.Length; i++) {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }
            return true;
        }

        Task<bool> ISpurtRepository.EntityExists<T>(T entityName)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SaveAllChanges()
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<Event>> ISpurtRepository.GetEvents()
        {
            throw new NotImplementedException();
        }

        public Task<Photo> GetPhoto(int id)
        {
            throw new NotImplementedException();
        }

        public Task<Photo> GetMainPhoto(int id)
        {
            throw new NotImplementedException();
        }

        public void DeleteUser(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ForgotPassword(User user)
        {
            throw new NotImplementedException();
        }
    }
}