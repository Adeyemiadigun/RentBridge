using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleLiveInspectionRequestPerLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A unique index cannot be created while duplicates exist, so clean
            // first. Doing this unconditionally is deliberate: the dev database
            // was audited and is clean, but the deployed one cannot be audited
            // from here, and a migration that only works against known-clean
            // data is a deploy that breaks on the day production disagrees.
            //
            // Resolution rule, per lease with more than one live request: keep
            // the furthest-along request, because that is the one the lifecycle
            // would have acted on, and cancel the rest. Ties break on the most
            // recent proposed/scheduled date and then on Id so the outcome is
            // deterministic and re-runnable.
            //
            // Rows already in a terminal state are untouched, and Completed rows
            // are never touched because they fall outside the live filter.
            migrationBuilder.Sql(
                """
                WITH ranked AS (
                    SELECT "Id",
                           row_number() OVER (
                               PARTITION BY "LeaseId"
                               ORDER BY
                                   CASE "Status"
                                       WHEN 'ReschedulePending' THEN 3
                                       WHEN 'Confirmed'       THEN 2
                                       WHEN 'Pending'         THEN 1
                                   END DESC,
                                   "ProposedDate"  DESC NULLS LAST,
                                   "ScheduledDate" DESC NULLS LAST,
                                   "PreferredDate" DESC,
                                   "Id"
                           ) AS rn
                    FROM inspection_requests
                    WHERE "Status" IN ('Pending', 'Confirmed', 'ReschedulePending')
                )
                UPDATE inspection_requests AS ir
                SET "Status" = 'Cancelled'
                FROM ranked
                WHERE ir."Id" = ranked."Id"
                  AND ranked.rn > 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_inspection_requests_LeaseId",
                table: "inspection_requests");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_requests_LeaseId_live",
                table: "inspection_requests",
                column: "LeaseId",
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Confirmed', 'ReschedulePending')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: the duplicate rows this migration cancelled are not
            // restored. Which one was correct is not recoverable after the
            // fact, and guessing would be worse than leaving them cancelled.
            migrationBuilder.DropIndex(
                name: "IX_inspection_requests_LeaseId_live",
                table: "inspection_requests");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_requests_LeaseId",
                table: "inspection_requests",
                column: "LeaseId");
        }
    }
}
