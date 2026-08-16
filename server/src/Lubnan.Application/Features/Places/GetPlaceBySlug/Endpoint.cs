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
            var result = await sender.Send(new Query(slug, http.ResolveLocale(locale)), cancellationToken);

            // From what was served, not from what was asked for. A place with
            // no Arabic copy answers an Arabic request in English, and the
            // header has to say English or it is describing a body that does
            // not exist.
            if (result.IsSuccess)
            {
                http.SetContentLanguage(result.Value.Locale);
            }

            return result.ToHttpResult();
        })
        .WithName("GetPlaceBySlug")
        .WithSummary("One destination, resolved to a single language.")
        .WithTags("Places")
        .Produces<PlaceDetail>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem()
        .RequireRateLimiting("read")
        .CacheOutput("places");
}
