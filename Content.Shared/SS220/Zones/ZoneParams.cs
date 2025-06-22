// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using static Content.Shared.SS220.Zones.ZoneParams;

namespace Content.Shared.SS220.Zones;

[DataDefinition]
public sealed partial class ZoneParams
{
    public static readonly EntProtoId<ZoneComponent> DefaultZoneId = "BaseZone";
    public static readonly Color DefaultColor = Color.Gray;

    #region Fields
    /// <summary>
    /// The entity that this zone is assigned to.
    /// Used to determine local coordinates
    /// </summary>
    public EntityUid Container
    {
        get => _container ?? EntityUid.Invalid;
        set => TryChangeContainer(value);
    }

    [DataField("container"), ViewVariables(VVAccess.ReadOnly)]
    private EntityUid? _container;

    /// <summary>
    /// Name of the zone
    /// </summary>
    [DataField, ViewVariables]
    public string Name = string.Empty;

    /// <summary>
    /// ID of the zone's entity prototype
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId<ZoneComponent> ProtoID = DefaultZoneId;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    [DataField, ViewVariables]
    public Color Color = DefaultColor;

    /// <summary>
    /// Should the size of the zone be attached to the grid
    /// </summary>
    [DataField, ViewVariables]
    public bool AttachToGrid
    {
        get => _attachToGrid;
        set
        {
            _attachToGrid = value;
            RecalculateRegions();
        }
    }
    private bool _attachToGrid = false;

    /// <summary>
    /// Space cutting option.
    /// It only works if the <see cref="Container"/> is a grid
    /// </summary>
    [DataField, ViewVariables]
    public CutSpaceOptions CutSpaceOption
    {
        get => _cutSpaceOption;
        set
        {
            _cutSpaceOption = value;
            RecalculateRegions();
        }
    }
    private CutSpaceOptions _cutSpaceOption = CutSpaceOptions.None;

    /// <summary>
    /// Original size of the zone
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> OriginalRegion = new();

    /// <summary>
    /// Disabled zone size
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> DisabledRegion = new();

    /// <summary>
    /// The <see cref="OriginalRegion"/> with the cut-out <see cref="DisabledRegion"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> ActiveRegion = new();
    #endregion

    public ZoneParams()
    {
    }

    public ZoneParams(ZoneParams @params) : this()
    {
        CopyFrom(@params);
    }

    public ZoneParams(ZoneParamsState state) : this()
    {
        HandleState(state);
    }

    #region API
    public void ParseTags(string input)
    {
        // Cursed because GetFields() doesn't accesible on the client side

        var entMng = IoCManager.Resolve<IEntityManager>();
        if (TryParseTag(input, nameof(Container).ToLower(), out var value) &&
            NetEntity.TryParse(value, out var container))
            Container = entMng.GetEntity(container);

        if (TryParseTag(input, nameof(OriginalRegion).ToLower(), out value))
        {
            var boxesStrings = value.Split(";");
            var list = new List<Box2>();
            foreach (var str in boxesStrings)
            {
                if (MathHelperExtensions.TryParseBox2(str, out var box))
                    list.Add(box.Value);
            }
            OriginalRegion = list;
        }

        if (TryParseTag(input, nameof(Name).ToLower(), out value))
            Name = value;

        if (TryParseTag(input, nameof(ProtoID).ToLower(), out value))
            ProtoID = value;

        if (TryParseTag(input, nameof(Color).ToLower(), out value) &&
            Color.TryParse(value, out var color))
            Color = color;

        if (TryParseTag(input, nameof(AttachToGrid).ToLower(), out value) &&
            bool.TryParse(value, out var attach))
            AttachToGrid = attach;

        if (TryParseTag(input, nameof(CutSpaceOption).ToLower(), out value) &&
            Enum.TryParse<CutSpaceOptions>(value, out var cutSpaceOption))
            CutSpaceOption = cutSpaceOption;
    }

    private bool TryParseTag(string input, string tag, [NotNullWhen(true)] out string? value)
    {
        value = null;

        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(tag))
            return false;

        var pattern = @$"{Regex.Escape(tag)}=(?:""([^""]*)""|(\S+))";
        var regex = new Regex(pattern, RegexOptions.Compiled);

        var match = regex.Match(input);
        if (!match.Success)
            return false;

        for (var i = 1; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            if (group.Success)
                value = group.Value;
        }

        return value != null;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ZoneParams @params)
            return false;

        return Equals(@params);
    }

    public bool Equals(ZoneParams other)
    {
        var isFieldsEquals = Container == other.Container &&
            Name == other.Name &&
            ProtoID == other.ProtoID &&
            Color == other.Color &&
            AttachToGrid == other.AttachToGrid &&
            CutSpaceOption == other.CutSpaceOption;

        if (!isFieldsEquals)
            return false;

        var isRegionsEquals = IsRegionEquals(this, other, RegionType.Original) &&
            IsRegionEquals(this, other, RegionType.Disabled) &&
            IsRegionEquals(this, other, RegionType.Active);

        return isRegionsEquals;
    }

    public bool TryChangeContainer(EntityUid? newContainer)
    {
        if (newContainer != null)
        {
            var zonesSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();
            if (!zonesSys.IsValidContainer(newContainer.Value))
                return false;
        }

        _container = newContainer;
        return true;
    }

    public string[] GetTags()
    {
        // Cursed because GetFields() doesn't accesible on the client side

        var entMng = IoCManager.Resolve<IEntityManager>();
        var original = OriginalRegion.Select(b => b.ToString());
        var originalStr = string.Join("; ", original);

        return [
            $"{nameof(Container).ToLower()}={entMng.GetNetEntity(Container)}",
            $"{nameof(OriginalRegion).ToLower()}=\"{originalStr}\"",
            $"{nameof(Name).ToLower()}=\"{Name}\"",
            $"{nameof(ProtoID).ToLower()}=\"{ProtoID}\"",
            $"{nameof(Color).ToLower()}={Color.ToHex()}",
            $"{nameof(AttachToGrid).ToLower()}={AttachToGrid}",
            $"{nameof(CutSpaceOption).ToLower()}={CutSpaceOption}"
            ];
    }

    public void RecalculateRegions()
    {
        var original = OriginalRegion.AsEnumerable();
        var zoneSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();

        if (AttachToGrid)
            zoneSys.AttachToGrid(Container, ref original);

        IEnumerable<Box2> disabled = [];
        switch (CutSpaceOption)
        {
            case CutSpaceOptions.Dinamic:
                disabled = [.. zoneSys.GetSpaceBoxes(Container, original)];
                break;

            case CutSpaceOptions.Forever:
                zoneSys.CutSpace(Container, ref original, out _);
                break;
        }

        RecalculateRegion(ref disabled);
        DisabledRegion = [.. disabled];

        RecalculateRegion(ref original);
        OriginalRegion = [.. original];

        var active = MathHelperExtensions.SubstructBox(original, disabled);
        RecalculateRegion(ref active);
        ActiveRegion = [.. active];

        void RecalculateRegion(ref IEnumerable<Box2> boxes)
        {
            MathHelperExtensions.GetNonOverlappingBoxes(ref boxes);
            MathHelperExtensions.UnionInEqualSizedBoxes(ref boxes);
        }
    }

    public void SetOriginalSize(IEnumerable<Box2> newSize)
    {
        OriginalRegion = [.. newSize];
        RecalculateRegions();
    }

    public ZoneParams GetCopy()
    {
        return new ZoneParams(this);
    }

    public void CopyFrom(ZoneParams @params)
    {
        HandleState(@params.GetState());
    }

    public List<Box2> GetRegion(RegionType type)
    {
        return type switch
        {
            RegionType.Original => OriginalRegion,
            RegionType.Active => ActiveRegion,
            RegionType.Disabled => DisabledRegion,
            _ => ActiveRegion
        };
    }

    public static bool IsRegionEquals(ZoneParams left, ZoneParams right, RegionType region = RegionType.Original)
    {
        return IsRegionEquals(left, right, region, region);
    }

    public static bool IsRegionEquals(ZoneParams left, ZoneParams right, RegionType leftRegion, RegionType rightRegion)
    {
        var ourBoxes = GetSortedBoxes(left.GetRegion(leftRegion));
        var otherBoxes = GetSortedBoxes(right.GetRegion(rightRegion));
        return ourBoxes.SequenceEqual(otherBoxes);
    }

    public static IEnumerable<Box2> GetSortedBoxes(in IEnumerable<Box2> boxes)
    {
        var sorted = boxes.OrderBy(b => Box2.Area(b))
            .ThenBy(b => b.BottomLeft)
            .ThenBy(b => b.TopRight);

        return sorted;
    }

    public override int GetHashCode()
    {
        var sorted = GetSortedBoxes(OriginalRegion);
        return HashCode.Combine(Container, Name, ProtoID, Color, AttachToGrid, sorted);
    }

    public ZoneParamsState GetState()
    {
        var entMng = IoCManager.Resolve<IEntityManager>();
        return new ZoneParamsState(
            entMng.GetNetEntity(Container),
            Name,
            ProtoID,
            Color,
            AttachToGrid,
            CutSpaceOption,
            OriginalRegion,
            DisabledRegion,
            ActiveRegion);
    }

    public void HandleState(ZoneParamsState state)
    {
        var entMng = IoCManager.Resolve<IEntityManager>();
        _container = entMng.GetEntity(state.Container);
        Name = state.Name;
        ProtoID = state.ProtoID;
        Color = state.Color;
        _attachToGrid = state.AttachToGrid;
        _cutSpaceOption = state.CutSpaceOption;
        OriginalRegion = state.OriginalRegion;
        DisabledRegion = state.DisabledRegion;
        ActiveRegion = state.ActiveRegion;

        RecalculateRegions();
    }
    #endregion

    public static bool operator ==(ZoneParams? left, ZoneParams? right)
    {
        if (left is null)
            return right is null;
        else if (right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(ZoneParams? left, ZoneParams? right)
    {
        return !(left == right);
    }

    public enum CutSpaceOptions
    {
        None,
        Dinamic,
        Forever
    }

    public enum RegionType
    {
        Original,
        Active,
        Disabled
    }
}

[Serializable, NetSerializable]
public struct ZoneParamsState(
    NetEntity container,
    string name,
    EntProtoId<ZoneComponent> protoID,
    Color color,
    bool attachToGrid,
    CutSpaceOptions cutSpaceOption,
    List<Box2> originalRegion,
    List<Box2> disabledRegion,
    List<Box2> activeRegion)
{
    public NetEntity Container = container;
    public string Name = name;
    public EntProtoId<ZoneComponent> ProtoID = protoID;
    public Color Color = color;
    public bool AttachToGrid = attachToGrid;
    public CutSpaceOptions CutSpaceOption = cutSpaceOption;
    public List<Box2> OriginalRegion = originalRegion;
    public List<Box2> DisabledRegion = disabledRegion;
    public List<Box2> ActiveRegion = activeRegion;
}
