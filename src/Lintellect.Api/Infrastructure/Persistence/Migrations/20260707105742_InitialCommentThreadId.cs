using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lintellect.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommentThreadId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InitialCommentThreadId",
                table: "AnalysisJobs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialCommentThreadId",
                table: "AnalysisJobs");
        }
    }
}
