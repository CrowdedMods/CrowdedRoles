using System;
using System.Linq;
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
        public Action<int>? OnValueChanged { get; init; }
        public string[] Values { get; }
        private StringOption? Option;
        
        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            Value = opt.GetInt();
            
            ValueText = Values[Mathf.Clamp(Value, 0, Values.Length)];
            OnValueChanged?.Invoke(Value);
            if(Option != null)
            {
                Option.ValueText.Text = ValueText;
            }
            OptionsManager.ValueChanged();
        }
        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            var option = baseOption.TryCast<StringOption>();
            if (option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(StringOption)}");
                return;
            }

            option.Title = OptionsManager.CustomOptionStringName;
            option.TitleText.Text = Name;
            option.Value = Value;
            option.Values = Enumerable.Repeat<StringNames>(OptionsManager.CustomOptionStringName, Values.Length).ToArray();
            option.ValueText.Text = ValueText;
            option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
            Option = option;
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