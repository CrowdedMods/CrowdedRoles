using System;
using UnityEngine;

namespace CrowdedRoles.Api.Roles
{
    public abstract class BaseRole
    {
        public byte Id { get; internal set; }
        
        public bool AbleToKill { get; set; } = false;
        
        public abstract string Name { get; }
        public abstract Color Color { get; }
        
        public virtual Side Side { get; } = Side.Crewmate;
        public virtual Visibility Visibility { get; } = Visibility.Myself;
        public virtual string StartTip { get; } = "Do nothing but [FF0000FF]kiss";
        public virtual PatchFilter PatchFilterFlags { get; } = PatchFilter.None;
    
        public virtual string FormatName(string name)
        {
            return name;
        }

        public virtual bool KillFilter(PlayerControl me, GameData.PlayerInfo target)
        {
            return !target.Disconnected &&
                   me.PlayerId != target.PlayerId &&
                   !target.IsDead &&
                   !target.IsImpostor;
        }
    }
}