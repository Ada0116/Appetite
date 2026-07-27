using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Header("UI 引用")]
    public GameObject dialoguePanel;          // 整个对话框
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject nextIndicator;          // "点击继续"提示

    [Header("选项")]
    public GameObject optionsPanel;           // 选项面板
    public Button[] optionButtons;            // 选项按钮（最多3个）
    public TextMeshProUGUI[] optionTexts;     // 选项按钮上的 TMP 文字

    [Header("字体（可选，拖入即可替换）")]
    public TMP_FontAsset defaultFont;         // 对话文字字体
    public TMP_FontAsset optionFont;          // 选项文字字体

    [Header("对话结束事件")]
    public UnityEvent onDialogueEnded;        // 任意对话结束时触发

    private DialogueNode currentNode;         // 当前显示的节点
    private bool canAdvance = false;          // 是否允许点击下一句
    private bool isChoosing = false;          // 是否正在显示选项
    private string previousSceneName;         // 进入对话前的场景名（用于 ReturnToPrevious）

    public bool IsDialogueActive => currentNode != null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dialoguePanel.SetActive(false);      // 开始时不显示
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    void Start()
    {
        // 记录初始场景名
        previousSceneName = SceneManager.GetActiveScene().name;

        // 自动发现选项按钮（如果 Inspector 中未连线）
        AutoDiscoverOptions();

        // 修复按钮样式（白色文字在白色按钮上看不清）
        FixOptionButtonStyle();

        // 应用可选字体
        if (defaultFont != null)
        {
            if (speakerNameText != null) speakerNameText.font = defaultFont;
            if (dialogueText != null) dialogueText.font = defaultFont;
        }
        if (optionFont != null && optionTexts != null)
        {
            foreach (var ot in optionTexts)
            {
                if (ot != null) ot.font = optionFont;
            }
        }
    }

    // 如果 optionButtons / optionTexts 为空，从 OptionsPanel 的子对象中自动查找
    void AutoDiscoverOptions()
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("[DialogueManager] optionsPanel 为空，无法自动发现按钮。");
            return;
        }

        bool needButtons = (optionButtons == null || optionButtons.Length == 0 || optionButtons[0] == null);
        bool needTexts = (optionTexts == null || optionTexts.Length == 0 || optionTexts[0] == null);

        if (!needButtons && !needTexts) return;

        // 按名称查找 OptionButton1/2/3
        Button[] foundButtons = new Button[3];
        TextMeshProUGUI[] foundTexts = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            string btnName = "OptionButton" + (i + 1);
            Transform btnTransform = optionsPanel.transform.Find(btnName);
            if (btnTransform == null)
            {
                Debug.LogWarning($"[DialogueManager] 在 OptionsPanel 下找不到 {btnName}");
                continue;
            }

            foundButtons[i] = btnTransform.GetComponent<Button>();
            // 先尝试 TMP，再尝试 Legacy Text
            foundTexts[i] = btnTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (foundTexts[i] == null)
            {
                // 如果没有 TMP，尝试获取 Legacy Text 并动态替换
                var legacyText = btnTransform.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (legacyText != null)
                {
                    Debug.Log($"[DialogueManager] {btnName} 的文字是 Legacy Text，正在替换为 TMP...");
                    // 保留文字和颜色
                    string oldText = legacyText.text;
                    Color oldColor = legacyText.color;
                    GameObject textGo = legacyText.gameObject;
                    DestroyImmediate(legacyText);
                    foundTexts[i] = textGo.AddComponent<TextMeshProUGUI>();
                    foundTexts[i].text = oldText;
                    foundTexts[i].color = oldColor;
                    foundTexts[i].fontSize = 24;
                    foundTexts[i].alignment = TMPro.TextAlignmentOptions.Center;
                }
            }

            Debug.Log($"[DialogueManager] 找到 {btnName}: Button={foundButtons[i] != null}, TMP={foundTexts[i] != null}");
        }

        if (needButtons) optionButtons = foundButtons;
        if (needTexts) optionTexts = foundTexts;
    }

    // 让选项按钮可读：深色背景 + 同字体
    void FixOptionButtonStyle()
    {
        if (optionButtons == null) return;

        // 使用对话字体（如果没有单独设置 optionFont）
        TMP_FontAsset font = (optionFont != null) ? optionFont : defaultFont;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] == null) continue;
            // 按钮背景改为深灰色
            Image btnImg = optionButtons[i].GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            }
            // 文字改为浅色（在深色背景上可见），应用字体
            if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
            {
                optionTexts[i].color = new Color(0.9f, 0.9f, 0.85f, 1f);  // 暖白色
                optionTexts[i].fontSize = 24;
                if (font != null) optionTexts[i].font = font;
            }
        }
    }

    // 开始一段对话
    public void StartDialogue(DialogueNode startNode)
    {
        if (startNode == null) return;

        // 每次开始新对话时记录当前场景
        previousSceneName = SceneManager.GetActiveScene().name;
        dialoguePanel.SetActive(true);
        DisplayNode(startNode);
    }

    void DisplayNode(DialogueNode node)
    {
        if (node == null)
        {
            EndDialogue();
            return;
        }

        currentNode = node;
        canAdvance = false;
        isChoosing = false;

        // 设置说话人和内容
        if (speakerNameText != null)
            speakerNameText.text = node.speakerName;
        if (dialogueText != null)
            dialogueText.text = node.text;

        // 判断是否有选项
        if (node.options != null && node.options.Count > 0)
        {
            // === 显示选项按钮 ===
            if (optionsPanel != null)
                optionsPanel.SetActive(true);
            if (nextIndicator != null)
                nextIndicator.SetActive(false);

            isChoosing = true;

            // 配置每个按钮
            int optionCount = node.options.Count;
            int btnCount = (optionButtons != null) ? optionButtons.Length : 0;
            for (int i = 0; i < btnCount; i++)
            {
                if (optionButtons[i] == null) continue;

                if (i < optionCount)
                {
                    // 启用按钮，设置文字和点击事件
                    optionButtons[i].gameObject.SetActive(true);
                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                        optionTexts[i].text = node.options[i].optionText;

                    // 移除旧的监听器，添加新的（闭包捕获）
                    optionButtons[i].onClick.RemoveAllListeners();
                    int index = i;  // 捕获当前索引
                    optionButtons[i].onClick.AddListener(() => SelectOption(index));
                    Debug.Log($"[DialogueManager] 选项 {i} 已绑定: \"{node.options[i].optionText}\" -> {optionButtons[i].gameObject.name} (interactable={optionButtons[i].interactable})");
                }
                else
                {
                    // 多余的按钮隐藏
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // === 无选项：显示"点击继续" ===
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            if (nextIndicator != null)
                nextIndicator.SetActive(true);

            canAdvance = true;
        }
    }

    // 玩家选择一个选项
    public void SelectOption(int index)
    {
        Debug.Log($"[DialogueManager] SelectOption({index}) 被调用, isChoosing={isChoosing}, currentNode={currentNode != null}, options count={currentNode?.options?.Count}");

        if (!isChoosing || currentNode == null)
        {
            Debug.LogWarning($"[DialogueManager] SelectOption 被阻止: isChoosing={isChoosing}, node={currentNode != null}");
            return;
        }
        if (index < 0 || index >= currentNode.options.Count)
        {
            Debug.LogWarning($"[DialogueManager] SelectOption 索引越界: {index}/{currentNode.options.Count}");
            return;
        }

        isChoosing = false;
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        DialogueOption chosen = currentNode.options[index];
        Debug.Log($"[DialogueManager] 选择了: \"{chosen.optionText}\", nextNode={chosen.nextNode != null}");

        // 饥饿值变化（预留：后续接入玩家状态系统）
        if (chosen.hungerChange != 0)
        {
            Debug.Log($"[Dialogue] 饥饿值变化: {chosen.hungerChange}");
        }

        if (chosen.nextNode != null)
        {
            DisplayNode(chosen.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    // 玩家尝试推进对话（点击/按键）
    public void AdvanceDialogue()
    {
        if (!canAdvance || currentNode == null || isChoosing) return;

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
        // 保存结束时的节点引用
        DialogueNode endingNode = currentNode;

        // 隐藏 UI
        dialoguePanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (nextIndicator != null)
            nextIndicator.SetActive(false);

        currentNode = null;
        canAdvance = false;
        isChoosing = false;

        // 触发 Inspector 事件
        onDialogueEnded?.Invoke();

        // 检查结束行为
        if (endingNode != null)
        {
            switch (endingNode.endAction)
            {
                case DialogueEndAction.LoadScene:
                    if (!string.IsNullOrEmpty(endingNode.endActionSceneName))
                    {
                        SceneManager.LoadScene(endingNode.endActionSceneName);
                    }
                    else
                    {
                        Debug.LogWarning("[Dialogue] endAction 为 LoadScene，但未指定场景名");
                    }
                    break;

                case DialogueEndAction.ReturnToPrevious:
                    if (!string.IsNullOrEmpty(previousSceneName))
                    {
                        SceneManager.LoadScene(previousSceneName);
                    }
                    else
                    {
                        Debug.LogWarning("[Dialogue] endAction 为 ReturnToPrevious，但没有记录 previousSceneName");
                    }
                    break;

                case DialogueEndAction.None:
                default:
                    // 不做任何事
                    break;
            }
        }
    }
}
