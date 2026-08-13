using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LiveMinimapHUD : MonoBehaviour, IPointerClickHandler
{
    [Header("Layout")]
    [Min(120f)] [SerializeField] private float mapSize = 220f;
    [Min(1f)] [SerializeField] private float worldRadius = 28f;
    [SerializeField] private Vector2 topRightMargin = new Vector2(24f, 24f);
    [Min(240f)] [SerializeField] private float expandedMapSize = 700f;
    [Min(1f)] [SerializeField] private float expandedWorldRadius = 75f;

    [Header("World View")]
    [SerializeField] private bool showWorld = true;
    [Range(128, 512)] [SerializeField] private int renderTextureSize = 256;
    [SerializeField] private LayerMask worldCullingMask = ~0;
    [SerializeField] private float minimapCameraZ = -100f;
    [SerializeField] private float minimapCameraDepth = -100f;
    [SerializeField] private Color cameraBackgroundColor = new Color(0.035f, 0.055f, 0.075f, 1f);

    [Header("Markers")]
    [Min(3f)] [SerializeField] private float localPlayerSize = 15f;
    [Min(3f)] [SerializeField] private float otherPlayerSize = 12f;
    [Min(2f)] [SerializeField] private float enemySize = 8f;
    [SerializeField] private Color localPlayerColor = new Color(1f, 0.82f, 0.12f, 1f);
    [SerializeField] private Color otherPlayerColor = new Color(0.2f, 0.62f, 1f, 1f);
    [SerializeField] private Color enemyColor = new Color(0.95f, 0.18f, 0.18f, 1f);

    [Header("Waypoint Marker In Map")]
    [Min(0.1f)] [SerializeField] private float waypointArrivalDistance = 2f;
    [Min(6f)] [SerializeField] private float waypointMarkerSize = 18f;
    [SerializeField] private Color waypointColor = new Color(0.1f, 0.9f, 1f, 1f);
    [SerializeField] private KeyCode removeWaypointKey = KeyCode.Delete;
    [SerializeField] private bool showWaypointOnMapByDefault = true;
    [SerializeField] private bool showHudDirectionArrowByDefault = true;

    [Header("HUD Direction Arrow (Outside Map)")]
    [Tooltip("Leave empty to keep using the current generated triangle.")]
    [SerializeField] private Sprite customDirectionArrowSprite;
    [SerializeField] private Color directionArrowColor = new Color(0.1f, 0.9f, 1f, 1f);
    [SerializeField] private Vector2 directionArrowSize = new Vector2(40f, 40f);
    [SerializeField] private Vector2 directionArrowScreenAnchor = new Vector2(0.5f, 0.05f);
    [Tooltip("Moves the complete waypoint guidance group on the screen. This affects both the arrow and distance text.")]
    [SerializeField] private Vector2 directionArrowScreenPosition = new Vector2(0f, 42f);
    [Tooltip("Moves only the HUD arrow inside its guidance group, without moving the distance text.")]
    [SerializeField] private Vector2 directionArrowLocalPosition = new Vector2(0f, -16f);
    [Tooltip("Adjust this if a custom sprite's pointed end uses a different default direction.")]
    [SerializeField] private float directionArrowRotationOffset = 90f;

    [Header("Waypoint Distance Text")]
    [Tooltip("Leave empty to use TextMeshPro's default font asset.")]
    [SerializeField] private TMP_FontAsset waypointDistanceFont;
    [Min(1f)] [SerializeField] private float waypointDistanceFontSize = 22f;
    [SerializeField] private Color waypointDistanceTextColor = Color.white;
    [SerializeField] private Vector2 waypointDistanceTextSize = new Vector2(220f, 38f);
    [SerializeField] private Vector2 waypointDistanceTextPosition = Vector2.zero;
    [SerializeField] private TextAlignmentOptions waypointDistanceTextAlignment = TextAlignmentOptions.Bottom;
    [Tooltip("Use {0} where the rounded distance should appear, for example: {0} m.")]
    [SerializeField] private string waypointDistanceFormat = "{0} m";

    [Header("Appearance")]
    [SerializeField] private Color backgroundColor = new Color(0.035f, 0.055f, 0.075f, 0.88f);
    [SerializeField] private Color borderColor = new Color(0.72f, 0.58f, 0.28f, 0.95f);
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.1f);
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    private static LiveMinimapHUD instance;
    private static Transform requestedLocalPlayer;

    private readonly List<Image> markerPool = new List<Image>(32);
    private RectTransform mapArea;
    private RectTransform panelRect;
    private RectTransform horizontalGrid;
    private RectTransform verticalGrid;
    private GameObject panel;
    private bool isExpanded;
    private Sprite circleSprite;
    private Texture2D circleTexture;
    private Transform localPlayer;
    private Camera minimapCamera;
    private RenderTexture worldTexture;
    private Image waypointMarker;
    private Image mapDirectionArrow;
    private RectTransform directionIndicator;
    private Image directionArrow;
    private TextMeshProUGUI distanceText;
    private Text waypointHelpText;
    private bool hasWaypoint;
    private Vector2 waypointPosition;
    private Sprite arrowSprite;
    private Texture2D arrowTexture;
    private bool showWaypointOnMap;
    private bool showHudDirectionArrow;

    private const string ShowWaypointOnMapKey = "Navigation.ShowWaypointOnMap";
    private const string ShowHudDirectionArrowKey = "Navigation.ShowHudDirectionArrow";

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
        showWaypointOnMap = PlayerPrefs.GetInt(ShowWaypointOnMapKey, showWaypointOnMapByDefault ? 1 : 0) != 0;
        showHudDirectionArrow = PlayerPrefs.GetInt(ShowHudDirectionArrowKey, showHudDirectionArrowByDefault ? 1 : 0) != 0;
        BuildUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            ToggleExpanded();

        if (Input.GetKeyDown(removeWaypointKey))
            ClearWaypoint();

        bool shouldRenderWorld = showWorld && panel.activeSelf;
        if (minimapCamera != null)
            minimapCamera.enabled = shouldRenderWorld;

        if (!panel.activeSelf)
            return;

        ResolveLocalPlayer();
        UpdateWaypoint();
        RefreshMarkers();
    }

    private void LateUpdate()
    {
        if (minimapCamera == null || localPlayer == null || !minimapCamera.enabled)
            return;

        Vector3 playerPosition = localPlayer.position;
        minimapCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, minimapCameraZ);
        minimapCamera.orthographicSize = CurrentWorldRadius;
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
            float currentMapSize = CurrentMapSize;
            float currentWorldRadius = CurrentWorldRadius;
            float pixelsPerWorldUnit = (currentMapSize * 0.5f - 10f) / Mathf.Max(1f, currentWorldRadius);
            float radiusSquared = currentWorldRadius * currentWorldRadius;

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

        RefreshWaypointMarker();
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
        panelRect = panel.GetComponent<RectTransform>();
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

        Mask mapMask = background.gameObject.AddComponent<Mask>();
        mapMask.showMaskGraphic = true;

        if (showWorld)
            BuildWorldView();

        horizontalGrid = CreateGridLine("Horizontal Grid", new Vector2(mapSize - 18f, 1f));
        verticalGrid = CreateGridLine("Vertical Grid", new Vector2(1f, mapSize - 18f));
        BuildWaypointUI();

        panel.GetComponent<Image>().raycastTarget = true;
        ApplyMapState();
    }

    private void BuildWorldView()
    {
        int textureSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(renderTextureSize, 128, 512));
        worldTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32)
        {
            name = "Live Minimap World",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        worldTexture.Create();

        GameObject cameraObject = new GameObject("Live Minimap Camera", typeof(Camera));
        cameraObject.transform.SetParent(transform, false);
        minimapCamera = cameraObject.GetComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = worldRadius;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = cameraBackgroundColor;
        minimapCamera.cullingMask = worldCullingMask;
        minimapCamera.depth = minimapCameraDepth;
        minimapCamera.nearClipPlane = 0.1f;
        minimapCamera.farClipPlane = 1000f;
        minimapCamera.targetTexture = worldTexture;
        minimapCamera.allowHDR = false;
        minimapCamera.allowMSAA = false;

        GameObject worldImageObject = new GameObject("Live World", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        worldImageObject.transform.SetParent(mapArea, false);
        RawImage worldImage = worldImageObject.GetComponent<RawImage>();
        worldImage.texture = worldTexture;
        worldImage.color = Color.white;
        worldImage.raycastTarget = false;
        worldImage.rectTransform.anchorMin = Vector2.zero;
        worldImage.rectTransform.anchorMax = Vector2.one;
        worldImage.rectTransform.offsetMin = Vector2.zero;
        worldImage.rectTransform.offsetMax = Vector2.zero;
        worldImage.transform.SetAsFirstSibling();
    }

    private RectTransform CreateGridLine(string objectName, Vector2 size)
    {
        Image line = CreateImage(objectName, mapArea, gridColor, null);
        line.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        line.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        line.rectTransform.anchoredPosition = Vector2.zero;
        line.rectTransform.sizeDelta = size;
        return line.rectTransform;
    }

    private void BuildWaypointUI()
    {
        waypointMarker = CreateImage("Waypoint", mapArea, waypointColor, null);
        waypointMarker.rectTransform.sizeDelta = new Vector2(waypointMarkerSize, waypointMarkerSize);
        waypointMarker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        waypointMarker.raycastTarget = false;
        waypointMarker.gameObject.SetActive(false);

        mapDirectionArrow = CreateImage("Waypoint Map Direction Arrow", mapArea, waypointColor, GetDirectionArrowSprite());
        mapDirectionArrow.rectTransform.sizeDelta = new Vector2(waypointMarkerSize * 1.25f, waypointMarkerSize * 1.25f);
        mapDirectionArrow.raycastTarget = false;
        mapDirectionArrow.gameObject.SetActive(false);

        GameObject indicatorObject = new GameObject("Waypoint Direction", typeof(RectTransform));
        indicatorObject.transform.SetParent(transform, false);
        directionIndicator = indicatorObject.GetComponent<RectTransform>();
        directionIndicator.anchorMin = directionArrowScreenAnchor;
        directionIndicator.anchorMax = directionArrowScreenAnchor;
        directionIndicator.pivot = directionArrowScreenAnchor;
        directionIndicator.anchoredPosition = directionArrowScreenPosition;
        directionIndicator.sizeDelta = new Vector2(220f, 70f);

        directionArrow = CreateImage("HUD Direction Arrow", directionIndicator, directionArrowColor, GetDirectionArrowSprite());
        ApplyDirectionArrowSettings();

        distanceText = CreateTMPText("Waypoint Distance", directionIndicator);
        distanceText.font = waypointDistanceFont != null ? waypointDistanceFont : TMP_Settings.defaultFontAsset;
        distanceText.fontSize = Mathf.Max(1f, waypointDistanceFontSize);
        distanceText.color = waypointDistanceTextColor;
        distanceText.alignment = waypointDistanceTextAlignment;
        distanceText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        distanceText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        distanceText.rectTransform.pivot = new Vector2(0.5f, 0f);
        distanceText.rectTransform.anchoredPosition = waypointDistanceTextPosition;
        distanceText.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1f, waypointDistanceTextSize.x),
            Mathf.Max(1f, waypointDistanceTextSize.y));

        waypointHelpText = CreateText("Waypoint Help", transform, 18, TextAnchor.MiddleCenter);
        waypointHelpText.text = "Left click: Set waypoint   Right click / Delete: Remove";
        waypointHelpText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        waypointHelpText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        waypointHelpText.rectTransform.pivot = new Vector2(0.5f, 1f);
        waypointHelpText.rectTransform.anchoredPosition = new Vector2(0f, -(expandedMapSize * 0.5f + 18f));
        waypointHelpText.rectTransform.sizeDelta = new Vector2(620f, 32f);

        directionIndicator.gameObject.SetActive(false);
        waypointHelpText.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (isExpanded)
                ClearWaypoint();
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!isExpanded)
        {
            ToggleExpanded();
            return;
        }

        if (localPlayer == null || mapArea == null)
            return;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapArea, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        float halfSize = Mathf.Min(mapArea.rect.width, mapArea.rect.height) * 0.5f;
        if (halfSize <= 0f || localPoint.sqrMagnitude > halfSize * halfSize)
            return;

        if (hasWaypoint)
        {
            Vector2 existingPoint = WorldOffsetToMapPoint(waypointPosition - (Vector2)localPlayer.position);
            if ((localPoint - existingPoint).sqrMagnitude <= waypointMarkerSize * waypointMarkerSize)
            {
                ClearWaypoint();
                return;
            }
        }

        waypointPosition = (Vector2)localPlayer.position + localPoint * (CurrentWorldRadius / halfSize);
        hasWaypoint = true;
        UpdateWaypoint();
        RefreshWaypointMarker();
    }

    private Sprite GetDirectionArrowSprite()
    {
        if (customDirectionArrowSprite != null)
            return customDirectionArrowSprite;

        if (arrowSprite == null)
            arrowSprite = CreateArrowSprite(32, out arrowTexture);

        return arrowSprite;
    }

    private void ApplyDirectionArrowSettings()
    {
        if (directionIndicator != null)
        {
            directionIndicator.anchorMin = directionArrowScreenAnchor;
            directionIndicator.anchorMax = directionArrowScreenAnchor;
            directionIndicator.pivot = directionArrowScreenAnchor;
            directionIndicator.anchoredPosition = directionArrowScreenPosition;
        }

        if (directionArrow == null)
            return;

        directionArrow.sprite = GetDirectionArrowSprite();
        directionArrow.color = directionArrowColor;
        directionArrow.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        directionArrow.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        directionArrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        directionArrow.rectTransform.anchoredPosition = directionArrowLocalPosition;
        directionArrow.rectTransform.sizeDelta = new Vector2(
            Mathf.Max(1f, directionArrowSize.x),
            Mathf.Max(1f, directionArrowSize.y));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        directionArrowSize.x = Mathf.Max(1f, directionArrowSize.x);
        directionArrowSize.y = Mathf.Max(1f, directionArrowSize.y);

        if (Application.isPlaying)
            ApplyDirectionArrowSettings();
    }
#endif

    public static bool ShowWaypointOnMap => instance != null
        ? instance.showWaypointOnMap
        : PlayerPrefs.GetInt(ShowWaypointOnMapKey, 1) != 0;

    public static bool ShowHudDirectionArrow => instance != null
        ? instance.showHudDirectionArrow
        : PlayerPrefs.GetInt(ShowHudDirectionArrowKey, 1) != 0;

    public static void SetShowWaypointOnMap(bool value)
    {
        PlayerPrefs.SetInt(ShowWaypointOnMapKey, value ? 1 : 0);
        PlayerPrefs.Save();
        if (instance == null)
            return;

        instance.showWaypointOnMap = value;
        instance.RefreshWaypointMarker();
    }

    public static void SetShowHudDirectionArrow(bool value)
    {
        PlayerPrefs.SetInt(ShowHudDirectionArrowKey, value ? 1 : 0);
        PlayerPrefs.Save();
        if (instance == null)
            return;

        instance.showHudDirectionArrow = value;
        instance.UpdateWaypoint();
    }

    private void UpdateWaypoint()
    {
        if (!hasWaypoint || localPlayer == null)
        {
            if (directionIndicator != null)
                directionIndicator.gameObject.SetActive(false);
            return;
        }

        Vector2 direction = waypointPosition - (Vector2)localPlayer.position;
        float distance = direction.magnitude;
        if (distance <= waypointArrivalDistance)
        {
            ClearWaypoint();
            return;
        }

        directionIndicator.gameObject.SetActive(showHudDirectionArrow);
        if (!showHudDirectionArrow)
            return;

        int roundedDistance = Mathf.CeilToInt(distance);
        distanceText.text = string.IsNullOrEmpty(waypointDistanceFormat)
            ? roundedDistance.ToString()
            : waypointDistanceFormat.Replace("{0}", roundedDistance.ToString());
        directionArrow.rectTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + directionArrowRotationOffset);
    }

    private void RefreshWaypointMarker()
    {
        if (waypointMarker == null || mapDirectionArrow == null)
            return;

        if (!showWaypointOnMap || !hasWaypoint || localPlayer == null)
        {
            waypointMarker.gameObject.SetActive(false);
            mapDirectionArrow.gameObject.SetActive(false);
            return;
        }

        Vector2 offset = waypointPosition - (Vector2)localPlayer.position;
        float visibleRadius = CurrentWorldRadius;
        bool waypointIsVisible = offset.sqrMagnitude <= visibleRadius * visibleRadius;
        waypointMarker.gameObject.SetActive(waypointIsVisible);
        mapDirectionArrow.gameObject.SetActive(!waypointIsVisible);

        if (waypointIsVisible)
        {
            waypointMarker.rectTransform.sizeDelta = new Vector2(waypointMarkerSize, waypointMarkerSize);
            waypointMarker.rectTransform.anchoredPosition = WorldOffsetToMapPoint(offset);
            waypointMarker.transform.SetAsLastSibling();
            return;
        }

        Vector2 mapDirection = offset.normalized;
        float edgeRadius = CurrentMapSize * 0.5f - waypointMarkerSize * 1.5f;
        mapDirectionArrow.rectTransform.sizeDelta = new Vector2(waypointMarkerSize * 1.25f, waypointMarkerSize * 1.25f);
        mapDirectionArrow.rectTransform.anchoredPosition = mapDirection * Mathf.Max(1f, edgeRadius);
        mapDirectionArrow.rectTransform.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(mapDirection.y, mapDirection.x) * Mathf.Rad2Deg + 90f);
        mapDirectionArrow.transform.SetAsLastSibling();
    }

    private Vector2 WorldOffsetToMapPoint(Vector2 worldOffset)
    {
        float pixelsPerWorldUnit = (CurrentMapSize * 0.5f - 10f) / Mathf.Max(1f, CurrentWorldRadius);
        return worldOffset * pixelsPerWorldUnit;
    }

    private void ClearWaypoint()
    {
        hasWaypoint = false;
        if (waypointMarker != null)
            waypointMarker.gameObject.SetActive(false);
        if (mapDirectionArrow != null)
            mapDirectionArrow.gameObject.SetActive(false);
        if (directionIndicator != null)
            directionIndicator.gameObject.SetActive(false);
    }

    private float CurrentMapSize => isExpanded ? Mathf.Max(mapSize, expandedMapSize) : mapSize;
    private float CurrentWorldRadius => isExpanded ? Mathf.Max(worldRadius, expandedWorldRadius) : worldRadius;

    private void ToggleExpanded()
    {
        isExpanded = !isExpanded;
        ApplyMapState();
    }

    private void ApplyMapState()
    {
        if (panelRect == null)
            return;

        float currentSize = CurrentMapSize;
        if (isExpanded)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
        }
        else
        {
            panelRect.anchorMin = Vector2.one;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = Vector2.one;
            panelRect.anchoredPosition = new Vector2(-topRightMargin.x, -topRightMargin.y);
        }

        panelRect.sizeDelta = new Vector2(currentSize + 12f, currentSize + 12f);
        if (horizontalGrid != null)
            horizontalGrid.sizeDelta = new Vector2(currentSize - 18f, 1f);
        if (verticalGrid != null)
            verticalGrid.sizeDelta = new Vector2(1f, currentSize - 18f);
        if (waypointHelpText != null)
        {
            waypointHelpText.gameObject.SetActive(isExpanded);
            waypointHelpText.rectTransform.anchoredPosition = new Vector2(0f, -(currentSize * 0.5f + 18f));
        }

        RefreshMarkers();
    }

    private static TextMeshProUGUI CreateTMPText(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private static Text CreateText(string objectName, Transform parent, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
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

    private static Sprite CreateArrowSprite(int size, out Texture2D texture)
    {
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Waypoint Arrow",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            float halfWidth = (y / (float)(size - 1)) * (size * 0.45f);
            for (int x = 0; x < size; x++)
            {
                bool inside = Mathf.Abs(x - (size - 1) * 0.5f) <= halfWidth;
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
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

        if (minimapCamera != null)
        {
            minimapCamera.targetTexture = null;
            Destroy(minimapCamera.gameObject);
        }

        if (worldTexture != null)
        {
            if (worldTexture.IsCreated())
                worldTexture.Release();
            Destroy(worldTexture);
        }

        if (circleSprite != null)
            Destroy(circleSprite);
        if (circleTexture != null)
            Destroy(circleTexture);
        if (arrowSprite != null)
            Destroy(arrowSprite);
        if (arrowTexture != null)
            Destroy(arrowTexture);
    }
}
