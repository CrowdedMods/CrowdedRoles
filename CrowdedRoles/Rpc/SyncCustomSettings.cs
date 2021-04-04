using System;
using System.Collections.Generic;
using CrowdedRoles.Options;
using CrowdedRoles.Roles;
using Hazel;
using Reactor;
using Reactor.Networking;

namespace CrowdedRoles.Rpc
{
    [RegisterCustomRpc((uint)CustomRpcCalls.SyncCustomSettings)]
    internal class SyncCustomSettings : PlayerCustomRpc<RoleApiPlugin, SyncCustomSettings.Data>
    {
        public SyncCustomSettings(RoleApiPlugin plugin, uint id) : base(plugin, id)
        {
        }

        public struct Data
        {
            public Dictionary<RoleData, byte> limits;
            public Dictionary<string, List<byte[]>> options;
        }

        public override RpcLocalHandling LocalHandling { get; } = RpcLocalHandling.None;
        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.limits.Count);
            foreach (var (role, limit) in data.limits)
            {
                role.Serialize(writer);
                writer.Write(limit);
            }
            
            writer.Write(data.options.Count);
            foreach (var (plugin, options) in data.options)
            {
                writer.Write(plugin);
                writer.Write(options.Count);
                foreach (byte[] option in options)
                {
                    writer.WriteBytesAndSize(option);
                }
            }
        }

        public override Data Read(MessageReader reader)
        {
            var limits = new Dictionary<RoleData, byte>();
            {
                int length = reader.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    limits.Add(
                        RoleData.Deserialize(reader),
                        reader.ReadByte()
                    );
                }
            }

            var options = new Dictionary<string, List<byte[]>>();
            {
                int length = reader.ReadInt32();
                for (int i = 0; i < length; i++)
                {
                    string guid = reader.ReadString();
                    int len = reader.ReadInt32();
                    var values = new List<byte[]>(len);
                    for (int j = 0; j < len; j++)
                    {
                        values.Add(reader.ReadBytesAndSize());
                    }
                    options.Add(guid, values);
                }
            }

            return new Data
            {
                limits = limits,
                options = options
            };
        }

        public override void Handle(PlayerControl host, Data data)
        {
            if (host == null)
            {
                RoleApiPlugin.Logger.LogWarning($"Invalid sender of {nameof(SyncCustomSettings)}");
                return;
            }

            if (host.PlayerId != GameData.Instance.GetHost().PlayerId)
            {
                RoleApiPlugin.Logger.LogWarning($"{host.PlayerId} sent {nameof(SyncCustomSettings)}, but was not a host");
                return;
            }

            foreach (var (roleData, limit) in data.limits)
            {
                BaseRole? role = RoleManager.GetRoleByData(roleData);
                if (role == null)
                {
                    throw new NullReferenceException($"Cannot find role by data {roleData.pluginId}:{roleData.localId}");
                }
                RoleManager.Limits[role] = limit;
                CustomOption? limitOption = OptionsManager.LimitOptions[role];
                limitOption?.ByteValueChanged(BitConverter.GetBytes((float)limit));
            }

            foreach (var (guid, values) in data.options)
            {
                List<CustomOption>? options = OptionsManager.CustomOptions[guid];
                if (options == null)
                {
                    throw new NullReferenceException($"Cannot find registered options by plugin {guid}");
                }

                for (int i = 0; i < values.Count; i++)
                {
                    options[i].ByteValueChanged(values[i]);
                }
            }
            
            OptionsManager.ValueChanged();
        }
    }
}