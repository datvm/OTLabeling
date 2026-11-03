using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class ReinitializedMigrations : Migration
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
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookLabels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookLabels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookOrders", x => x.Id);
                });

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
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Subclassifier = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "", collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTags", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "BookLabelMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookLabelId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookLabelMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookLabelMappings_BibleBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookLabelMappings_BookLabels_BookLabelId",
                        column: x => x.BookLabelId,
                        principalTable: "BookLabels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookOrderItems_BibleBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookOrderItems_BookOrders_BookOrderId",
                        column: x => x.BookOrderId,
                        principalTable: "BookOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerseTagParents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentTagId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerseTagParents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerseTagParents_VerseTags_ParentTagId",
                        column: x => x.ParentTagId,
                        principalTable: "VerseTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerseTagParents_VerseTags_TagId",
                        column: x => x.TagId,
                        principalTable: "VerseTags",
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

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_Code",
                table: "BibleBooks",
                column: "Code",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_BookLabelMappings_BookId_BookLabelId",
                table: "BookLabelMappings",
                columns: new[] { "BookId", "BookLabelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookLabelMappings_BookLabelId",
                table: "BookLabelMappings",
                column: "BookLabelId");

            migrationBuilder.CreateIndex(
                name: "IX_BookLabels_Name",
                table: "BookLabels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookOrderItems_BookId",
                table: "BookOrderItems",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookOrderItems_BookOrderId_BookId_Order",
                table: "BookOrderItems",
                columns: new[] { "BookOrderId", "BookId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookOrders_Code",
                table: "BookOrders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookOrders_Name",
                table: "BookOrders",
                column: "Name",
                unique: true);

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
                name: "IX_VerseTagParents_ParentTagId",
                table: "VerseTagParents",
                column: "ParentTagId");

            migrationBuilder.CreateIndex(
                name: "IX_VerseTagParents_TagId_ParentTagId",
                table: "VerseTagParents",
                columns: new[] { "TagId", "ParentTagId" },
                unique: true);

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
                name: "IX_VerseTags_Name_Subclassifier",
                table: "VerseTags",
                columns: new[] { "Name", "Subclassifier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookLabelMappings");

            migrationBuilder.DropTable(
                name: "BookOrderItems");

            migrationBuilder.DropTable(
                name: "LabelRunTags");

            migrationBuilder.DropTable(
                name: "VerseTagMappings");

            migrationBuilder.DropTable(
                name: "VerseTagParents");

            migrationBuilder.DropTable(
                name: "VerseTagResolutions");

            migrationBuilder.DropTable(
                name: "BookLabels");

            migrationBuilder.DropTable(
                name: "BookOrders");

            migrationBuilder.DropTable(
                name: "VerseTags");

            migrationBuilder.DropTable(
                name: "BibleVerses");

            migrationBuilder.DropTable(
                name: "LabelRuns");

            migrationBuilder.DropTable(
                name: "BibleBooks");
        }
    }
}
