using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddedRunResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelRunChapters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LabelRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    Chapter = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Filled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SummarySoFar = table.Column<string>(type: "TEXT", nullable: false, defaultValue: ""),
                    ImmediateContext = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelRunChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelRunChapters_BibleBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabelRunChapters_LabelRuns_LabelRunId",
                        column: x => x.LabelRunId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunChapters_BookId",
                table: "LabelRunChapters",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunChapters_LabelRunId_BookId_Chapter",
                table: "LabelRunChapters",
                columns: new[] { "LabelRunId", "BookId", "Chapter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunChapters_LabelRunId_SortOrder",
                table: "LabelRunChapters",
                columns: new[] { "LabelRunId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelRunChapters");
        }
    }
}
