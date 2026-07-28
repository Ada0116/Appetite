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

    /// <summary>
    /// 自动推进前的钩子。返回 false 则中断自动串联，改为结束对话。
    /// 用于在 NPC 对话之间需要玩家走到下一个 NPC 的场景。
    /// </summary>
    public static System.Func<DialogueNode, bool> OnBeforeAutoAdvance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Debug.Log($"[DialogueManager] Awake: 新 singleton 实例已注册。dialoguePanel={dialoguePanel != null}, optionsPanel={optionsPanel != null}, optionButtons={optionButtons != null}/{optionButtons?.Length}");
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] Awake: 已存在 singleton，销毁重复实例。当前场景: {SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
            return;
        }

        // 确保在 Canvas 下渲染（如果 prefab 被直接放在场景中作为根对象）
        if (GetComponentInParent<Canvas>() == null)
        {
            Debug.Log("[DialogueManager] 未检测到父 Canvas，自动添加 Canvas 组件以支持 UI 渲染。");
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            UnityEngine.UI.CanvasScaler scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);      // 开始时不显示
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    void OnDestroy()
    {
        // 清理静态引用，防止指向已销毁的对象
        if (instance == this)
            instance = null;
    }

    void OnEnable()
    {
        // 如果静态引用丢失（场景重载等边界情况），重新注册
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        // 记录初始场景名
        previousSceneName = SceneManager.GetActiveScene().name;

        // 自动发现所有 UI 引用（prefab 修改会导致 Inspector 引用丢失）
        AutoDiscoverAll();

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

    // 自动发现所有基本 UI 引用（dialoguePanel, speakerNameText, dialogueText, nextIndicator）
    // 按名称从 GameObject 层级中查找，完全绕过 Inspector 序列化引用
    void AutoDiscoverAll()
    {
        // 如果 dialoguePanel 为空，默认指向自己的 GameObject
        if (dialoguePanel == null)
        {
            dialoguePanel = gameObject;
            Debug.Log("[DialogueManager] AutoDiscoverAll: dialoguePanel 为空，默认指向 gameObject。");
        }

        // 按名称查找子对象
        if (speakerNameText == null)
        {
            Transform t = transform.Find("SpeakerNameText");
            if (t == null) t = FindChildRecursive(transform, "SpeakerNameText");
            if (t != null)
            {
                speakerNameText = t.GetComponent<TextMeshProUGUI>();
                Debug.Log($"[DialogueManager] AutoDiscoverAll: 找到 speakerNameText -> {t.name}");
            }
        }

        if (dialogueText == null)
        {
            Transform t = transform.Find("DialogueText");
            if (t == null) t = FindChildRecursive(transform, "DialogueText");
            if (t != null)
            {
                dialogueText = t.GetComponent<TextMeshProUGUI>();
                Debug.Log($"[DialogueManager] AutoDiscoverAll: 找到 dialogueText -> {t.name}");
            }
        }

        if (nextIndicator == null)
        {
            Transform t = transform.Find("NextIndicator");
            if (t == null) t = FindChildRecursive(transform, "NextIndicator");
            if (t != null)
            {
                nextIndicator = t.gameObject;
                Debug.Log($"[DialogueManager] AutoDiscoverAll: 找到 nextIndicator -> {t.name}");
            }
        }

        Debug.Log($"[DialogueManager] AutoDiscoverAll 完成: panel={dialoguePanel != null}, speakerName={speakerNameText != null}, dialogue={dialogueText != null}, next={nextIndicator != null}");
    }

    // 按名称从 DialoguePanel 层级中查找所有 UI 元素
    // 完全不依赖 Inspector 中拖入的引用（prefab 修改会导致引用丢失）
    void AutoDiscoverOptions()
    {
        // Step 1: 确保 optionsPanel 引用有效
        if (optionsPanel == null && dialoguePanel != null)
        {
            Transform optPanelTransform = dialoguePanel.transform.Find("OptionsPanel");
            if (optPanelTransform != null)
                optionsPanel = optPanelTransform.gameObject;
            else
                Debug.LogWarning("[DialogueManager] 在 DialoguePanel 下找不到 OptionsPanel，尝试递归查找...");
        }

        // 如果 optionsPanel 仍然为空，尝试从当前 GameObject 的层级中查找
        if (optionsPanel == null)
        {
            Transform optPanelTransform = transform.Find("OptionsPanel");
            if (optPanelTransform == null)
                optPanelTransform = FindChildRecursive(transform, "OptionsPanel");
            if (optPanelTransform != null)
                optionsPanel = optPanelTransform.gameObject;
        }

        if (optionsPanel == null)
        {
            Debug.LogError("[DialogueManager] 无法找到 OptionsPanel！所有选项按钮将无法使用。");
            return;
        }

        Debug.Log($"[DialogueManager] OptionsPanel 已定位: {optionsPanel.name}, active={optionsPanel.activeSelf}");

        // Step 2: 按名称查找 OptionButton1/2/3
        Button[] foundButtons = new Button[3];
        TextMeshProUGUI[] foundTexts = new TextMeshProUGUI[3];
        int foundCount = 0;

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
            if (foundButtons[i] == null)
            {
                Debug.LogWarning($"[DialogueManager] {btnName} 上没有 Button 组件");
                continue;
            }

            // 查找 TMP 文本（包括非激活的子对象）
            foundTexts[i] = btnTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (foundTexts[i] == null)
            {
                var legacyText = btnTransform.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (legacyText != null)
                {
                    Debug.Log($"[DialogueManager] {btnName} 的文字是 Legacy Text，正在替换为 TMP...");
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

            foundCount++;
            Debug.Log($"[DialogueManager] 自动发现 {btnName}: Button={foundButtons[i] != null}, TMP={foundTexts[i] != null}");
        }

        // Step 3: 总是替换引用
        optionButtons = foundButtons;
        optionTexts = foundTexts;
        Debug.Log($"[DialogueManager] 选项按钮发现完成: {foundCount}/3 个可用。");
    }

    // 递归查找子对象（按名称）
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // 让选项按钮可读：深色背景 + 同字体
    void FixOptionButtonStyle()
    {
        // 修复 OptionsPanel 的背景 Image：关闭 raycastTarget 防止拦截按钮点击
        if (optionsPanel != null)
        {
            Image panelImg = optionsPanel.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.raycastTarget = false;
            }
        }

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
        if (dialoguePanel != null)
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
            // === 运行时紧急修复：如果引用仍然无效，重试自动发现 ===
            if (optionButtons == null || optionButtons.Length == 0 || optionButtons[0] == null)
            {
                Debug.LogWarning("[DialogueManager] 显示选项时发现按钮引用无效，执行紧急修复...");
                AutoDiscoverOptions();
            }

            // === 显示选项按钮 ===
            if (optionsPanel != null)
                optionsPanel.SetActive(true);
            if (nextIndicator != null)
                nextIndicator.SetActive(false);

            isChoosing = true;

            // 配置每个按钮
            int optionCount = node.options.Count;
            int btnCount = (optionButtons != null) ? optionButtons.Length : 0;
            int boundCount = 0;
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
                    boundCount++;
                    Debug.Log($"[DialogueManager] 选项 {i} 已绑定: \"{node.options[i].optionText}\" -> {optionButtons[i].gameObject.name} (interactable={optionButtons[i].interactable})");
                }
                else
                {
                    // 多余的按钮隐藏
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            if (boundCount == 0)
            {
                Debug.LogError($"[DialogueManager] 未能绑定任何选项按钮！optionButtons 有效条目数: {btnCount}, options.Count: {optionCount}");
            }

            // 强制刷新 Canvas，确保按钮的 raycast 数据立即可用
            Canvas.ForceUpdateCanvases();
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
            if (OnBeforeAutoAdvance != null && !OnBeforeAutoAdvance(chosen.nextNode))
            {
                EndDialogue();
                return;
            }
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
            // 检查是否有外部钩子需要中断自动串联
            if (OnBeforeAutoAdvance != null && !OnBeforeAutoAdvance(currentNode.nextNode))
            {
                EndDialogue();
                return;
            }
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
