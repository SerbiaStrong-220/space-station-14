using Content.Shared.SS220.Felinid;

namespace Content.Client.SS220.Felinid;

/// <summary>
/// Client-side host for the shared recoil impulse so shooting stays predicted.
/// The knockdown roll is server-authoritative and lives in the server system.
/// </summary>
public sealed class GunRecoilModifierSystem : SharedGunRecoilModifierSystem;
