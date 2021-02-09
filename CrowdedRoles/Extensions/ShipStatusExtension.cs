using CrowdedRoles.Roles;

namespace CrowdedRoles.Extensions
{
    public static class ShipStatusExtension
    {
        public static void GameEnded(this ShipStatus _)
        {
            RoleManager.PlayerRoles.Clear();
        }
    }
}
