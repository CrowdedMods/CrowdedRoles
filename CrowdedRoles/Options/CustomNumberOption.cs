using System;
using CrowdedRoles.Extensions;

namespace CrowdedRoles.Options
{
    public class CustomNumberOption : CustomOption
    {
        public CustomNumberOption(string name, FloatRange validRange) : base(name)
        {
            Value = 0;
            ValidRange = validRange;
            ValueText = string.Format(ValueFormat, Value);
        }

        public float Increment { get; init; } = 1f;
        private FloatRange ValidRange { get; }
        internal bool ZeroIsInfinity { get; init; }
        internal string ValueFormat { get; init; } = "{0}";
        public Action<float>? OnValueChanged { get; init; }
        
        public float Value { get; private set; }
        
        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            ValueChanged(opt.GetFloat());
            PlayerControl.LocalPlayer.RpcSyncCustomSettings();
            OptionsManager.ValueChanged();
        }

        private void ValueChanged(float newValue)
        {
            Value = newValue;
            ValueText = string.Format(ValueFormat, Value);
            OnValueChanged?.Invoke(Value);
        }

        internal override byte[] ToBytes()
        {
            return BitConverter.GetBytes(Value);
        }

        internal override void ByteValueChanged(byte[] newValue)
        {
            ValueChanged(BitConverter.ToSingle(newValue));
        }

        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            var option = baseOption.TryCast<NumberOption>();
            if (option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(NumberOption)}");
                return;
            }
            
            option.Title = OptionsManager.CustomOptionStringName;
            option.TitleText.Text = Name;
            option.Increment = Increment;
            option.FormatString = ValueFormat;
            option.ValidRange = ValidRange;
            option.ZeroIsInfinity = ZeroIsInfinity;
            option.Value = Value;
            option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
        }
    }
}