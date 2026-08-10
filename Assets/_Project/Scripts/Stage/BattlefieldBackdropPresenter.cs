using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattlefieldBackdropPresenter : MonoBehaviour
{
    private const string SceneName = "Main";
    private const int GridRadius = 2;
    private const int TilePixels = 96;
    private const float TilePixelsPerUnit = 6f;

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
        Sprite sprite = CreateBattlefieldFloor();

        targetCamera = Camera.main;
        tileSize = sprite.bounds.size;
        int index = 0;

        for (int y = -GridRadius; y <= GridRadius; y++)
        {
            for (int x = -GridRadius; x <= GridRadius; x++)
            {
                GameObject tile = new GameObject($"BattlefieldFloor_{x}_{y}");
                tile.transform.SetParent(transform, false);
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = Color.white;
                renderer.sortingOrder = -1000;
                tiles[index++] = renderer;
            }
        }

        RepositionTiles();
    }

    private static Sprite CreateBattlefieldFloor()
    {
        Texture2D texture = new Texture2D(
            TilePixels,
            TilePixels,
            TextureFormat.RGBA32,
            false
        )
        {
            name = "BattlefieldFloor_LowContrast",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        Color32 baseStone = new Color32(43, 49, 49, 255);
        Color32 secondStone = new Color32(47, 53, 51, 255);
        Color32 seam = new Color32(34, 39, 41, 255);
        Color32 softDetail = new Color32(52, 57, 53, 255);
        Color32[] pixels = new Color32[TilePixels * TilePixels];

        for (int y = 0; y < TilePixels; y++)
        {
            for (int x = 0; x < TilePixels; x++)
            {
                int stoneX = x / 24;
                int stoneY = y / 24;
                bool isSeam = x % 24 == 0 || y % 24 == 0;
                int hash = x * 73856093 ^ y * 19349663;
                Color32 color = (stoneX + stoneY) % 2 == 0
                    ? baseStone
                    : secondStone;

                if (isSeam)
                {
                    color = seam;
                }
                else if ((hash & 255) < 5)
                {
                    color = softDetail;
                }

                pixels[y * TilePixels + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, TilePixels, TilePixels),
            new Vector2(0.5f, 0.5f),
            TilePixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );
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
