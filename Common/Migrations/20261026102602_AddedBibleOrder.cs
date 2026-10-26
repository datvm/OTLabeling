using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Common.Migrations
{
    /// <inheritdoc />
    public partial class AddedBibleOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookLabelMappings");

            migrationBuilder.DropTable(
                name: "BookOrderItems");

            migrationBuilder.DropTable(
                name: "BookLabels");

            migrationBuilder.DropTable(
                name: "BookOrders");
        }
    }
}
