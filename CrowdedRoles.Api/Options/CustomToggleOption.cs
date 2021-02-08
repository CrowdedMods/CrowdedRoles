using System;

namespace CrowdedRoles.Api.Options
{
    public class CustomToggleOption : CustomOption
    {
        public CustomToggleOption(string name) : base(name)
        {
            ValueText = Value ? "On" : "Off";
        }

        public bool Value { get; private set; }

        public Action<bool>? OnValueChanged;

        private void OnValueChangedRaw(OptionBehaviour opt)
        {
            Value = opt.GetBool();
            ValueText = Value ? "On" : "Off";
            OnValueChanged?.Invoke(Value);
            OptionsManager.ValueChanged();
        }

        internal override void ImplementOption(ref OptionBehaviour baseOption)
        {
            var option = baseOption.TryCast<ToggleOption>();
            if (option == null)
            {
                RoleApiPlugin.Logger.LogError($"Object `{baseOption.name}` is not {nameof(ToggleOption)}");
                return;
            }
            
            option.Title = OptionsManager.CustomOptionStringName;
            option.TitleText.Text = Name; // why the heck it's not in OptionBehaviour
            option.CheckMark.enabled = Value;
            option.OnValueChanged = (Action<OptionBehaviour>) OnValueChangedRaw;
            RoleApiPlugin.Logger.LogDebug($"Added toggle option {Name}");
        }
    }
}