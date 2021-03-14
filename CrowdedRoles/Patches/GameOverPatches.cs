using System.Linq;
using CrowdedRoles.Extensions;
using CrowdedRoles.GameOverReasons;
using HarmonyLib;
using Hazel;
using System;
using System.Reflection;
using CrowdedRoles.Roles;
using System.Collections.Generic;
using InnerNet;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CrowdedRoles.Patches
{
    internal static class GameOverPatches
    {
        private static readonly int Color = Shader.PropertyToID("_Color");

        [HarmonyPatch] 
        private static class StatsManagerFixes
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(StatsManager).GetMethod("EOAFLEMOMGI")!; // AddDrawReason
                yield return typeof(StatsManager).GetMethod("PGNIBFCEGEH")!; // AddWinReason
                yield return typeof(StatsManager).GetMethod("PAHEDCNAGLH")!; // AddLoseReason
            }
            private static bool Prefix([HarmonyArgument(0)] GameOverReason reason)
            {
                return !reason.IsCustom();
            }
        }

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
        private static class EndGameManager_SetEverythingUp
        {
            private static readonly Color GhostColor = new (1f, 1f, 1f, 0.5f);
            private static bool Prefix(EndGameManager __instance)
            {
                if (!TempData.EndReason.IsCustom())
                {
                    return true;
                }

                var reason = CustomGameOverReasonManager.EndReason;
                bool youWon = CustomGameOverReasonManager.Winners.Any(w => w.IsYou);

                __instance.WinText.Text = reason.WinText;
                __instance.WinText.Color = reason.GetWinTextColor(youWon);
                __instance.BackgroundBar.material.SetColor(Color, reason.GetBackgroundColor(youWon));
                
                AudioClip? sound = reason.Stinger;
                SoundManager.Instance.PlayDynamicSound(
                    "Stinger",
                    sound == null ? __instance.ImpostorStinger : sound,
                    false,
                    (DynamicSound.GetDynamicsFunction) __instance.Method_58 // GetStingerVol
                );

                for (int i = 0; i < CustomGameOverReasonManager.ShownWinners.Count; i++)
                {
                    var winner = CustomGameOverReasonManager.ShownWinners[i];
                    int oddness = (i + 1) / 2;
                    PoolablePlayer player = Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
                    player.transform.localPosition = new Vector3(
                        0.8f * (i % 2 == 0 ? -1 : 1) * oddness * 1 - oddness * 0.035f,
                        __instance.BaseY - 0.25f + oddness * 0.1f,
                        (i == 0 ? -8 : -1) + oddness * 0.01f
                    ) * 1.25f;
                    float scale = 1f - oddness * 0.075f;
                    var scaleVec = new Vector3(scale, scale, scale) * 1.25f;
                    player.transform.localScale = scaleVec;
                    if (winner.IsDead)
                    {
                        player.Body.sprite = __instance.GhostSprite;
                        player.SetDeadFlipX(i % 2 == 1);
                        player.HatSlot.color = GhostColor;
                    }
                    else
                    {
                        player.SetFlipX(i % 2 == 0);
                        DestroyableSingleton<HatManager>.Instance.Method_60(player.SkinSlot, winner.SkinId); // SetSkin
                    }
                    PlayerControl.SetPlayerMaterialColors(winner.ColorId, player.Body);
                    player.HatSlot.SetHat(winner.HatId, winner.ColorId);
                    PlayerControl.SetPetImage(winner.PetId, winner.ColorId, player.PetSlot);
                    player.NameText.Text = winner.Name;
                    player.NameText.transform.localScale = global::Extensions.Inv(scaleVec);
                }

                return false;
            }

            private static void Postfix(EndGameManager __instance)
            {
                if (!TempData.EndReason.IsCustom())
                {
                    var allPlayers = GameData.Instance.AllPlayers.ToArray();
                    foreach (var player in __instance.GetComponentsInChildren<PoolablePlayer>())
                    {
                        var pl = allPlayers.FirstOrDefault(p => p.PlayerName == player.NameText.Text); // yes idk what to do else
                        if(pl == null) continue;
                        var role = pl.GetRole();
                        if (role != null)
                        {
                            player.NameText.Color = role.Color;
                            player.NameText.Text = role.FormatName(pl);
                        }
                    }
                }
                    
                RoleManager.GameEnded();
            }
        }

        [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
        private static class AmongUsClient_OnGameEnd
        {
            private static void Prefix([HarmonyArgument(0)] GameOverReason reason)
            {
                if (reason.IsCustom())
                {
                    CustomGameOverReasonManager.Winners = CustomGameOverReasonManager.EndReason.Winners.Select(p => new WinningPlayerData(p)).ToList();
                    CustomGameOverReasonManager.ShownWinners = CustomGameOverReasonManager.EndReason.ShownWinners.Select(p => new WinningPlayerData(p)).ToList();
                }
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(AmongUsClient.Nested_1), nameof(AmongUsClient.Nested_1.MoveNext))]
        private static class AmongUsClient_CoEndGame
        {
            private static void Prefix(AmongUsClient.Nested_1 __instance)
            {
                if (__instance.__state == 0 && !TempData.EndReason.IsCustom())
                {
                    bool flag = TempData.DidHumansWin(TempData.EndReason);
                    TempData.winners.Clear();
                    foreach (var player in GameData.Instance.AllPlayers
                        .ToArray()
                        .Where(p => p != null && 
                                    (TempData.EndReason == GameOverReason.HumansDisconnect || 
                                     TempData.EndReason == GameOverReason.ImpostorDisconnect || 
                                     !flag && p.IsImpostor ||
                                     p.GetTeam() == (flag ? Team.Crewmate : Team.Impostor))))
                    {
                        TempData.winners.Add(new WinningPlayerData(player));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Method_48))]
        private static class InnerNetClient_OnDisconnected
        {
            private static void Postfix()
            {
                RoleManager.GameEnded();
            }
        }
        
        [HarmonyPatch]
        private static class EndGamePatches
        {
            private static bool isCustom;

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RpcEndGame))]
            private static void RpcEndGame([HarmonyArgument(0)] GameOverReason reason)
            {
                isCustom = reason.IsCustom();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.FinishEndGame))]
            private static void FinishEndGame([HarmonyArgument(0)] MessageWriter writer)
            {
                if (isCustom)
                {
                    CustomGameOverReasonManager.EndReason.Deserialize(writer);
                }
            }

            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleMessage))]
            private static bool HandleMessage(InnerNetClient __instance, [HarmonyArgument(0)] MessageReader reader)
            {
                if (reader.Tag != 8)
                {
                    return true;
                }

                if (__instance.GameId != reader.ReadInt32() || __instance.GameState == InnerNetClient.GameStates.Ended)
                {
                    return true;
                }

                __instance.GameState = InnerNetClient.GameStates.Ended;
                lock (__instance.allClients)
                {
                    __instance.allClients.Clear();
                }

                var reason = (GameOverReason) reader.ReadSByte();
                bool showAd = reader.ReadBoolean();

                if (reason.IsCustom())
                {
                    CustomGameOverReasonManager.EndReason = CustomGameOverReason.Serialize(reader);
                }

                lock (__instance.Dispatcher)
                {
                    __instance.Dispatcher.Add((Action)(() => __instance.OnGameEnd(reason, showAd)));
                    return false;
                }
            }
        }
    }
}