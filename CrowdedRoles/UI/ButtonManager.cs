using System.Collections.Generic;

namespace CrowdedRoles.UI
{
    internal static class ButtonManager
    {
        public static List<CooldownButton> RegisteredButtons { get; } = new();
        public static List<CooldownButton> ActiveButtons { get; } = new();
    }
}