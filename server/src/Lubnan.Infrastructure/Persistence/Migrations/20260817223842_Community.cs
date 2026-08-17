using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lubnan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Community : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "community_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    place_slug = table.Column<string>(type: "citext", maxLength: 80, nullable: true),
                    plate = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_posts", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_posts_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "community_post_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_post_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_post_comments_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_community_post_comments_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "community_post_likes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_post_likes", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_post_likes_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_community_post_likes_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_community_post_comments_author_id",
                table: "community_post_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_post_comments_post_created",
                table: "community_post_comments",
                columns: new[] { "post_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_community_post_likes_post_user",
                table: "community_post_likes",
                columns: new[] { "post_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_community_post_likes_user_id",
                table: "community_post_likes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_posts_author",
                table: "community_posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_community_posts_created_at",
                table: "community_posts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_community_posts_place",
                table: "community_posts",
                column: "place_slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "community_post_comments");

            migrationBuilder.DropTable(
                name: "community_post_likes");

            migrationBuilder.DropTable(
                name: "community_posts");
        }
    }
}
