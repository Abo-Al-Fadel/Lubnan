using Lubnan.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lubnan.Infrastructure.Persistence.Configurations;

internal sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("community_posts");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.AuthorId).HasColumnName("author_id").IsRequired();
        builder.Property(p => p.Body)
            .HasColumnName("body")
            .HasMaxLength(CommunityPost.BodyMaxLength)
            .IsRequired();

        builder.Property(p => p.PlaceSlug)
            .HasColumnName("place_slug")
            .HasColumnType("citext")
            .HasMaxLength(80);

        builder.Property(p => p.Plate)
            .HasColumnName("plate")
            .HasMaxLength(CommunityPost.PlateMaxLength);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(p => p.CreatedAt).HasDatabaseName("ix_community_posts_created_at");
        builder.HasIndex(p => p.AuthorId).HasDatabaseName("ix_community_posts_author");
        builder.HasIndex(p => p.PlaceSlug).HasDatabaseName("ix_community_posts_place");

        builder.HasOne<Lubnan.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Likes)
            .WithOne()
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Comments)
            .WithOne()
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Likes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
{
    public void Configure(EntityTypeBuilder<PostLike> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("community_post_likes");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.PostId).HasColumnName("post_id");
        builder.Property(l => l.UserId).HasColumnName("user_id");
        builder.Property(l => l.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(l => new { l.PostId, l.UserId })
            .IsUnique()
            .HasDatabaseName("ix_community_post_likes_post_user");

        builder.HasOne<Lubnan.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("community_post_comments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.PostId).HasColumnName("post_id");
        builder.Property(c => c.AuthorId).HasColumnName("author_id");
        builder.Property(c => c.Body)
            .HasColumnName("body")
            .HasMaxLength(PostComment.MaxLength)
            .IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(c => new { c.PostId, c.CreatedAt })
            .HasDatabaseName("ix_community_post_comments_post_created");

        builder.HasOne<Lubnan.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
