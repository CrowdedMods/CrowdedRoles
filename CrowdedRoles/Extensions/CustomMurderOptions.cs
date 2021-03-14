using System;

namespace CrowdedRoles.Extensions
{
    /// <summary>
    /// Murder options for <see cref="PlayerControlExtension.CustomMurderPlayer"/>
    /// </summary>
    [Flags] public enum CustomMurderOptions : uint
    {
        None        = 0,
        /// <summary>
        /// Force kill even if killer is not able to kill or/and already dead etc
        /// </summary>
        Force       = 1 << 1,
        /// <summary>
        /// Do not <see cref="CustomNetworkTransform.SnapTo"/> to a target
        /// </summary>
        NoSnap      = 1 << 2,
        /// <summary>
        /// Do not show kill animation
        /// </summary>
        NoAnimation = 1 << 3,
    }
}