using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitbucketCustomServices.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookSecretToRepository : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WebhookSecret",
                table: "Repositories",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WebhookSecret",
                table: "Repositories");
        }
    }
}
