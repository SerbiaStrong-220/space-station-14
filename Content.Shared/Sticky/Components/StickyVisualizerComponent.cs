using Robust.Shared.Serialization;

namespace Content.Shared.Sticky.Components;

﻿using DrawDepth;

/// <summary>
/// Sets the sprite's draw depth depending on whether it's stuck.
/// </summary>
[RegisterComponent]
public sealed partial class StickyVisualizerComponent : Component
{
    /// <summary>
    /// What sprite draw depth gets set to when stuck to something.
    /// </summary>
    [DataField]
    public int StuckDrawDepth = (int) DrawDepth.Overdoors;

    /// <summary>
    /// The sprite's original draw depth before being stuck.
    /// </summary>
    [DataField]
    public int OriginalDrawDepth;

    // SS220 rotate ent face to the user begin
    /// <summary>
    /// Is sprite not performing rotation when stuck
    /// </summary>
    [DataField]
    public bool StuckNoRotation = false;

    [DataField]
    public bool OriginalNoRotation;
    // SS220 rotate ent face to the user end
}

[Serializable, NetSerializable]
public enum StickyVisuals : byte
{
    IsStuck
}
