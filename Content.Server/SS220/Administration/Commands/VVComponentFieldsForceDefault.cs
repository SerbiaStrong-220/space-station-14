// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Created special for SS200 with love by Alan Wake (https://github.com/aw-c)

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;
using System.Linq;
using System.Reflection;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed partial class VVComponentFieldsForceDefault : LocalizedCommands
{
    public override string Command => "vvcffd";
    public override string Description => Loc.GetString("vv-componentfildsforcedefault-desc");
    public override string Help => Loc.GetString("vv-componentfildsforcedefault-help");

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var length = args.Length;

        if (length < 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity))
        {
            shell.WriteLine(Loc.GetString("vv-componentfildsforcedefault-error-invalid-uid"));
            return;
        }

        if (!_entityManager.TryGetEntity(netEntity, out var uid))
        {
            shell.WriteLine(Loc.GetString("vv-componentfildsforcedefault-error-notpresentedonserver"));
            return;
        }

        var comps = _entityManager.GetComponents(uid.Value);

        var lookUpName = args[1].ToLower();

        var exists = comps.Any(x => x.GetType().Name.ToLower() == lookUpName);

        if (!exists)
        {
            shell.WriteLine(Loc.GetString("vv-componentfildsforcedefault-error-notexistsonentity"));
            return;
        }

        var foundComp = comps.First(x => x.GetType().Name.ToLower() == lookUpName);
        var members = foundComp
            .GetType()
            .GetMembers()
            .Where(x => x.DeclaringType != typeof(Component))
            .Where(x => x.MemberType == MemberTypes.Property || x.MemberType == MemberTypes.Field);

        var membersLength = members.Count();

        List<string> proceedMembers = new(membersLength);
        List<string> erorredMembers = new(membersLength);

        foreach (var member in members)
        {
            var isProperty = member.MemberType == MemberTypes.Property;

            // простите меня пожалуйста.
            var realType = Nullable
                .GetUnderlyingType(isProperty
                ? (member as PropertyInfo)!.PropertyType
                : (member as FieldInfo)!.FieldType);

            try
            {
                object valueToSet;

                dynamic info = isProperty ? (member as PropertyInfo)! : (member as FieldInfo)!;

                if (realType == null)
                {
                    if (isProperty)
                        realType = info.PropertyType;
                    else
                        realType = info.FieldType;
                }

                if (realType.IsAssignableFrom(typeof(NetEntity))
                    || realType.IsAssignableFrom(typeof(EntityUid))
                    || realType.IsAssignableFrom(typeof(IAsType<EntityUid>)))
                    throw new("Potential UB");

                if (realType.IsGenericType
                    && realType.GetGenericTypeDefinition() == typeof(ProtoId<>)
                    && typeof(IPrototype).IsAssignableFrom(realType.GetGenericArguments()[0]))
                {
                    Type prototypeType = realType.GetGenericArguments()[0];

                    var suitableProto = _proto.EnumeratePrototypes(prototypeType).First();

                    if (suitableProto is null)
                        throw new($"Not found prototypes for type {prototypeType}");

                    string protoId = suitableProto.ID;

                    var protoIdType = typeof(ProtoId<>).MakeGenericType(prototypeType);
                    var constructor = protoIdType.GetConstructor(new[] { typeof(string) });

                    if (constructor == null)
                        throw new InvalidOperationException($"Constructor with string parameter not found for {protoIdType}");

                    valueToSet = constructor.Invoke(new object[] { protoId });
                }
                else
                {
                    if (realType.IsAssignableFrom(typeof(string)))
                        valueToSet = string.Empty;
                    else
                        valueToSet = CreateInstanceWithDefaults(realType);
                }

                info.SetValue(foundComp, valueToSet);

                proceedMembers.Add(member.Name);
            }
            catch (Exception)
            {
                erorredMembers.Add(member.Name);
            }
        }

        bool hasAnyOutput = false;

        if (erorredMembers.Count > 0)
        {
            shell.WriteError($"Unable to fill default with members: {String.Join(", ", erorredMembers)}");
            hasAnyOutput = true;
        }

        if (proceedMembers.Count > 0)
        {
            shell.WriteLine($"Successfully filled members: {String.Join(", ", proceedMembers)}");
            hasAnyOutput = true;
        }

        if (!hasAnyOutput)
            shell.WriteLine($"Looks like component doesn't have any suitable members.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {

        switch (args.Length)
        {
            case 1:
                return CompletionResult
                    .FromHint(Loc.GetString("vv-componentfildsforcedefault-hint-1"));

            case 2:
                if (NetEntity.TryParse(args[0], out var netEntity)
                    && _entityManager.TryGetEntity(netEntity, out var uid))
                {
                    return CompletionResult
                        .FromHintOptions(_entityManager.GetComponents(uid.Value).Select(x => x.GetType().Name), null);
                }

                return CompletionResult
                    .FromHint(Loc.GetString("vv-componentfildsforcedefault-hint-1-notfound", ("uid", args[0])));

            // TODO: Перенести команду из Server в Shared.
            //case >= 3:
            //    return CompletionResult
            //        .FromHint(Loc.GetString("vv-componentfildsforcedefault-hint-3"));

            default:
                return CompletionResult.Empty;
        }
    }

    private object CreateInstanceWithDefaults(Type type)
    {
        object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        var constructor = constructors.FirstOrDefault();

        if (constructor == null)
            throw new InvalidOperationException("No public constructors found");

        var parameters = constructor.GetParameters();

        var args = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
            args[i] = GetDefaultValue(parameters[i].ParameterType);

        return Activator.CreateInstance(type, args)!;
    }
}
