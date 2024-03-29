﻿using Microsoft.EntityFrameworkCore;
using MiniatureGolf.DAL.Models;

namespace MiniatureGolf.DAL;

public class MiniatureGolfContext : DbContext
{
    #region Tables
    public DbSet<Game> Games { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<TeamPlayer> TeamPlayers { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<PlayerCourseHit> PlayerCourseHits { get; set; }
    #endregion Tables

    #region ctor
    public MiniatureGolfContext(DbContextOptions<MiniatureGolfContext> options) : base(options)
    {

    }
    #endregion ctor

    #region Methods
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>()
                .HasIndex(a => a.GUID)
                .IsUnique();

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

        modelBuilder.Entity<TeamPlayer>()
            .HasKey(a => new { a.TeamId, a.PlayerId });

        modelBuilder.Entity<TeamPlayer>()
            .HasOne(a => a.Team)
            .WithMany(a => a.TeamPlayers)
            .HasForeignKey(a => a.TeamId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeamPlayer>()
            .HasOne(a => a.Player)
            .WithMany(a => a.TeamPlayers)
            .HasForeignKey(a => a.PlayerId)
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
