﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiniatureGolf.DAL;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace MiniatureGolf.DAL.Migrations
{
    [DbContext(typeof(MiniatureGolfContext))]
    partial class MiniatureGolfContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.0.0-preview5.19227.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Course", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("GameId");

                    b.Property<int>("Number");

                    b.Property<int>("Par");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Game", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreationTime");

                    b.Property<DateTime>("FinishTime");

                    b.Property<string>("GUID");

                    b.Property<DateTime>("StartTime");

                    b.Property<int>("StateId");

                    b.HasKey("Id");

                    b.HasIndex("StateId");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<int>("Number");

                    b.Property<int>("TeamId");

                    b.HasKey("Id");

                    b.HasIndex("TeamId");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.PlayerCourseHit", b =>
                {
                    b.Property<int>("CourseId");

                    b.Property<int>("PlayerId");

                    b.Property<int?>("HitCount");

                    b.HasKey("CourseId", "PlayerId");

                    b.HasIndex("PlayerId");

                    b.ToTable("PlayerCourseHits");
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.State", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.ToTable("States");
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Team", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("CurrentCourseNumber");

                    b.Property<int>("GameId");

                    b.Property<string>("Name");

                    b.Property<int>("Number");

                    b.HasKey("Id");

                    b.HasIndex("GameId");

                    b.ToTable("Teams");
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Course", b =>
                {
                    b.HasOne("MiniatureGolf.DAL.Models.Game", "Game")
                        .WithMany("Courses")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Game", b =>
                {
                    b.HasOne("MiniatureGolf.DAL.Models.State", "State")
                        .WithMany("Games")
                        .HasForeignKey("StateId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Player", b =>
                {
                    b.HasOne("MiniatureGolf.DAL.Models.Team", "Team")
                        .WithMany("Players")
                        .HasForeignKey("TeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.PlayerCourseHit", b =>
                {
                    b.HasOne("MiniatureGolf.DAL.Models.Course", "Course")
                        .WithMany("PlayerCourseHits")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MiniatureGolf.DAL.Models.Player", "Player")
                        .WithMany("PlayerCourseHits")
                        .HasForeignKey("PlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MiniatureGolf.DAL.Models.Team", b =>
                {
                    b.HasOne("MiniatureGolf.DAL.Models.Game", "Game")
                        .WithMany("Teams")
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
