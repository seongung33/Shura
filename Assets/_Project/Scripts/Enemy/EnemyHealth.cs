using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyHealth : NetworkBehaviour, IDamageable, IElementReceiver
{
    [SerializeField]
    private float maxHealth = 100f;

    [SerializeField]
    private bool isBoss;

    [Header("Experience Drop")]

    [SerializeField]
    private ExperienceOrb experienceOrbPrefab;

    [SerializeField]
    private int experienceReward = 1;

    [Header("Hit Effect")]

    [SerializeField]
    private GameObject hitEffectPrefab;

    [SerializeField]
    private Transform hitEffectPoint;

    [SerializeField]
    [Min(0.05f)]
    private float hitEffectLifetime = 0.5f;

    [Header("Element Synergy")]

    [Min(0.1f)]
    [SerializeField]
    private float synergyWindow = 4f;

    [Min(0f)]
    [SerializeField]
    private float synergyInternalCooldown = 2f;

    [Min(0f)]
    [SerializeField]
    private float synergyDamage = 10f;

    [Min(0.1f)]
    [SerializeField]
    private float synergyRadius = 2.5f;

    [Min(0.1f)]
    [SerializeField]
    private float synergyFeedbackLifetime = 1f;

    private readonly NetworkVariable<float> networkHealth =
        new NetworkVariable<float>();

    private float localHealth;
    private bool isDead;
    private ElementalStatusController elementalStatus;
    private float runtimeExperienceReward;
    private int experienceBundleSize = 1;
    private ExperienceDropAccumulator dropAccumulator;

    public ExperienceOrb ExperienceOrbPrefab => experienceOrbPrefab;

    public float CurrentHealth
    {
        get
        {
            return IsSpawned ? networkHealth.Value : localHealth;
        }
    }

    public float MaxHealth => maxHealth;
    public bool IsBoss => isBoss;

    private void Awake()
    {
        localHealth = maxHealth;
        runtimeExperienceReward = experienceReward;
        isDead = false;
        elementalStatus = new ElementalStatusController();
    }

    public void ConfigureRuntime(
        float healthMultiplier,
        float configuredExperienceReward,
        ExperienceDropAccumulator configuredAccumulator,
        int configuredBundleSize
    )
    {
        maxHealth *= Mathf.Max(0.01f, healthMultiplier);
        localHealth = maxHealth;
        runtimeExperienceReward = Mathf.Max(0f, configuredExperienceReward);
        dropAccumulator = configuredAccumulator;
        experienceBundleSize = Mathf.Max(1, configuredBundleSize);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsValidDamage(damage))
        {
            return;
        }

        if (!IsSpawned)
        {
            ApplyDamage(damage);
            return;
        }

        if (IsServer)
        {
            ApplyDamage(damage);
            return;
        }

        RequestDamageRpc(damage);
    }

    public void RecordElement(ElementType element, ulong sourcePlayerId)
    {
        if ((IsSpawned && !IsServer) || isDead)
        {
            return;
        }

        elementalStatus ??= new ElementalStatusController();

        if (!elementalStatus.TryApply(
                element,
                sourcePlayerId,
                Time.time,
                synergyWindow,
                synergyInternalCooldown,
                out SynergyReaction reaction
            ))
        {
            return;
        }

        PlaySynergyFeedbackSynced(reaction);
        ApplySynergyDamage(reaction);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDamageRpc(float damage)
    {
        if (!IsValidDamage(damage))
        {
            return;
        }

        ApplyDamage(damage);
    }

    private void ApplyDamage(float damage)
    {
        if (isDead)
        {
            return;
        }

        float nextHealth = Mathf.Max(0f, CurrentHealth - damage);

        if (IsSpawned)
        {
            networkHealth.Value = nextHealth;
        }
        else
        {
            localHealth = nextHealth;
        }

        if (isBoss)
        {
            Debug.Log($"보스 체력: {nextHealth} / {maxHealth}");
        }

        PlayHitEffectSynced(damage, nextHealth <= 0f);

        if (nextHealth <= 0f)
        {
            Die();
        }
    }

    private void PlayHitEffectSynced(float damage, bool defeated)
    {
        if (!IsSpawned)
        {
            PlayHitEffect(damage, defeated);
            return;
        }

        if (IsServer)
        {
            PlayHitEffectRpc(damage, defeated);
        }
    }

    [Rpc(
        SendTo.ClientsAndHost,
        InvokePermission = RpcInvokePermission.Server
    )]
    private void PlayHitEffectRpc(float damage, bool defeated)
    {
        PlayHitEffect(damage, defeated);
    }

    private void PlayHitEffect(float damage, bool defeated)
    {
        CombatFeedbackPresenter.PlayHit(this, damage, defeated);

        if (hitEffectPrefab == null)
        {
            return;
        }
        Vector3 spawnPosition = hitEffectPoint != null
            ? hitEffectPoint.position
            : transform.position;

        GameObject effect = Instantiate(
            hitEffectPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Destroy(effect, hitEffectLifetime);
    }

    private void ApplySynergyDamage(SynergyReaction reaction)
    {
        if (reaction == SynergyReaction.Shock)
        {
            EnemyHealth nearest = FindNearestOtherEnemy();

            if (nearest != null)
            {
                nearest.TakeDamage(synergyDamage);
            }

            return;
        }

        if (reaction != SynergyReaction.Shatter)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            synergyRadius,
            1 << gameObject.layer
        );
        HashSet<int> damagedIds = new HashSet<int>();

        foreach (Collider2D hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null || !damagedIds.Add(enemy.GetInstanceID()))
            {
                continue;
            }

            enemy.TakeDamage(synergyDamage);
        }
    }

    private EnemyHealth FindNearestOtherEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            synergyRadius,
            1 << gameObject.layer
        );
        EnemyHealth nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            EnemyHealth candidate = hit.GetComponentInParent<EnemyHealth>();

            if (candidate == null || candidate == this || candidate.isDead)
            {
                continue;
            }

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private void PlaySynergyFeedbackSynced(SynergyReaction reaction)
    {
        if (IsSpawned)
        {
            ShowSynergyFeedbackRpc(reaction);
            return;
        }

        ShowSynergyFeedback(reaction);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ShowSynergyFeedbackRpc(SynergyReaction reaction)
    {
        ShowSynergyFeedback(reaction);
    }

    private void ShowSynergyFeedback(SynergyReaction reaction)
    {
        GameObject feedbackObject = new GameObject($"Synergy_{reaction}");
        feedbackObject.transform.position = transform.position + Vector3.up * 0.8f;

        TextMesh textMesh = feedbackObject.AddComponent<TextMesh>();
        textMesh.text = reaction == SynergyReaction.Shock ? "감전!" : "분쇄!";
        textMesh.color = reaction == SynergyReaction.Shock
            ? ElementUtil.GetColor(ElementType.Lightning)
            : ElementUtil.GetColor(ElementType.Ice);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.15f;
        textMesh.fontSize = 32;
        MeshRenderer meshRenderer = feedbackObject.GetComponent<MeshRenderer>();
        meshRenderer.sortingOrder = 20;

        if (Application.isPlaying)
        {
            Destroy(feedbackObject, synergyFeedbackLifetime);
        }
        else
        {
            DestroyImmediate(feedbackObject);
        }
    }


    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (IsSpawned)
        {
            if (IsServer)
            {
                DropExperience();
                NetworkObject.Despawn(true);
            }

            return;
        }

        DropExperience();

        Destroy(gameObject);
    }

    private void DropExperience()
    {
        if (dropAccumulator != null)
        {
            dropAccumulator.Add(
                transform.position,
                runtimeExperienceReward,
                experienceBundleSize
            );
            return;
        }

        if (experienceOrbPrefab == null)
        {
            return;
        }

        ExperienceOrb experienceOrb = Instantiate(
            experienceOrbPrefab,
            transform.position,
            Quaternion.identity
        );
        experienceOrb.Initialize(Mathf.Max(1, Mathf.RoundToInt(runtimeExperienceReward)));

        if (IsSpawned)
        {
            NetworkObject orbNetworkObject =
                experienceOrb.GetComponent<NetworkObject>();

            if (orbNetworkObject != null)
            {
                orbNetworkObject.Spawn();
            }
            else
            {
                Destroy(experienceOrb.gameObject);
            }
        }
    }

    private static bool IsValidDamage(float damage)
    {
        return damage > 0f &&
            !float.IsNaN(damage) &&
            !float.IsInfinity(damage);
    }

    [ContextMenu("Test Damage")]
    private void TestDamage()
    {
        TakeDamage(25f);
    }
}
