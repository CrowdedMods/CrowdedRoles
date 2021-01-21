using System;
using Hazel;

namespace CrowdedRoles.Api.Extensions
{
    public interface ISerializable
    {
        public static ISerializable Deserialize(MessageReader reader)
        {
            throw new NotImplementedException();
        }
        public void Serialize(MessageWriter writer);
    }
}