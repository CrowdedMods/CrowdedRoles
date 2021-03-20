using System;
using System.Reflection;
using CrowdedRoles.UI;
using HarmonyLib;

namespace CrowdedRoles.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterCustomButtonAttribute : Attribute
    {
        public static void Register()
        {
            Register(Assembly.GetCallingAssembly());
        }

        public static void Register(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<RegisterCustomButtonAttribute>();

                if (attribute != null)
                {
                    if (!type.IsSubclassOf(typeof(CooldownButton)))
                    {
                        throw new InvalidOperationException($"Type {type.FullDescription()} must extend {nameof(CooldownButton)}.");
                    }

                    ButtonManager.RegisteredButtons.Add((CooldownButton)Activator.CreateInstance(type));
                }
            }
        }
    }
}