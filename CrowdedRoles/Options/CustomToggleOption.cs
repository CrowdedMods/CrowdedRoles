using System;
using CrowdedRoles.Extensions;

namespace CrowdedRoles.Options
{
    public class CustomToggleOption : CustomOption
    {
        public CustomToggleOption(string name) : base(name)
        {
            ValueText = Value ? "On" : "Off";
        }

        public bool Value { get; private set; }

        public Action<bool>? OnValueChanged { get; init; }

        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            ValueChanged(opt.GetBool());
            PlayerControl.LocalPlayer.RpcSyncCustomSettings();
            OptionsManager.ValueChanged();
        }

        private void ValueChanged(bool newValue)
        {
            Value = newValue;
            ValueText = Value ? "On" : "Off";
            OnValueChanged?.Invoke(Value);
        }

        internal override byte[] ToBytes()
        {
            return BitConverter.GetBytes(Value);
        }

        internal override void ByteValueChanged(byte[] newValue)
        {
            ValueChanged(BitConverter.ToBoolean(newValue));
        }

        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            var option = baseOption.TryCast<ToggleOption>();
            if (option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(ToggleOption)}");
                return;
            }
            
            option.TitleText.Text = Name; // why the heck it's not in OptionBehaviour
            option.CheckMark.enabled = Value;
            option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
            RoleApiPlugin.Logger.LogDebug($"Added toggle option {Name}");
        }
    }
}