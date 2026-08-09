using System;
using System.Collections.Generic;
using Shura.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 로비에서 서버가 확정한 캐릭터 ID를 플레이어와 함께 보존하고,
/// 모든 클라이언트에서 동일한 전투 구성을 적용한다.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkPlayerCharacter : NetworkBehaviour
{
    private const string LobbySceneName = "MultiPlayerLobby";

    [SerializeField]
    private List<CharacterData> characters = new List<CharacterData>();

    [SerializeField]
    private Transform visualRoot;

    private readonly NetworkVariable<int> characterId =
        new NetworkVariable<int>(-1);

    private SpriteRenderer fallbackRenderer;
    private GameObject activeVisual;
    private NetworkLobbyState lobbyState;
    private int appliedCharacterId = int.MinValue;

    public int CharacterId => characterId.Value;
    public CharacterData Character => GetCharacter(characterId.Value);

    private void Awake()
    {
        fallbackRenderer = GetComponent<SpriteRenderer>();

        if (visualRoot == null)
        {
            Transform candidate = transform.Find("CharacterVisualRoot");
            visualRoot = candidate != null ? candidate : transform;
        }
    }

    public override void OnNetworkSpawn()
    {
        characterId.OnValueChanged += HandleCharacterChanged;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (IsServer)
        {
            TryBindLobbyState();
        }

        ApplyCharacter(characterId.Value);
        RefreshVisualVisibility(SceneManager.GetActiveScene().name);
    }

    public override void OnNetworkDespawn()
    {
        characterId.OnValueChanged -= HandleCharacterChanged;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindLobbyState();
    }

    private void Update()
    {
        if (!IsServer || (lobbyState == null && characterId.Value >= 0))
        {
            return;
        }

        TryBindLobbyState();

        if (lobbyState != null)
        {
            TrySynchronizeLobbySelection();
        }
    }

    private void TryBindLobbyState()
    {
        if (lobbyState != null)
        {
            return;
        }

        lobbyState = FindFirstObjectByType<NetworkLobbyState>();

        if (lobbyState == null)
        {
            return;
        }

        lobbyState.StateChanged += HandleLobbyStateChanged;
        TrySynchronizeLobbySelection();
    }

    private void UnbindLobbyState()
    {
        if (lobbyState != null)
        {
            lobbyState.StateChanged -= HandleLobbyStateChanged;
            lobbyState = null;
        }
    }

    private void HandleLobbyStateChanged()
    {
        TrySynchronizeLobbySelection();
    }

    private void TrySynchronizeLobbySelection()
    {
        if (lobbyState == null ||
            !lobbyState.TryGetPlayerSelection(
                OwnerClientId,
                out int selectedCharacterId
            ) ||
            GetCharacter(selectedCharacterId) == null ||
            characterId.Value == selectedCharacterId)
        {
            return;
        }

        characterId.Value = selectedCharacterId;
    }

    private void HandleCharacterChanged(int previous, int current)
    {
        ApplyCharacter(current);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshVisualVisibility(scene.name);
    }

    private void RefreshVisualVisibility(string sceneName)
    {
        bool isVisible = sceneName != LobbySceneName;

        if (activeVisual != null)
        {
            activeVisual.SetActive(isVisible);
        }

        if (fallbackRenderer != null)
        {
            fallbackRenderer.enabled = activeVisual == null && isVisible;
        }
    }

    private void ApplyCharacter(int selectedCharacterId)
    {
        CharacterData data = GetCharacter(selectedCharacterId);

        if (data == null)
        {
            data = GetCharacter(0);
            selectedCharacterId = data != null ? 0 : -1;
        }

        if (data == null || appliedCharacterId == selectedCharacterId)
        {
            return;
        }

        appliedCharacterId = selectedCharacterId;
        ApplyVisual(data);

        GetComponent<NetworkPlayerMovement>()?.ConfigureBaseSpeed(
            data.MoveSpeed
        );

        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        playerHealth?.ConfigureMaxHealth(data.MaxHealth);

        GetComponent<DirectionalAutoAttack>()?.ConfigureBasicSkill(
            data.BasicSkill
        );
        GetComponent<PlayerRuntimeGrowth>()?.ConfigureBasicSkill(
            data.BasicSkill
        );

        NetworkPlayerProgression progression =
            GetComponent<NetworkPlayerProgression>();

        IReadOnlyList<SkillData> networkStartingSkills =
            GetNetworkStartingSkills(data);

        GetComponent<AutoSkillCaster>()?.ConfigureSkills(
            networkStartingSkills
        );

        GetComponent<NetworkSkillCastRelay>()?.ConfigureAllowedSkills(
            data.BasicSkill,
            networkStartingSkills,
            data.LevelUpSkills
        );

        progression?.ConfigureCharacter(data);

        if (IsServer)
        {
            GetComponent<NetworkPlayerHealth>()?.ResetHealthServer();
        }

        Debug.Log(
            $"캐릭터 전투 구성 적용: clientId={OwnerClientId}, " +
            $"characterId={selectedCharacterId}, name={data.characterName}"
        );
    }

    private void ApplyVisual(CharacterData data)
    {
        if (activeVisual != null)
        {
            Destroy(activeVisual);
        }

        if (data.GameplayVisualPrefab == null)
        {
            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled = true;
            }

            Debug.LogWarning(
                $"{data.name}에 전투 외형 프리팹이 없어 기본 표시를 사용합니다."
            );
            RefreshVisualVisibility(SceneManager.GetActiveScene().name);
            return;
        }

        activeVisual = Instantiate(
            data.GameplayVisualPrefab,
            visualRoot
        );
        activeVisual.name = $"{data.characterName}Visual";
        activeVisual.transform.SetLocalPositionAndRotation(
            Vector3.zero,
            Quaternion.identity
        );
        activeVisual.transform.localScale = Vector3.one;

        if (fallbackRenderer != null)
        {
            fallbackRenderer.enabled = false;
        }

        RefreshVisualVisibility(SceneManager.GetActiveScene().name);
    }

    private CharacterData GetCharacter(int selectedCharacterId)
    {
        if (selectedCharacterId < 0 ||
            selectedCharacterId >= characters.Count)
        {
            return null;
        }

        return characters[selectedCharacterId];
    }

    private static IReadOnlyList<SkillData> GetNetworkStartingSkills(
        CharacterData character
    )
    {
        if (character == null || character.StartingSkills == null)
        {
            return Array.Empty<SkillData>();
        }

        if (!character.UsesLevelUpSkillPool)
        {
            return character.StartingSkills;
        }

        List<SkillData> innateSkills = new();

        foreach (SkillData startingSkill in character.StartingSkills)
        {
            bool isLevelUpSkill = false;

            foreach (SkillData levelUpSkill in character.LevelUpSkills)
            {
                if (startingSkill == levelUpSkill)
                {
                    isLevelUpSkill = true;
                    break;
                }
            }

            if (startingSkill != null && !isLevelUpSkill)
            {
                innateSkills.Add(startingSkill);
            }
        }

        return innateSkills;
    }
}
