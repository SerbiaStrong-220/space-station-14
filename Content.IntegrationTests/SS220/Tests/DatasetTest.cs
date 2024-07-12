// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Data;
using System.Linq;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests;

[TestFixture]
public sealed class SS220DatasetTest
{
    [Test, Order(1)] // to make sure that null entry in dataset wont fail other tests
    public async Task CheckForNullEntriesTest()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        var protos = protoMan.EnumeratePrototypes<DatasetPrototype>().OrderBy(p => p.ID);

        foreach (var proto in protos)
            foreach (var line in proto.Values)
                Assert.That(line is not null, $"Current dataset prototype ID is {proto.ID}");
        // null values generates by .yml if you write " - ", to make a empty string use " - !!string"
        // null values wont fatal until f.e. random name generator tries to use that value
        await pair.CleanReturnAsync();
    }
}
