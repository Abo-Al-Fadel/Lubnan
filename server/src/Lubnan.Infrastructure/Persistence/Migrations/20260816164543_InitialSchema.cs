using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lubnan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    region = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    plate_frame = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    plate_hero = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    plate_mosaic = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    plate_rail = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    plate_subject = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_places", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "place_callouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    x = table.Column<double>(type: "double precision", nullable: false),
                    y = table.Column<double>(type: "double precision", nullable: false),
                    text = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_callouts", x => x.id);
                    table.CheckConstraint("ck_place_callouts_within_frame", "x >= 0 AND x <= 1 AND y >= 0 AND y <= 1");
                    table.ForeignKey(
                        name: "fk_place_callouts_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "place_facts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_facts", x => x.id);
                    table.ForeignKey(
                        name: "fk_place_facts_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "place_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    local_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    note = table.Column<string>(type: "text", nullable: false),
                    standfirst = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_translations", x => x.id);
                    table.CheckConstraint("ck_place_translations_name_not_blank", "length(btrim(name)) > 0");
                    table.ForeignKey(
                        name: "fk_place_translations_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_place_callouts_place_ordinal",
                table: "place_callouts",
                columns: new[] { "place_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_place_facts_place_ordinal",
                table: "place_facts",
                columns: new[] { "place_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_place_translations_place_locale",
                table: "place_translations",
                columns: new[] { "place_id", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_places_published_region_order",
                table: "places",
                columns: new[] { "region", "display_order" },
                filter: "published_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_places_slug",
                table: "places",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "place_callouts");

            migrationBuilder.DropTable(
                name: "place_facts");

            migrationBuilder.DropTable(
                name: "place_translations");

            migrationBuilder.DropTable(
                name: "places");
        }
    }
}
