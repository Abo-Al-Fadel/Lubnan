using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Lubnan.Architecture.Tests;

/// <summary>
/// The dependency rules, as tests.
/// </summary>
/// <remarks>
/// A diagram in a README describes what somebody intended in month one. These
/// fail the build in month eighteen, when the person adding the twentieth
/// feature has never read that README and a <c>using</c> statement is one
/// keystroke away.
/// <para>
/// This is the file that makes the rest of the structure hold as the codebase
/// grows, which is the whole reason it exists before there are twenty features
/// rather than after.
/// </para>
/// </remarks>
public sealed class LayerTests
{
    private static readonly Assembly Domain = typeof(Lubnan.Domain.Common.Entity).Assembly;
    private static readonly Assembly Application = Lubnan.Application.DependencyInjection.Assembly;
    private static readonly Assembly Infrastructure = typeof(Lubnan.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_depends_on_nothing()
    {
        // Not "depends on few things" — nothing. A domain that can see EF ends
        // up with a rule expressed as a query, and a rule expressed as a query
        // is a rule you cannot test without a database.
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Lubnan.Application",
                "Lubnan.Infrastructure",
                "Lubnan.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "FluentValidation")
            .GetResult();

        Assert.True(result.IsSuccessful, Explain("Domain reaches outward", result));
    }

    [Fact]
    public void Application_does_not_know_how_anything_is_stored()
    {
        // EF Core's abstractions are allowed here and the provider is not. The
        // boundary that matters is Postgres, not the ORM: a slice writing its
        // own query is the point of a slice, but a slice that names Npgsql
        // cannot be run against anything else.
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny("Lubnan.Infrastructure", "Lubnan.Api", "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, Explain("Application reaches into Infrastructure", result));
    }

    [Fact]
    public void Infrastructure_does_not_know_about_the_web()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("Lubnan.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Explain("Infrastructure reaches into the API", result));
    }

    [Fact]
    public void Handlers_never_touch_HTTP()
    {
        // The cost of letting a slice own its endpoint is that ASP.NET types
        // are reachable from a handler. This is the rule that closes it: only
        // the Endpoint class talks to HTTP, so every handler stays testable by
        // calling a method.
        var result = Types.InAssembly(Application)
            .That()
            .HaveNameEndingWith("Handler")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Explain("A handler depends on ASP.NET", result));
    }

    [Fact]
    public void Handlers_are_sealed_and_internal()
    {
        // Internal because nothing outside the slice should call a handler
        // directly — that is what ISender is for, and a direct call skips
        // validation, logging and the transaction.
        var result = Types.InAssembly(Application)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .DoNotHaveNameMatching(".*Behavior.*")
            .Should()
            .BeSealed()
            .And()
            .NotBePublic()
            .GetResult();

        Assert.True(result.IsSuccessful, Explain("A handler is public or unsealed", result));
    }

    [Fact]
    public void Domain_entities_are_not_records()
    {
        // A record's value equality is wrong for an entity: two places with the
        // same fields and different ids are two places, and a record would call
        // them equal. Entity implements identity equality instead.
        var records = Types.InAssembly(Domain)
            .That()
            .Inherit(typeof(Lubnan.Domain.Common.Entity))
            .GetTypes()
            .Where(type => type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) is not null)
            .Select(type => type.Name)
            .ToList();

        Assert.True(records.Count == 0, $"These entities are records: {string.Join(", ", records)}");
    }

    private static string Explain(string rule, TestResult result) =>
        $"{rule}: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
