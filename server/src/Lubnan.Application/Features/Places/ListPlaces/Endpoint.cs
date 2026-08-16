using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lubnan.Application.Features.Places.ListPlaces;

internal sealed class Endpoint : IEndpoint
{
    // Transport only: read the request, send the query, render the result.
    // No parsing, no branching, no error wording. Everything that could be
    // wrong about this request is decided by the validator, and everything
    // about how a failure looks is decided by ToHttpResult.
    public void Map(IEndpointRouteBuilder app) => app
        .MapGet("/api/v1/places", async (
            string? region,
            string? category,
            string? locale,
            HttpRequest http,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var resolved = http.ResolveLocale(locale);
            var result = await sender.Send(new Query(resolved, region, category), cancellationToken);

            // Card copy falls back per row, so a mixed list is possible. The
            // requested locale is the honest single answer for the collection;
            // a client needing per-item precision reads the detail endpoint,
            // which reports exactly what it served.
            if (result.IsSuccess)
            {
                http.SetContentLanguage(resolved.Code);
            }

            return result.ToHttpResult();
        })
        .WithName("ListPlaces")
        .WithSummary("Published destinations, in editorial order.")
        .WithTags("Places")
        .Produces<IReadOnlyList<PlaceSummary>>()
        .ProducesValidationProblem()
        .RequireRateLimiting("read")
        .CacheOutput("places");
}
