using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MiniatureGolf.DAL.Models;
using System;
using System.Collections.Generic;
using System.Text;

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

        public void EnsureDBExistanceWithInitialData()
        {
            if ((this.GetService<IDatabaseCreator>() as RelationalDatabaseCreator).Exists() == false)
            {
                this.Database.Migrate();

                // inital data inserts here
                this.States.Add(new State() { Id = 1, Text = "Created" });
                this.States.Add(new State() { Id = 5, Text = "Configuring" });
                this.States.Add(new State() { Id = 10, Text = "Running" });
                this.States.Add(new State() { Id = 15, Text = "Finished" });

                this.SaveChanges();
            }
        }
        #endregion Methods
    }
}
