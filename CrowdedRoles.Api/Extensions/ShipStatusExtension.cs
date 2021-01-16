

namespace CrowdedRoles.Api.Extensions
{
    public static class ShipStatusExtension
    {

        public static void GameEnded(this ShipStatus _)
        {
            PlayerControlExtension.PlayerRoles.Clear();
        }
    }
}
