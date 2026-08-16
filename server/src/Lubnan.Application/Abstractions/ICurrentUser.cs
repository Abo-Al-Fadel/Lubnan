namespace Lubnan.Application.Abstractions;

/// <summary>
/// Who is asking. Null when nobody is: the public half of this site is most of
/// it, and an abstraction that pretends every request is authenticated pushes
/// a null check into every handler anyway.
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
