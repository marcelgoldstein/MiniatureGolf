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
        }
        #endregion Methods
    }
}
