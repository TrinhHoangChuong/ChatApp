using System.Linq;
using System.Collections.Generic;
using ChatApp.Models;      // chắc chắn bạn có User model ở đây
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Data
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }

        public User? GetUserById(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id);
        }

        public User? GetUserByUsername(string username)
        {
            return _context.Users.FirstOrDefault(u => u.Username == username);
        }

        public User CreateUser(string username, string password)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                PasswordHash = hashedPassword

            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public bool UpdateUser(int id, string newUsername)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;

            user.Username = newUsername;
            _context.SaveChanges();
            return true;
        }

        public bool DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;

            _context.Users.Remove(user);
            _context.SaveChanges();
            return true;
        }
    }
}
