using Microsoft.AspNetCore.Routing;

namespace Lubnan.Application.Abstractions;

/// <summary>
/// One route, mapped by the slice that owns it.
/// </summary>
/// <remarks>
/// Implementations are discovered by scanning at startup, so adding a feature
/// never means editing a central registration file. That file is the thing
/// that collects a merge conflict on every branch and, worse, becomes the
/// place where somebody registers an endpoint in the wrong group and gives it
/// the wrong authorization by accident.
/// </remarks>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
