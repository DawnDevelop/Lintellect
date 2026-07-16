using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lintellect.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Closes the check-then-insert dedupe race in SubmitAnalysisCommandHandler: at most one
    /// Pending/Running job may exist per (provider, project, repo, PR). Existing duplicate
    /// active jobs (from crashes/restarts) are failed first so the index can be created.
    /// </summary>
    public partial class ActiveJobUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "AnalysisJobs"
                SET "Status" = 'Failed',
                    "ErrorMessage" = 'Superseded duplicate active job (active-job unique index backfill)',
                    "CompletedAt" = now()
                WHERE "Id" IN (
                    SELECT "Id" FROM (
                        SELECT "Id", ROW_NUMBER() OVER (
                            PARTITION BY
                                ("AnalysisRequest"->>'GitProvider'),
                                COALESCE("AnalysisRequest"#>>'{GitInfo,ProjectName}', ''),
                                ("AnalysisRequest"#>>'{GitInfo,RepositoryName}'),
                                ("AnalysisRequest"#>>'{GitInfo,PullRequestId}')
                            ORDER BY "Created" DESC) AS row_number
                        FROM "AnalysisJobs"
                        WHERE "Status" IN ('Pending', 'Running')
                    ) ranked
                    WHERE row_number > 1
                );

                CREATE UNIQUE INDEX "IX_AnalysisJobs_ActiveJobPerPullRequest"
                ON "AnalysisJobs" (
                    ("AnalysisRequest"->>'GitProvider'),
                    COALESCE("AnalysisRequest"#>>'{GitInfo,ProjectName}', ''),
                    ("AnalysisRequest"#>>'{GitInfo,RepositoryName}'),
                    ("AnalysisRequest"#>>'{GitInfo,PullRequestId}')
                )
                WHERE "Status" IN ('Pending', 'Running');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_AnalysisJobs_ActiveJobPerPullRequest";""");
        }
    }
}
