using CrowdedRoles.Api.Roles;

namespace CrowdedRoles.Api.Extensions
{
    public static class ShipStatusExtension
    {
        public static void GameEnded(this ShipStatus _)
        {
            RoleManager.PlayerRoles.Clear();
        }
    }
}
