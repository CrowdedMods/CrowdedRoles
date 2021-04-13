#if DEBUG
using System.Collections.Generic;
using System.Linq;
using BepInEx.IL2CPP;
using CrowdedRoles.Attributes;
using CrowdedRoles.Extensions;
using CrowdedRoles.GameOverReasons;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using CrowdedRoles.UI;
using HarmonyLib;
using UnityEngine;

namespace CrowdedRoles
{
    [RegisterCustomRole]
    public class TestRole : BaseRole
    {
        public TestRole(BasePlugin plugin) : base(plugin)
        {
        }

        public override string Name { get; } = "TestRole";
        public override Color Color { get; } = Color.cyan;
        public override Visibility Visibility { get; } = Visibility.Team;
        public override string Description { get; } = "say meow pls";
        public override bool CanKill(PlayerControl? target) =>
            target == null ||
            !target.AmOwner &&
            !target.Data.IsDead &&
            !PlayerControl.LocalPlayer.IsTeamedWith(target);
        public override bool CanSabotage(SystemTypes? sabotage) => sabotage != SystemTypes.LifeSupp;
        public override bool CanVent(Vent _) => true;
        public override Team Team { get; } = Team.Impostor;
        public override bool PreKill(ref PlayerControl killer, ref PlayerControl target, ref CustomMurderOptions options)
        {
            options |= CustomMurderOptions.NoAnimation | CustomMurderOptions.NoSnap;
            return true;
        }

        public override void AssignTasks(PlayerTaskList taskList, IEnumerable<GameData.TaskInfo> defaultTasks)
        {
            taskList.AddStringTask("I love you kitty");
            taskList.AddNormalTasks(defaultTasks);
            taskList.TaskCompletion = TaskCompletion.Required;
        }
    }

    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.HandleHud))]
    internal class TestPatch
    {
        private static void Postfix()
        {
            if (Input.GetKeyDown(KeyCode.F6) && PlayerControl.LocalPlayer != null && AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcCustomEndGame<TestRoleWon>();
            }
        }
    }

    [RegisterCustomGameOverReason]
    public class TestRoleWon : CustomGameOverReason
    {
        public TestRoleWon(BasePlugin plugin) : base(plugin)
        {
        }

        public override string Name { get; } = "TestRole won";
        public override string WinText { get; } = "haha fools";
        public override IEnumerable<GameData.PlayerInfo> Winners => 
            GameData.Instance.AllPlayers.ToArray().Where(p => p.Is<TestRole>());

        public override Color GetWinTextColor(bool youWon)
        {
            return youWon ? Color.cyan : Color.red;
        }

        public override Color GetBackgroundColor(bool youWon)
        {
            return youWon ? Palette.CrewmateBlue : Palette.ImpostorRed;
        }
    }

    [RegisterCustomButton]
    public class TestButton : CooldownButton
    {
        public override float MaxTimer => 5f;
        public override float EffectDuration => 3f;
        public override Sprite DefaultSprite => TranslationController.Instance.GetImage(ImageNames.ReportButton);
        public override IPosition Position { get; } = new AutomaticPosition();

        public override bool OnClick()
        {
            if (Active)
            {
                Sprite = TranslationController.Instance.GetImage(ImageNames.VentButton);
                PlayerControl.LocalPlayer.RpcMurderPlayer(PlayerControl.LocalPlayer);
                return true;
            }
            
            Active = true;
            Sprite = TranslationController.Instance.GetImage(ImageNames.ReportButton);
            return false;
        }

        public override bool CanUse() => true;
    }

    [RegisterCustomButton]
    public class UselessButton : CooldownButton
    {
        public override float MaxTimer => 5f;
        public override float EffectDuration => 3f;
        public override Sprite DefaultSprite => TranslationController.Instance.GetImage(ImageNames.KillButton);
        public override IPosition Position { get; } = new AutomaticPosition();

        public override bool OnClick() => true;

        public override bool CanUse() => true;
    }

    public static class MyCustomOptions
    {
        public static CustomToggleOption ToggleMe { get; } = new ("Super cool toggle option")
        { 
            OnValueChanged = v => RoleApiPlugin.Logger.LogDebug($"new test bool: {v}")
        };

        public static CustomNumberOption IncrementMe { get; } = new ("Fake cooldown", new FloatRange(10, 100))
        {
            Increment = 0.25f,
            SuffixType = NumberSuffixes.Seconds
        };

        public static CustomStringOption FixMe { get; } = new ("Omg still no arrows", new[] {"Everyone", "AOU", "No one"});

        public static void RegisterOptions(BasePlugin plugin)
        {
            new OptionPluginWrapper(plugin)
                .AddCustomOption(IncrementMe)
                .AddCustomOption(ToggleMe)
                .AddCustomOption(FixMe);
            // OR
            // new OptionPluginWrapper(plugin)
            //     .AddCustomOptions(new CustomOption[]
            //     {
            //         IncrementMe,
            //         ToggleMe,
            //         FixMe
            //     });
        }
    }
}
#endif