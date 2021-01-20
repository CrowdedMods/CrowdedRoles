using System;

namespace CrowdedRoles.Api.Roles
{
    [Flags] public enum PatchFilter : uint
    {
        None            = 0,
        KillButton      = 1 << 1,
        AmountOption    = 1 << 2,
        IntroCutScene   = 1 << 3,
        MeetingHud      = 1 << 4,
        SelectInfected  = 1 << 5
    }
}