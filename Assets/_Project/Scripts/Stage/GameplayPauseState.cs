using System.Collections.Generic;

public static class GameplayPauseState
{
    private static readonly HashSet<NetworkPlayerProgression>
        ActiveLevelUps = new();

    public static bool IsLevelUpActive => ActiveLevelUps.Count > 0;

    public static void SetLevelUpActive(
        NetworkPlayerProgression progression,
        bool isActive
    )
    {
        if (progression == null)
        {
            return;
        }

        ActiveLevelUps.RemoveWhere(candidate => candidate == null);

        if (isActive)
        {
            ActiveLevelUps.Add(progression);
        }
        else
        {
            ActiveLevelUps.Remove(progression);
        }
    }
}
