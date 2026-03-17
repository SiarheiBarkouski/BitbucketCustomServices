using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitbucketCustomServices.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToRepositoryCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "RepositoryCredentials",
                type: "TEXT",
                nullable: true);

            // Migrate existing BasicTokenAuth: copy Username to Email
            migrationBuilder.Sql(
                "UPDATE RepositoryCredentials SET Email = Username WHERE AuthType = 1 AND (Email IS NULL OR Email = '')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "RepositoryCredentials");
        }
    }
}
