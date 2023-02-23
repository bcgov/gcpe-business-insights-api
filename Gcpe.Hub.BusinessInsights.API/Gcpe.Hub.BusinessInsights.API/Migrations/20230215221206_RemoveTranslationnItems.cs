using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gcpe.Hub.BusinessInsights.API.Migrations
{
    public partial class RemoveTranslationnItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Urls_TranslationItems_TranslationItemId",
                table: "Urls");

            migrationBuilder.DropTable(
                name: "TranslationItems");

            migrationBuilder.DropIndex(
                name: "IX_Urls_TranslationItemId",
                table: "Urls");

            migrationBuilder.AddColumn<int>(
                name: "NewsReleaseItemId",
                table: "Urls",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Urls_NewsReleaseItemId",
                table: "Urls",
                column: "NewsReleaseItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Urls_NewsReleaseItems_NewsReleaseItemId",
                table: "Urls",
                column: "NewsReleaseItemId",
                principalTable: "NewsReleaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Urls_NewsReleaseItems_NewsReleaseItemId",
                table: "Urls");

            migrationBuilder.DropIndex(
                name: "IX_Urls_NewsReleaseItemId",
                table: "Urls");

            migrationBuilder.DropColumn(
                name: "NewsReleaseItemId",
                table: "Urls");

            migrationBuilder.CreateTable(
                name: "TranslationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ministry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PublishDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TranslationItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Urls_TranslationItemId",
                table: "Urls",
                column: "TranslationItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Urls_TranslationItems_TranslationItemId",
                table: "Urls",
                column: "TranslationItemId",
                principalTable: "TranslationItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
