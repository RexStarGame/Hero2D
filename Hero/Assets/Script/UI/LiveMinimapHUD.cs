using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LiveMinimapHUD : MonoBehaviour
{
    [Header("Layout")]
    [Min(120f)] [SerializeField] private float mapSize = 220f;
    [Min(1f)] [SerializeField] private float worldRadius = 28f;
    [SerializeField] private Vector2 topRightMargin = new Vector2(24f, 24f);

    [Header("Markers")]
    [Min(3f)] [SerializeField] private float localPlayerSize = 15f;
    [Min(3f)] [SerializeField] private float otherPlayerSize = 12f;
    [Min(2f)] [SerializeField] private float enemySize = 8f;
    [SerializeField] private Color localPlayerColor = new Color(1f, 0.82f, 0.12f, 1f);
    [SerializeField] private Color otherPlayerColor = new Color(0.2f, 0.62f, 1f, 1f);
    [SerializeField] private Color enemyColor = new Color(0.95f, 0.18f, 0.18f, 1f);

    [Header("Appearance")]
    [SerializeField] private Color backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.88f);
    [SerializeField] private Color borderColor = new Color(0.72f, 0.58f, 0.28f, 0.95f);
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    private static LiveMinimapHUD instance;
    private static Transform requestedLocalPlayer;

    private readonly List<Image> markerPool = new List<Image>(32);
    private RectTransform mapArea;
    private GameObject panel;
    private Sprite circleSprite;
    private Texture2D circleTexture;
    private Transform localPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        requestedLocalPlayer = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAutomatically()
    {
        if (FindAnyObjectByType<LiveMinimapHUD>() != null)
            return;

        GameObject root = new GameObject("Live Minimap HUD");
        root.AddComponent<LiveMinimapHUD>();
    }

    public static void SetLocalPlayer(Transform player)
    {
        requestedLocalPlayer = player;
        if (instance != null)
            instance.localPlayer = player;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            panel.SetActive(!panel.activeSelf);

        if (!panel.activeSelf)
            return;

        ResolveLocalPlayer();
        RefreshMarkers();
    }

    private void ResolveLocalPlayer()
    {
        if (requestedLocalPlayer != null)
        {
            localPlayer = requestedLocalPlayer;
            return;
        }

        if (localPlayer != null && localPlayer.gameObject.activeInHierarchy)
            return;

        localPlayer = null;
        IReadOnlyList<MinimapTarget2D> targets = MinimapTarget2D.ActiveTargets;
        for (int i = 0; i < targets.Count; i++)
        {
            MinimapTarget2D target = targets[i];
            if (target != null && target.Kind == MinimapTargetKind.Player)
            {
                localPlayer = target.TrackedTransform;
                break;
            }
        }
    }

    private void RefreshMarkers()
    {
        int used = 0;
        if (localPlayer != null)
        {
            IReadOnlyList<MinimapTarget2D> targets = MinimapTarget2D.ActiveTargets;
            float pixelsPerWorldUnit = (mapSize * 0.5f - 10f) / Mathf.Max(1f, worldRadius);
            float radiusSquared = worldRadius * worldRadius;

            for (int i = 0; i < targets.Count; i++)
            {
                MinimapTarget2D target = targets[i];
                if (target == null || !target.isActiveAndEnabled)
                    continue;

                Vector2 offset = (Vector2)(target.TrackedTransform.position - localPlayer.position);
                bool isLocalPlayer = target.TrackedTransform == localPlayer;
                if (!isLocalPlayer && offset.sqrMagnitude > radiusSquared)
                    continue;

                Image marker = GetMarker(used++);
                ConfigureMarker(marker, target.Kind, isLocalPlayer);
                marker.rectTransform.anchoredPosition = isLocalPlayer
                    ? Vector2.zero
                    : offset * pixelsPerWorldUnit;
            }
        }

        for (int i = used; i < markerPool.Count; i++)
            markerPool[i].gameObject.SetActive(false);
    }

    private Image GetMarker(int index)
    {
        if (index < markerPool.Count)
        {
            Image existing = markerPool[index];
            existing.gameObject.SetActive(true);
            return existing;
        }

        GameObject markerObject = new GameObject("Minimap Marker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markerObject.transform.SetParent(mapArea, false);
        Image marker = markerObject.GetComponent<Image>();
        marker.sprite = circleSprite;
        marker.raycastTarget = false;
        markerPool.Add(marker);
        return marker;
    }

    private void ConfigureMarker(Image marker, MinimapTargetKind kind, bool isLocalPlayer)
    {
        float size;
        if (isLocalPlayer)
        {
            size = localPlayerSize;
            marker.color = localPlayerColor;
            marker.transform.SetAsLastSibling();
        }
        else if (kind == MinimapTargetKind.Player)
        {
            size = otherPlayerSize;
            marker.color = otherPlayerColor;
        }
        else
        {
            size = enemySize;
            marker.color = enemyColor;
        }

        marker.rectTransform.sizeDelta = new Vector2(size, size);
    }

    private void BuildUI()
    {
        circleSprite = CreateCircleSprite(64, out circleTexture);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        panel = CreateImage("Minimap Panel", transform, backgroundColor, circleSprite).gameObject;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.sizeDelta = new Vector2(mapSize + 12f, mapSize + 12f);
        panelRect.anchoredPosition = new Vector2(-topRightMargin.x, -topRightMargin.y);

        Image border = CreateImage("Border", panel.transform, borderColor, circleSprite);
        border.rectTransform.anchorMin = Vector2.zero;
        border.rectTransform.anchorMax = Vector2.one;
        border.rectTransform.offsetMin = new Vector2(3f, 3f);
        border.rectTransform.offsetMax = new Vector2(-3f, -3f);

        Image background = CreateImage("Map Area", border.transform, backgroundColor, circleSprite);
        mapArea = background.rectTransform;
        mapArea.anchorMin = Vector2.zero;
        mapArea.anchorMax = Vector2.one;
        mapArea.offsetMin = new Vector2(4f, 4f);
        mapArea.offsetMax = new Vector2(-4f, -4f);

        CreateGridLine("Horizontal Grid", new Vector2(mapSize - 18f, 1f));
        CreateGridLine("Vertical Grid", new Vector2(1f, mapSize - 18f));
    }

    private void CreateGridLine(string objectName, Vector2 size)
    {
        Image line = CreateImage(objectName, mapArea, gridColor, null);
        line.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        line.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        line.rectTransform.anchoredPosition = Vector2.zero;
        line.rectTransform.sizeDelta = size;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color, Sprite sprite)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite CreateCircleSprite(int size, out Texture2D texture)
    {
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Minimap Circle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        float feather = 1.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - distance) / feather + 1f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (circleSprite != null)
            Destroy(circleSprite);
        if (circleTexture != null)
            Destroy(circleTexture);
    }
}
