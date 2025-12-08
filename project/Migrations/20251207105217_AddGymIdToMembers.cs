using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proje.Migrations
{
    /// <inheritdoc />
    public partial class AddGymIdToMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GymId",
                table: "Members",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_GymId",
                table: "Members",
                column: "GymId");

            migrationBuilder.AddForeignKey(
                name: "FK_Members_Gyms_GymId",
                table: "Members",
                column: "GymId",
                principalTable: "Gyms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_Gyms_GymId",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_GymId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "GymId",
                table: "Members");
        }
    }
}
