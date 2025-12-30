using Microsoft.EntityFrameworkCore;
using SMWYG.Models;

namespace SMWYG
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Server> Servers { get; set; } = null!;
        public DbSet<ServerMember> ServerMembers { get; set; } = null!;
        public DbSet<Channel> Channels { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<InviteToken> InviteTokens { get; set; } = null!;
        public DbSet<ActiveStream> ActiveStreams { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Server>().ToTable("servers");
            modelBuilder.Entity<ServerMember>().ToTable("server_members");
            modelBuilder.Entity<Channel>().ToTable("channels");
            modelBuilder.Entity<Message>().ToTable("messages");
            modelBuilder.Entity<InviteToken>().ToTable("invite_tokens");
            modelBuilder.Entity<ActiveStream>().ToTable("active_streams");

            modelBuilder.Entity<Server>()
                .HasOne(s => s.Owner)
                .WithMany(u => u.OwnedServers)
                .HasForeignKey(s => s.OwnerId)
                .HasPrincipalKey(u => u.Id);

            // Composite primary key
            modelBuilder.Entity<ServerMember>()
                .HasKey(sm => new { sm.ServerId, sm.UserId });

            // === FIX: Explicitly map foreign key column names ===
            modelBuilder.Entity<ServerMember>()
                .Property(sm => sm.ServerId)
                .HasColumnName("server_id");

            modelBuilder.Entity<ServerMember>()
                .Property(sm => sm.UserId)
                .HasColumnName("user_id");

            // Now configure relationships properly
            modelBuilder.Entity<ServerMember>()
                .HasOne(sm => sm.Server)
                .WithMany(s => s.Members)
                .HasForeignKey(sm => sm.ServerId)
                .HasPrincipalKey(s => s.Id);

            modelBuilder.Entity<ServerMember>()
                .HasOne(sm => sm.User)
                .WithMany(u => u.ServerMemberships)
                .HasForeignKey(sm => sm.UserId)
                .HasPrincipalKey(u => u.Id);

            // Unique channel name per server
            modelBuilder.Entity<Channel>()
                .HasIndex(c => new { c.ServerId, c.Name })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}