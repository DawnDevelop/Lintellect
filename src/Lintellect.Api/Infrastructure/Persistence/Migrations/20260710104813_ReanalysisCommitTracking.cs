using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lintellect.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReanalysisCommitTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReanalysisBaseCommitId",
                table: "AnalysisJobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCommitId",
                table: "AnalysisJobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReanalysisBaseCommitId",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SourceCommitId",
                table: "AnalysisJobs");
        }
    }
}
