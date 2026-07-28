using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 医院场景设置脚本。
/// 场景加载时自动开始医院对话链：
/// 护士→医生→注射→光照→黑猫→ExplorationScene
/// </summary>
public class HospitalSceneSetup : MonoBehaviour
{
    [Header("对话起点（拖入 H1_Nurse1 资产）")]
    public DialogueNode startNode;

    void Start()
    {
        Debug.Log($"[HospitalSceneSetup] Start - 场景: {SceneManager.GetActiveScene().name}");

        // 确保 EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 确保 InputHandler（鼠标点击推进对话）
        if (FindObjectOfType<InputHandler>() == null)
        {
            GameObject inputGo = new GameObject("InputHandler");
            inputGo.AddComponent<InputHandler>();
        }

        StartCoroutine(StartDialogueDelayed());
    }

    System.Collections.IEnumerator StartDialogueDelayed()
    {
        yield return null;

        if (DialogueManager.instance == null)
        {
            Debug.LogError("[HospitalSceneSetup] DialogueManager.instance 为空！场景中是否有 DialoguePanel prefab？");
            yield break;
        }

        if (startNode == null)
        {
            Debug.LogError("[HospitalSceneSetup] startNode 为空！请在 Inspector 中拖入 H1_Nurse1 资产。");
            yield break;
        }

        Debug.Log($"[HospitalSceneSetup] 开始对话: {startNode.name}, speaker: {startNode.speakerName}");
        GameProgress.ResetAll();
        DialogueManager.instance.StartDialogue(startNode);
        Debug.Log($"[HospitalSceneSetup] StartDialogue 完成。IsDialogueActive: {DialogueManager.instance.IsDialogueActive}");
    }
}
