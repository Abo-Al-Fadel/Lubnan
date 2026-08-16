using Lubnan.Application.Abstractions.Messaging;
using Lubnan.Domain.Common;

namespace Lubnan.Application.Features.Places.GetPlaceBySlug;

/// <summary>Everything the place page renders, in one round trip.</summary>
public sealed record Query(string Slug, Locale Locale) : IQuery<Result<PlaceDetail>>;
