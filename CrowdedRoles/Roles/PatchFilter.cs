using System;

namespace CrowdedRoles.Roles
{
    /// <summary>
    /// Not fully implemented and needs some improving
    /// </summary>
    [Flags] public enum PatchFilter : uint
    {
        None            = 0,
        /// <summary>
        /// Do not show kill button
        /// </summary>
        KillButton      = 1 << 1,
        /// <summary>
        /// Do not create limit option for this role
        /// </summary>
        AmountOption    = 1 << 2,
        IntroCutScene   = 1 << 3,
        /// <summary>
        /// Do not set name colors/formatting in MeetingHud
        /// </summary>
        MeetingHud      = 1 << 4
    }
}