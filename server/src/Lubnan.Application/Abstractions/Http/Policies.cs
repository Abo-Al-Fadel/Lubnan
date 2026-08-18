namespace Lubnan.Application.Abstractions.Http;

/// <summary>
/// Authorization policy names.
/// </summary>
/// <remarks>
/// Here rather than in the API project because the endpoints that reference
/// them live in the slices, and a slice cannot name something in
/// <c>Lubnan.Api</c> — the dependency points the other way, and the
/// architecture tests enforce it.
/// <para>
/// Constants rather than string literals for the usual reason, which matters
/// more here than most places: <c>RequireAuthorization("CanModrate")</c> is not
/// a build error and not a startup error. ASP.NET Core throws only when a
/// request reaches the endpoint — so a typo ships, and the first person to find
/// it is a moderator staring at a 500.
/// </para>
/// </remarks>
public static class Policies
{
    /// <summary>Administers other people's accounts and content.</summary>
    public const string CanModerate = "CanModerate";
}

/// <summary>Role names, matching the claims the token carries.</summary>
public static class Roles
{
    public const string Admin = "admin";
}
