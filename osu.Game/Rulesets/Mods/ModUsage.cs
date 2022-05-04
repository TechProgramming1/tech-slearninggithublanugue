// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Mods
{
    /// <summary>
    /// The context in which a <see cref="Mod"/> is playable.
    /// </summary>
    public enum ModUsage
    {
        /// <summary>
        /// This mod can be used for a per-user gameplay session.
        /// </summary>
        SoloLocal,

        /// <summary>
        /// This mod can be used in multiplayer but must be applied to all users.
        /// This is generally the case for mods which affect the length of gameplay.
        /// </summary>
        MultiplayerGlobal,

        /// <summary>
        /// This mod can be used in multiplayer either at a room or per-player level (i.e. "free mod").
        /// </summary>
        MultiplayerLocal,
    }
}
