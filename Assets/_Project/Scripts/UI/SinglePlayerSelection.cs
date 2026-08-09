public static class SinglePlayerSelection
{
    public static CharacterData Character { get; private set; }

    public static void SetCharacter(CharacterData character)
    {
        Character = character;
    }

    public static void Clear()
    {
        Character = null;
    }
}
