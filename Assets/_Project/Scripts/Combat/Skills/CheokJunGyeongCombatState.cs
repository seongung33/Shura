using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CheokJunGyeongCombatState : MonoBehaviour
{
    private struct Buff
    {
        public float SpeedMultiplier;
        public float BasicAttackIntervalMultiplier;
        public float BasicAttackRangeMultiplier;
        public bool Afterimage;
    }

    private readonly Dictionary<int, Buff> buffs = new();

    public float BasicAttackIntervalMultiplier { get; private set; } = 1f;
    public float BasicAttackRangeMultiplier { get; private set; } = 1f;
    public bool HasAfterimage { get; private set; }

    public static CheokJunGyeongCombatState GetOrAdd(GameObject owner)
    {
        CheokJunGyeongCombatState state =
            owner.GetComponent<CheokJunGyeongCombatState>();
        return state != null
            ? state
            : owner.AddComponent<CheokJunGyeongCombatState>();
    }

    public void SetBuff(
        int key,
        float speedMultiplier,
        float basicAttackIntervalMultiplier = 1f,
        float basicAttackRangeMultiplier = 1f,
        bool afterimage = false
    )
    {
        buffs[key] = new Buff
        {
            SpeedMultiplier = Mathf.Max(0.1f, speedMultiplier),
            BasicAttackIntervalMultiplier = Mathf.Clamp(
                basicAttackIntervalMultiplier,
                0.1f,
                1f
            ),
            BasicAttackRangeMultiplier = Mathf.Max(
                1f,
                basicAttackRangeMultiplier
            ),
            Afterimage = afterimage
        };
        Recalculate();
    }

    public void RemoveBuff(int key)
    {
        if (buffs.Remove(key))
        {
            Recalculate();
        }
    }

    private void Recalculate()
    {
        float speed = 1f;
        float interval = 1f;
        float range = 1f;
        bool afterimage = false;

        foreach (Buff buff in buffs.Values)
        {
            speed *= buff.SpeedMultiplier;
            interval *= buff.BasicAttackIntervalMultiplier;
            range *= buff.BasicAttackRangeMultiplier;
            afterimage |= buff.Afterimage;
        }

        BasicAttackIntervalMultiplier = interval;
        BasicAttackRangeMultiplier = range;
        HasAfterimage = afterimage;

        Shura.Player.PlayerController localMovement =
            GetComponent<Shura.Player.PlayerController>();
        NetworkPlayerMovement networkMovement =
            GetComponent<NetworkPlayerMovement>();

        if (localMovement != null)
        {
            localMovement.SpeedMultiplier = speed;
        }

        if (networkMovement != null)
        {
            networkMovement.SpeedMultiplier = speed;
        }
    }

    private void OnDestroy()
    {
        Shura.Player.PlayerController localMovement =
            GetComponent<Shura.Player.PlayerController>();
        NetworkPlayerMovement networkMovement =
            GetComponent<NetworkPlayerMovement>();

        if (localMovement != null)
        {
            localMovement.SpeedMultiplier = 1f;
        }

        if (networkMovement != null)
        {
            networkMovement.SpeedMultiplier = 1f;
        }
    }
}
