namespace CrowdedRoles.Roles
{
    public enum Team : byte
    {
        /// <summary>
        /// Wins with crew
        /// </summary>
        Crewmate = 0,
        /// <summary>
        /// Wins with Impostors, also they are able to see role holders if <see cref="Visibility"/> allows it
        /// </summary>
        Impostor = 1,
        /// <summary>
        /// Doesn't win by in-game win reasons
        /// </summary>
        Alone = 2,
        /// <summary>
        /// Doesn't win by in-game win reasons
        /// </summary>
        SameRole = 3
    }
}
