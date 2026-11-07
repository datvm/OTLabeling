using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddedTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelRunTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LabelRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelRunTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelRunTopics_LabelRuns_LabelRunId",
                        column: x => x.LabelRunId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Question = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerseTopicAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VerseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Answer = table.Column<string>(type: "TEXT", nullable: false),
                    Probability = table.Column<decimal>(type: "TEXT", nullable: false),
                    LabelRunId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTopicAnswers", x => x.Id);
                    table.CheckConstraint("CK_VerseTopicAnswer_Answer", "[Answer] IN ('Yes', 'No', 'Ambiguous')");
                    table.ForeignKey(
                        name: "FK_VerseTopicAnswers_BibleVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "BibleVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerseTopicAnswers_LabelRuns_LabelRunId",
                        column: x => x.LabelRunId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabelRunTopicAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LabelRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    VerseId = table.Column<int>(type: "INTEGER", nullable: false),
                    LabelRunTopicId = table.Column<int>(type: "INTEGER", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: false),
                    Probability = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelRunTopicAnswers", x => x.Id);
                    table.CheckConstraint("CK_LabelRunTopicAnswer_Answer", "[Answer] IN ('Yes', 'No', 'Ambiguous')");
                    table.ForeignKey(
                        name: "FK_LabelRunTopicAnswers_BibleVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "BibleVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabelRunTopicAnswers_LabelRunTopics_LabelRunTopicId",
                        column: x => x.LabelRunTopicId,
                        principalTable: "LabelRunTopics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabelRunTopicAnswers_LabelRuns_LabelRunId",
                        column: x => x.LabelRunId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTopicAnswers_LabelRunId_VerseId_LabelRunTopicId",
                table: "LabelRunTopicAnswers",
                columns: new[] { "LabelRunId", "VerseId", "LabelRunTopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTopicAnswers_LabelRunTopicId",
                table: "LabelRunTopicAnswers",
                column: "LabelRunTopicId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTopicAnswers_VerseId",
                table: "LabelRunTopicAnswers",
                column: "VerseId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTopics_LabelRunId_Code",
                table: "LabelRunTopics",
                columns: new[] { "LabelRunId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTopics_LabelRunId_SortOrder",
                table: "LabelRunTopics",
                columns: new[] { "LabelRunId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Code",
                table: "Topics",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerseTopicAnswers_Code",
                table: "VerseTopicAnswers",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTopicAnswers_LabelRunId",
                table: "VerseTopicAnswers",
                column: "LabelRunId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTopicAnswers_VerseId_Code",
                table: "VerseTopicAnswers",
                columns: new[] { "VerseId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelRunTopicAnswers");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "VerseTopicAnswers");

            migrationBuilder.DropTable(
                name: "LabelRunTopics");
        }
    }
}
