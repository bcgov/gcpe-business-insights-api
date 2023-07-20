using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gcpe.Hub.BusinessInsights.API.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsReleaseItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Headline = table.Column<string>(type: "TEXT", nullable: true),
                    Ministry = table.Column<string>(type: "TEXT", nullable: true),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseType = table.Column<int>(type: "INTEGER", nullable: false),
                    PublishDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsReleaseItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Urls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Href = table.Column<string>(type: "TEXT", nullable: true),
                    PublishDateTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    NewsReleaseItemId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Urls_NewsReleaseItems_NewsReleaseItemId",
                        column: x => x.NewsReleaseItemId,
                        principalTable: "NewsReleaseItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Urls_NewsReleaseItemId",
                table: "Urls",
                column: "NewsReleaseItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Urls");

            migrationBuilder.DropTable(
                name: "NewsReleaseItems");
        }
    }
}
