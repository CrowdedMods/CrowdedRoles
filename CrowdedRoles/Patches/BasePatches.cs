using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using CrowdedRoles.Extensions;
using CrowdedRoles.Roles;
using CrowdedRoles.Rpc;
using Reactor;

namespace CrowdedRoles.Patches
{
    internal static class BasePatches
    {
        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.SelectInfected))]
        private static class ShipStatus_SelectInfected
        {
            public static void Postfix()
            {
                var goodPlayers = new List<byte>();
                foreach(var p in GameData.Instance.AllPlayers)
                {
                    if(!p.Disconnected && !p.IsImpostor)
                    {
                        goodPlayers.Add(p.PlayerId);
                    }
                }

                foreach((BaseRole role, byte limit) in RoleManager.Limits)
                {
                    if (limit == 0) continue; // fast skip
                    if (role.PatchFilterFlags.HasFlag(PatchFilter.SelectInfected)) continue;
                    
                    List<byte> shuffledPlayers = goodPlayers.OrderBy(_ => new Guid()).ToList();
                    goodPlayers = shuffledPlayers.Skip(limit).ToList();
                    
                    Rpc<SelectCustomRole>.Instance.Send(
                        new SelectCustomRole.Data {
                            role = role.Data, 
                            holders = shuffledPlayers.Take(limit).ToArray()
                        }
                    );
                }
            }
        }

        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
        private static class IntroCutScene_BeginCrewmate
        {
            private static readonly int Color = Shader.PropertyToID("_Color");

            private static bool Prefix(ref IntroCutscene __instance)
            {
                BaseRole? myRole = PlayerControl.LocalPlayer.GetRole();
                if (myRole == null || myRole.PatchFilterFlags.HasFlag(PatchFilter.IntroCutScene))
                {
                    return true;
                }

                List<PlayerControl> myTeam = new();
                // {
                //     PlayerControl.LocalPlayer
                // };
                // if (myRole.Visibility != Visibility.Myself)
                // {
                    myTeam.AddRange(
                    PlayerControl.LocalPlayer.GetTeam()
                        .OrderBy(p => p != PlayerControl.LocalPlayer)
                    );
                // }

                __instance.Title.Text = myRole.Name;
                __instance.Title.Color = myRole.Color;
                __instance.BackgroundBar.material.SetColor(Color, myRole.Color);
                __instance.ImpostorText.Text = myRole.StartTip;
                
                for(var i = 0; i < myTeam.Count; i++)
                {
                    GameData.PlayerInfo data = myTeam[i].Data;
                    int oddness = (i + 1) / 2;
                    PoolablePlayer player = UnityEngine.Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
                    player.transform.localPosition = new Vector3(
                        0.8f* oddness * (i % 2 == 0 ? -1 : 1) * (1 - oddness * 0.08f),
                        __instance.BaseY - 0.25f + oddness * 0.1f,
                        (i == 0 ? -8 : -1) + oddness * 0.01f
                    ) * 1.5f;
                    player.SetFlipX(i % 2 == 0);
                    PlayerControl.SetPlayerMaterialColors(data.ColorId, player.Body);
                    DestroyableSingleton<HatManager>.Instance.Method_4(player.SkinSlot, data.SkinId);
                    player.HatSlot.SetHat(data.HatId, data.ColorId);
                    PlayerControl.SetPetImage(data.PetId, data.ColorId, player.PetSlot);
                    float scale = (i == 0 ? 1.8f : 1.5f) - oddness * 0.18f;
                    player.transform.localScale = player.NameText.transform.localScale = new Vector3(scale, scale, scale);
                    player.NameText.Text = myRole.FormatName(player.NameText.Text);
                    if (i > 0 && myRole.Visibility != Visibility.Everyone)
                    {
                        player.NameText.gameObject.SetActive(true);
                    }
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Method_7))]
        [HarmonyPatch(new[] { typeof(GameData.PlayerInfo) })]
        static class MeetingHud_CreateButton
        {
            static void Postfix([HarmonyArgument(0)] ref GameData.PlayerInfo data, ref PlayerVoteArea __result)
            {
                BaseRole? role = data.Object.GetRole();
                if (role?.PatchFilterFlags.HasFlag(PatchFilter.MeetingHud) ?? false)
                {
                    return;
                }
                
                if(PlayerControl.LocalPlayer.CanSee(data.Object))
                {
                    __result.NameText.Color = role?.Color ?? Palette.ImpostorRed;
                    if (role != null)
                    {
                        __result.NameText.Text = role.FormatName(__result.NameText.Text);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
        private static class EndGameManager_Start
        {
            private static void Postfix()
            {
                ShipStatus.Instance.GameEnded();
            }
        }

        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        private static class IntroCutScene_CoBegin
        {
            private static void Postfix(bool __result)
            {
                if (!__result) // yield break
                {
                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        BaseRole? role = player.GetRole();
                        if (PlayerControl.LocalPlayer.CanSee(player))
                        {
                            player.nameText.Color = role?.Color ?? Palette.ImpostorRed;
                            if (role != null)
                            {
                                player.nameText.Text = role.FormatName(player.nameText.Text);
                            }
                        }
                    }
                }
            }
        }
    }
}
