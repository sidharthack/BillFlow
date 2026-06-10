using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BillFlow.NotificationService.Migrations
{
    /// <inheritdoc />
    public partial class AddBodyToNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "NotificationLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                table: "NotificationLogs");
        }
    }
}
