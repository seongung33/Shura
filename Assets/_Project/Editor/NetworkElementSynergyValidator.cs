using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class NetworkElementSynergyValidator
{
    private const int EnemyLayer = 8;
    private const float InitialHealth = 100f;
    private const float SynergyDamage = 10f;

    [MenuItem("Shura/Validate/Network Element Synergy")]
    public static void Run()
    {
        ValidateStatusRules();
        ValidateShockDamage();
        ValidateShatterDamage();

        Debug.Log(
            "[NetworkElementSynergyValidator] PASS: " +
            "different-player window, cooldown, shock chain, shatter area"
        );
    }

    public static void RunBatch()
    {
        Run();
    }

    private static void ValidateStatusRules()
    {
        ElementalStatusController status = new ElementalStatusController();

        AssertNoReaction(status, ElementType.Water, 1, 0f);
        AssertReaction(status, ElementType.Lightning, 2, 1f, SynergyReaction.Shock);
        AssertNoReaction(status, ElementType.Water, 1, 1.1f);
        AssertNoReaction(status, ElementType.Lightning, 2, 1.2f);

        ElementalStatusController samePlayer = new ElementalStatusController();
        AssertNoReaction(samePlayer, ElementType.Ice, 3, 0f);
        AssertNoReaction(samePlayer, ElementType.Earth, 3, 1f);

        ElementalStatusController expired = new ElementalStatusController();
        AssertNoReaction(expired, ElementType.Ice, 1, 0f);
        AssertNoReaction(expired, ElementType.Earth, 2, 5f);

        ElementalStatusController shatter = new ElementalStatusController();
        AssertNoReaction(shatter, ElementType.Earth, 1, 0f);
        AssertReaction(shatter, ElementType.Ice, 2, 1f, SynergyReaction.Shatter);
    }

    private static void ValidateShockDamage()
    {
        RunDamageValidation(
            ElementType.Water,
            ElementType.Lightning,
            InitialHealth,
            InitialHealth - SynergyDamage
        );
    }

    private static void ValidateShatterDamage()
    {
        RunDamageValidation(
            ElementType.Ice,
            ElementType.Earth,
            InitialHealth - SynergyDamage,
            InitialHealth - SynergyDamage
        );
    }

    private static void RunDamageValidation(
        ElementType first,
        ElementType second,
        float expectedPrimaryHealth,
        float expectedSecondaryHealth
    )
    {
        GameObject primaryObject = null;
        GameObject secondaryObject = null;

        try
        {
            EnemyHealth primary = CreateEnemy("PrimaryEnemy", Vector2.zero, out primaryObject);
            EnemyHealth secondary = CreateEnemy("SecondaryEnemy", Vector2.right, out secondaryObject);
            Physics2D.SyncTransforms();

            primary.RecordElement(first, 1);
            primary.RecordElement(second, 2);

            if (!Mathf.Approximately(primary.CurrentHealth, expectedPrimaryHealth) ||
                !Mathf.Approximately(secondary.CurrentHealth, expectedSecondaryHealth))
            {
                throw new InvalidOperationException(
                    $"{first}+{second}: expected HP " +
                    $"{expectedPrimaryHealth}/{expectedSecondaryHealth}, actual " +
                    $"{primary.CurrentHealth}/{secondary.CurrentHealth}"
                );
            }
        }
        finally
        {
            if (secondaryObject != null)
            {
                UnityEngine.Object.DestroyImmediate(secondaryObject);
            }

            if (primaryObject != null)
            {
                UnityEngine.Object.DestroyImmediate(primaryObject);
            }
        }
    }

    private static EnemyHealth CreateEnemy(
        string name,
        Vector2 position,
        out GameObject enemyObject
    )
    {
        enemyObject = new GameObject(name);
        enemyObject.layer = EnemyLayer;
        enemyObject.transform.position = position;
        enemyObject.AddComponent<BoxCollider2D>();
        EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
        InvokeNonPublic(health, "Awake");
        return health;
    }

    private static void AssertNoReaction(
        ElementalStatusController status,
        ElementType element,
        ulong playerId,
        float time
    )
    {
        if (status.TryApply(element, playerId, time, 4f, 2f, out _))
        {
            throw new InvalidOperationException($"Unexpected reaction for {element}.");
        }
    }

    private static void AssertReaction(
        ElementalStatusController status,
        ElementType element,
        ulong playerId,
        float time,
        SynergyReaction expected
    )
    {
        if (!status.TryApply(element, playerId, time, 4f, 2f, out SynergyReaction actual) ||
            actual != expected)
        {
            throw new InvalidOperationException(
                $"Expected {expected} for {element}, actual {actual}."
            );
        }
    }

    private static void InvokeNonPublic(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (method == null)
        {
            throw new MissingMethodException(target.GetType().Name, methodName);
        }

        method.Invoke(target, null);
    }
}
