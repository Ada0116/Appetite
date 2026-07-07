using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("地图引用")]
    public RectTransform mapImageRect;     // 场景中的 MapImage（用于读取贴图和尺寸）
    public Transform player;               // 玩家 Transform

    [Header("世界到地图的映射")]
    public Vector2 worldSize = new Vector2(500, 500);
    public Vector2 worldOrigin = new Vector2(-250, -250);

    [Header("全屏地图")]
    public KeyCode mapKey = KeyCode.M;     // 打开/关闭按键

    // 运行时创建的全屏地图
    private GameObject fullMapPanel;
    private RectTransform fullMapImageRect;
    private RectTransform playerMarkerRect;
    private bool isMapOpen = false;
    private Canvas parentCanvas;
    private Texture mapTexture;
    private Vector2 mapTexSize;

    void Start()
    {
        // 隐藏小地图面板
        HideMinimapPanel();

        // 获取地图贴图和尺寸
        if (mapImageRect != null)
        {
            RawImage rawImg = mapImageRect.GetComponent<RawImage>();
            if (rawImg != null)
                mapTexture = rawImg.texture;
            mapTexSize = mapImageRect.sizeDelta;
        }

        // 找到 Canvas
        parentCanvas = FindObjectOfType<Canvas>();
    }

    void Update()
    {
        if (Input.GetKeyDown(mapKey))
            ToggleFullMap();

        if (isMapOpen && Input.GetKeyDown(KeyCode.Escape))
            ToggleFullMap();

        // 全屏地图打开时，实时更新玩家位置标记
        if (isMapOpen && playerMarkerRect != null)
            UpdatePlayerMarker();
    }

    void HideMinimapPanel()
    {
        // 找到 MinimapPanel 并隐藏（MapImage 的父节点就是 MinimapPanel）
        if (mapImageRect != null)
        {
            Transform panel = mapImageRect.parent;
            if (panel != null)
                panel.gameObject.SetActive(false);
        }
    }

    // ==================== 全屏地图 ====================

    void ToggleFullMap()
    {
        if (fullMapPanel == null)
            CreateFullMapPanel();

        if (fullMapPanel == null) return;

        isMapOpen = !isMapOpen;
        fullMapPanel.SetActive(isMapOpen);

        if (isMapOpen)
            UpdatePlayerMarker();
    }

    void CreateFullMapPanel()
    {
        if (parentCanvas == null)
        {
            Debug.LogError("MinimapController: 找不到 Canvas，无法创建全屏地图。");
            return;
        }

        // --- 全屏覆盖层 ---
        fullMapPanel = new GameObject("FullMapPanel", typeof(RectTransform));
        RectTransform overlayRect = fullMapPanel.GetComponent<RectTransform>();
        overlayRect.SetParent(parentCanvas.transform, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        overlayRect.SetAsLastSibling();

        // 半透明黑色背景，点击关闭
        Image bg = fullMapPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        bg.raycastTarget = true;
        Button closeBtn = fullMapPanel.AddComponent<Button>();
        closeBtn.onClick.AddListener(ToggleFullMap);

        // --- 地图图片 ---
        GameObject mapGo = new GameObject("MapImage", typeof(RectTransform));
        RawImage mapRaw = mapGo.AddComponent<RawImage>();
        mapRaw.texture = mapTexture;
        mapRaw.raycastTarget = false;
        fullMapImageRect = mapGo.GetComponent<RectTransform>();
        fullMapImageRect.SetParent(overlayRect, false);
        fullMapImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        fullMapImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        fullMapImageRect.pivot = new Vector2(0.5f, 0.5f);

        // 计算显示尺寸（占屏幕 85%，保持地图宽高比）
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector2 screenSize = canvasRect.sizeDelta;
        float maxW = screenSize.x * 0.85f;
        float maxH = screenSize.y * 0.85f;

        float aspect;
        if (mapTexture != null)
            aspect = (float)mapTexture.width / mapTexture.height;
        else
            aspect = mapTexSize.x / mapTexSize.y;

        float displayW, displayH;
        if (maxW / aspect <= maxH)
        {
            displayW = maxW;
            displayH = maxW / aspect;
        }
        else
        {
            displayH = maxH;
            displayW = maxH * aspect;
        }
        fullMapImageRect.sizeDelta = new Vector2(displayW, displayH);

        // --- 玩家标记 ---
        GameObject markerGo = new GameObject("PlayerMarker", typeof(RectTransform));
        Image markerImg = markerGo.AddComponent<Image>();
        markerImg.raycastTarget = false;
        markerImg.color = Color.white;
        markerImg.preserveAspect = true;

        // 尝试用小地图的玩家图标精灵
        if (mapImageRect != null)
        {
            Transform playerIconTr = mapImageRect.Find("PlayerIcon");
            if (playerIconTr == null)
            {
                // PlayerIcon 可能在 MinimapPanel 下（场景原始结构）
                Transform panel = mapImageRect.parent;
                if (panel != null)
                    playerIconTr = panel.Find("PlayerIcon");
            }
            if (playerIconTr != null)
            {
                Image iconImg = playerIconTr.GetComponent<Image>();
                if (iconImg != null && iconImg.sprite != null)
                {
                    markerImg.sprite = iconImg.sprite;
                }
            }
        }

        // 如果没有精灵，生成白色圆点
        if (markerImg.sprite == null)
        {
            markerImg.sprite = CreateCircleSprite(32, Color.white);
        }

        playerMarkerRect = markerGo.GetComponent<RectTransform>();
        playerMarkerRect.SetParent(fullMapImageRect, false);
        playerMarkerRect.anchorMin = Vector2.zero;
        playerMarkerRect.anchorMax = Vector2.zero;
        playerMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        playerMarkerRect.sizeDelta = new Vector2(36f, 36f);

        fullMapPanel.SetActive(false);
    }

    void UpdatePlayerMarker()
    {
        if (player == null || fullMapImageRect == null || playerMarkerRect == null)
            return;

        // 归一化玩家位置
        float normX = Mathf.InverseLerp(worldOrigin.x, worldOrigin.x + worldSize.x, player.position.x);
        float normY = Mathf.InverseLerp(worldOrigin.y, worldOrigin.y + worldSize.y, player.position.z);

        // 映射到全屏地图的显示尺寸
        Vector2 displaySize = fullMapImageRect.sizeDelta;
        playerMarkerRect.anchoredPosition = new Vector2(
            normX * displaySize.x,
            normY * displaySize.y
        );
    }

    // ==================== 工具方法 ====================

    Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = (size - 1) / 2f;
        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = 1f - Mathf.SmoothStep(radius - 1.5f, radius + 1.5f, dist);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha * color.a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
