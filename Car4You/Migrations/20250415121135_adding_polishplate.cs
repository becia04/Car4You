using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Car4You.Migrations
{
    /// <inheritdoc />
    public partial class adding_polishplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PolishPlate",
                table: "Cars",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolishPlate",
                table: "Cars");
        }
    }
}
