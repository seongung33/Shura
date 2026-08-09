using System.Collections.Generic;

public static class GameplayPauseState
{
    private static readonly HashSet<NetworkPlayerProgression>
        ActiveLevelUps = new();
    private static readonly HashSet<PlayerRelicInventory>
        ActiveRelicChoices = new();

    public static bool IsLevelUpActive =>
        ActiveLevelUps.Count > 0 || ActiveRelicChoices.Count > 0;

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

    public static void SetRelicChoiceActive(
        PlayerRelicInventory inventory,
        bool isActive
    )
    {
        if (inventory == null)
        {
            return;
        }

        ActiveRelicChoices.RemoveWhere(candidate => candidate == null);

        if (isActive)
        {
            ActiveRelicChoices.Add(inventory);
        }
        else
        {
            ActiveRelicChoices.Remove(inventory);
        }
    }
}
