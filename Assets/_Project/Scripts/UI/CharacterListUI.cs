using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterListUI : MonoBehaviour
{
    [SerializeField] private Transform characterGrid;
    [SerializeField] private CharacterSlotUI characterSlotPrefab;
    [SerializeField] private CharacterInfoUI characterInfoUI;
    [SerializeField] private MyPlayerSlotUI myPlayerSlotUI;

    [Header("멀티플레이에서만 연결")]
    [SerializeField] private NetworkLobbyState networkLobbyState;
    [SerializeField] private MyPlayerSlotUI otherPlayerSlotUI;

    [Header("등장할 캐릭터")]
    [SerializeField] private List<CharacterData> characters;

    private bool lobbyStateSubscribed;
    private int pendingCharacterId = -1;

    public CharacterData SelectedCharacter => GetCharacter(pendingCharacterId);

    private void Start()
    {
        CreateCharacterSlots();
        CharacterSelectVisualTheme.Apply(transform);

        int firstCharacterId = FindFirstCharacterId();

        if (firstCharacterId < 0)
            return;

        pendingCharacterId = firstCharacterId;
        ShowLocalCharacter(firstCharacterId);

        if (otherPlayerSlotUI != null)
        {
            otherPlayerSlotUI.gameObject.SetActive(false);
        }

        if (networkLobbyState != null)
        {
            StartCoroutine(ConnectLobbyState());
        }
    }

    private void OnDestroy()
    {
        if (lobbyStateSubscribed && networkLobbyState != null)
        {
            networkLobbyState.StateChanged -= RefreshNetworkPlayers;
        }
    }

    private void CreateCharacterSlots()
    {
        if (characterGrid == null || characterSlotPrefab == null)
            return;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData data = characters[i];

            if (data == null)
                continue;

            CharacterSlotUI slot =
                Instantiate(characterSlotPrefab, characterGrid);

            slot.Initialize(
                i,
                data,
                characterInfoUI,
                myPlayerSlotUI,
                HandleCharacterSelected
            );

            CharacterSelectVisualTheme.StyleCharacterCard(slot.transform);
        }
    }

    private IEnumerator ConnectLobbyState()
    {
        float waitTime = 0f;

        while (networkLobbyState != null &&
               !networkLobbyState.IsSpawned &&
               waitTime < 5f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (networkLobbyState == null ||
            !networkLobbyState.IsSpawned)
        {
            Debug.LogError(
                "NetworkLobbyState가 네트워크에 Spawn되지 않았습니다."
            );
            yield break;
        }

        networkLobbyState.StateChanged += RefreshNetworkPlayers;
        lobbyStateSubscribed = true;

        networkLobbyState.RequestCharacterSelection(
            pendingCharacterId
        );

        RefreshNetworkPlayers();
    }

    private void HandleCharacterSelected(int characterId)
    {
        pendingCharacterId = characterId;
        ShowLocalCharacter(characterId);

        if (networkLobbyState != null &&
            networkLobbyState.IsSpawned)
        {
            networkLobbyState.RequestCharacterSelection(
                characterId
            );
        }
    }

    public void StartSinglePlayerGame()
    {
        CharacterData selectedCharacter = SelectedCharacter;

        if (selectedCharacter == null)
        {
            Debug.LogError("선택된 캐릭터가 없어 싱글 플레이를 시작할 수 없습니다.");
            return;
        }

        SinglePlayerSelection.SetCharacter(selectedCharacter);
        SceneManager.LoadScene("Main");
    }

    public void BackToMainMenu()
    {
        SinglePlayerSelection.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    private void RefreshNetworkPlayers()
    {
        if (networkLobbyState == null ||
            NetworkManager.Singleton == null)
        {
            return;
        }

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        bool foundOtherPlayer = false;

        for (int slotIndex = 0; slotIndex < 2; slotIndex++)
        {
            if (!networkLobbyState.TryGetSlot(
                    slotIndex,
                    out ulong clientId,
                    out int characterId))
            {
                continue;
            }

            CharacterData data = GetCharacter(characterId);

            if (clientId == localClientId)
            {
                if (data != null)
                {
                    myPlayerSlotUI?.ShowCharacter(data);
                }

                continue;
            }

            foundOtherPlayer = true;

            if (data != null && otherPlayerSlotUI != null)
            {
                otherPlayerSlotUI.gameObject.SetActive(true);
                otherPlayerSlotUI.ShowCharacter(data);
            }
        }

        if (!foundOtherPlayer && otherPlayerSlotUI != null)
        {
            otherPlayerSlotUI.gameObject.SetActive(false);
        }
    }

    private void ShowLocalCharacter(int characterId)
    {
        CharacterData data = GetCharacter(characterId);

        if (data == null)
            return;

        characterInfoUI?.ShowCharacter(data);
        myPlayerSlotUI?.ShowCharacter(data);
    }

    private CharacterData GetCharacter(int characterId)
    {
        if (characterId < 0 ||
            characterId >= characters.Count)
        {
            return null;
        }

        return characters[characterId];
    }

    private int FindFirstCharacterId()
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null)
            {
                return i;
            }
        }

        return -1;
    }
}
