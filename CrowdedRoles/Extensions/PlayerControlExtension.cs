using System;
using System.Collections.Generic;
using System.Linq;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using Reactor;
using Reactor.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrowdedRoles.Extensions
{
    public static class PlayerControlExtension
    {
        private static readonly int Mask = Shader.PropertyToID("_Mask");

        public static void InitRole(this PlayerControl player, BaseRole? role = null)
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

        public static T? GetRole<T>(this PlayerControl player) where T : BaseRole
            => player.GetRole() as T;

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

        public static bool HasRole(this GameData.PlayerInfo player)
            => player.HasCustomRole() || player.IsImpostor;

        public static bool HasRole(this PlayerControl player)
            => player.Data.HasRole();

        public static bool CanKill(this GameData.PlayerInfo player, PlayerControl target)
            => player.GetRole()?.CanKill(target) ?? player.IsImpostor && !player.Object.IsTeamedWith(target);

        public static bool CanKill(this PlayerControl player, PlayerControl target)
            => player.Data.CanKill(target);
        
        public static bool IsTeamedWith(this PlayerControl me, PlayerControl other)
        {
            BaseRole? role = me.GetRole();
            BaseRole? theirRole = other.GetRole();
            return role?.Team switch
            {
                Team.Alone => me == other,
                Team.Crewmate => true,
                Team.Impostor => other.Data.IsImpostor || theirRole?.Team == Team.Impostor,
                Team.SameRole => role == theirRole,
                _ => theirRole == null
                    ? me.Data.IsImpostor == other.Data.IsImpostor
                    : other.IsTeamedWith(me) // there's no way it's gonna overflow
            };
        }

        public static bool IsTeamedWithNonCrew(this PlayerControl me, PlayerControl other)
        {
            var myRole = me.GetRole();
            return myRole?.Team switch
            {
                Team.Crewmate => myRole == other.GetRole(),
                _ => me.IsTeamedWith(other)
            };
        }

        public static bool IsVisibleTeammate(this PlayerControl me, PlayerControl other)
        {
            return me.IsTeamedWith(other) && me.CanSee(other);
        }
        
        public static IEnumerable<PlayerControl> GetTeam(this PlayerControl player)
        {
            return GameData.Instance.AllPlayers
                .ToArray()
                .Where(p => !p.Disconnected && p.Object != null && p.Object.IsTeamedWith(player))
                .Select(p => p.Object)
                .ToList();
        }

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

        public static void CustomSetTasks(this PlayerControl player, PlayerTaskList tasks)
        {
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
            
            global::Extensions.Method_2(player.myTasks.Cast<Il2CppSystem.Collections.Generic.IList<PlayerTask>>()); // DestroyAll
            player.myTasks = new Il2CppSystem.Collections.Generic.List<PlayerTask>(tasks.NormalTasks.Count + tasks.StringTasks.Count);
            for (int i = 0; i < player.myTasks.Capacity; i++)
            {
                player.myTasks.Add(null);
            }

            if (!RoleManager.TaskCompletions.TryAdd(player.PlayerId, tasks.TaskCompletion))
            {
                RoleManager.TaskCompletions[player.PlayerId] = tasks.TaskCompletion;
            }

            var gameObject = new GameObject("_Player");
            if (player.Data.IsImpostor)
            {
                ImportantTextTask task = gameObject.AddComponent<ImportantTextTask>();
                task.transform.SetParent(player.transform, false);
                task.Text = TranslationController.Instance.GetString(StringNames.ImpostorTask, Array.Empty<Il2CppSystem.Object>()) +
                            "\n[FFFFFFFF]" +
                            TranslationController.Instance.GetString(StringNames.FakeTasks, Array.Empty<Il2CppSystem.Object>());
                player.myTasks.Insert(0, task);
            }

            byte k = 0;
            foreach (var (id, task) in tasks.NormalTasks)
            {
                var normalTask = Object.Instantiate(task, player.transform);
                normalTask.Id = k++;
                normalTask.Owner = player;
                normalTask.Initialize();
                player.myTasks[id] = normalTask;
            }

            foreach (var (id, text) in tasks.StringTasks)
            {
                ImportantTextTask task = gameObject.AddComponent<ImportantTextTask>();
                task.transform.SetParent(player.transform, false);
                task.Text = "[FFFFFFFF]" + text; // haha funny   
                player.myTasks[id] = task;
            }
        }

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

        public static bool CanSee(this PlayerControl me, PlayerControl whom)
        {
            BaseRole? role = whom.GetRole();
            return role?.Visibility switch
            {
                Visibility.Myself => me.PlayerId == whom.PlayerId,
                Visibility.Team => whom.IsTeamedWith(me),
                Visibility.Everyone => true,
                _ =>  !whom.Data.IsImpostor || me.GetRole()?.Team == Team.Impostor
            };
        }
        
        public static void CustomMurderPlayer(this PlayerControl killer, PlayerControl target, CustomMurderOptions options = CustomMurderOptions.None)
        {
            if (killer.AmOwner && Constants.Method_3()) // ShouldPlaySfx
            {
                SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f);
                killer.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
            }
            
            DestroyableSingleton<Telemetry>.Instance.WriteMurder();
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
                    target.Method_84(); // ClearTasks
                    text.Text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GhostImpostor, new (Array.Empty<Il2CppSystem.Object>()));
                }
                else if (!PlayerControl.GameOptions.GhostsDoTasks)
                {
                    target.Method_84(); // ClearTasks
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
