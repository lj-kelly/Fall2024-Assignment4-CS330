using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fall2024_Assignment4_CS330.Migrations
{
    /// <inheritdoc />
    public partial class gameHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "TTTModel",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TTTModel_ApplicationUserId",
                table: "TTTModel",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TTTModel_AspNetUsers_ApplicationUserId",
                table: "TTTModel",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TTTModel_AspNetUsers_ApplicationUserId",
                table: "TTTModel");

            migrationBuilder.DropIndex(
                name: "IX_TTTModel_ApplicationUserId",
                table: "TTTModel");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "TTTModel");
        }
    }
}
