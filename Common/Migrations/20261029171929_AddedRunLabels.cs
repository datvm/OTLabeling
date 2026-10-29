using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddedRunLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabelRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RunKey = table.Column<string>(type: "TEXT", nullable: false),
                    Step = table.Column<string>(type: "TEXT", nullable: false),
                    Books = table.Column<string>(type: "TEXT", nullable: false),
                    Chapters = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    ImmediateContext = table.Column<string>(type: "TEXT", nullable: false),
                    Rpm = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerseTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "BINARY"),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerseTags_VerseTags_ParentId",
                        column: x => x.ParentId,
                        principalTable: "VerseTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerseTagResolutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VerseId = table.Column<int>(type: "INTEGER", nullable: false),
                    RunAId = table.Column<int>(type: "INTEGER", nullable: false),
                    RunBId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChosenRunId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTagResolutions", x => x.Id);
                    table.CheckConstraint("CK_VerseTagResolution_Choice", "[RunAId] <> [RunBId] AND ([ChosenRunId] = [RunAId] OR [ChosenRunId] = [RunBId])");
                    table.ForeignKey(
                        name: "FK_VerseTagResolutions_BibleVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "BibleVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerseTagResolutions_LabelRuns_ChosenRunId",
                        column: x => x.ChosenRunId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerseTagResolutions_LabelRuns_RunAId",
                        column: x => x.RunAId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerseTagResolutions_LabelRuns_RunBId",
                        column: x => x.RunBId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabelRunTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LabelRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    VerseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VerseTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelRunTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabelRunTags_BibleVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "BibleVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabelRunTags_LabelRuns_LabelRunId",
                        column: x => x.LabelRunId,
                        principalTable: "LabelRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabelRunTags_VerseTags_VerseTagId",
                        column: x => x.VerseTagId,
                        principalTable: "VerseTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerseTagMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VerseId = table.Column<int>(type: "INTEGER", nullable: false),
                    VerseTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagIndex = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTagMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerseTagMappings_BibleVerses_VerseId",
                        column: x => x.VerseId,
                        principalTable: "BibleVerses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerseTagMappings_VerseTags_VerseTagId",
                        column: x => x.VerseTagId,
                        principalTable: "VerseTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabelRuns_RunKey",
                table: "LabelRuns",
                column: "RunKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTags_LabelRunId_VerseId_TagIndex",
                table: "LabelRunTags",
                columns: new[] { "LabelRunId", "VerseId", "TagIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTags_LabelRunId_VerseId_VerseTagId",
                table: "LabelRunTags",
                columns: new[] { "LabelRunId", "VerseId", "VerseTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTags_VerseId",
                table: "LabelRunTags",
                column: "VerseId");

            migrationBuilder.CreateIndex(
                name: "IX_LabelRunTags_VerseTagId",
                table: "LabelRunTags",
                column: "VerseTagId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagMappings_VerseId_TagIndex",
                table: "VerseTagMappings",
                columns: new[] { "VerseId", "TagIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagMappings_VerseId_VerseTagId",
                table: "VerseTagMappings",
                columns: new[] { "VerseId", "VerseTagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagMappings_VerseTagId",
                table: "VerseTagMappings",
                column: "VerseTagId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagResolutions_ChosenRunId",
                table: "VerseTagResolutions",
                column: "ChosenRunId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagResolutions_RunAId",
                table: "VerseTagResolutions",
                column: "RunAId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagResolutions_RunBId",
                table: "VerseTagResolutions",
                column: "RunBId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagResolutions_VerseId",
                table: "VerseTagResolutions",
                column: "VerseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerseTags_Name",
                table: "VerseTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerseTags_ParentId",
                table: "VerseTags",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabelRunTags");

            migrationBuilder.DropTable(
                name: "VerseTagMappings");

            migrationBuilder.DropTable(
                name: "VerseTagResolutions");

            migrationBuilder.DropTable(
                name: "VerseTags");

            migrationBuilder.DropTable(
                name: "LabelRuns");
        }
    }
}
