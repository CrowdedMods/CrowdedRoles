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
    /// <summary>
    /// Wrapper for role selecting
    /// </summary>
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

    /// <summary>
    /// Reveal role if exiled
    /// </summary>
    public enum RevealRole
    {
        Never,
        /// <summary>
        /// Depends on <see cref="GameOptionsData.ConfirmImpostor"/>
        /// </summary>
        Default,
        Always
    }
    
    public abstract class BaseRole
    {
        internal RoleData Data { get; }
        public byte Limit => RoleManager.Limits.GetValueOrDefault(this);
        
        public abstract string Name { get; }
        /// <summary>
        /// Color used in nicknames, cutscenes etc
        /// </summary>
        public abstract Color Color { get; }
        
        public virtual Team Team { get; } = Team.Crewmate;
        public virtual Visibility Visibility { get; } = Visibility.Myself;
        public virtual string Description { get; } = ""; // R.I.P. "do nothing but [FF0000FF]kiss" meme :C
        public virtual PatchFilter PatchFilterFlags { get; } = PatchFilter.None;
        public virtual RevealRole RevealExiledRole { get; } = RevealRole.Default;

        /// <summary>
        /// Can role holder kill or not. If <see cref="target"/> is null, api asks you, can this person kill in general (to enable kill button for example), so you should return true if <see cref="target"/> is null <br/>
        /// Be careful comparing it with local player, Host calls <c>CanKill(null)</c> on host-requested kill
        /// </summary>
        public virtual bool CanKill(PlayerControl? target) => false;
        public virtual bool CanVent(Vent vent) => false;
        /// <summary>
        /// Can role holder sabotage or not. If <see cref="sabotage"/> is null, api asks you, can this person sabotage in general (to enable sabotage button for example), so you should return true if <see cref="sabotage"/> is null
        /// </summary>
        public virtual bool CanSabotage(SystemTypes? sabotage) => false;
        /// <summary>
        /// Name formatting. Not tested and not fully implemented
        /// </summary>
        public virtual string FormatName(GameData.PlayerInfo player) => player.PlayerName;
        /// <summary>
        /// "Prefix" when role holder tries to kill. Obsolete and gonna be removed with event system
        /// </summary>
        public virtual bool PreKill(ref PlayerControl killer, ref PlayerControl target, ref CustomMurderOptions options) => true;

        /// <summary>
        /// Custom task selecting at game start. Not being called on death yet
        /// </summary>
        /// <param name="taskList">Task list you should change with its methods</param>
        /// <param name="defaultTasks">Tasks that were assigned by host with default game rules</param>
        public virtual void AssignTasks(PlayerTaskList taskList, IEnumerable<GameData.TaskInfo> defaultTasks)
        {
            taskList.AddNormalTasks(defaultTasks);
        }

        /// <summary>
        /// Called when <see cref="player"/> gets this role
        /// </summary>
        public virtual void OnRoleAssign(PlayerControl player)
        {
        }
        
        /// <summary>
        /// Custom role selecting, random by default.
        /// </summary>
        /// <param name="holders">Info about all players and their roles. Compare this with <see cref="RoleHolders.Impostors"/> to know if player is an impostor, because <see cref="GameData.PlayerInfo.IsImpostor"/> is not set at this moment</param>
        /// <param name="limit">Not important, just a value set in game settings or wherever else</param>
        /// <returns></returns>
        public virtual IEnumerable<GameData.PlayerInfo> SelectHolders(RoleHolders holders, byte limit)
        {
            var rand = new System.Random();
            return holders.Crewmates.OrderBy(_ => rand.Next()).Take(limit).ToList();
        }

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