using System;

namespace CrowdedRoles.Extensions
{
    [Flags] public enum CustomMurderOptions : uint
    {
        None        = 0,
        Force       = 1 << 1,
        NoSnap      = 1 << 2,
        NoAnimation = 1 << 3,
    }
}