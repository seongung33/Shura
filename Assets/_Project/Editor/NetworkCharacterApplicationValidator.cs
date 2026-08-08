using UnityEditor;
using UnityEngine;

public static class NetworkCharacterApplicationValidator
{
    private const string PlayerPrefabPath =
        "Assets/_Project/Prefabs/Players/NetworkPlayer.prefab";

    private const string JumongDataPath =
        "Assets/_Project/Scripts/Data/Characters/JumongData.asset";

    [MenuItem("Shura/Validate/Network Character Application")]
    public static void Validate()
    {
        GameObject playerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        CharacterData jumong =
            AssetDatabase.LoadAssetAtPath<CharacterData>(JumongDataPath);

        Require(playerPrefab != null, "NetworkPlayer 프리팹을 찾지 못했습니다.");
        Require(jumong != null, "JumongData를 찾지 못했습니다.");

        NetworkPlayerCharacter character =
            playerPrefab.GetComponent<NetworkPlayerCharacter>();

        Require(character != null, "NetworkPlayerCharacter가 없습니다.");
        Require(jumong.GameplayVisualPrefab != null, "주몽 전투 외형이 비어 있습니다.");
        Require(jumong.BasicSkill != null, "주몽 기본공격이 비어 있습니다.");
        Require(jumong.StartingSkills.Count == 3, "주몽 시작 스킬은 3개여야 합니다.");
        Require(jumong.MaxHealth > 0f, "주몽 체력이 올바르지 않습니다.");
        Require(jumong.MoveSpeed > 0f, "주몽 이동속도가 올바르지 않습니다.");

        SerializedObject serializedCharacter =
            new SerializedObject(character);
        SerializedProperty characters =
            serializedCharacter.FindProperty("characters");
        SerializedProperty visualRoot =
            serializedCharacter.FindProperty("visualRoot");

        Require(characters != null && characters.arraySize > 0,
            "플레이어 캐릭터 목록이 비어 있습니다.");
        Require(
            characters.GetArrayElementAtIndex(0).objectReferenceValue == jumong,
            "캐릭터 ID 0이 JumongData를 참조하지 않습니다."
        );
        Require(
            visualRoot != null && visualRoot.objectReferenceValue != null,
            "CharacterVisualRoot 참조가 비어 있습니다."
        );

        Debug.Log(
            "[NetworkCharacterApplicationValidator] PASS: " +
            "lobby character consumer, Jumong visual, stats, basic skill, " +
            "three starting skills"
        );
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
