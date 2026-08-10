using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattlefieldBackdropPresenter : MonoBehaviour
{
    private const string SceneName = "Main";
    private const string ResourcePath = "Backgrounds/jangsan_forest_floor";
    private const int GridRadius = 2;

    // Keep the authored forest floor, but push it slightly darker and cooler so
    // the full-colour enemy sprites remain the visual focus during combat.
    private static readonly Color BackgroundTint =
        new Color(0.64f, 0.69f, 0.76f, 1f);

    private readonly SpriteRenderer[] tiles =
        new SpriteRenderer[(GridRadius * 2 + 1) * (GridRadius * 2 + 1)];

    private Camera targetCamera;
    private Vector2 tileSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName ||
            FindFirstObjectByType<BattlefieldBackdropPresenter>() != null)
        {
            return;
        }

        new GameObject("BattlefieldBackdropRuntime")
            .AddComponent<BattlefieldBackdropPresenter>();
    }

    private void Awake()
    {
        Sprite sprite = Resources.Load<Sprite>(ResourcePath);
        if (sprite == null)
        {
            Debug.LogWarning($"전투 배경을 찾을 수 없습니다: {ResourcePath}");
            enabled = false;
            return;
        }

        targetCamera = Camera.main;
        tileSize = sprite.bounds.size;
        int index = 0;

        for (int y = -GridRadius; y <= GridRadius; y++)
        {
            for (int x = -GridRadius; x <= GridRadius; x++)
            {
                GameObject tile = new GameObject($"ForestFloor_{x}_{y}");
                tile.transform.SetParent(transform, false);
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = BackgroundTint;
                renderer.sortingOrder = -1000;
                tiles[index++] = renderer;
            }
        }

        RepositionTiles();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
        RepositionTiles();
    }

    private void RepositionTiles()
    {
        if (targetCamera == null || tileSize.x <= 0f || tileSize.y <= 0f)
        {
            return;
        }

        Vector3 cameraPosition = targetCamera.transform.position;
        float centerX = Mathf.Round(cameraPosition.x / tileSize.x) * tileSize.x;
        float centerY = Mathf.Round(cameraPosition.y / tileSize.y) * tileSize.y;
        int index = 0;

        for (int y = -GridRadius; y <= GridRadius; y++)
        {
            for (int x = -GridRadius; x <= GridRadius; x++)
            {
                tiles[index++].transform.position = new Vector3(
                    centerX + x * tileSize.x,
                    centerY + y * tileSize.y,
                    2f
                );
            }
        }
    }
}
