namespace CrowdedRoles.Api.Options
{
    public abstract class CustomOption
    {
        public string Name { get; }
        protected internal string ValueText { get; protected set; } = "None";

        protected CustomOption(string name)
        {
            Name = name;
        }

        internal abstract void ImplementOption(ref OptionBehaviour baseOption);
    }
}