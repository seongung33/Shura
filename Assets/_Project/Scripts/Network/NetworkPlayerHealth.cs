using Shura.Player;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerHealth))]
public class NetworkPlayerHealth : NetworkBehaviour
{
    private readonly NetworkVariable<float> currentHealth =
        new NetworkVariable<float>();

    private readonly NetworkVariable<bool> isDead =
        new NetworkVariable<bool>();

    private PlayerHealth playerHealth;

    public bool IsDead => isDead.Value;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    public override void OnNetworkSpawn()
    {
        currentHealth.OnValueChanged += OnHealthChanged;
        isDead.OnValueChanged += OnDeathStateChanged;

        if (IsServer)
        {
            currentHealth.Value = playerHealth.MaxHealth;
            isDead.Value = false;
        }

        ApplyStateToPlayer();
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
        isDead.OnValueChanged -= OnDeathStateChanged;
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
        ApplyStateToPlayer();
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
