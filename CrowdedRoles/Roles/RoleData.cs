using System;
using Hazel;

namespace CrowdedRoles.Roles
{
    internal class RoleData
    {
        public readonly string pluginId;
        public readonly byte localId;

        public void Serialize(MessageWriter writer)
        {
            writer.Write(pluginId);
            writer.Write(localId);
        }

        public static RoleData Deserialize(MessageReader reader)
        {
            return new(
                reader.ReadString(),
                reader.ReadByte()
            );
        }
        
        public RoleData(string guid, byte id)
        {
            pluginId = guid;
            localId = id;
        }

        public static bool operator ==(RoleData? me, RoleData? other) => me?.Equals(other) ?? ReferenceEquals(other, null);
        public static bool operator !=(RoleData? me, RoleData? other)=> !(me == other); // c# is cool

        private bool Equals(RoleData? other)
        {
            return pluginId == other?.pluginId && localId == other.localId;
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) &&
                   obj.GetType() == GetType() &&
                   Equals((RoleData) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(pluginId, localId);
        }
    }
}