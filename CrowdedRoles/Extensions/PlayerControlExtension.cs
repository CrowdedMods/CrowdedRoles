using System;
using System.Collections.Generic;
using System.Linq;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using Reactor;
using Reactor.Extensions;
using UnityEngine;

namespace CrowdedRoles.Extensions
{
    public static class PlayerControlExtension
    {
        private static readonly int Mask = Shader.PropertyToID("_Mask");

        public static void InitRole(this PlayerControl player, BaseRole? role = null)
        {
            if (role is null) return;
            try
            {
                RoleManager.PlayerRoles.Add(player.PlayerId, role);
            }
            catch(ArgumentException)
            {
                RoleApiPlugin.Logger.LogWarning($"{role.Name} already exists in {nameof(RoleManager.PlayerRoles)}, redefining...");
                RoleManager.PlayerRoles[player.PlayerId] = role;
            }
        }

        public static BaseRole? GetRole(this PlayerControl player)
        {
            return RoleManager.PlayerRoles.GetValueOrDefault(player.PlayerId);
        }

        public static T? GetRole<T>(this PlayerControl player) where T : BaseRole
        {
            return player.GetRole() as T;
        }

        public static bool Is<T>(this PlayerControl player) where T : BaseRole
        {
            return player.GetRole<T>() != null;
        }

        public static bool IsTeamedWith(this PlayerControl me, PlayerControl other)
        {
            return me.GetRole()?.Equals(other.GetRole()) ?? 
                   other.GetRole() == null && me.Data.IsImpostor == other.Data.IsImpostor;            
        }
        
        public static IEnumerable<PlayerControl> GetTeam(this PlayerControl player)
        {
            var result = new List<PlayerControl>();
            BaseRole? ownRole = player.GetRole();
            switch (ownRole?.Visibility)
            {
                case Visibility.Everyone:
                    result = PlayerControl.AllPlayerControls.ToArray().ToList();
                    break;
                case Visibility.Myself:
                    result.Add(player);
                    break;
                case Visibility.Team:
                    result = PlayerControl.AllPlayerControls.ToArray().Where(p => p.IsTeamedWith(player)).ToList();
                    break;
            }
            // foreach(var p in PlayerControl.AllPlayerControls)
            // {
            //     if(p != null && player.IsTeamedWith(p.Data))
            //     {
            //         result.Add(p);
            //     }
            // }

            return result;
        }

        public static void RpcCustomMurderPlayer(this PlayerControl me, PlayerControl target, CustomMurderOptions options = CustomMurderOptions.None)
        {
            Rpc<CustomKill>.Instance.Send(new CustomKill.Data
            {
                target = target.PlayerId,
                options = options
            });
        }

        public static void RpcSyncCustomSettings(this PlayerControl me)
        {
            Rpc<SyncCustomSettings>.Instance.Send(new SyncCustomSettings.Data
            {
                limits = RoleManager.EditableLimits.ToDictionary(p => p.Key.Data, p => p.Value),
                options = OptionsManager.CustomOptions.ToDictionary(p => p.Key, p => p.Value.Select(o => o.ToBytes()).ToList())
            });
        }

        public static bool CanSee(this PlayerControl me, PlayerControl whom)
        {
            BaseRole? role = whom.GetRole();
            return role?.Visibility switch
            {
                Visibility.Myself => me.PlayerId == whom.PlayerId,
                Visibility.Team => whom.IsTeamedWith(me) || role.Side == (me.Data.IsImpostor ? Side.Impostor : Side.Crewmate) ,
                Visibility.Everyone => true,
                _ => me.GetRole()?.Side == (whom.Data.IsImpostor ? Side.Impostor : Side.Crewmate) 
            };
        }
        
        public static void CustomMurderPlayer(this PlayerControl killer, PlayerControl? target, CustomMurderOptions options = CustomMurderOptions.None)
        {
            #region Checks

            if (AmongUsClient.Instance.IsGameOver)
            {
                RoleApiPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill when game is over");
                return;
            }

            BaseRole? role = killer.GetRole();
            if (target is null || role is null)
            {
                // ReSharper disable once Unity.NoNullPropagation
                RoleApiPlugin.Logger.LogWarning($"Null kill ({killer.PlayerId} -> {target?.PlayerId ?? -1})");
                return;
            }
            
            if(killer.Data.IsDead || killer.Data.Disconnected )
            {
                if (!options.HasFlag(CustomMurderOptions.Force))
                {
                    RoleApiPlugin.Logger.LogWarning($"Not allowed kill ({killer.PlayerId} -> {target.PlayerId})");
                    return;
                }
                RoleApiPlugin.Logger.LogDebug($"Forced bad kill ({killer.PlayerId} -> {target.PlayerId})");
            }

            if (!options.HasFlag(CustomMurderOptions.Force) && !role.PreKill(ref killer, ref target, ref options))
            {
                RoleApiPlugin.Logger.LogDebug($"Custom kill ({killer.PlayerId} -> {target.PlayerId}) is cancelled by a plugin");
                return;
            }

            if (target.Data is null || target.Data.IsDead)
            {
                RoleApiPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill {target.PlayerId}, but they are already dead");
                return;
            }

            if (killer.AmOwner && Constants.Method_3()) // ShouldPlaySfx
            {
                SoundManager.Instance.PlaySound(killer.KillSfx, false, 0.8f);
                killer.SetKillTimer(PlayerControl.GameOptions.KillCooldown);
            }

            #endregion
            
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
                if (!PlayerControl.GameOptions.GhostsDoTasks)
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
            //killer.MyPhysics.StartCoroutine(killer.KillAnimations.Random().CoPerformKill(killer, target));
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
                GameOptionsData.KillDistances[Mathf.Clamp(PlayerControl.GameOptions.KillDistance, 0, 2)];
            
            foreach (GameData.PlayerInfo player in GameData.Instance.AllPlayers)
            {
                PlayerControl obj = player.Object;
                if(obj == null || !role.KillFilter(me, player)) continue;
                Vector2 vec = obj.GetTruePosition() - myPos;
                float magnitude = vec.magnitude;
                if (magnitude <= lowestDistance && !PhysicsHelpers.AnyNonTriggersBetween(myPos, vec.normalized,
                    magnitude, Constants.ShipAndObjectsMask))
                {
                    result = obj;
                    lowestDistance = magnitude;
                }
            }

            return result;
        }
    }
}
