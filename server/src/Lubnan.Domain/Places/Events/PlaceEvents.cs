using Lubnan.Domain.Common;

namespace Lubnan.Domain.Places.Events;

/// <summary>
/// A place became visible to the public. Consumers invalidate caches, warm the
/// CDN and rebuild the sitemap — none of which the Places slice should know
/// about, which is the whole reason this is an event and not a method call.
/// </summary>
public sealed record PlacePublished(Guid PlaceId, string Slug) : DomainEvent;

/// <summary>Withdrawn from public view. The row is kept; only visibility moved.</summary>
public sealed record PlaceUnpublished(Guid PlaceId, string Slug) : DomainEvent;

/// <summary>
/// Editorial content changed in one language. Carries the locale so a consumer
/// can invalidate one cache key instead of all three.
/// </summary>
public sealed record PlaceTranslationRevised(Guid PlaceId, string Slug, string Locale) : DomainEvent;
