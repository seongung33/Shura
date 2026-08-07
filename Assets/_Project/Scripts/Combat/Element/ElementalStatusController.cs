using System.Collections.Generic;

public sealed class ElementalStatusController
{
    private struct ElementRecord
    {
        public ulong SourcePlayerId;
        public float AppliedAt;
    }

    private readonly Dictionary<ElementType, ElementRecord> records = new Dictionary<ElementType, ElementRecord>();
    private readonly Dictionary<SynergyReaction, float> nextReactionTimes =
        new Dictionary<SynergyReaction, float>();

    public bool TryApply(
        ElementType element,
        ulong sourcePlayerId,
        float appliedAt,
        float comboWindow,
        float internalCooldown,
        out SynergyReaction reaction
    )
    {
        reaction = SynergyReaction.None;

        if (element == ElementType.None || sourcePlayerId == ulong.MaxValue)
        {
            return false;
        }

        foreach (ElementType previousElement in GetCandidates(element))
        {
            if (!records.TryGetValue(previousElement, out ElementRecord previous))
            {
                continue;
            }

            bool isWithinWindow = appliedAt - previous.AppliedAt <= comboWindow;
            bool isDifferentPlayer = previous.SourcePlayerId != sourcePlayerId;

            if (isWithinWindow && isDifferentPlayer &&
                SynergyResolver.TryResolve(previousElement, element, out reaction))
            {
                if (nextReactionTimes.TryGetValue(reaction, out float nextTime) &&
                    appliedAt < nextTime)
                {
                    reaction = SynergyReaction.None;
                    break;
                }

                records.Remove(previousElement);
                records.Remove(element);
                nextReactionTimes[reaction] = appliedAt + internalCooldown;
                return true;
            }
        }

        records[element] = new ElementRecord
        {
            SourcePlayerId = sourcePlayerId,
            AppliedAt = appliedAt
        };

        return false;
    }

    private static IEnumerable<ElementType> GetCandidates(ElementType element)
    {
        switch (element)
        {
            case ElementType.Water: yield return ElementType.Lightning; break;
            case ElementType.Lightning: yield return ElementType.Water; break;
            case ElementType.Ice: yield return ElementType.Earth; break;
            case ElementType.Earth: yield return ElementType.Ice; break;
        }
    }
}
