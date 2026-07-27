using UnityEngine;

/// <summary>
/// 临时脚本：在精神世界场景中自动创建狗 NPC。
/// Demo 阶段使用。将此脚本挂到 SpiritWorld 场景中任意 GameObject 上，
/// 在 Inspector 中将 Dog1 资产拖入 startNode 字段即可。
/// 狗会出现在玩家出生点附近，走过去按 F 键即可开始对话。
/// </summary>
public class SpiritWorldSetup : MonoBehaviour
{
    [Header("对话起点（拖入 Dog1 资产）")]
    public DialogueNode startNode;

    [Header("狗的位置（相对于玩家出生点）")]
    public Vector3 dogOffset = new Vector3(3, 0, 3);

    [Header("交互键")]
    public KeyCode interactKey = KeyCode.F;

    void Start()
    {
        CreateDogNPC();
    }

    void CreateDogNPC()
    {
        // 找到玩家位置
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 spawnPos = (player != null) ? player.transform.position + dogOffset : dogOffset;

        // --- 创建狗 NPC GameObject ---
        GameObject dog = new GameObject("DogNPC");
        dog.transform.position = spawnPos;

        // SpriteRenderer（占位图形，深色代表狗）
        SpriteRenderer sr = dog.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite();
        sr.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        sr.sortingOrder = 1;
        // 调整缩放让狗可见（大约 2x2 单位）
        dog.transform.localScale = new Vector3(2, 2, 1);

        // 碰撞体（触发区域）
        BoxCollider col = dog.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3, 3, 3);
        col.center = new Vector3(0, 1, 0);

        // 指示器子对象（靠近时显示的黄色光晕）
        GameObject indicator = new GameObject("GlowIndicator");
        indicator.transform.SetParent(dog.transform);
        indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
        indicator.transform.localScale = Vector3.one;
        indicator.SetActive(false);

        SpriteRenderer indicatorSr = indicator.AddComponent<SpriteRenderer>();
        indicatorSr.sprite = CreatePlaceholderSprite();
        indicatorSr.color = new Color(1, 1, 0, 0.4f);
        indicatorSr.sortingOrder = 2;

        // DialogueTrigger
        DialogueTrigger trigger = dog.AddComponent<DialogueTrigger>();
        trigger.triggerMode = DialogueTrigger.TriggerMode.OnProximity;
        trigger.interactKey = interactKey;
        trigger.visualIndicator = indicator;
        trigger.startNode = startNode;

        if (startNode != null)
            Debug.Log($"[SpiritWorldSetup] 狗 NPC 已创建在 {spawnPos}，按 {interactKey} 开始对话。");
        else
            Debug.LogWarning("[SpiritWorldSetup] startNode 未设置！请在 Inspector 中拖入 Dog1 资产。");
    }

    Sprite CreatePlaceholderSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0), 64);
    }
}
