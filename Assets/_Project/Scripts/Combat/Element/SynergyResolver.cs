public static class SynergyResolver
{
    public static bool TryResolve(ElementType first, ElementType second, out SynergyReaction reaction)
    {
        reaction = SynergyReaction.None;

        if (IsPair(first, second, ElementType.Water, ElementType.Lightning))
        {
            reaction = SynergyReaction.Shock;
            return true;
        }

        if (IsPair(first, second, ElementType.Ice, ElementType.Earth))
        {
            reaction = SynergyReaction.Shatter;
            return true;
        }

        return false;
    }

    private static bool IsPair(ElementType first, ElementType second, ElementType expectedFirst, ElementType expectedSecond)
    {
        return (first == expectedFirst && second == expectedSecond) ||
            (first == expectedSecond && second == expectedFirst);
    }
}
