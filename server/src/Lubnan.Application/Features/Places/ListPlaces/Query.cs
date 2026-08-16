using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;

namespace Lubnan.Application.Features.Places.ListPlaces;

/// <summary>
/// The published destinations, in editorial order, optionally narrowed to one
/// region or category. Backs the mosaic and the rail on <c>/explore</c>.
/// </summary>
/// <remarks>
/// The filters arrive as strings rather than as enums, and that is deliberate.
/// If the endpoint parsed them it would have to answer a bad value itself, in
/// its own shape, with its own wording — and eleven endpoints later there are
/// three conventions for "unknown filter". Keeping them as strings means the
/// validator rejects them and the failure leaves through the one path every
/// other failure uses.
/// </remarks>
public sealed record Query(Locale Locale, string? Region = null, string? Category = null)
    : IQuery<Result<IReadOnlyList<PlaceSummary>>>;
