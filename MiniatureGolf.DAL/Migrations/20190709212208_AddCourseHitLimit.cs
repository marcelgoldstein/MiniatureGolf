using Microsoft.EntityFrameworkCore.Migrations;

namespace MiniatureGolf.DAL.Migrations
{
    public partial class AddCourseHitLimit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CourseHitLimit",
                table: "Games",
                nullable: false,
                defaultValue: 7);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CourseHitLimit",
                table: "Games");
        }
    }
}
