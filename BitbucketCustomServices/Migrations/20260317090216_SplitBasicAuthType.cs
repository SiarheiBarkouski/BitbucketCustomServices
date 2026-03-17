using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitbucketCustomServices.Migrations
{
    /// <inheritdoc />
    public partial class SplitBasicAuthType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate existing AuthType values: old Basic(0)->BasicTokenAuth(1), old AuthToken(1)->AuthToken(2)
            migrationBuilder.Sql(
                "UPDATE RepositoryCredentials SET AuthType = 2 WHERE AuthType = 1; " +
                "UPDATE RepositoryCredentials SET AuthType = 1 WHERE AuthType = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: BasicTokenAuth(1)->Basic(0), AuthToken(2)->AuthToken(1)
            migrationBuilder.Sql(
                "UPDATE RepositoryCredentials SET AuthType = 0 WHERE AuthType = 1; " +
                "UPDATE RepositoryCredentials SET AuthType = 1 WHERE AuthType = 2;");
        }
    }
}
