using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Entities.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BibleBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BibleVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    Chapter = table.Column<int>(type: "INTEGER", nullable: false),
                    VerseNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    AccumulatedVerseNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BibleVerses_BibleBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_Name",
                table: "BibleBooks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleVerses_BookId_AccumulatedVerseNumber",
                table: "BibleVerses",
                columns: new[] { "BookId", "AccumulatedVerseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleVerses_BookId_Chapter_VerseNumber",
                table: "BibleVerses",
                columns: new[] { "BookId", "Chapter", "VerseNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BibleVerses");

            migrationBuilder.DropTable(
                name: "BibleBooks");
        }
    }
}
