using Lubnan.Application.Abstractions;
using Lubnan.Application.Abstractions.Http;
using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Lubnan.Application.Features.Flights.ListFlights;

public sealed record Query : IQuery<Result<FlightBoardDto>>;

internal sealed class Handler(IFlightBoard board)
    : IQueryHandler<Query, Result<FlightBoardDto>>
{
    public async Task<Result<FlightBoardDto>> Handle(Query query, CancellationToken cancellationToken)
    {
        var snapshot = await board.GetAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success(snapshot);
    }
}

internal sealed class Endpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) => app
        .MapGet("/api/v1/flights", async (ISender sender, CancellationToken cancellationToken) =>
            (await sender.Send(new Query(), cancellationToken)).ToHttpResult())
        .WithName("ListFlights")
        .WithSummary("Today's arrivals and departures at Beirut–Rafic Hariri.")
        .WithTags("Flights")
        .Produces<FlightBoardDto>()
        .RequireRateLimiting(RateLimits.Read);
}
