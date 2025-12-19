using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proje.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagesTableV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderMemberId = table.Column<int>(type: "int", nullable: true),
                    SenderTrainerId = table.Column<int>(type: "int", nullable: true),
                    ReceiverMemberId = table.Column<int>(type: "int", nullable: true),
                    ReceiverTrainerId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ReplyToMessageId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.CheckConstraint("CK_Messages_Receiver", "([ReceiverMemberId] IS NOT NULL AND [ReceiverTrainerId] IS NULL) OR ([ReceiverMemberId] IS NULL AND [ReceiverTrainerId] IS NOT NULL)");
                    table.CheckConstraint("CK_Messages_Sender", "([SenderMemberId] IS NOT NULL AND [SenderTrainerId] IS NULL) OR ([SenderMemberId] IS NULL AND [SenderTrainerId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Messages_Members_ReceiverMemberId",
                        column: x => x.ReceiverMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Messages_Members_SenderMemberId",
                        column: x => x.SenderMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Messages_Messages_ReplyToMessageId",
                        column: x => x.ReplyToMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Messages_Trainers_ReceiverTrainerId",
                        column: x => x.ReceiverTrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_Messages_Trainers_SenderTrainerId",
                        column: x => x.SenderTrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_CreatedDate",
                table: "Messages",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IsRead",
                table: "Messages",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverMemberId",
                table: "Messages",
                column: "ReceiverMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverTrainerId",
                table: "Messages",
                column: "ReceiverTrainerId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReplyToMessageId",
                table: "Messages",
                column: "ReplyToMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderMemberId",
                table: "Messages",
                column: "SenderMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderTrainerId",
                table: "Messages",
                column: "SenderTrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
