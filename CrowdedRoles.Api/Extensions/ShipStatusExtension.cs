using System;
using System.Collections.Generic;
using System.Text;

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
