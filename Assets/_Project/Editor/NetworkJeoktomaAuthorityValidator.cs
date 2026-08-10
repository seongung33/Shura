using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class NetworkJeoktomaAuthorityValidator
{
    private const int EnemyLayer = 8;
    private const float InitialHealth = 100f;
    private const float TestDamage = 10f;

    [MenuItem("Shura/Validate/Network Jeoktoma Authority")]
    public static void Run()
    {
        ValidateZoneDamage(false, InitialHealth - TestDamage);
        ValidateZoneDamage(true, InitialHealth);
        ValidateNetworkMovementMultiplier();

        Debug.Log(
            "[NetworkJeoktomaAuthorityValidator] PASS: " +
            "server zone damage, visual-only safety, movement multiplier"
        );
    }

    public static void RunBatch()
    {
        Run();
    }

    private static void ValidateZoneDamage(
        bool visualOnly,
        float expectedHealth
    )
    {
        GameObject targetObject = null;
        GameObject zoneObject = null;

        try
        {
            targetObject = new GameObject("ValidationEnemy");
            targetObject.layer = EnemyLayer;
            targetObject.AddComponent<BoxCollider2D>();
            EnemyHealth targetHealth =
                targetObject.AddComponent<EnemyHealth>();
            InvokeNonPublic(targetHealth, "Awake");

            zoneObject = new GameObject("ValidationZone");
            ElementalZone zone = zoneObject.AddComponent<ElementalZone>();
            zone.Initialize(
                ElementType.None,
                TestDamage,
                1 << EnemyLayer,
                visualOnly
            );

            Physics2D.SyncTransforms();
            InvokeNonPublic(zone, "Update");

            if (!Mathf.Approximately(targetHealth.CurrentHealth, expectedHealth))
            {
                throw new InvalidOperationException(
                    $"visualOnly={visualOnly}: expected HP " +
                    $"{expectedHealth}, actual {targetHealth.CurrentHealth}"
                );
            }
        }
        finally
        {
            if (zoneObject != null)
            {
                UnityEngine.Object.DestroyImmediate(zoneObject);
            }

            if (targetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }
    }

    private static void ValidateNetworkMovementMultiplier()
    {
        GameObject ownerObject = null;
        GameObject dashObject = null;

        try
        {
            ownerObject = new GameObject("ValidationNetworkPlayer");
            ownerObject.AddComponent<Rigidbody2D>();
            NetworkPlayerMovement movement =
                ownerObject.AddComponent<NetworkPlayerMovement>();

            dashObject = new GameObject("ValidationJeoktoma");
            JeoktomaDash dash = dashObject.AddComponent<JeoktomaDash>();
            dash.Cast(new SkillCastContext
            {
                Owner = ownerObject,
                Origin = Vector2.zero,
                Direction = Vector2.right,
                Damage = TestDamage,
                Element = ElementType.None,
                EnemyLayer = 1 << EnemyLayer,
                VisualOnly = true,
                SourcePlayerId = ulong.MaxValue
            });

            if (movement.SpeedMultiplier <= 1f)
            {
                throw new InvalidOperationException(
                    "Jeoktoma did not increase NetworkPlayerMovement speed."
                );
            }

            InvokeNonPublic(dash, "EndDash");

            if (!Mathf.Approximately(movement.SpeedMultiplier, 1f))
            {
                throw new InvalidOperationException(
                    "Jeoktoma did not restore NetworkPlayerMovement speed."
                );
            }
        }
        finally
        {
            if (dashObject != null)
            {
                UnityEngine.Object.DestroyImmediate(dashObject);
            }

            if (ownerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(ownerObject);
            }
        }
    }

    private static void InvokeNonPublic(
        object target,
        string methodName
    )
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (method == null)
        {
            throw new MissingMethodException(
                target.GetType().Name,
                methodName
            );
        }

        method.Invoke(target, null);
    }
}
