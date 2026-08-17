using Lubnan.Domain.Common;
using Lubnan.Domain.Users;
using Lubnan.Domain.Users.Events;
using Xunit;

namespace Lubnan.Domain.Tests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 9, 0, 0, TimeSpan.Zero);

    private static User Reader() => User.Register(
        Email.Create("reader@example.com").Value,
        DisplayName.Create("Reader").Value,
        "hash-v1",
        Now).Value;

    [Fact]
    public void Rehashing_a_password_does_not_end_sessions_or_raise_a_change()
    {
        var user = Reader();
        user.ConfirmEmail(Now);
        user.StartSession("refresh-hash", Now, TimeSpan.FromDays(30));

        Assert.True(user.RehashPassword("hash-v2").IsSuccess);
        Assert.Equal("hash-v2", user.PasswordHash);
        Assert.Single(user.Sessions.Where(s => s.IsActive));
        Assert.Empty(user.DomainEvents.OfType<UserPasswordChanged>());
    }

    [Fact]
    public void Changing_a_password_ends_every_session()
    {
        var user = Reader();
        user.ConfirmEmail(Now);
        user.StartSession("refresh-hash", Now, TimeSpan.FromDays(30));

        Assert.True(user.ChangePassword("hash-v2", Now.AddMinutes(1)).IsSuccess);
        Assert.Empty(user.Sessions.Where(s => s.IsActive));
        Assert.Single(user.DomainEvents.OfType<UserPasswordChanged>());
    }

    [Fact]
    public void A_second_registration_attempt_is_an_event_not_an_error()
    {
        var user = Reader();
        user.NoteRegistrationAttempt(Now.AddSeconds(1));

        Assert.Single(user.DomainEvents.OfType<UserRegistrationReattempted>());
    }
}
