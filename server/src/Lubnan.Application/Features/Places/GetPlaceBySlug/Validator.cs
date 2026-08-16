using FluentValidation;
using Lubnan.Domain.Places;

namespace Lubnan.Application.Features.Places.GetPlaceBySlug;

internal sealed class Validator : AbstractValidator<Query>
{
    public Validator() =>
        // Rejected before it reaches the database. A slug is a narrow shape,
        // so anything outside it cannot match a row — and answering 400 rather
        // than running the query means a scanner probing the route with SQL
        // fragments never gets as far as the query planner.
        RuleFor(q => q.Slug)
            .Must(slug => Slug.Create(slug).IsSuccess)
            .WithMessage("A slug is lowercase letters, digits and single hyphens.");
}
