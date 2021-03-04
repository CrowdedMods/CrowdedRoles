using System;
using System.Globalization;
using BepInEx.Configuration;
using CrowdedRoles.Extensions;

namespace CrowdedRoles.Options
{
    public class CustomNumberOption : CustomOption
    {
        public CustomNumberOption(string name, FloatRange validRange) : base(name)
        {
            ValidRange = validRange;
        }

        private ConfigEntry<float> SavedValue = null!;

        public float Increment { get; init; } = 1f;
        private FloatRange ValidRange { get; }
        public bool ZeroIsInfinity { get; init; } = false;
        private readonly string _valueFormat = "{0}";
        public string ValueFormat
        {
            get => _valueFormat;
            init
            {
                _valueFormat = value;
                ValueText = string.Format(value, Value);
            }
        }
        public Action<float>? OnValueChanged { get; init; }

        private float _value;
        public float Value
        {
            get => _value;
            private set
            {
                _value = value;
                ValueText = string.Format(ValueFormat, value);
            } 
        }

        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            UpdateValue(SavedValue.Value = opt.GetFloat());
            PlayerControl.LocalPlayer.RpcSyncCustomSettings();
            OptionsManager.ValueChanged();
        }

        private void UpdateValue(float newValue)
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
            UpdateValue(BitConverter.ToSingle(newValue));
        }

        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            var option = baseOption.TryCast<NumberOption>();
            if (option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(NumberOption)}");
                return;
            }
            
            option.TitleText.Text = Name;
            option.Increment = Increment;
            option.FormatString = ValueFormat;
            option.ValidRange = ValidRange;
            option.ZeroIsInfinity = ZeroIsInfinity;
            option.Value = Value;
            option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
        }

        internal override void LoadValue(ConfigFile file, string guid, string name = "")
        {
            SavedValue = file.Bind(guid, OptionsManager.MakeSaveNameValid(name == "" ? Name : name), ValidRange.min);

            Value = SavedValue.Value;
        }
    }
}