using System;
using BepInEx.Configuration;
using CrowdedRoles.Extensions;

namespace CrowdedRoles.Options
{
    public class CustomToggleOption : CustomOption
    {
        public CustomToggleOption(string name) : base(name)
        {
        }

        private bool _value;

        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                ValueText = value ? "On" : "Off";
            }
        }

        public Action<bool>? OnValueChanged { get; init; }

        private ConfigEntry<bool> SavedValue = null!;

        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            UpdateValue(SavedValue.Value = opt.GetBool());
            PlayerControl.LocalPlayer.RpcSyncCustomSettings();
            OptionsManager.ValueChanged();
        }

        private void UpdateValue(bool newValue)
        {
            Value = newValue;
            OnValueChanged?.Invoke(Value);
        }

        internal override byte[] ToBytes()
        {
            return BitConverter.GetBytes(Value);
        }

        internal override void ByteValueChanged(byte[] newValue)
        {
            UpdateValue(BitConverter.ToBoolean(newValue));
        }

        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            var option = baseOption.TryCast<ToggleOption>();
            if (option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(ToggleOption)}");
                return;
            }
            
            option.TitleText.text = Name; // why the heck it's not in OptionBehaviour
            option.CheckMark.enabled = Value;
            option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
        }

        internal override void LoadValue(ConfigFile file, string guid, string name = "")
        {
            SavedValue = file.Bind(guid, OptionsManager.MakeSaveNameValid(name == "" ? Name : name), Value);

            Value = SavedValue.Value;
        }
    }
}