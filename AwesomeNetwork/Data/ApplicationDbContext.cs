using AwesomeNetwork.Configs;
using AwesomeNetwork.Models.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace AwesomeNetwork.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {

        public DbSet<User> Users => base.Users;
        public DbSet<Friend> Friends { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            //Database.EnsureCreated();
            Database.MigrateAsync();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration<Friend>(new FriendConfiguration());
        }
    }
}