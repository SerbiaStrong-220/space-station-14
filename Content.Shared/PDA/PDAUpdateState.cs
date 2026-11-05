using Content.Shared.CartridgeLoader;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public sealed class PdaUpdateState : CartridgeLoaderUiState // WTF is this. what. I ... fuck me I just want net entities to work
        // TODO purge this shit
        //AAAAAAAAAAAAAAAA
    {
        public bool FlashlightEnabled;
        public bool HasPen;
        public bool HasPai;
        public PdaIdInfoText PdaOwnerInfo;
        public PdaIdExtendedInfo PdaOwnerInfoExtended; // ss220 add additional info for pda
        public string? StationName;
        public bool HasUplink;
        public bool CanPlayMusic;
        public string? Address;

        public PdaUpdateState(
            List<NetEntity> programs,
            NetEntity? activeUI,
            bool flashlightEnabled,
            bool hasPen,
            bool hasPai,
            PdaIdInfoText pdaOwnerInfo,
            PdaIdExtendedInfo pdaOwnerInfoExtended, // ss220 add additional info for pda
            string? stationName,
            bool hasUplink = false,
            bool canPlayMusic = false,
            string? address = null)
            : base(programs, activeUI)
        {
            FlashlightEnabled = flashlightEnabled;
            HasPen = hasPen;
            HasPai = hasPai;
            PdaOwnerInfo = pdaOwnerInfo;
            PdaOwnerInfoExtended = pdaOwnerInfoExtended; // ss220 add additional info for pda
            HasUplink = hasUplink;
            CanPlayMusic = canPlayMusic;
            StationName = stationName;
            Address = address;
        }
    }

    [Serializable, NetSerializable]
    public struct PdaIdInfoText
    {
        public string? ActualOwnerName;
        public string? IdOwner;
        public string? JobTitle;
        //ss220 add color for job in pda start
        public string? CardColor;
        //ss220 add color for job in pda end
        public string? StationAlertLevel;
        public Color StationAlertColor;
    }

    // ss220 add additional info for pda start
    [Serializable, NetSerializable]
    public struct PdaIdExtendedInfo
    {
        public GeneralStationRecord? Record;
        public NetEntity? IdCard;
    }
    // ss220 add additional info for pda end
}
