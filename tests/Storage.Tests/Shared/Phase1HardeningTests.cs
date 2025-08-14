#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using FluentAssertions;
using Cloudbrick.DataExplorer.Storage.Configuration;
using System;
using System.Text.Json.Nodes;
using Xunit;

namespace Cloudbrick.DataExplorer.Storage.Tests.Shared;


public class Phase1HardeningTests
{
    [Fact]
    public void JsonDiff_Redacts_Secrets()
    {
        var oldObj = JsonNode.Parse("""
        { "Name":"Ada","Secrets":{"Password":"p1","Token":"t1"} }
        """)!.AsObject();
        var newObj = JsonNode.Parse("""
        { "Name":"Ada","Secrets":{"Password":"p2","Token":"t2"} }
        """)!.AsObject();

        var opts = new JsonDiffOptions
        {
            RootPath = "Data",
            DiffArrays = true,
            RedactPath = p => p.StartsWith("Data.Secrets.", StringComparison.OrdinalIgnoreCase),
            RedactValue = _ => JsonValue.Create("REDACTED")
        };

        var changes = new JsonDiff(opts).Compute(oldObj, newObj, principalId: "test");
        changes.Should().ContainKey("Data.Secrets.Password");
        changes["Data.Secrets.Password"].NewValue!.GetValue<string>().Should().Be("REDACTED");
        changes["Data.Secrets.Password"].OldValue!.GetValue<string>().Should().Be("REDACTED");
    }

    [Theory]
    [InlineData("db-main")]
    [InlineData("db.main_1")]
    [InlineData("DBMAIN")]
    public void Validate_DatabaseId_Accepts_Good(string id)
        => FluentActions.Invoking(() => StorageNameRules.ValidateDatabaseId(id)).Should().NotThrow();

    [Theory]
    [InlineData("-bad")]
    [InlineData("bad!")]
    [InlineData("this-name-is-way-too-long-because-it-exceeds-sixty-four-characters-12345")]
    public void Validate_DatabaseId_Rejects_Bad(string id)
        => FluentActions.Invoking(() => StorageNameRules.ValidateDatabaseId(id)).Should().Throw<ArgumentException>();

    [Fact]
    public void Guard_Size_Throws_When_Too_Big()
    {
        var big = new string('x', 600_000); // 600 KB (UTF-8)
        var item = new StorageItem { Data = new JsonObject { ["blob"] = big } };
        var limits = new StorageLimits { MaxItemSizeBytes = 512 * 1024 };
        FluentActions.Invoking(() => StorageGuards.EnsureWithinLimits(item, limits))
                     .Should().Throw<InvalidOperationException>();
    }
}