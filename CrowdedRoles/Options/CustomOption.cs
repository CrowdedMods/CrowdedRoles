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

        public T Register<T>(OptionPluginWrapper wrapper) where T : CustomOption
        {
            return wrapper.AddCustomOption((T)this);
        }
        
        public T Register<T>(BasePlugin plugin) where T : CustomOption
        {
            return OptionsManager.AddCustomOption(plugin, (T)this);
        }

        internal abstract byte[] ToBytes();
        internal abstract void ByteValueChanged(byte[] newValue);

        internal abstract void ImplementOption(ref OptionBehaviour baseOption);
    }
}