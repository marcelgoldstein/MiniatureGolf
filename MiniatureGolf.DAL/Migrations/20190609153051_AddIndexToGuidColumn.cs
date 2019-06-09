using Microsoft.EntityFrameworkCore.Migrations;

namespace MiniatureGolf.DAL.Migrations
{
    public partial class AddIndexToGuidColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Games_GUID",
                table: "Games",
                column: "GUID",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_GUID",
                table: "Games");
        }
    }
}
