// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Zones;

//[DataDefinition]
//public sealed partial class ZoneParams
//{
//    public override bool Equals(object? obj)
//    {
//        if (obj is not ZoneParams @params)
//            return false;

//        return Equals(@params);
//    }

//    public bool Equals(ZoneParams other)
//    {
//        var isFieldsEquals = Container == other.Container &&
//            Name == other.Name &&
//            ProtoID == other.ProtoID &&
//            Color == other.Color &&
//            AttachToGrid == other.AttachToGrid &&
//            CutSpaceOption == other.CutSpaceOption;

//        if (!isFieldsEquals)
//            return false;

//        var isRegionsEquals = IsRegionEquals(this, other, RegionType.Original) &&
//            IsRegionEquals(this, other, RegionType.Disabled) &&
//            IsRegionEquals(this, other, RegionType.Active);

//        return isRegionsEquals;
//    }

//    public bool TryChangeContainer(EntityUid? newContainer)
//    {
//        if (newContainer != null)
//        {
//            var zonesSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();
//            if (!zonesSys.IsValidContainer(newContainer.Value))
//                return false;
//        }

//        _container = newContainer;
//        return true;
//    }

//    public void RecalculateRegions()
//    {
//        var original = OriginalRegion.AsEnumerable();
//        var zoneSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();

//        if (AttachToGrid)
//            zoneSys.AttachToGrid(Container, ref original);

//        IEnumerable<Box2> disabled = [];
//        switch (CutSpaceOption)
//        {
//            case CutSpaceOptions.Dinamic:
//                disabled = [.. zoneSys.GetSpaceBoxes(Container, original)];
//                break;

//            case CutSpaceOptions.Forever:
//                zoneSys.CutSpace(Container, ref original, out _);
//                break;
//        }

//        RecalculateRegion(ref disabled);
//        DisabledRegion = [.. disabled];

//        RecalculateRegion(ref original);
//        OriginalRegion = [.. original];

//        var active = MathHelperExtensions.SubstructBox(original, disabled);
//        RecalculateRegion(ref active);
//        ActiveRegion = [.. active];

//        void RecalculateRegion(ref IEnumerable<Box2> boxes)
//        {
//            MathHelperExtensions.GetNonOverlappingBoxes(ref boxes);
//            MathHelperExtensions.UnionInEqualSizedBoxes(ref boxes);
//        }
//    }

//    public void SetOriginalSize(IEnumerable<Box2> newSize)
//    {
//        OriginalRegion = [.. newSize];
//        RecalculateRegions();
//    }

//    public ZoneParams GetCopy()
//    {
//        return new ZoneParams(this);
//    }

//    public void CopyFrom(ZoneParams @params)
//    {
//        HandleState(@params.GetState());
//    }

//    public List<Box2> GetRegion(RegionType type)
//    {
//        return type switch
//        {
//            RegionType.Original => OriginalRegion,
//            RegionType.Active => ActiveRegion,
//            RegionType.Disabled => DisabledRegion,
//            _ => ActiveRegion
//        };
//    }

//    public static bool IsRegionEquals(ZoneParams left, ZoneParams right, RegionType region = RegionType.Original)
//    {
//        return IsRegionEquals(left, right, region, region);
//    }

//    public static bool IsRegionEquals(ZoneParams left, ZoneParams right, RegionType leftRegion, RegionType rightRegion)
//    {
//        var ourBoxes = GetSortedBoxes(left.GetRegion(leftRegion));
//        var otherBoxes = GetSortedBoxes(right.GetRegion(rightRegion));
//        return ourBoxes.SequenceEqual(otherBoxes);
//    }

//    public static IEnumerable<Box2> GetSortedBoxes(in IEnumerable<Box2> boxes)
//    {
//        var sorted = boxes.OrderBy(b => Box2.Area(b))
//            .ThenBy(b => b.BottomLeft)
//            .ThenBy(b => b.TopRight);

//        return sorted;
//    }

//    public override int GetHashCode()
//    {
//        var sorted = GetSortedBoxes(OriginalRegion);
//        return HashCode.Combine(Container, Name, ProtoID, Color, AttachToGrid, sorted);
//    }

//    public ZoneParamsState GetState()
//    {
//        var entMng = IoCManager.Resolve<IEntityManager>();
//        return new ZoneParamsState(
//            entMng.GetNetEntity(Container),
//            Name,
//            ProtoID,
//            Color,
//            AttachToGrid,
//            CutSpaceOption,
//            OriginalRegion,
//            DisabledRegion,
//            ActiveRegion);
//    }

//    public void HandleState(ZoneParamsState state)
//    {
//        var entMng = IoCManager.Resolve<IEntityManager>();
//        _container = entMng.GetEntity(state.Container);
//        Name = state.Name;
//        ProtoID = state.ProtoID;
//        Color = state.Color;
//        _attachToGrid = state.AttachToGrid;
//        _cutSpaceOption = state.CutSpaceOption;
//        OriginalRegion = state.OriginalRegion;
//        DisabledRegion = state.DisabledRegion;
//        ActiveRegion = state.ActiveRegion;

//        RecalculateRegions();
//    }

//    public static bool operator ==(ZoneParams? left, ZoneParams? right)
//    {
//        if (left is null)
//            return right is null;
//        else if (right is null)
//            return false;

//        return left.Equals(right);
//    }

//    public static bool operator !=(ZoneParams? left, ZoneParams? right)
//    {
//        return !(left == right);
//    }
//}

//[Serializable, NetSerializable]
//public struct ZoneParamsState(
//    NetEntity container,
//    string name,
//    EntProtoId<ZoneComponent> protoID,
//    Color color,
//    bool attachToGrid,
//    CutSpaceOptions cutSpaceOption,
//    List<Box2> originalRegion,
//    List<Box2> disabledRegion,
//    List<Box2> activeRegion)
//{
//    public NetEntity Container = container;
//    public string Name = name;
//    public EntProtoId<ZoneComponent> ProtoID = protoID;
//    public Color Color = color;
//    public bool AttachToGrid = attachToGrid;
//    public CutSpaceOptions CutSpaceOption = cutSpaceOption;
//    public List<Box2> OriginalRegion = originalRegion;
//    public List<Box2> DisabledRegion = disabledRegion;
//    public List<Box2> ActiveRegion = activeRegion;
//}
