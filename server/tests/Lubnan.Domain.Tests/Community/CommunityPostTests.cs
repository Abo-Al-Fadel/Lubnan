using Lubnan.Domain.Community;
using Xunit;

namespace Lubnan.Domain.Tests.Community;

public sealed class CommunityPostTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Author = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Reader = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Publish_trims_and_keeps_the_words()
    {
        var post = CommunityPost.Publish(Author, "  First light at Tyre.  ", "tyre", "D3", Now);

        Assert.True(post.IsSuccess);
        Assert.Equal("First light at Tyre.", post.Value.Body);
        Assert.Equal("tyre", post.Value.PlaceSlug);
    }

    [Fact]
    public void Publish_refuses_an_empty_body()
    {
        var post = CommunityPost.Publish(Author, "   ", null, null, Now);

        Assert.True(post.IsFailure);
        Assert.Equal("post.body.length", post.Error.Code);
    }

    [Fact]
    public void Publish_refuses_a_bidi_override()
    {
        var post = CommunityPost.Publish(Author, "Hello \u202Eworld", null, null, Now);

        Assert.True(post.IsFailure);
        Assert.Equal("post.body.characters", post.Error.Code);
    }

    [Fact]
    public void A_person_can_like_a_post_only_once()
    {
        var post = CommunityPost.Publish(Author, "Byblos harbour still works.", "byblos", null, Now).Value;

        Assert.True(post.Like(Reader, Now).IsSuccess);
        Assert.True(post.Like(Reader, Now).IsSuccess);
        Assert.Single(post.Likes);
    }

    [Fact]
    public void Unlike_is_idempotent()
    {
        var post = CommunityPost.Publish(Author, "Byblos harbour still works.", "byblos", null, Now).Value;
        post.Like(Reader, Now);
        post.Unlike(Reader);
        post.Unlike(Reader);

        Assert.Empty(post.Likes);
    }

    [Fact]
    public void A_comment_is_owned_by_the_writer()
    {
        var post = CommunityPost.Publish(Author, "Byblos harbour still works.", "byblos", null, Now).Value;
        var comment = post.AddComment(Reader, "I was there last April.", Now);

        Assert.True(comment.IsSuccess);
        Assert.Equal(Reader, comment.Value.AuthorId);

        var stolen = post.RemoveComment(comment.Value.Id, Author, isAdmin: false);
        Assert.True(stolen.IsFailure);
        Assert.Equal("comment.forbidden", stolen.Error.Code);

        Assert.True(post.RemoveComment(comment.Value.Id, Reader, isAdmin: false).IsSuccess);
        Assert.Empty(post.Comments);
    }

    [Fact]
    public void An_admin_can_remove_someone_elses_comment()
    {
        var post = CommunityPost.Publish(Author, "Byblos harbour still works.", "byblos", null, Now).Value;
        var comment = post.AddComment(Reader, "Spam.", Now).Value;

        Assert.True(post.RemoveComment(comment.Id, Author, isAdmin: true).IsSuccess);
        Assert.Empty(post.Comments);
    }
}
