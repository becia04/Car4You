using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Car4You.Migrations
{
    /// <inheritdoc />
    public partial class version : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Cars");


            migrationBuilder.AddColumn<int>(
                name: "VarsionId",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VersionId",
                table: "Cars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Version",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CarModelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Version", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cars_VersionId",
                table: "Cars",
                column: "VersionId");


            migrationBuilder.AddForeignKey(
                name: "FK_Cars_Version_VersionId",
                table: "Cars",
                column: "VersionId",
                principalTable: "Version",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropForeignKey(
                name: "FK_Cars_Version_VersionId",
                table: "Cars");

            migrationBuilder.DropTable(
                name: "Version");

            migrationBuilder.DropIndex(
                name: "IX_Cars_VersionId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VarsionId",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "VersionId",
                table: "Cars");


            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Cars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

        }
    }
}
