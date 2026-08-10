using UnityEditor;
using UnityEngine;

public static class NetworkCharacterApplicationValidator
{
    private const string PlayerPrefabPath =
        "Assets/_Project/Prefabs/Players/NetworkPlayer.prefab";

    private const string OfflinePlayerPrefabPath =
        "Assets/_Project/Prefabs/Players/Player.prefab";

    private const string JumongDataPath =
        "Assets/_Project/Scripts/Data/Characters/JumongData.asset";

    private const string CheokDataPath =
        "Assets/_Project/Scripts/Data/Characters/CheokJunGyeongData.asset";

    [MenuItem("Shura/Validate/Network Character Application")]
    public static void Validate()
    {
        GameObject playerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        CharacterData jumong =
            AssetDatabase.LoadAssetAtPath<CharacterData>(JumongDataPath);
        CharacterData cheok =
            AssetDatabase.LoadAssetAtPath<CharacterData>(CheokDataPath);
        GameObject offlinePlayerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(OfflinePlayerPrefabPath);

        Require(playerPrefab != null, "NetworkPlayer 프리팹을 찾지 못했습니다.");
        Require(jumong != null, "JumongData를 찾지 못했습니다.");
        Require(cheok != null, "CheokJunGyeongData를 찾지 못했습니다.");
        Require(offlinePlayerPrefab != null, "싱글 Player 프리팹을 찾지 못했습니다.");

        NetworkPlayerCharacter character =
            playerPrefab.GetComponent<NetworkPlayerCharacter>();

        Require(character != null, "NetworkPlayerCharacter가 없습니다.");
        Require(jumong.GameplayVisualPrefab != null, "주몽 전투 외형이 비어 있습니다.");
        Require(jumong.BasicSkill != null, "주몽 기본공격이 비어 있습니다.");
        Require(jumong.StartingSkills.Count == 3, "주몽 시작 스킬은 3개여야 합니다.");
        Require(jumong.MaxHealth > 0f, "주몽 체력이 올바르지 않습니다.");
        Require(jumong.MoveSpeed > 0f, "주몽 이동속도가 올바르지 않습니다.");
        Require(cheok.CharacterId == 1, "척준경 캐릭터 ID는 1이어야 합니다.");
        Require(cheok.BasicSkill != null, "척준경 기본공격이 비어 있습니다.");
        Require(cheok.BasicSkill.Damage > 0f, "척준경 기본공격 피해량이 0입니다.");
        Require(cheok.BasicSkill.SkillPrefab != null, "척준경 기본공격 프리팹이 비어 있습니다.");
        Require(
            offlinePlayerPrefab.GetComponent<DirectionalAutoAttack>() != null,
            "싱글 Player 프리팹에 척준경 근접 공격 컴포넌트가 없습니다."
        );
        Require(
            playerPrefab.GetComponent<DirectionalAutoAttack>() != null,
            "NetworkPlayer 프리팹에 척준경 근접 공격 컴포넌트가 없습니다."
        );

        SerializedObject serializedCharacter =
            new SerializedObject(character);
        SerializedProperty characters =
            serializedCharacter.FindProperty("characters");
        SerializedProperty visualRoot =
            serializedCharacter.FindProperty("visualRoot");

        Require(characters != null && characters.arraySize >= 2,
            "네트워크 플레이어 캐릭터 목록에 2명이 모두 필요합니다.");
        Require(
            characters.GetArrayElementAtIndex(0).objectReferenceValue == jumong,
            "캐릭터 ID 0이 JumongData를 참조하지 않습니다."
        );
        Require(
            characters.GetArrayElementAtIndex(1).objectReferenceValue == cheok,
            "캐릭터 ID 1이 CheokJunGyeongData를 참조하지 않습니다."
        );
        Require(
            visualRoot != null && visualRoot.objectReferenceValue != null,
            "CharacterVisualRoot 참조가 비어 있습니다."
        );

        ValidateCheokMeleeDamage(cheok);

        Debug.Log(
            "[NetworkCharacterApplicationValidator] PASS: " +
            "single/network character consumers, Jumong and Cheok data, " +
            "basic skills and combat components"
        );
    }

    private static void ValidateCheokMeleeDamage(CharacterData cheok)
    {
        const int enemyLayer = 8;
        GameObject target = new GameObject("CheokDamageValidationTarget");

        try
        {
            target.layer = enemyLayer;
            target.transform.position = Vector3.right;
            target.AddComponent<BoxCollider2D>();
            EnemyHealth health = target.AddComponent<EnemyHealth>();
            health.ConfigureRuntime(1f, 0f, null, 1);
            float healthBefore = health.CurrentHealth;

            Physics2D.SyncTransforms();
            CheokJunGyeongDamage.Arc(
                Vector2.zero,
                Vector2.right,
                Mathf.Max(2f, cheok.BasicSkill.Range),
                120f,
                cheok.BasicSkill.Damage,
                ElementType.None,
                ulong.MaxValue,
                1 << enemyLayer
            );

            Require(
                health.CurrentHealth < healthBefore,
                "척준경 근접 공격 판정이 적에게 피해를 적용하지 못합니다."
            );
        }
        finally
        {
            Object.DestroyImmediate(target);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
