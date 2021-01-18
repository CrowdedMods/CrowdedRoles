using UnityEngine;
using System;

namespace CrowdedRoles.Api.Roles
{
    public class CustomRole
    {
        internal byte Id;
        private static bool DefaultKillFilter(PlayerControl me, GameData.PlayerInfo target)
        {
            return 
                !target.Disconnected &&
                me.PlayerId != target.PlayerId &&
                !target.IsDead &&
                !target.IsImpostor;
        }

        public readonly string Name;
        public readonly Color Color;
        public Func<PlayerControl, GameData.PlayerInfo, bool> KillFilter = DefaultKillFilter;
        public bool AbleToKill = false;
        public Func<string, string> NameFormat = s => s;
        public string StartTip = "Do nothing but [00FF00FF]kiss";
        public Side Side = Side.Crewmate;
        public Visibility Visibility = Visibility.Myself;
        public PatchFilter PatchFilterFlags = 0;

        public CustomRole(string name, Color color = new Color())
        {
            Name = name;
            Color = color;
        }

        public bool Equals(CustomRole? other)
        {
            return Id == other?.Id;
        }
    }
}
