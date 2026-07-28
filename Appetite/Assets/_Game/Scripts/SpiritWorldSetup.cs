using UnityEngine;

/// <summary>
/// 精神世界场景设置脚本。
/// 流程：
/// 1. 场景加载 → 播放黑猫引子对话（SpiritCat）
/// 2. 引子结束后 → 生成狗 NPC
/// 3. 玩家走近狗按 F → 开始狗对话（Dog1）
/// 4. 狗对话结束后 → 设置返回标志 → 加载 ExplorationScene
/// </summary>
public class SpiritWorldSetup : MonoBehaviour
{
    [Header("黑猫引子（场景加载时自动播放）")]
    public DialogueNode spiritCatStartNode;    // SC1_CatDarkness

    [Header("狗对话起点")]
    public DialogueNode dogStartNode;          // Dog1

    [Header("狗的位置（相对于玩家出生点）")]
    public Vector3 dogOffset = new Vector3(3, 0, 3);

    [Header("交互键")]
    public KeyCode interactKey = KeyCode.F;

    private GameObject dogNPC;

    void Start()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        StartCoroutine(PlaySpiritCatIntro());
    }

    System.Collections.IEnumerator PlaySpiritCatIntro()
    {
        yield return null;

        if (spiritCatStartNode != null && DialogueManager.instance != null)
        {
            Debug.Log("[SpiritWorldSetup] 开始播放黑猫引子对话。");
            DialogueManager.instance.onDialogueEnded.AddListener(OnCatIntroEnded);
            DialogueManager.instance.StartDialogue(spiritCatStartNode);
        }
        else
        {
            Debug.LogWarning("[SpiritWorldSetup] spiritCatStartNode 为空，直接生成狗 NPC。");
            CreateDogNPC();
        }
    }

    void OnCatIntroEnded()
    {
        DialogueManager.instance.onDialogueEnded.RemoveListener(OnCatIntroEnded);
        Debug.Log("[SpiritWorldSetup] 黑猫引子结束，生成狗 NPC。");
        CreateDogNPC();
    }

    void OnDogDialogueEnded()
    {
        DialogueManager.instance.onDialogueEnded.RemoveListener(OnDogDialogueEnded);
        GameProgress.hasReturnedFromSpiritWorld = true;
        Debug.Log("[SpiritWorldSetup] 狗对话结束，已设置 hasReturnedFromSpiritWorld = true");
    }

    void CreateDogNPC()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 spawnPos = (player != null) ? player.transform.position + dogOffset : dogOffset;

        dogNPC = new GameObject("DogNPC");
        dogNPC.transform.position = spawnPos;

        SpriteRenderer sr = dogNPC.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite();
        sr.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        sr.sortingOrder = 1;
        dogNPC.transform.localScale = new Vector3(2, 2, 1);

        BoxCollider col = dogNPC.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3, 3, 3);
        col.center = new Vector3(0, 1, 0);

        GameObject indicator = new GameObject("GlowIndicator");
        indicator.transform.SetParent(dogNPC.transform);
        indicator.transform.localPosition = new Vector3(0, 2.5f, 0);
        indicator.transform.localScale = Vector3.one;
        indicator.SetActive(false);

        SpriteRenderer indicatorSr = indicator.AddComponent<SpriteRenderer>();
        indicatorSr.sprite = CreatePlaceholderSprite();
        indicatorSr.color = new Color(1, 1, 0, 0.4f);
        indicatorSr.sortingOrder = 2;

        DialogueTrigger trigger = dogNPC.AddComponent<DialogueTrigger>();
        trigger.triggerMode = DialogueTrigger.TriggerMode.OnProximity;
        trigger.interactKey = interactKey;
        trigger.visualIndicator = indicator;
        trigger.startNode = dogStartNode;

        // 监听狗对话结束（猫引子已结束，下一次对话结束就是狗对话）
        if (DialogueManager.instance != null)
            DialogueManager.instance.onDialogueEnded.AddListener(OnDogDialogueEnded);

        if (dogStartNode != null)
            Debug.Log($"[SpiritWorldSetup] 狗 NPC 已创建在 {spawnPos}，按 {interactKey} 开始对话。");
        else
            Debug.LogWarning("[SpiritWorldSetup] dogStartNode 未设置！请在 Inspector 中拖入 Dog1 资产。");
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
