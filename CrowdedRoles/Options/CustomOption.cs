using BepInEx.Configuration;
using BepInEx.IL2CPP;

namespace CrowdedRoles.Options
{
    public abstract class CustomOption
    {
        public string Name { get; }
        protected internal string ValueText { get; protected set; } = "None";

        protected CustomOption(string name)
        {
            Name = name;
        }

        public void Register<T>(OptionPluginWrapper wrapper) where T : CustomOption
        {
            wrapper.AddCustomOption((T)this);
        }
        
        public void Register(BasePlugin plugin)
        {
            OptionsManager.AddCustomOption(plugin, this);
        }

        internal abstract byte[] ToBytes();
        internal abstract void ByteValueChanged(byte[] newValue);

        internal abstract void ImplementOption(ref OptionBehaviour baseOption);
        internal abstract void LoadValue(ConfigFile file, string guid, string name = "");
    }
}