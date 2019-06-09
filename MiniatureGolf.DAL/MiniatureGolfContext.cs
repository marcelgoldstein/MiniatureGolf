using Microsoft.EntityFrameworkCore;
using MiniatureGolf.DAL.Models;

namespace MiniatureGolf.DAL
{
    public class MiniatureGolfContext : DbContext
    {
        #region Tables
        public DbSet<Game> Games { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<PlayerCourseHit> PlayerCourseHits { get; set; }
        public DbSet<State> States { get; set; }
        #endregion Tables

        #region ctor
        public MiniatureGolfContext(DbContextOptions options) : base(options)
        {

        }
        #endregion ctor

        #region Methods
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ForNpgsqlUseIdentityByDefaultColumns();

            modelBuilder.Entity<Game>()
                .HasMany(a => a.Teams)
                .WithOne(a => a.Game)
                .HasForeignKey(a => a.GameId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Game>()
                .HasMany(a => a.Courses)
                .WithOne(a => a.Game)
                .HasForeignKey(a => a.GameId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Game>()
                .HasOne(a => a.State)
                .WithMany(a => a.Games)
                .HasForeignKey(a => a.StateId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Team>()
                .HasMany(a => a.Players)
                .WithOne(a => a.Team)
                .HasForeignKey(a => a.TeamId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerCourseHit>()
                .HasKey(a => new { a.CourseId, a.PlayerId });

            modelBuilder.Entity<PlayerCourseHit>()
                .HasOne(a => a.Course)
                .WithMany(a => a.PlayerCourseHits)
                .HasForeignKey(a => a.CourseId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PlayerCourseHit>()
                .HasOne(a => a.Player)
                .WithMany(a => a.PlayerCourseHits)
                .HasForeignKey(a => a.PlayerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

        }
        #endregion Methods
    }
}
