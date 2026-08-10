using System.Collections.Generic;
using Shura.Player;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerHealth))]
public class NetworkPlayerHealth : NetworkBehaviour
{
    private const float ReviveRadius = 2.5f;
    private const float ReviveChannelDuration = 4f;
    private const float EliteWaitReduction = 10f;

    private static readonly HashSet<NetworkPlayerHealth> SpawnedPlayers = new();

    private readonly NetworkVariable<float> currentHealth =
        new NetworkVariable<float>();

    private readonly NetworkVariable<bool> isDead =
        new NetworkVariable<bool>();

    private readonly NetworkVariable<bool> reviveReady =
        new NetworkVariable<bool>();

    private readonly NetworkVariable<float> reviveProgress =
        new NetworkVariable<float>();

    private PlayerHealth playerHealth;
    private float reviveAvailableTime;
    private float reviveChannelProgress;
    private int deathCount;
    private GameObject reviveVisual;
    private TextMesh reviveText;
    private Material reviveMaterial;

    public bool IsDead => isDead.Value;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    public override void OnNetworkSpawn()
    {
        SpawnedPlayers.Add(this);
        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeathStateChanged;

        if (IsServer)
        {
            currentHealth.Value = playerHealth.MaxHealth;
            isDead.Value = false;
            reviveReady.Value = false;
            reviveProgress.Value = 0f;
            reviveChannelProgress = 0f;
            deathCount = 0;
        }

        ApplyStateToPlayer();

        if (IsOwner)
        {
            NetworkTeammateOffscreenIndicator indicator =
                GetComponent<NetworkTeammateOffscreenIndicator>();
            indicator ??=
                gameObject.AddComponent<NetworkTeammateOffscreenIndicator>();
            indicator.enabled = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        SpawnedPlayers.Remove(this);
        currentHealth.OnValueChanged -= OnHealthChanged;
        isDead.OnValueChanged -= OnDeathStateChanged;
        DestroyReviveVisual();

        NetworkTeammateOffscreenIndicator indicator =
            GetComponent<NetworkTeammateOffscreenIndicator>();
        if (indicator != null)
        {
            indicator.enabled = false;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            UpdateReviveServer();
        }

        UpdateReviveVisual();
    }

    public void TakeDamageServer(float amount)
    {
        if (!IsServer ||
            isDead.Value ||
            !IsValidAmount(amount))
        {
            return;
        }

        currentHealth.Value = Mathf.Max(
            0f,
            currentHealth.Value - amount
        );

        if (currentHealth.Value <= 0f)
        {
            isDead.Value = true;
            BeginReviveWaitServer();
        }

        ApplyStateToPlayer();
    }

    public bool RequestHeal(float amount)
    {
        if (isDead.Value ||
            !IsValidAmount(amount) ||
            currentHealth.Value >= playerHealth.MaxHealth)
        {
            return false;
        }

        if (IsServer)
        {
            ApplyHealServer(amount);
        }
        else if (IsOwner)
        {
            RequestHealRpc(amount);
        }
        else
        {
            return false;
        }

        return true;
    }

    public void ResetHealthServer()
    {
        if (!IsServer)
        {
            return;
        }

        currentHealth.Value = playerHealth.MaxHealth;
        isDead.Value = false;
        reviveReady.Value = false;
        reviveProgress.Value = 0f;
        reviveAvailableTime = 0f;
        reviveChannelProgress = 0f;
        deathCount = 0;
        ApplyStateToPlayer();
    }

    public static void ApplyEliteReviveBonusServer()
    {
        foreach (NetworkPlayerHealth player in SpawnedPlayers)
        {
            if (player != null && player.IsServer && player.isDead.Value)
            {
                player.ReduceReviveWaitServer(EliteWaitReduction);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RequestHealRpc(float amount)
    {
        ApplyHealServer(amount);
    }

    private void ApplyHealServer(float amount)
    {
        if (!IsServer ||
            isDead.Value ||
            !IsValidAmount(amount))
        {
            return;
        }

        currentHealth.Value = Mathf.Min(
            playerHealth.MaxHealth,
            currentHealth.Value + amount
        );

        ApplyStateToPlayer();
    }

    private void BeginReviveWaitServer()
    {
        if (!IsServer)
        {
            return;
        }

        deathCount++;
        reviveAvailableTime = Time.time + GetReviveDelay(deathCount);
        reviveReady.Value = false;
        reviveProgress.Value = 0f;
        reviveChannelProgress = 0f;
    }

    private void UpdateReviveServer()
    {
        if (!isDead.Value)
        {
            return;
        }

        if (GameplayPauseState.IsLevelUpActive)
        {
            if (!reviveReady.Value)
            {
                reviveAvailableTime += Time.deltaTime;
            }

            return;
        }

        if (!reviveReady.Value)
        {
            if (Time.time < reviveAvailableTime)
            {
                return;
            }

            reviveReady.Value = true;
        }

        bool rescuerPresent = HasLivingTeammateInRangeServer();
        reviveChannelProgress = rescuerPresent
            ? reviveChannelProgress + Time.deltaTime
            : 0f;

        if (reviveChannelProgress >= ReviveChannelDuration)
        {
            ReviveServer();
            return;
        }

        if (Mathf.Abs(reviveChannelProgress - reviveProgress.Value) >= 0.1f ||
            (!rescuerPresent && reviveProgress.Value > 0f))
        {
            reviveProgress.Value = reviveChannelProgress;
        }
    }

    private bool HasLivingTeammateInRangeServer()
    {
        float radiusSquared = ReviveRadius * ReviveRadius;

        foreach (NetworkPlayerHealth teammate in SpawnedPlayers)
        {
            if (teammate == null ||
                teammate == this ||
                !teammate.IsSpawned ||
                teammate.isDead.Value)
            {
                continue;
            }

            if ((teammate.transform.position - transform.position).sqrMagnitude <=
                radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    private void ReviveServer()
    {
        currentHealth.Value = playerHealth.MaxHealth;
        isDead.Value = false;
        reviveReady.Value = false;
        reviveProgress.Value = 0f;
        reviveAvailableTime = 0f;
        reviveChannelProgress = 0f;
        ApplyStateToPlayer();
    }

    private void ReduceReviveWaitServer(float seconds)
    {
        if (!IsServer || !isDead.Value || reviveReady.Value)
        {
            return;
        }

        reviveAvailableTime = Mathf.Max(
            Time.time,
            reviveAvailableTime - Mathf.Max(0f, seconds)
        );
    }

    private void UpdateReviveVisual()
    {
        bool shouldShow = IsSpawned && isDead.Value && reviveReady.Value;

        if (!shouldShow)
        {
            DestroyReviveVisual();
            return;
        }

        if (reviveVisual == null)
        {
            CreateReviveVisual();
        }

        if (reviveText != null)
        {
            float remaining = Mathf.Max(
                0f,
                ReviveChannelDuration - reviveProgress.Value
            );
            reviveText.text = $"REVIVE {remaining:0.0}s";
        }
    }

    private void CreateReviveVisual()
    {
        reviveVisual = new GameObject("ReviveZone");
        reviveVisual.transform.SetParent(transform, false);

        LineRenderer line = reviveVisual.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 48;
        line.startWidth = 0.08f;
        line.endWidth = 0.08f;
        line.startColor = new Color(0.25f, 1f, 0.55f, 0.9f);
        line.endColor = line.startColor;
        line.sortingOrder = 25;
        reviveMaterial = new Material(Shader.Find("Sprites/Default"));
        line.material = reviveMaterial;

        for (int index = 0; index < line.positionCount; index++)
        {
            float angle = index / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(index, new Vector3(
                Mathf.Cos(angle) * ReviveRadius,
                Mathf.Sin(angle) * ReviveRadius,
                0f
            ));
        }

        GameObject textObject = new GameObject("ReviveText");
        textObject.transform.SetParent(reviveVisual.transform, false);
        textObject.transform.localPosition = Vector3.up * 0.8f;
        reviveText = textObject.AddComponent<TextMesh>();
        reviveText.anchor = TextAnchor.MiddleCenter;
        reviveText.alignment = TextAlignment.Center;
        reviveText.characterSize = 0.12f;
        reviveText.fontSize = 32;
        reviveText.color = new Color(0.65f, 1f, 0.75f, 1f);
        textObject.GetComponent<MeshRenderer>().sortingOrder = 26;
    }

    private void DestroyReviveVisual()
    {
        if (reviveVisual != null)
        {
            Destroy(reviveVisual);
            reviveVisual = null;
            reviveText = null;
        }

        if (reviveMaterial != null)
        {
            Destroy(reviveMaterial);
            reviveMaterial = null;
        }
    }

    private static float GetReviveDelay(int deaths)
    {
        return deaths <= 1 ? 10f : deaths == 2 ? 30f : 60f;
    }

    private void OnHealthChanged(float previousValue, float newValue)
    {
        ApplyStateToPlayer();
    }

    private void OnDeathStateChanged(bool previousValue, bool newValue)
    {
        ApplyStateToPlayer();
    }

    private void ApplyStateToPlayer()
    {
        playerHealth.ApplyNetworkState(
            currentHealth.Value,
            isDead.Value
        );
    }

    private static bool IsValidAmount(float amount)
    {
        return amount > 0f &&
            !float.IsNaN(amount) &&
            !float.IsInfinity(amount);
    }
}
