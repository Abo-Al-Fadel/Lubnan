using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lubnan.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Makes <c>account_events</c> append-only in the database, not just in the
    /// code.
    /// </summary>
    /// <remarks>
    /// Until now the guarantee was that no code path updates or deletes an
    /// audit row. That is worth having and it is not worth much: it protects
    /// against a careless change, and not at all against the thing the audit
    /// log exists for — somebody who has the application's own database
    /// credentials. They can simply issue the DELETE the application never
    /// issues, and the record of what they did goes with it.
    ///
    /// A rule enforced by the database survives that. These triggers refuse
    /// UPDATE and DELETE from every caller including the table's owner, so the
    /// history of an incident cannot be edited by whoever is causing it.
    ///
    /// Triggers rather than a GRANT, deliberately. Managed Postgres — Neon,
    /// Supabase, RDS — hands you one role that owns the schema and runs
    /// migrations, and revoking DELETE from an owner is either impossible or
    /// undone by the next `ALTER TABLE`. A trigger is enforced regardless of
    /// who is connected.
    ///
    /// The escape hatch is deliberate and loud: an operator with a genuine
    /// legal reason to erase a row (a GDPR erasure order naming a specific
    /// event) drops the trigger, does the work, and puts it back. That is three
    /// visible statements in a session log rather than a silent DELETE.
    /// </remarks>
    public partial class AuditLogAppendOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION lubnan_account_events_append_only()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION
                        'account_events is append-only: % is not permitted', TG_OP
                        USING HINT = 'Drop trigger account_events_no_change if an erasure order genuinely requires it, then restore it.';
                END;
                $$;
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER account_events_no_change
                BEFORE UPDATE OR DELETE ON account_events
                FOR EACH ROW
                EXECUTE FUNCTION lubnan_account_events_append_only();
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS account_events_no_change ON account_events;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS lubnan_account_events_append_only();");
        }
    }
}
