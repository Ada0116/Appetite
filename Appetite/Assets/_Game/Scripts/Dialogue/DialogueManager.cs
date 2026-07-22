using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI 引用")]
    public GameObject dialoguePanel;          // 整个对话框
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject nextIndicator;          // “点击继续”提示

    private DialogueNode currentNode;         // 当前显示的节点
    private bool canAdvance = false;          // 是否允许点击下一句

    void Awake()
    {
        instance = this;
        dialoguePanel.SetActive(false);      // 开始时不显示
    }

    // 开始一段对话
    public void StartDialogue(DialogueNode startNode)
    {
        dialoguePanel.SetActive(true);
        DisplayNode(startNode);
    }

    void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        canAdvance = false;

        speakerNameText.text = node.speakerName;
        dialogueText.text = node.text;

        // 隐藏选项面板（现在还没有，先不管）
        // optionsPanel.SetActive(false);

        // 显示“点击继续”
        nextIndicator.SetActive(true);
        canAdvance = true;
    }

    // 玩家尝试推进对话
    public void AdvanceDialogue()
    {
        if (!canAdvance || currentNode == null) return;

        if (currentNode.nextNode != null)
        {
            DisplayNode(currentNode.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNode = null;
    }
}