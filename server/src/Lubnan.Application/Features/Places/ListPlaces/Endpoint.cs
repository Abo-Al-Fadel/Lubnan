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
            var query = new Query(http.ResolveLocale(locale), region, category);
            return (await sender.Send(query, cancellationToken)).ToHttpResult();
        })
        .WithName("ListPlaces")
        .WithSummary("Published destinations, in editorial order.")
        .WithTags("Places")
        .Produces<IReadOnlyList<PlaceSummary>>()
        .ProducesValidationProblem()
        .CacheOutput("places");
}
