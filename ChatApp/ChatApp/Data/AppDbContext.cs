using ChatApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<GuildMembership> GuildMemberships { get; set; }
        public DbSet<ChannelMembership> ChannelMemberships { get; set; }
        public DbSet<GuildInvitation> GuildInvitations { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
    }
}
