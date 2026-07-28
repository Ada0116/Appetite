using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ExplorationScene 主世界设置。
/// - 首次进入：按顺序生成 NPC（一次只显示下一个），走到附近按 F 触发对话。
/// - 从精神世界返回：自动播放觉醒对话链。
/// </summary>
public class ExplorationSceneSetup : MonoBehaviour
{
    [Header("--- 从精神世界返回时的觉醒对话 ---")]
    public DialogueNode awakeningStartNode;
    public Vector3 wakeUpPosition = new Vector3(167, 2, 19);

    [Header("--- NPC 触发器（按顺序出现） ---")]
    public bool createNPCs = true;

    [Header("NPC 1: 前同事（最先出现）")]
    public DialogueNode npc1StartNode;
    public Vector3 npc1Position = new Vector3(5, 0, 5);
    public Color npc1Color = new Color(0.3f, 0.5f, 0.8f);
    public string npc1Label = "前同事";

    [Header("NPC 2: 面包师")]
    public DialogueNode npc2StartNode;
    public Vector3 npc2Position = new Vector3(10, 0, 8);
    public Color npc2Color = new Color(0.8f, 0.6f, 0.2f);
    public string npc2Label = "面包师";

    [Header("NPC 3: 社区阿姨")]
    public DialogueNode npc3StartNode;
    public Vector3 npc3Position = new Vector3(15, 0, 3);
    public Color npc3Color = new Color(0.8f, 0.3f, 0.4f);
    public string npc3Label = "社区阿姨";

    [Header("NPC 4: 电脑")]
    public DialogueNode npc4StartNode;
    public Vector3 npc4Position = new Vector3(20, 0, 0);
    public Color npc4Color = new Color(0.3f, 0.7f, 0.3f);
    public string npc4Label = "电脑";

    [Header("交互键")]
    public KeyCode interactKey = KeyCode.F;

    // 内部状态
    private List<GameObject> npcList = new List<GameObject>();
    private List<DialogueNode> npcStartNodes = new List<DialogueNode>();
    private int currentNPCIndex = -1; // -1 = 还没开始，0 = 第一个可见

    void Start()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        if (FindObjectOfType<InputHandler>() == null)
        {
            GameObject inputGo = new GameObject("InputHandler");
            inputGo.AddComponent<InputHandler>();
        }

        StartCoroutine(SetupDelayed());
    }

    System.Collections.IEnumerator SetupDelayed()
    {
        yield return null;

        if (GameProgress.hasReturnedFromSpiritWorld)
        {
            GameProgress.hasReturnedFromSpiritWorld = false;

            // 把玩家传送到电脑的位置（在电脑前醒来）
            TeleportPlayerToComputer();

            if (awakeningStartNode != null && DialogueManager.instance != null)
            {
                Debug.Log("[ExplorationSceneSetup] 从精神世界返回，自动播放觉醒对话链。");
                DialogueManager.instance.StartDialogue(awakeningStartNode);
            }
        }
        else if (createNPCs)
        {
            // 收集有效的 NPC 配置
            var configs = new (DialogueNode node, Vector3 pos, Color color, string label)[]
            {
                (npc1StartNode, npc1Position, npc1Color, npc1Label),
                (npc2StartNode, npc2Position, npc2Color, npc2Label),
                (npc3StartNode, npc3Position, npc3Color, npc3Label),
                (npc4StartNode, npc4Position, npc4Color, npc4Label),
            };

            // 创建所有 NPC（先全部隐藏）
            for (int i = 0; i < configs.Length; i++)
            {
                var cfg = configs[i];
                if (cfg.node == null) continue;

                GameObject npc = CreateNPC($"NPC_{i+1}_{cfg.label}", cfg.node, cfg.pos, cfg.color);
                npc.SetActive(false); // 初始隐藏
                npcList.Add(npc);
                npcStartNodes.Add(cfg.node);
            }

            // 显示第一个 NPC
            if (npcList.Count > 0)
            {
                currentNPCIndex = 0;
                npcList[0].SetActive(true);
                Debug.Log($"[ExplorationSceneSetup] 显示第 1 个 NPC: {npcList[0].name}");
            }

            // 设置自动串联中断钩子
            DialogueManager.OnBeforeAutoAdvance = OnBeforeAutoAdvance;

            // 监听对话结束，显示下一个 NPC
            if (DialogueManager.instance != null)
            {
                DialogueManager.instance.onDialogueEnded.AddListener(OnDialogueEnded);
            }
        }
    }

    /// <summary>
    /// 当对话尝试自动串联到下一个节点时调用。
    /// 如果下一个节点是"下一个 NPC 的起点"，返回 false 中断串联。
    /// </summary>
    bool OnBeforeAutoAdvance(DialogueNode nextNode)
    {
        // 检查是不是下一个 NPC 的起始节点
        int nextIdx = currentNPCIndex + 1;
        if (nextIdx < npcStartNodes.Count && npcStartNodes[nextIdx] == nextNode)
        {
            Debug.Log($"[ExplorationSceneSetup] 检测到串联到 NPC{nextIdx+1}，中断对话。");
            return false; // 中断自动串联
        }
        return true; // 允许继续
    }

    void TeleportPlayerToComputer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 targetPos = wakeUpPosition;
            player.transform.position = targetPos;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            // 2D 物理
            Rigidbody2D rb2d = player.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                rb2d.velocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }

            Debug.Log($"[ExplorationSceneSetup] 玩家已传送到电脑位置: {targetPos}");
        }
        else
        {
            Debug.LogWarning("[ExplorationSceneSetup] 找不到 Player（Tag），无法传送。");
        }
    }

    void OnDialogueEnded()
    {
        // 隐藏当前 NPC，显示下一个
        int nextIdx = currentNPCIndex + 1;
        if (nextIdx < npcList.Count)
        {
            // 隐藏当前
            npcList[currentNPCIndex].SetActive(false);
            // 显示下一个
            currentNPCIndex = nextIdx;
            npcList[nextIdx].SetActive(true);
            Debug.Log($"[ExplorationSceneSetup] 隐藏 NPC{nextIdx}，显示 NPC{nextIdx + 1}: {npcList[nextIdx].name}");
        }
        else
        {
            // 最后一个也隐藏
            npcList[currentNPCIndex].SetActive(false);
            Debug.Log("[ExplorationSceneSetup] 所有 NPC 对话已完成。");
        }
    }

    GameObject CreateNPC(string name, DialogueNode startNode, Vector3 position, Color color)
    {
        GameObject npc = new GameObject(name);
        npc.transform.position = position;

        SpriteRenderer sr = npc.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite();
        sr.color = color;
        sr.sortingOrder = 1;
        npc.transform.localScale = new Vector3(2, 2, 1);

        BoxCollider col = npc.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3, 3, 3);
        col.center = new Vector3(0, 1.5f, 0);

        GameObject indicator = new GameObject("GlowIndicator");
        indicator.transform.SetParent(npc.transform);
        indicator.transform.localPosition = new Vector3(0, 3f, 0);
        indicator.transform.localScale = Vector3.one;
        indicator.SetActive(false);

        SpriteRenderer indicatorSr = indicator.AddComponent<SpriteRenderer>();
        indicatorSr.sprite = CreatePlaceholderSprite();
        indicatorSr.color = new Color(1, 1, 0, 0.5f);
        indicatorSr.sortingOrder = 2;

        DialogueTrigger trigger = npc.AddComponent<DialogueTrigger>();
        trigger.triggerMode = DialogueTrigger.TriggerMode.OnProximity;
        trigger.interactKey = interactKey;
        trigger.visualIndicator = indicator;
        trigger.startNode = startNode;

        return npc;
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

    void OnDrawGizmos()
    {
        if (!createNPCs) return;
        DrawNPCGizmo(npc1Position, npc1Color, npc1Label);
        DrawNPCGizmo(npc2Position, npc2Color, npc2Label);
        DrawNPCGizmo(npc3Position, npc3Color, npc3Label);
        DrawNPCGizmo(npc4Position, npc4Color, npc4Label);
    }

    void DrawNPCGizmo(Vector3 pos, Color color, string label)
    {
        Gizmos.color = color;
        Gizmos.DrawCube(pos + Vector3.up * 1.5f, new Vector3(1, 3, 1));
        Gizmos.DrawWireCube(pos + Vector3.up * 1.5f, new Vector3(3, 3, 3));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 3.5f, label);
#endif
    }
}
