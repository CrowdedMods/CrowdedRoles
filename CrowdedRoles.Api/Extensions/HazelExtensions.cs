using System;
using Hazel;

namespace CrowdedRoles.Api.Extensions
{
    public static class HazelExtensions
    {
        public static T Read<T>(this MessageReader reader) where T : ISerializable<T>
        {
            return Activator.CreateInstance<T>().Deserialize(reader);
        }

        public static void Write<T>(this MessageWriter writer, T data) where T : ISerializable<T>
        {
            data.Serialize(writer);
        }
    }
}