using Hazel;

namespace CrowdedRoles.Api.Extensions
{
    public static class HazelExtensions
    {
        public static T Read<T>(this MessageReader reader) where T : ISerializable
        {
            return (T)ISerializable.Deserialize(reader);
        }

        public static void Write<T>(this MessageWriter writer, T data) where T : ISerializable
        {
            data.Serialize(writer);
        }
    }
}