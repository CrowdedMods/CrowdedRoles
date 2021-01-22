using Hazel;

namespace CrowdedRoles.Api.Extensions
{
    public interface ISerializable<out T> where T : ISerializable<T>
    {
        void Serialize(MessageWriter writer);
        T Deserialize (MessageReader reader);
    }
}