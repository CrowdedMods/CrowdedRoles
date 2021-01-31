using System;
using System.Collections.Generic;
using CrowdedRoles.Api.Roles;
using CrowdedRoles.Api.Rpc;
using Reactor;
using Reactor.Extensions;
using UnityEngine;

namespace CrowdedRoles.Api.Extensions
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
                MainPlugin.Logger.LogWarning($"{role.Name} already exists in {nameof(RoleManager.PlayerRoles)}, redefining...");
                RoleManager.PlayerRoles[player.PlayerId] = role;
            }
        }

        public static BaseRole? GetRole(this PlayerControl player)
            => RoleManager.PlayerRoles.GetValueOrDefault(player.PlayerId);

        public static T? GetRole<T>(this PlayerControl player) where T : BaseRole
            => player.GetRole() as T;

        public static bool Is<T>(this PlayerControl player) where T : BaseRole
            => player.GetRole<T>() is not null;

        public static bool IsTeamedWith(this PlayerControl me, byte other) 
            => me.IsTeamedWith(GameData.Instance!.GetPlayerById(other)!);

        public static bool IsTeamedWith(this PlayerControl me, GameData.PlayerInfo other)
            => me.GetRole()?.Equals(other.Object.GetRole()) ?? me.Data.IsImpostor == other.IsImpostor;
        
        public static List<PlayerControl> GetTeam(this PlayerControl player)
        {
            var result = new List<PlayerControl>();
            foreach(var p in PlayerControl.AllPlayerControls)
            {
                if(p != null && player.IsTeamedWith(p.PlayerId))
                {
                    result.Add(p);
                }
            }

            return result;
        }

        public static void RpcCustomMurderPlayer(this PlayerControl me, PlayerControl target, bool noSnap = false)
        {
            me.Send<CustomKill>(new CustomKill.Data
            {
                target = target.PlayerId,
                noSnap = noSnap
            });
        }

        public static bool CanSee(this PlayerControl me, PlayerControl whom)
        {
            return whom.GetRole()?.Visibility switch
            {
                Visibility.Myself => me.PlayerId == whom.PlayerId,
                Visibility.Team => whom.IsTeamedWith(me.PlayerId),
                Visibility.Everyone => true,
                _ => false
            };
        }

        public static void ForceCustomMurderPlayer(this PlayerControl killer, PlayerControl? target, bool noSnap = false)
            => killer.CustomMurderPlayer(target, noSnap, true);
        
        public static void CustomMurderPlayer(this PlayerControl killer, PlayerControl? target, bool noSnap = false, bool force = false)
        {
            #region Checks

            if (AmongUsClient.Instance.IsGameOver)
            {
                MainPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill when game is over");
                return;
            }

            BaseRole? role = killer.GetRole();
            if (target is null || role is null)
            {
                // ReSharper disable once Unity.NoNullPropagation
                MainPlugin.Logger.LogWarning($"Null kill ({killer.PlayerId} -> {target?.PlayerId ?? -1})");
                return;
            }
            
            if(killer.Data.IsDead || killer.Data.Disconnected )
            {
                if (!force)
                {
                    MainPlugin.Logger.LogWarning($"Not allowed kill ({killer.PlayerId} -> {target.PlayerId})");
                    return;
                }
                MainPlugin.Logger.LogDebug($"Forced bad kill ({killer.PlayerId} -> {target.PlayerId})");
            }

            if (!force && !role.PreKill(ref killer, ref target))
            {
                MainPlugin.Logger.LogDebug($"Custom kill ({killer.PlayerId} -> {target.PlayerId}) is cancelled by a plugin");
                return;
            }

            if (target.Data is null || target.Data.IsDead)
            {
                MainPlugin.Logger.LogWarning($"{killer.PlayerId} tried to kill {target.PlayerId}, but they are already dead");
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
                DestroyableSingleton<HudManager>.Instance.KillOverlay.ShowOne(killer.Data, target.Data);
                DestroyableSingleton<HudManager>.Instance.ShadowQuad.gameObject.SetActive(false);
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

            Coroutines.Start(killer.KillAnimations.Random().CoPerformCustomKill(killer, target, noSnap));
            //killer.MyPhysics.StartCoroutine(killer.KillAnimations.Random().CoPerformKill(killer, target));
        }
    }
}
