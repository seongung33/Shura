using Unity.Netcode;
using UnityEngine;

public sealed class EliteEnemyVisual : NetworkBehaviour
{
    private readonly NetworkVariable<bool> eliteState = new NetworkVariable<bool>();

    [SerializeField] private Color auraColor = new Color(0.2f, 0.9f, 0.92f, 0.88f);
    [SerializeField, Min(0.5f)] private float auraRadius = 0.95f;
    [SerializeField, Min(0f)] private float scalePulse = 0.045f;

    private bool localElite;
    private Transform auraRoot;
    private SpriteRenderer[] renderers;
    private Color[] originalColors;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        for (int index = 0; index < renderers.Length; index++)
        {
            originalColors[index] = renderers[index].color;
        }
    }

    public void Configure(bool elite)
    {
        localElite = elite;

        if (IsSpawned && IsServer)
        {
            eliteState.Value = elite;
        }

        if (!IsSpawned)
        {
            ApplyState(elite);
        }
    }

    public override void OnNetworkSpawn()
    {
        eliteState.OnValueChanged += HandleEliteChanged;

        if (IsServer)
        {
            eliteState.Value = localElite;
        }

        ApplyState(eliteState.Value);
    }

    public override void OnNetworkDespawn()
    {
        eliteState.OnValueChanged -= HandleEliteChanged;
    }

    private void HandleEliteChanged(bool previousValue, bool newValue)
    {
        ApplyState(newValue);
    }

    private void Update()
    {
        bool elite = IsSpawned ? eliteState.Value : localElite;
        if (!elite)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 3.4f) * scalePulse;
        if (auraRoot != null)
        {
            auraRoot.localScale = Vector3.one * pulse;
            auraRoot.localEulerAngles = new Vector3(0f, 0f, Time.time * 24f);
        }
    }

    private void ApplyState(bool elite)
    {
        localElite = elite;
        for (int index = 0; index < renderers.Length; index++)
        {
            if (renderers[index] != null)
            {
                renderers[index].color = elite
                    ? Color.Lerp(originalColors[index], auraColor, 0.22f)
                    : originalColors[index];
            }
        }

        if (elite && auraRoot == null)
        {
            auraRoot = CreateAura();
        }

        if (auraRoot != null)
        {
            auraRoot.localScale = Vector3.one;
            auraRoot.gameObject.SetActive(elite);
        }
    }

    private Transform CreateAura()
    {
        GameObject aura = new GameObject("EliteDokkaebiAura");
        aura.transform.SetParent(transform, false);
        aura.transform.localPosition = new Vector3(0f, -0.12f, 0f);

        LineRenderer line = aura.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 36;
        line.startWidth = line.endWidth = 0.075f;
        line.startColor = line.endColor = auraColor;
        line.sortingOrder = 8;
        line.material = new Material(Shader.Find("Sprites/Default"));

        for (int index = 0; index < line.positionCount; index++)
        {
            float angle = index / (float)line.positionCount * Mathf.PI * 2f;
            float wave = index % 2 == 0 ? 1f : 0.84f;
            line.SetPosition(index, new Vector3(
                Mathf.Cos(angle) * auraRadius * wave,
                Mathf.Sin(angle) * auraRadius * 0.42f * wave,
                0f
            ));
        }

        return aura.transform;
    }
}
