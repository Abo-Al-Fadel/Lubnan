using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lubnan.Application.Features.Places.GetPlaceBySlug;

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapGet("/api/v1/places/{slug}", async (
            string slug,
            string? locale,
            HttpRequest http,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var query = new Query(slug, http.ResolveLocale(locale));
            return (await sender.Send(query, cancellationToken)).ToHttpResult();
        })
        .WithName("GetPlaceBySlug")
        .WithSummary("One destination, resolved to a single language.")
        .WithTags("Places")
        .Produces<PlaceDetail>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .CacheOutput("places");
}
