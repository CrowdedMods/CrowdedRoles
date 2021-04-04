using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using Reactor;
using Reactor.Extensions;
using Reactor.Networking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrowdedRoles.Extensions
{
    /// <summary>
    /// Extensions for <see cref="PlayerControl"/> and <see cref="GameData.PlayerInfo"/>
    /// </summary>
    public static class PlayerControlExtension
    {
        private static readonly int Mask = Shader.PropertyToID("_Mask");

        internal static void InitRole(this PlayerControl player, BaseRole? role = null)
        {
            if (role == null) return;
            
            if (!RoleManager.PlayerRoles.TryAdd(player.PlayerId, role))
            { 
                RoleApiPlugin.Logger.LogWarning($"{player.PlayerId} already has a role, redefining..."); 
                RoleManager.PlayerRoles[player.PlayerId] = role;
            }
        }

        public static BaseRole? GetRole(this PlayerControl player)
            => player.Data.GetRole();

        public static BaseRole? GetRole(this GameData.PlayerInfo player)
            => RoleManager.PlayerRoles.GetValueOrDefault(player.PlayerId);

        /// <summary>
        /// Get a specific role if player has it, otherwise null
        /// </summary>
        public static T? GetRole<T>(this PlayerControl player) where T : BaseRole
            => player.GetRole() as T;

        /// <summary>
        /// Get a specific role if player has it, otherwise null
        /// </summary>
        public static T? GetRole<T>(this GameData.PlayerInfo player) where T : BaseRole
            => player.GetRole() as T;

        public static bool Is<T>(this PlayerControl player) where T : BaseRole
            => player.GetRole<T>() != null;

        public static bool Is<T>(this GameData.PlayerInfo player) where T : BaseRole
            => player.GetRole<T>() != null;

        public static bool HasCustomRole(this GameData.PlayerInfo player)
            => player.GetRole() != null;
        
        public static bool HasCustomRole(this PlayerControl player)
            => player.GetRole() != null;

        /// <summary>
        /// True if <see cref="player"/> has custom role or an Impostor
        /// </summary>
        public static bool HasRole(this GameData.PlayerInfo player)
            => player.HasCustomRole() || player.IsImpostor;

        /// <summary>
        /// True if <see cref="player"/> has custom role or an Impostor
        /// </summary>
        public static bool HasRole(this PlayerControl player)
            => player.Data.HasRole();

        /// <summary>
        /// True if <see cref="player"/> has permissions to kill <see cref="target"/><br/>
        /// or <see cref="player"/> is an Impostor and <see cref="target"/> is not
        /// </summary>
        public static bool CanKill(this GameData.PlayerInfo player, PlayerControl? target)
            => player.GetRole()?.CanKill(target) ?? player.IsImpostor && (target == null || target.GetTeam() != Team.Impostor);

        /// <summary>
        /// True if <see cref="player"/> has permissions to kill <see cref="target"/><br/>
        /// or <see cref="player"/> is an Impostor and <see cref="target"/> is not
        /// </summary>
        public static bool CanKill(this PlayerControl player, PlayerControl? target)
            => player.Data.CanKill(target);

        /// <summary>
        /// Works both for custom roles and impostors. Complicated.<br/>
        /// May be replaced with <see cref="IsTeamedWithNonCrew(PlayerControl,GameData.PlayerInfo)"/> in future
        /// </summary>
        public static bool IsTeamedWith(this PlayerControl me, PlayerControl other)
            => me.IsTeamedWith(other.Data);

        /// <summary>
        /// Works both for custom roles and impostors. Complicated.<br/>
        /// May be replaced with <see cref="IsTeamedWithNonCrew(PlayerControl,GameData.PlayerInfo)"/> in future
        /// </summary>
        public static bool IsTeamedWith(this PlayerControl me, GameData.PlayerInfo other)
            => me.Data.IsTeamedWith(other);

        /// <summary>
        /// Works both for custom roles and impostors. Complicated.<br/>
        /// May be replaced with <see cref="IsTeamedWithNonCrew(GameData.PlayerInfo,GameData.PlayerInfo)"/> in future
        /// </summary>
        public static bool IsTeamedWith(this GameData.PlayerInfo me, GameData.PlayerInfo other)
        {
            BaseRole? role = me.GetRole();
            BaseRole? theirRole = other.GetRole();
            return role?.Team switch
            {
                Team.Alone => me.PlayerId == other.PlayerId,
                Team.Crewmate => true,
                Team.Impostor => other.GetTeam() == Team.Impostor,
                Team.SameRole => role == theirRole,
                _ => theirRole == null
                    ? me.IsImpostor == other.IsImpostor
                    : other.IsTeamedWith(me)
            };
        }

        /// <summary>
        /// Checks if players are teamed, but returns false if <see cref="other"/> is a crewmate. Complicated and needs reworking
        /// </summary>
        public static bool IsTeamedWithNonCrew(this GameData.PlayerInfo me, GameData.PlayerInfo other)
        {
            var myRole = me.GetRole();
            return myRole?.Team switch
            {
                Team.Crewmate => myRole == other.GetRole(),
                _ => me.IsTeamedWith(other)
            };
        }

        /// <summary>
        /// Checks if players are teamed, but returns false if <see cref="other"/> is a crewmate. Complicated and needs reworking
        /// </summary>
        public static bool IsTeamedWithNonCrew(this PlayerControl me, GameData.PlayerInfo other)
            => me.Data.IsTeamedWithNonCrew(other);
        
        public static IEnumerable<PlayerControl> GetTeammates(this PlayerControl player)
        {
            return GameData.Instance.AllPlayers
                .ToArray()
                .Where(p => !p.Disconnected && p.Object != null && p.Object.IsTeamedWith(player))
                .Select(p => p.Object)
                .ToList();
        }

        /// <summary>
        /// Gets <see cref="player"/>'s <see cref="Team"/> even if they don't have a role
        /// </summary>
        public static Team GetTeam(this PlayerControl player) => player.Data.GetTeam();

        /// <summary>
        /// Gets <see cref="player"/>'s <see cref="Team"/> even if they don't have a role
        /// </summary>
        public static Team GetTeam(this GameData.PlayerInfo player)
            => player.GetRole()?.Team ?? (player.IsImpostor ? Team.Impostor : Team.Crewmate);

        public static TaskCompletion GetTaskCompletion(this GameData.PlayerInfo player)
        {
            if (!RoleManager.TaskCompletions.TryGetValue(player.PlayerId, out TaskCompletion result))
            {
                result = player.IsImpostor ? TaskCompletion.Fake :
                    player.IsDead && !PlayerControl.GameOptions.GhostsDoTasks ? TaskCompletion.Optional : TaskCompletion.Required;
            }

            return result;
        }
        
        public static TaskCompletion GetTaskCompletion(this PlayerControl player)
            => player.Data.GetTaskCompletion();

        internal static IEnumerator CustomSetTasks(this PlayerControl player, PlayerTaskList tasks)
        {
            while (!ShipStatus.Instance)
            {
                yield return null;
            }
            BaseRole? role = player.GetRole();
            if (player.AmOwner)
            {
                HudManager.Instance.TaskStuff.SetActive(true);
                StatsManager.Instance.GamesStarted += 1;
                if (player.Data.IsImpostor)
                {
                    StatsManager.Instance.TimesImpostor += 1;
                    StatsManager.Instance.CrewmateStreak = 0;
                    HudManager.Instance.KillButton.gameObject.SetActive(true);
                } else if (role == null)
                {
                    StatsManager.Instance.TimesCrewmate += 1;
                    StatsManager.Instance.CrewmateStreak += 1;
                    HudManager.Instance.KillButton.gameObject.SetActive(false);
                }
                else
                {
                    StatsManager.Instance.CrewmateStreak = 0;
                    HudManager.Instance.KillButton.gameObject.SetActive(role.CanKill(null));
                }
            }
            
            global::Extensions.LCBABMOODEH(player.myTasks.Cast<Il2CppSystem.Collections.Generic.IList<PlayerTask>>()); //player.myTasks.Cast<Il2CppSystem.Collections.Generic.IList<PlayerTask>>().LCBABMOODEH(); // DestroyAll
            player.myTasks = new Il2CppSystem.Collections.Generic.List<PlayerTask>(tasks.NormalTasks.Count + tasks.StringTasks.Count);
            for (int i = 0; i < player.myTasks.Capacity; i++)
            {
                player.myTasks.Add(null);
            }

            if (!RoleManager.TaskCompletions.TryAdd(player.PlayerId, tasks.TaskCompletion))
            {
                RoleManager.TaskCompletions[player.PlayerId] = tasks.TaskCompletion;
            }

            if (player.Data.IsImpostor)
            {
                var gameObject = new GameObject("_Player");
                gameObject.transform.SetParent(player.transform, false);
                ImportantTextTask task = gameObject.AddComponent<ImportantTextTask>();
                task.transform.SetParent(player.transform, false);
                task.Text = TranslationController.Instance.GetString(StringNames.ImpostorTask, Array.Empty<Il2CppSystem.Object>()) +
                            "\n[FFFFFFFF]" +
                            TranslationController.Instance.GetString(StringNames.FakeTasks, Array.Empty<Il2CppSystem.Object>());
                player.myTasks.Insert(0, task);
            }

            byte k = 0;
            foreach (var task in tasks.NormalTasks)
            {
                var normalTask = Object.Instantiate(ShipStatus.Instance.GetTaskById(task.TypeId), player.transform);
                normalTask.Id = k++;
                normalTask.Owner = player;
                normalTask.Initialize();
                player.myTasks[(Index) (int)task.Id] = normalTask;
            }

            foreach (var (id, text) in tasks.StringTasks)
            {
                Logger<RoleApiPlugin>.Message($"{id}:{text}");
                var gameObject = new GameObject($"CustomStringTask_{id}");
                gameObject.transform.SetParent(player.transform, false);
                ImportantTextTask task = gameObject.AddComponent<ImportantTextTask>();
                task.Text = "[FFFFFFFF]" + text; // haha funny   
                player.myTasks[(Index) id] = task;
            }
        }

        /// <summary>
        /// Sends a host-requested kill with <see cref="CustomMurderPlayer"/><br/>
        /// To avoid desync we've implemented host-requested kills:<br/>
        /// Every player sends a request to a host when presses kill button. Host checks if you're able to kill, not dead etc and then executes it
        /// </summary>
        public static void RpcCustomMurderPlayer(this PlayerControl me, PlayerControl target, CustomMurderOptions options = CustomMurderOptions.None)
        {
            if (!options.HasFlag(CustomMurderOptions.Force) && !(me.GetRole()?.PreKill(ref me, ref target, ref options) ?? true)) 
            {
                RoleApiPlugin.Logger.LogDebug($"Custom kill ({me.PlayerId} -> {target.PlayerId}) is cancelled by a plugin");
                return;
            }
            
            Rpc<CmdCustomKill>.Instance.SendTo(AmongUsClient.Instance.HostId, new CmdCustomKill.Data
            {
                target = target,
                options = options
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_">host</param>
        /// <param name="target">target's <see cref="InnerNet.InnerNetObject.OwnerId"/> to send an rpc to, -1 if to everyone</param>
        public static void RpcSyncCustomSettings(this PlayerControl _, int target = -1)
        {
            var data = new SyncCustomSettings.Data
            {
                limits = RoleManager.EditableLimits.ToDictionary(p => p.Key.Data, p => p.Value),
                options = OptionsManager.CustomOptions.ToDictionary(p => p.Key,
                    p => p.Value.Select(o => o.ToBytes()).ToList())
            };
            if (target == -1)
            {
                Rpc<SyncCustomSettings>.Instance.Send(data);
            }
            else
            {
                Rpc<SyncCustomSettings>.Instance.SendTo(target, data);
            }
        }

        /// <summary>
        /// True if <see cref="me"/> can see <see cref="other"/>'s role. Complicated.
        /// May be replaced with <see cref="CanSeeSpecial(GameData.PlayerInfo,GameData.PlayerInfo)"/> in future
        /// </summary>
        public static bool CanSee(this GameData.PlayerInfo me, GameData.PlayerInfo other)
        {
            return other.GetRole()?.Visibility switch
            {
                Visibility.Myself => me.PlayerId == other.PlayerId,
                Visibility.Team => other.IsTeamedWith(me),
                Visibility.Everyone => true,
                _ =>  !other.IsImpostor || me.GetRole()?.Team == Team.Impostor
            };
        }

        /// <summary>
        /// True if <see cref="me"/> can see <see cref="other"/>'s role. Complicated.
        /// May be replaced with <see cref="CanSeeSpecial(PlayerControl,PlayerControl)"/> in future
        /// </summary>
        public static bool CanSee(this PlayerControl me, PlayerControl other)
        {
            BaseRole? role = other.GetRole();
            return role?.Visibility switch
            {
                Visibility.Myself => me == other,
                Visibility.Team => other.IsTeamedWith(me),
                Visibility.Everyone => true,
                _ =>  !other.Data.IsImpostor || me.GetRole()?.Team == Team.Impostor
            };
        }

        /// <summary>
        /// True if <see cref="other"/> has a special role and <see cref="me"/> can see it
        /// </summary>
        public static bool CanSeeSpecial(this GameData.PlayerInfo me, GameData.PlayerInfo other)
        {
            BaseRole? theirRole = other.GetRole();
            return theirRole?.Visibility switch
            {
                Visibility.Team => other.IsTeamedWithNonCrew(me),
                null => me.GetRole() != null && other.CanSeeSpecial(me),
                _ => me.GetRole() == theirRole
            };
        }

        /// <summary>
        /// True if <see cref="other"/> has a special role and <see cref="me"/> can see it
        /// </summary>
        public static bool CanSeeSpecial(this PlayerControl me, PlayerControl other)
            => me.Data.CanSeeSpecial(other.Data);
        
        /// <summary>
        /// Api's reimplementation of <see cref="PlayerControl.MurderPlayer"/><br/>
        /// Gets called by <see cref="CrowdedRoles.Rpc.CustomKill"/>, so shouldn't be used in regular code
        /// </summary>
        public static void CustomMurderPlayer(this PlayerControl killer, PlayerControl target, CustomMurderOptions options = CustomMurderOptions.None)
        {
            if (killer.AmOwner && Constants.DECMMJMOCKM()) // ShouldPlaySfx
            {
                SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f);
                killer.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
            }

            target.gameObject.layer = LayerMask.NameToLayer("Ghost");

            if (target.AmOwner)
            {
                if (Minigame.Instance)
                {
                    try
                    {
                        Minigame.Instance.Close();
                        Minigame.Instance.Close();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (!options.HasFlag(CustomMurderOptions.NoAnimation))
                {
                    HudManager.Instance.KillOverlay.ShowOne(killer.Data, target.Data);
                }
                HudManager.Instance.ShadowQuad.gameObject.SetActive(false);
                target.nameText.GetComponent<MeshRenderer>().material.SetInt(Mask, 0);
                target.RpcSetScanner(false);
                ImportantTextTask text = new GameObject("_Player").AddComponent<ImportantTextTask>();
                text.transform.SetParent(killer.transform, false);
                if (target.Data.IsImpostor)
                {
                    target.JBNJBHMIBOM(); // ClearTasks
                    text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostImpostor, new (Array.Empty<Il2CppSystem.Object>()));
                }
                else if (!PlayerControl.GameOptions.GhostsDoTasks)
                {
                    target.JBNJBHMIBOM(); // ClearTasks
                    text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostIgnoreTasks, new(Array.Empty<Il2CppSystem.Object>()));
                }
                else
                {
                    text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostDoTasks, new(Array.Empty<Il2CppSystem.Object>()));
                }
                target.myTasks.Insert(0, text);
            }

            Coroutines.Start(killer.KillAnimations.Random().CoPerformCustomKill(killer, target, options));
        }

        /// <summary>
        /// Api's reimplementation of <see cref="PlayerControl.FindClosestTarget"/><br/>
        /// Gets called in <see cref="PlayerControl.FixedUpdate"/>, so shouldn't be used in regular code
        /// </summary>
        public static PlayerControl? CustomFindClosetTarget(this PlayerControl me)
        {
            if (!ShipStatus.Instance)
            {
                return null;
            }
            
            BaseRole? role = me.GetRole();
            if (role == null)
            {
                return null;
            }

            PlayerControl? result = null;
            Vector2 myPos = me.GetTruePosition();
            float lowestDistance =
                GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, GameOptionsData.KillDistances.Length-1)];
            
            foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            {
                if(player.Data == null || player.Data.Disconnected || !role.CanKill(player)) continue;
                Vector2 vec = player.GetTruePosition() - myPos;
                float magnitude = vec.magnitude;
                if (magnitude <= lowestDistance && !PhysicsHelpers.AnyNonTriggersBetween(myPos, vec.normalized,
                    magnitude, Constants.ShipAndObjectsMask))
                {
                    result = player;
                    lowestDistance = magnitude;
                }
            }

            return result;
        }
    }
}
