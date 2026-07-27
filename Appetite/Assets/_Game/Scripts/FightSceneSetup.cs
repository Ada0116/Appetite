using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 临时脚本：在 Fight 场景中自动创建 Canvas + 按钮，用于跳转到精神世界。
/// Demo 阶段使用，后续会替换为真正的战斗系统 UI。
/// 将此脚本挂到 Fight 场景中任意 GameObject 上即可。
/// </summary>
public class FightSceneSetup : MonoBehaviour
{
    [Header("按钮设置")]
    public string buttonText = "进入精神世界";
    public string targetSceneName = "SpiritWorld";
    public Vector2 buttonSize = new Vector2(300, 80);
    public Vector2 buttonPosition = new Vector2(0, -100);

    void Start()
    {
        CreateUI();
    }

    void CreateUI()
    {
        // --- EventSystem（如果场景中没有） ---
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // --- Canvas ---
        GameObject canvasGo = new GameObject("TempCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        // --- SceneLoader ---
        SceneLoader sceneLoader = canvasGo.AddComponent<SceneLoader>();

        // --- Button ---
        GameObject buttonGo = CreateButton(canvasGo.transform, buttonText, buttonSize, buttonPosition);

        // 绑定点击事件
        Button btn = buttonGo.GetComponent<Button>();
        string sceneName = targetSceneName; // 捕获
        btn.onClick.AddListener(() => sceneLoader.LoadSceneByName(sceneName));
    }

    GameObject CreateButton(Transform parent, string text, Vector2 size, Vector2 anchoredPos)
    {
        GameObject buttonGo = new GameObject("Btn_ToSpiritWorld");
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

        // 背景图
        Image image = buttonGo.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        Button button = buttonGo.AddComponent<Button>();
        button.targetGraphic = image;

        // 文字（TMP）
        GameObject textGo = new GameObject("Text (TMP)");
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return buttonGo;
    }
}
