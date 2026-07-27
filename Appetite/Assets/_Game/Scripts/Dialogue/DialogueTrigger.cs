using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode { OnStart, OnProximity }

    public DialogueNode startNode;
    public TriggerMode triggerMode = TriggerMode.OnStart;

    [Header("靠近触发设置")]
    public KeyCode interactKey = KeyCode.F;       // 默认 F 键交互
    public GameObject visualIndicator;            // 靠近时显示的提示（发光/图标）

    private bool playerInRange = false;

    void Start()
    {
        if (triggerMode == TriggerMode.OnStart)
        {
            if (DialogueManager.instance != null)
                DialogueManager.instance.StartDialogue(startNode);
        }
        else if (triggerMode == TriggerMode.OnProximity)
        {
            // 靠近模式：初始隐藏指示器
            if (visualIndicator != null)
                visualIndicator.SetActive(false);
        }
    }

    void Update()
    {
        if (triggerMode != TriggerMode.OnProximity) return;
        if (!playerInRange) return;
        if (DialogueManager.instance == null) return;

        // 对话进行中不再触发
        if (DialogueManager.instance.IsDialogueActive) return;

        if (Input.GetKeyDown(interactKey))
        {
            DialogueManager.instance.StartDialogue(startNode);
            // 开始对话后隐藏指示器
            if (visualIndicator != null)
                visualIndicator.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerMode != TriggerMode.OnProximity) return;
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (visualIndicator != null)
                visualIndicator.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (triggerMode != TriggerMode.OnProximity) return;
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (visualIndicator != null)
                visualIndicator.SetActive(false);
        }
    }

    // 2D 碰撞兼容
    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerMode != TriggerMode.OnProximity) return;
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (visualIndicator != null)
                visualIndicator.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (triggerMode != TriggerMode.OnProximity) return;
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (visualIndicator != null)
                visualIndicator.SetActive(false);
        }
    }
}
