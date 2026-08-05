using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class NetworkProjectileAuthorityValidator
{
    private const int EnemyLayer = 8;
    private const float InitialHealth = 100f;
    private const float TestDamage = 10f;

    [MenuItem("Shura/Validate/Network Projectile Authority")]
    public static void Run()
    {
        ValidateDamage(false, InitialHealth - TestDamage);
        ValidateDamage(true, InitialHealth);

        Debug.Log(
            "[NetworkProjectileAuthorityValidator] PASS: " +
            "서버 투사체 피해 1회, 시각 복제본 피해 0회"
        );
    }

    public static void RunBatch()
    {
        Run();
    }

    private static void ValidateDamage(
        bool visualOnly,
        float expectedHealth
    )
    {
        GameObject targetObject = null;
        GameObject projectileObject = null;

        try
        {
            targetObject = new GameObject("ValidationEnemy");
            targetObject.layer = EnemyLayer;

            BoxCollider2D targetCollider =
                targetObject.AddComponent<BoxCollider2D>();
            EnemyHealth targetHealth =
                targetObject.AddComponent<EnemyHealth>();
            InvokeNonPublic(targetHealth, "Awake");

            projectileObject = new GameObject("ValidationProjectile");
            StraightProjectile projectile =
                projectileObject.AddComponent<StraightProjectile>();

            projectile.Cast(new SkillCastContext
            {
                Owner = projectileObject,
                Origin = Vector2.zero,
                Direction = Vector2.right,
                Damage = TestDamage,
                ProjectileSpeed = 0f,
                Element = ElementType.None,
                EnemyLayer = 1 << EnemyLayer,
                VisualOnly = visualOnly
            });

            InvokeNonPublic(projectile, "OnTriggerEnter2D", targetCollider);

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
            if (projectileObject != null)
            {
                UnityEngine.Object.DestroyImmediate(projectileObject);
            }

            if (targetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }
    }

    private static void InvokeNonPublic(
        object target,
        string methodName,
        params object[] arguments
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

        method.Invoke(target, arguments);
    }
}
