using FluentValidation;
using Lubnan.Domain.Places;

namespace Lubnan.Application.Features.Places.ListPlaces;

internal sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        // An unknown filter is a 400, not an empty list. An empty list would
        // let a typo read as "there is nothing in that region", which is a bug
        // that renders perfectly and therefore survives to production.
        RuleFor(q => q.Region)
            .Must(BeAKnownRegion)
            .WithMessage($"Unknown region. Expected one of: {Names<Region>()}.");

        RuleFor(q => q.Category)
            .Must(BeAKnownCategory)
            .WithMessage($"Unknown category. Expected one of: {Names<PlaceCategory>()}.");
    }

    private static bool BeAKnownRegion(string? value) =>
        value is null || RegionNames.TryParse(value, out _);

    private static bool BeAKnownCategory(string? value) =>
        value is null || Enum.TryParse<PlaceCategory>(value, ignoreCase: true, out _);

    private static string Names<TEnum>()
        where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetNames<TEnum>());
}
