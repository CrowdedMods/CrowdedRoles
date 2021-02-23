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
                if (RoleManager.rolesSet)
                {
                    RoleApiPlugin.Logger.LogWarning("Trying to override roles");
                    return;
                }
                var goodPlayers = GameData.Instance.AllPlayers.ToArray()
                    .Where(p => !p.Disconnected && !p.IsImpostor && p.GetRole() == null)
                    .ToList();

                var holders = new Dictionary<RoleData, byte[]>();

                foreach (var (role, limit) in RoleManager.Limits)
                {
                    if(limit == 0) continue;
                    holders.Add(role.Data, role.SelectHolders(goodPlayers, limit).Select(p => p.PlayerId).ToArray());
                    goodPlayers = GameData.Instance.AllPlayers.ToArray()
                        .Where(p => !p.Disconnected && !p.IsImpostor  && p.GetRole() == null)
                        .ToList();
                }
                
                Rpc<SelectCustomRole>.Instance.Send(holders);
                RoleManager.rolesSet = true;
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

                List<PlayerControl> myTeam = PlayerControl.AllPlayerControls.ToArray()
                    .Where(p => PlayerControl.LocalPlayer.CanSee(p))
                    .OrderBy(p => !p.AmOwner)
                    .ToList();

                __instance.Title.Text = myRole.Name;
                __instance.Title.Color = myRole.Color;
                __instance.BackgroundBar.material.SetColor(Color, myRole.Color);
                __instance.ImpostorText.Text = myRole.Description;
                
                for(var i = 0; i < myTeam.Count; i++)
                {
                    GameData.PlayerInfo data = myTeam[i].Data;
                    int oddness = (i + 1) / 2;
                    PoolablePlayer player = Object.Instantiate(__instance.PlayerPrefab, __instance.transform);
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

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(IntroCutscene.CoBegin__d), nameof(IntroCutscene.CoBegin__d.MoveNext))]
        private static class IntroCutScene_CoBegin
        {
            private static bool Prefix(ref bool __result)
            {
                return RoleManager.rolesSet && (__result = true); // wait until we set our roles
            }
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
