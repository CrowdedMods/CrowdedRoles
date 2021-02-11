using System;

namespace CrowdedRoles.Roles
{
    [Flags] public enum PlayerAbilities : byte
    {
        None     = 0,
        Kill     = 1 << 1,
        Sabotage = 1 << 2,
        Vent     = 1 << 3,
    }
}