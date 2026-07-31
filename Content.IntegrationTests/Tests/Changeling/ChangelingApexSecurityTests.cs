// SS220 changeling Apex tracker
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingApexSecurityTests
{
    /// <summary>
    /// Apex may reveal the presentation data promised by the ability, but must not hand the client stable network
    /// entity identifiers for every living crew member outside its PVS.
    /// </summary>
    [Test]
    public void TargetSelectionPayloadUsesOpaqueTokens()
    {
        var serializedTypes = GetDeclaredMemberTypes(typeof(ChangelingApexTargetEntry))
            .Concat(GetDeclaredMemberTypes(typeof(ChangelingApexTargetSelectedMessage)))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(serializedTypes, Does.Not.Contain(typeof(NetEntity)));
            Assert.That(
                typeof(ChangelingApexTargetEntry)
                    .GetProperty(nameof(ChangelingApexTargetEntry.SelectionToken))!
                    .PropertyType,
                Is.EqualTo(typeof(uint)));
            Assert.That(
                typeof(ChangelingApexTargetSelectedMessage)
                    .GetProperty(nameof(ChangelingApexTargetSelectedMessage.SelectionToken))!
                    .PropertyType,
                Is.EqualTo(typeof(uint)));
        });
    }

    /// <summary>
    /// The transform menu needs the owner-visible identity snapshots, but original victim references and DNA are
    /// authoritative server data. Replicating them would reveal stable entity identifiers outside the owner's PVS.
    /// </summary>
    [Test]
    public void IdentityStateDoesNotNetworkOriginalBodiesOrGenomes()
    {
        var type = typeof(ChangelingIdentityComponent);

        Assert.Multiple(() =>
        {
            Assert.That(IsAutoNetworked(type, nameof(ChangelingIdentityComponent.StoredIdentities)), Is.True);
            Assert.That(IsAutoNetworked(type, nameof(ChangelingIdentityComponent.ConsumedIdentities)), Is.False);
            Assert.That(IsAutoNetworked(type, nameof(ChangelingIdentityComponent.StoredGenomes)), Is.False);
            Assert.That(IsAutoNetworked(type, nameof(ChangelingIdentityComponent.AbsorbedGenomes)), Is.False);
            Assert.That(IsAutoNetworked(type, nameof(ChangelingIdentityComponent.CurrentGenome)), Is.False);
        });
    }

    private static bool IsAutoNetworked(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field {type.Name}.{fieldName}.");
        return field!.IsDefined(typeof(AutoNetworkedFieldAttribute));
    }

    private static IEnumerable<Type> GetDeclaredMemberTypes(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance |
                                   BindingFlags.Public |
                                   BindingFlags.NonPublic |
                                   BindingFlags.DeclaredOnly;

        return type.GetFields(flags)
            .Select(field => field.FieldType)
            .Concat(type.GetProperties(flags).Select(property => property.PropertyType));
    }
}
