using System;
using System.Linq;
using BepInEx.Configuration;
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
        }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueText = Values[Mathf.Clamp(value, 0, Values.Length)];
            }
        }
        public string StringValue => Values[Value];
        public Action<int>? OnValueChanged { get; init; }
        public string[] Values { get; }
        private StringOption? Option;
        private ConfigEntry<int> SavedValue = null!;
        
        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            UpdateValue(SavedValue.Value = opt.GetInt());
            PlayerControl.LocalPlayer.RpcSyncCustomSettings();
            OptionsManager.ValueChanged();
        }

        private void UpdateValue(int index)
        {
            Value = index;
            OnValueChanged?.Invoke(Value);
            if(Option != null)
            {
                Option.ValueText.text = ValueText;
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

            Option.TitleText.text = Name;
            Option.Value = Value;
            Option.Values = Enumerable.Repeat<StringNames>(OptionsManager.CustomOptionStringName, Values.Length).ToArray();
            Option.ValueText.text = ValueText;
            Option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
        }

        internal override void LoadValue(ConfigFile file, string guid, string name = "")
        {
            SavedValue = file.Bind(guid, OptionsManager.MakeSaveNameValid(name == "" ? Name : name), Value);

            Value = SavedValue.Value;
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