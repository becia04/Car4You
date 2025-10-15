using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Car4You.Migrations
{
    /// <inheritdoc />
    public partial class NextTechnicalBad_NextOc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<DateTime>(
                name: "NextOc",
                table: "Cars",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextTechnicalBad",
                table: "Cars",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.DropColumn(
                name: "NextOc",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "NextTechnicalBad",
                table: "Cars");

        }
    }
}
