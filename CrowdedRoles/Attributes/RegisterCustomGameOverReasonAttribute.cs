using System;
using System.Reflection;
using BepInEx.IL2CPP;
using CrowdedRoles.GameOverReasons;
using HarmonyLib;

namespace CrowdedRoles.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterCustomGameOverReasonAttribute : Attribute
    {
        public static void Register(BasePlugin plugin)
        {
            Register(Assembly.GetCallingAssembly(), plugin);
        }

        public static void Register(Assembly assembly, BasePlugin plugin)
        {
            foreach (var type in assembly.GetTypes())
            {
                var attribute = type.GetCustomAttribute<RegisterCustomGameOverReasonAttribute>();

                if (attribute != null)
                {
                    if (!type.IsSubclassOf(typeof(CustomGameOverReason)))
                    {
                        throw new InvalidOperationException($"Type {type.FullDescription()} must extend {nameof(CustomGameOverReason)}.");
                    }

                    Activator.CreateInstance(type, plugin);
                }
            }
        }
    }
}