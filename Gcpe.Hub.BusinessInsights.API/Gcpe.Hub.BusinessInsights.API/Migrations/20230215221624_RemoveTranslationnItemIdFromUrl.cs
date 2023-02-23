using Microsoft.EntityFrameworkCore.Migrations;

namespace Gcpe.Hub.BusinessInsights.API.Migrations
{
    public partial class RemoveTranslationnItemIdFromUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TranslationItemId",
                table: "Urls");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TranslationItemId",
                table: "Urls",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
