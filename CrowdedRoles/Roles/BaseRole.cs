using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BepInEx;
using BepInEx.IL2CPP;
using CrowdedRoles.Extensions;
using CrowdedRoles.Options;
using UnityEngine;

namespace CrowdedRoles.Roles
{
    public class RoleHolders
    {
        public ReadOnlyCollection<GameData.PlayerInfo> AllPlayers { get; }
        public List<GameData.PlayerInfo> Impostors { get; }

        public IEnumerable<GameData.PlayerInfo> Crewmates =>
            AllPlayers.Where(p => 
                Impostors.All(pl => pl.PlayerId != p.PlayerId) && 
                !CustomRoleHolders.Values.Any(h => h.Contains(p))
            );

        public Dictionary<BaseRole, IEnumerable<GameData.PlayerInfo>> CustomRoleHolders { get; init; } = new();

        internal RoleHolders(IEnumerable<GameData.PlayerInfo> players, IEnumerable<GameData.PlayerInfo> impostors)
        {
            AllPlayers = players.ToList().AsReadOnly();
            Impostors = impostors.ToList();
        }
    }
    public abstract class BaseRole
    {
        internal RoleData Data { get; }
        public byte Limit => RoleManager.Limits.GetValueOrDefault(this);
        
        public abstract string Name { get; }
        public abstract Color Color { get; }
        
        public virtual Team Team { get; } = Team.Crewmate;
        public virtual Visibility Visibility { get; } = Visibility.Myself;
        public virtual string Description { get; } = "Do nothing but [FF0000FF]kiss";
        public virtual PatchFilter PatchFilterFlags { get; } = PatchFilter.None;

        public virtual bool CanKill(PlayerControl? target) => false;
        public virtual bool CanVent(Vent vent) => false;
        public virtual bool CanSabotage(SystemTypes? sabotage) => false;

        protected BaseRole(BasePlugin plugin)
        {
            var guid = MetadataHelper.GetMetadata(plugin).GUID;
            
            if (!RoleManager.Roles.ContainsKey(guid))
            {
                RoleManager.Roles.Add(guid, new Dictionary<byte, BaseRole>());
            }

            Dictionary<byte, BaseRole> localRoles = RoleManager.Roles[guid];
            Data = new RoleData(guid, (byte)localRoles.Count);
            
            localRoles.Add((byte)localRoles.Count, this);
            RoleManager.Roles[guid] = localRoles;
            RoleManager.Limits.Add(this, 0);
            OptionsManager.AddLimitOptionIfNecessary(this, guid);
        }
        
        public virtual string FormatName(GameData.PlayerInfo player) => player.PlayerName;

        public virtual bool PreKill(ref PlayerControl killer, ref PlayerControl target, ref CustomMurderOptions options)
        {
            return true;
        }

        public virtual IEnumerable<GameData.PlayerInfo> SelectHolders(RoleHolders holders, byte limit)
        {
            var rand = new System.Random();
            return holders.Crewmates.OrderBy(_ => rand.Next()).Take(limit).ToList();
        }

        public static bool operator ==(BaseRole? me, BaseRole? other) => me?.Data == other?.Data;
        public static bool operator !=(BaseRole? me, BaseRole? other) => me?.Data != other?.Data;
        private bool Equals(BaseRole other)
        {
            return Data.Equals(other.Data);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) &&
                   obj.GetType() == GetType() && 
                   Equals((BaseRole) obj);
        }

        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }
}