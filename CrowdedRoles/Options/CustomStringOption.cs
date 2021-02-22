using System;
using System.Linq;
using CrowdedRoles.Extensions;
using HarmonyLib;
using UnityEngine;

namespace CrowdedRoles.Options
{
    public class CustomStringOption : CustomOption
    {
        public CustomStringOption(string name, string[] values) : base(name)
        {
            Values = values;
            ValueText = Values[Value];
        }

        public int Value { get; private set; }
        public string StringValue => Values[Value];
        public Action<int>? OnValueChanged { get; init; }
        public string[] Values { get; }
        private StringOption? Option;
        
        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            UpdateValue(opt.GetInt());
            PlayerControl.LocalPlayer.RpcSyncCustomSettings();
            OptionsManager.ValueChanged();
        }

        private void UpdateValue(int index)
        {
            Value = index;
            ValueText = Values[Mathf.Clamp(Value, 0, Values.Length)];
            OnValueChanged?.Invoke(Value);
            if(Option != null)
            {
                Option.ValueText.Text = ValueText;
            }
        }

        internal override byte[] ToBytes()
        {
            return BitConverter.GetBytes(Value);
        }

        internal override void ByteValueChanged(byte[] newValue)
        {
            UpdateValue(BitConverter.ToInt32(newValue));
        }

        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            Option = baseOption.TryCast<StringOption>();
            if (Option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(StringOption)}");
                return;
            }

            Option.TitleText.Text = Name;
            Option.Value = Value;
            Option.Values = Enumerable.Repeat<StringNames>(OptionsManager.CustomOptionStringName, Values.Length).ToArray();
            Option.ValueText.Text = ValueText;
            Option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
    [HarmonyPriority(Priority.First)]
    internal static class StringOptionPatches
    {
        private static bool Prefix(ref StringOption __instance)
        {
            return __instance.Title != OptionsManager.CustomOptionStringName;
        }
    }
}