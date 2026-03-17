using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitbucketCustomServices.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureSwitchesToRepository : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CascadeMergeEnabled",
                table: "Repositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TelegramNotificationsEnabled",
                table: "Repositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CascadeMergeEnabled",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "TelegramNotificationsEnabled",
                table: "Repositories");
        }
    }
}
