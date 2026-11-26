using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITSupportSystem.API.Migrations
{
    /// <inheritdoc />
    public partial class PluralSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_ApplicationUserId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Ticket_TicketId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Log_AspNetUsers_ApplicationUserId",
                table: "Log");

            migrationBuilder.DropForeignKey(
                name: "FK_Log_Ticket_TicketId",
                table: "Log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ticket",
                table: "Ticket");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Log",
                table: "Log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comment",
                table: "Comment");

            migrationBuilder.RenameTable(
                name: "Ticket",
                newName: "Tickets");

            migrationBuilder.RenameTable(
                name: "Log",
                newName: "Logs");

            migrationBuilder.RenameTable(
                name: "Comment",
                newName: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_Log_TicketId",
                table: "Logs",
                newName: "IX_Logs_TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_Log_ApplicationUserId",
                table: "Logs",
                newName: "IX_Logs_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_TicketId",
                table: "Comments",
                newName: "IX_Comments_TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_ApplicationUserId",
                table: "Comments",
                newName: "IX_Comments_ApplicationUserId");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets",
                column: "TicketId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Logs",
                table: "Logs",
                column: "LogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatedById",
                table: "Tickets",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_ApplicationUserId",
                table: "Comments",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "TicketId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_AspNetUsers_ApplicationUserId",
                table: "Logs",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Tickets_TicketId",
                table: "Logs",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "TicketId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_CreatedById",
                table: "Tickets",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_ApplicationUserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tickets_TicketId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_AspNetUsers_ApplicationUserId",
                table: "Logs");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Tickets_TicketId",
                table: "Logs");

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_CreatedById",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CreatedById",
                table: "Tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Logs",
                table: "Logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Tickets");

            migrationBuilder.RenameTable(
                name: "Tickets",
                newName: "Ticket");

            migrationBuilder.RenameTable(
                name: "Logs",
                newName: "Log");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "Comment");

            migrationBuilder.RenameIndex(
                name: "IX_Logs_TicketId",
                table: "Log",
                newName: "IX_Log_TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_Logs_ApplicationUserId",
                table: "Log",
                newName: "IX_Log_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_TicketId",
                table: "Comment",
                newName: "IX_Comment_TicketId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_ApplicationUserId",
                table: "Comment",
                newName: "IX_Comment_ApplicationUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ticket",
                table: "Ticket",
                column: "TicketId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Log",
                table: "Log",
                column: "LogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comment",
                table: "Comment",
                column: "CommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_ApplicationUserId",
                table: "Comment",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Ticket_TicketId",
                table: "Comment",
                column: "TicketId",
                principalTable: "Ticket",
                principalColumn: "TicketId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Log_AspNetUsers_ApplicationUserId",
                table: "Log",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Log_Ticket_TicketId",
                table: "Log",
                column: "TicketId",
                principalTable: "Ticket",
                principalColumn: "TicketId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
