using System.Collections;
using System.Collections.Generic;
using Shura.Camera;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class JangsanbeomBossPattern : NetworkBehaviour
{
    private enum PatternKind
    {
        Howl,
        Pounce
    }

    [Header("Pattern Timing")]
    [SerializeField, Min(1f)] private float openingDelay = 2.5f;
    [SerializeField, Min(1f)] private float patternCooldown = 6.5f;
    [SerializeField, Range(0.1f, 0.9f)] private float phaseTwoThreshold = 0.5f;

    [Header("Howl")]
    [SerializeField, Min(0.1f)] private float howlTelegraph = 0.9f;
    [SerializeField, Min(0.5f)] private float howlRadius = 4.2f;
    [SerializeField, Min(0f)] private float howlDamage = 24f;

    [Header("Pounce")]
    [SerializeField, Min(0.1f)] private float pounceTelegraph = 0.65f;
    [SerializeField, Min(0.1f)] private float pounceDuration = 0.38f;
    [SerializeField, Min(0.5f)] private float pounceDistance = 6f;
    [SerializeField, Min(0.1f)] private float pounceHitRadius = 1.15f;
    [SerializeField, Min(0f)] private float pounceDamage = 32f;

    private EnemyHealth health;
    private EnemyController controller;
    private EnemyAttack contactAttack;
    private Rigidbody2D rigidBody;
    private SpriteRenderer[] renderers;
    private bool patternStarted;
    private PatternKind nextPattern;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        controller = GetComponent<EnemyController>();
        contactAttack = GetComponent<EnemyAttack>();
        rigidBody = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
        if (!IsSpawned)
        {
            BeginPatterns();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            BeginPatterns();
        }
    }

    private void BeginPatterns()
    {
        if (patternStarted)
        {
            return;
        }

        patternStarted = true;
        StartCoroutine(PatternLoop());
    }

    public void RestartAfterReposition()
    {
        StopAllCoroutines();
        patternStarted = false;
        FreezeMovement(0.15f);
        BeginPatterns();
    }

    private IEnumerator PatternLoop()
    {
        yield return WaitForGameplaySeconds(openingDelay);

        while (health != null && !health.IsDead)
        {
            bool phaseTwo = IsPhaseTwo();
            if (nextPattern == PatternKind.Howl)
            {
                yield return PerformHowl(phaseTwo);
                nextPattern = PatternKind.Pounce;
            }
            else
            {
                yield return PerformPounce(phaseTwo);
                nextPattern = PatternKind.Howl;
            }

            float cooldown = phaseTwo ? patternCooldown * 0.68f : patternCooldown;
            yield return WaitForGameplaySeconds(cooldown);
        }
    }

    private IEnumerator PerformHowl(bool phaseTwo)
    {
        float radius = phaseTwo ? howlRadius * 1.25f : howlRadius;
        float damage = phaseTwo ? howlDamage * 1.25f : howlDamage;
        FreezeMovement(howlTelegraph + 0.2f);
        PlayTelegraph(PatternKind.Howl, radius, howlTelegraph, phaseTwo);
        yield return WaitForGameplaySeconds(howlTelegraph);

        DamagePlayersInRadius(rigidBody.position, radius, damage, null);
        PlayImpact(radius, phaseTwo);
    }

    private IEnumerator PerformPounce(bool phaseTwo)
    {
        Transform target = FindNearestPlayer();
        if (target == null)
        {
            yield break;
        }

        float distance = phaseTwo ? pounceDistance * 1.2f : pounceDistance;
        float damage = phaseTwo ? pounceDamage * 1.2f : pounceDamage;
        Vector2 direction = ((Vector2)target.position - rigidBody.position).normalized;
        Vector2 destination = rigidBody.position + direction * distance;

        FreezeMovement(pounceTelegraph + pounceDuration + 0.15f);
        PlayTelegraph(PatternKind.Pounce, distance, pounceTelegraph, phaseTwo);
        yield return WaitForGameplaySeconds(pounceTelegraph);

        HashSet<GameObject> damagedPlayers = new HashSet<GameObject>();
        Vector2 start = rigidBody.position;
        float elapsed = 0f;

        while (elapsed < pounceDuration && health != null && !health.IsDead)
        {
            if (!GameplayPauseState.IsLevelUpActive)
            {
                elapsed += Time.fixedDeltaTime;
                float progress = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / pounceDuration)
                );
                rigidBody.MovePosition(Vector2.Lerp(start, destination, progress));
                DamagePlayersInRadius(
                    rigidBody.position,
                    pounceHitRadius,
                    damage,
                    damagedPlayers
                );
            }
            yield return new WaitForFixedUpdate();
        }

        PlayImpact(pounceHitRadius * 1.6f, phaseTwo);
    }

    private void FreezeMovement(float duration)
    {
        controller?.FreezeFor(duration);
        contactAttack?.FreezeFor(duration);
        rigidBody.linearVelocity = Vector2.zero;
    }

    private bool IsPhaseTwo()
    {
        return health != null &&
            health.MaxHealth > 0f &&
            health.CurrentHealth / health.MaxHealth <= phaseTwoThreshold;
    }

    private Transform FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject player in players)
        {
            if (player == null)
            {
                continue;
            }

            float distance = ((Vector2)player.transform.position - rigidBody.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player.transform;
            }
        }

        return nearest;
    }

    private static void DamagePlayersInRadius(
        Vector2 center,
        float radius,
        float damage,
        ISet<GameObject> alreadyDamaged
    )
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);

        foreach (Collider2D hit in hits)
        {
            PlayerHealthTarget target = ResolvePlayer(hit);
            if (target.Root == null ||
                (alreadyDamaged != null && !alreadyDamaged.Add(target.Root)))
            {
                continue;
            }

            target.Damageable.TakeDamage(damage);
        }
    }

    private readonly struct PlayerHealthTarget
    {
        public readonly GameObject Root;
        public readonly IDamageable Damageable;

        public PlayerHealthTarget(GameObject root, IDamageable damageable)
        {
            Root = root;
            Damageable = damageable;
        }
    }

    private static PlayerHealthTarget ResolvePlayer(Collider2D hit)
    {
        Transform current = hit != null ? hit.transform : null;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                IDamageable damageable = current.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    return new PlayerHealthTarget(current.gameObject, damageable);
                }
            }
            current = current.parent;
        }

        return default;
    }

    private void PlayTelegraph(
        PatternKind kind,
        float size,
        float duration,
        bool phaseTwo
    )
    {
        if (IsSpawned)
        {
            PlayTelegraphRpc((int)kind, size, duration, phaseTwo);
        }
        else
        {
            ShowTelegraph(kind, size, duration, phaseTwo);
        }
    }

    private void PlayImpact(float radius, bool phaseTwo)
    {
        if (IsSpawned)
        {
            PlayImpactRpc(radius, phaseTwo);
        }
        else
        {
            ShowImpact(radius, phaseTwo);
        }
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PlayTelegraphRpc(int kind, float size, float duration, bool phaseTwo)
    {
        ShowTelegraph((PatternKind)kind, size, duration, phaseTwo);
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void PlayImpactRpc(float radius, bool phaseTwo)
    {
        ShowImpact(radius, phaseTwo);
    }

    private void ShowTelegraph(PatternKind kind, float size, float duration, bool phaseTwo)
    {
        Color color = kind == PatternKind.Howl
            ? new Color(0.72f, 0.18f, 0.32f, 0.9f)
            : new Color(0.28f, 0.62f, 0.92f, 0.9f);
        if (phaseTwo)
        {
            color = Color.Lerp(color, new Color(0.95f, 0.18f, 0.18f, 1f), 0.45f);
        }

        StartCoroutine(PulseRenderers(color, duration));
        CreateRing("BossTelegraph", size, color, duration);
    }

    private void ShowImpact(float radius, bool phaseTwo)
    {
        Color color = phaseTwo
            ? new Color(1f, 0.16f, 0.12f, 0.95f)
            : new Color(0.9f, 0.38f, 0.18f, 0.9f);
        CreateRing("BossImpact", radius, color, 0.28f);
        CameraFollow cameraFollow = Camera.main?.GetComponent<CameraFollow>();
        cameraFollow?.Shake(phaseTwo ? 0.28f : 0.2f, 0.22f);
    }

    private IEnumerator PulseRenderers(Color warningColor, float duration)
    {
        Color[] originalColors = new Color[renderers.Length];
        for (int index = 0; index < renderers.Length; index++)
        {
            originalColors[index] = renderers[index].color;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 0.3f + Mathf.PingPong(elapsed * 5f, 0.55f);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].color = Color.Lerp(
                        originalColors[index],
                        warningColor,
                        pulse
                    );
                }
            }
            yield return null;
        }

        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null)
            {
                renderers[index].color = originalColors[index];
            }
        }
    }

    private void CreateRing(string objectName, float radius, Color color, float lifetime)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.position = transform.position;
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = 48;
        line.useWorldSpace = false;
        line.startWidth = line.endWidth = 0.09f;
        line.startColor = line.endColor = color;
        line.sortingOrder = 30;
        line.material = new Material(Shader.Find("Sprites/Default"));

        for (int index = 0; index < line.positionCount; index++)
        {
            float angle = index / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(index, new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            ));
        }

        Destroy(ringObject, lifetime);
    }

    private static IEnumerator WaitForGameplaySeconds(float duration)
    {
        float remaining = duration;
        while (remaining > 0f)
        {
            if (!GameplayPauseState.IsLevelUpActive)
            {
                remaining -= Time.deltaTime;
            }
            yield return null;
        }
    }
}
