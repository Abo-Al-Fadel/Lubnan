using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lubnan.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Model-only. The schema does not change, and both methods are empty on
    /// purpose.
    /// </summary>
    /// <remarks>
    /// The user entity stopped mapping a concurrency token, so the differ
    /// emitted <c>DropColumn("xmin", "users")</c>. There is no such column to
    /// drop: <c>xmin</c> is a PostgreSQL <em>system</em> column present on every
    /// table, which is why the original CreateTable never emitted it either —
    /// and <c>ALTER TABLE users DROP COLUMN xmin</c> fails outright with
    /// "cannot drop system column".
    ///
    /// The generated body was therefore removed rather than kept. The migration
    /// itself stays so that the model snapshot moves with the code, and
    /// <c>ef migrations has-pending-model-changes</c> — which CI runs — keeps
    /// passing.
    /// </remarks>
    public partial class FixConcurrencyToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. See the remarks above.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. See the remarks above.
        }
    }
}
