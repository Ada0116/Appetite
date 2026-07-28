using UnityEngine;

/// <summary>
/// ExplorationScene（主世界）场景设置脚本。
///
/// 两个入口，根据 GameProgress.hasReturnedFromSpiritWorld 区分：
/// 1. 从医院进入（首次）→ 自动播放同事→面包店→居委会→电脑→Fight 对话链
/// 2. 从精神世界返回 → 自动播放觉醒对话链（黑猫→脚印→浆果→主菜单）
///
/// 挂到 ExplorationScene 中任意 GameObject 上即可。
/// </summary>
public class ExplorationSceneSetup : MonoBehaviour
{
    [Header("首次进入（从医院来）的对话起点")]
    public DialogueNode firstEntryStartNode;   // C1_Meet（同事→面包店→居委会→电脑→Fight 全串联）

    [Header("从精神世界返回时的觉醒对话起点")]
    public DialogueNode awakeningStartNode;     // PW1_WakeWithCat

    void Start()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        StartCoroutine(StartDialogueDelayed());
    }

    System.Collections.IEnumerator StartDialogueDelayed()
    {
        yield return null;

        if (GameProgress.hasReturnedFromSpiritWorld)
        {
            // === 入口2：从精神世界返回 ===
            GameProgress.hasReturnedFromSpiritWorld = false;

            if (awakeningStartNode != null && DialogueManager.instance != null)
            {
                Debug.Log("[ExplorationSceneSetup] 从精神世界返回，自动播放觉醒对话链。");
                DialogueManager.instance.StartDialogue(awakeningStartNode);
            }
            else
            {
                Debug.LogWarning("[ExplorationSceneSetup] awakeningStartNode 或 DialogueManager 为空！");
            }
        }
        else
        {
            // === 入口1：从医院进入 ===
            if (firstEntryStartNode != null && DialogueManager.instance != null)
            {
                Debug.Log("[ExplorationSceneSetup] 首次进入主世界，自动播放对话链。");
                DialogueManager.instance.StartDialogue(firstEntryStartNode);
            }
            else
            {
                Debug.LogWarning("[ExplorationSceneSetup] firstEntryStartNode 或 DialogueManager 为空！");
            }
        }
    }
}
