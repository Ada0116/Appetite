using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Twine HTML → Unity DialogueNode 导入工具
///
/// 使用方法:
///   1. Unity 菜单 → Tools → Import Twine Dialogue
///   2. 选择 Twine 导出的 HTML 文件
///   3. 指定输出文件夹（默认为 Assets/_Game/Data/test）
///   4. 点击「导入」
///
/// Twine 语法映射:
///   [[下一句->target]]  (单个链接) → nextNode 自动推进
///   [[选项A->targetA]] [[选项B->targetB]] (多个链接) → options 选项列表
///   无链接的段落 → 对话结束节点
///
/// 支持的 Twine 标签 (tags):
///   load:SceneName  → endAction = LoadScene, endActionSceneName = SceneName
///   return          → endAction = ReturnToPrevious
///
/// 可选的说话人约定:
///   段落文本以 "说话人: 对话内容" 开头时，自动拆分为 speakerName + text
/// </summary>
public class TwineImporter : EditorWindow
{
    private string twineFilePath = "";
    private string outputFolder = "Assets/_Game/Data/test";

    [MenuItem("Tools/Import Twine Dialogue")]
    public static void ShowWindow()
    {
        var window = GetWindow<TwineImporter>("Twine Importer");
        window.minSize = new Vector2(500, 200);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Twine 对话导入工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- Input file ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Twine HTML 文件:", GUILayout.Width(120));
        twineFilePath = EditorGUILayout.TextField(twineFilePath);
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择 Twine HTML 文件", "", "html");
            if (!string.IsNullOrEmpty(path))
                twineFilePath = path;
        }
        EditorGUILayout.EndHorizontal();

        // --- Output folder ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("输出文件夹:", GUILayout.Width(120));
        outputFolder = EditorGUILayout.TextField(outputFolder);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择输出文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert absolute path to Assets-relative
                if (path.StartsWith(Application.dataPath))
                    outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);

        // --- Import button ---
        GUI.enabled = !string.IsNullOrEmpty(twineFilePath) && !string.IsNullOrEmpty(outputFolder);
        if (GUILayout.Button("导入", GUILayout.Height(40)))
        {
            ImportTwine();
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Twine 语法:\n" +
            "  [[下一句->目标段落]]           → 自动推进到下一句\n" +
            "  [[选项A->段落A]] [[选项B->段落B]] → 玩家选项\n" +
            "  无链接的段落                   → 对话结束\n\n" +
            "Tags 支持:\n" +
            "  load:SceneName  → 加载场景\n" +
            "  return          → 返回上一场景\n\n" +
            "说话人约定（可选）:\n" +
            "  段落文本以 \"名字: 内容\" 开头 → 自动设置 speakerName",
            MessageType.Info);
    }

    // ============================================================
    // 数据模型
    // ============================================================

    private class TwinePassage
    {
        public string pid;
        public string name;
        public string tags;
        public string rawText;
        public string cleanText;       // 去掉链接后的纯文本
        public List<TwineLink> links = new List<TwineLink>();
        public string speakerName;     // 从文本中提取的说话人
        public string dialogueText;    // 去掉说话人前缀后的对话文本
        public string endActionTag;    // 从 tags 中解析的结束行为标签
    }

    private class TwineLink
    {
        public string displayText;     // 链接显示文字
        public string targetName;      // 目标段落名称
    }

    // ============================================================
    // 主流程
    // ============================================================

    private void ImportTwine()
    {
        if (!File.Exists(twineFilePath))
        {
            EditorUtility.DisplayDialog("错误", $"找不到文件:\n{twineFilePath}", "确定");
            return;
        }

        // 确保输出文件夹存在
        EnsureFolderExists(outputFolder);

        // 读取并解析 Twine HTML
        string html = File.ReadAllText(twineFilePath);
        List<TwinePassage> passages = ParsePassages(html);

        if (passages.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "未在 HTML 中找到任何 Twine 段落 (tw-passagedata)", "确定");
            return;
        }

        Debug.Log($"[TwineImporter] 解析到 {passages.Count} 个段落:");
        foreach (var p in passages)
        {
            Debug.Log($"  [{p.name}] \"{Truncate(p.dialogueText, 50)}\" links={p.links.Count} speaker=\"{p.speakerName}\" tags=\"{p.tags}\"");
        }

        // 创建 DialogueNode 资产
        List<string> createdPaths = CreateDialogueAssets(passages);

        AssetDatabase.Refresh();

        // 选中输出文件夹
        var folderAsset = AssetDatabase.LoadAssetAtPath<Object>(outputFolder);
        if (folderAsset != null)
        {
            Selection.activeObject = folderAsset;
            EditorGUIUtility.PingObject(folderAsset);
        }

        EditorUtility.DisplayDialog("导入完成",
            $"成功导入 {createdPaths.Count} 个对话节点\n\n" +
            $"输出目录: {outputFolder}\n\n" +
            $"节点列表:\n" +
            string.Join("\n", createdPaths.ConvertAll(p => "  " + Path.GetFileNameWithoutExtension(p))),
            "确定");
    }

    // ============================================================
    // HTML 解析
    // ============================================================

    private List<TwinePassage> ParsePassages(string html)
    {
        var passages = new List<TwinePassage>();

        // 解析 <tw-passagedata> 元素（属性顺序可能变化，用多个前瞻匹配）
        string pattern = @"<tw-passagedata\s+[^>]*?pid=""([^""]*)""[^>]*?name=""([^""]*)""[^>]*?tags=""([^""]*)""[^>]*?>(.*?)</tw-passagedata>";
        var matches = Regex.Matches(html, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var passage = new TwinePassage
            {
                pid    = match.Groups[1].Value,
                name   = match.Groups[2].Value.Trim(),
                tags   = match.Groups[3].Value.Trim(),
                rawText = match.Groups[4].Value
            };

            // 解码 HTML 实体 (&amp; &lt; &gt; &#xxx; 等)
            passage.rawText = System.Net.WebUtility.HtmlDecode(passage.rawText);

            // 去掉 HTML 标签（Twine 文本中可能内嵌简单标签如 <br> <i> 等）
            passage.rawText = Regex.Replace(passage.rawText, @"<[^>]+>", "");

            ParseLinks(passage);
            ParseSpeakerAndText(passage);
            ParseEndAction(passage);

            passages.Add(passage);
        }

        return passages;
    }

    /// <summary>
    /// 解析 Twine 链接语法:
    ///   [[显示文字->目标段落]]
    ///   [[显示文字|目标段落]]    (备用语法)
    ///   [[目标段落]]             (显示文字 = 目标名称)
    /// </summary>
    private void ParseLinks(TwinePassage passage)
    {
        string text = passage.rawText;

        // 找到所有 [[...]] 块
        string linkPattern = @"\[\[(.+?)\]\]";
        var matches = Regex.Matches(text, linkPattern);

        foreach (Match match in matches)
        {
            string inner = match.Groups[1].Value.Trim();

            string displayText;
            string targetName;

            if (inner.Contains("->"))
            {
                var parts = inner.Split(new[] { "->" }, 2, System.StringSplitOptions.None);
                displayText = parts[0].Trim();
                targetName = parts[1].Trim();
            }
            else if (inner.Contains("|"))
            {
                var parts = inner.Split(new[] { '|' }, 2, System.StringSplitOptions.None);
                displayText = parts[0].Trim();
                targetName = parts[1].Trim();
            }
            else
            {
                // [[target]] — 显示文字与目标名相同
                displayText = inner;
                targetName = inner;
            }

            passage.links.Add(new TwineLink
            {
                displayText = displayText,
                targetName = targetName
            });
        }

        // 生成清洁文本（去掉所有 [[...]] 链接标记）
        passage.cleanText = Regex.Replace(text, @"\[\[.+?\]\]", "").Trim();
        passage.cleanText = Regex.Replace(passage.cleanText, @"\s+", " ").Trim();
    }

    /// <summary>
    /// 可选约定: "说话人: 对话内容" 或 "说话人：对话内容"
    /// 只在名字较短时（≤15字符）才视为说话人名，避免误匹配
    /// </summary>
    private void ParseSpeakerAndText(TwinePassage passage)
    {
        string text = passage.cleanText;

        if (string.IsNullOrEmpty(text))
        {
            passage.speakerName = "";
            passage.dialogueText = "";
            return;
        }

        // 匹配 "名字: 内容" 或 "名字：内容"（中文/英文冒号）
        var speakerMatch = Regex.Match(text, @"^(.+?)[：:]\s*(.*)");
        if (speakerMatch.Success && speakerMatch.Groups[1].Value.Trim().Length <= 15)
        {
            passage.speakerName = speakerMatch.Groups[1].Value.Trim();
            passage.dialogueText = speakerMatch.Groups[2].Value.Trim();
        }
        else
        {
            passage.speakerName = "";
            passage.dialogueText = text;
        }
    }

    /// <summary>
    /// 解析 tags 中的特殊指令:
    ///   load:SceneName  → 对话结束后加载场景
    ///   return          → 对话结束后返回上一场景
    /// </summary>
    private void ParseEndAction(TwinePassage passage)
    {
        if (string.IsNullOrEmpty(passage.tags))
            return;

        string[] tagArray = passage.tags.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string tag in tagArray)
        {
            if (tag.StartsWith("load:"))
                passage.endActionTag = tag;   // e.g. "load:Fight"
            else if (tag == "return")
                passage.endActionTag = "return";
        }
    }

    // ============================================================
    // 资产创建
    // ============================================================

    private List<string> CreateDialogueAssets(List<TwinePassage> passages)
    {
        var createdPaths = new List<string>();
        var nodeMap   = new Dictionary<string, DialogueNode>();  // passageName → node
        var nodePaths = new Dictionary<string, string>();         // passageName → assetPath

        // ---- 第一遍: 创建所有 ScriptableObject 资产（尚无引用） ----
        foreach (var passage in passages)
        {
            var node = ScriptableObject.CreateInstance<DialogueNode>();
            node.text = passage.dialogueText;
            node.speakerName = passage.speakerName;

            // 生成合法的文件名
            string fileName = SanitizeFileName(passage.name);
            string assetPath = $"{outputFolder}/{fileName}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(node, assetPath);
            nodeMap[passage.name]   = node;
            nodePaths[passage.name] = assetPath;
            createdPaths.Add(assetPath);
        }

        // ---- 第二遍: 设置节点间的引用 (nextNode / options) ----
        foreach (var passage in passages)
        {
            var node = nodeMap[passage.name];

            if (passage.links.Count == 0)
            {
                // 无链接 → 对话结束节点
                ApplyEndAction(node, passage);
            }
            else if (passage.links.Count == 1)
            {
                // 单个链接 → 自动推进 (nextNode)
                var targetName = passage.links[0].targetName;
                if (nodeMap.TryGetValue(targetName, out var targetNode))
                {
                    node.nextNode = targetNode;
                }
                else
                {
                    Debug.LogWarning($"[TwineImporter] 段落 \"{passage.name}\" 链接到不存在的目标 \"{targetName}\"，将作为结束节点");
                    ApplyEndAction(node, passage);
                }
            }
            else
            {
                // 多个链接 → 玩家选项 (options)
                node.options = new List<DialogueOption>();
                foreach (var link in passage.links)
                {
                    var option = new DialogueOption
                    {
                        optionText = link.displayText
                    };

                    if (nodeMap.TryGetValue(link.targetName, out var targetNode))
                    {
                        option.nextNode = targetNode;
                    }
                    else
                    {
                        Debug.LogWarning($"[TwineImporter] 段落 \"{passage.name}\" 的选项 \"{link.displayText}\" 链接到不存在的目标 \"{link.targetName}\"");
                    }

                    node.options.Add(option);
                }
            }

            EditorUtility.SetDirty(node);
        }

        AssetDatabase.SaveAssets();
        return createdPaths;
    }

    /// <summary>
    /// 应用结束行为（从 Twine tags 映射到 DialogueNode endAction）
    /// </summary>
    private void ApplyEndAction(DialogueNode node, TwinePassage passage)
    {
        if (string.IsNullOrEmpty(passage.endActionTag))
            return;

        if (passage.endActionTag.StartsWith("load:"))
        {
            node.endAction = DialogueEndAction.LoadScene;
            node.endActionSceneName = passage.endActionTag.Substring(5);
        }
        else if (passage.endActionTag == "return")
        {
            node.endAction = DialogueEndAction.ReturnToPrevious;
        }
    }

    // ============================================================
    // 辅助方法
    // ============================================================

    /// <summary>
    /// 确保 Assets 下的文件夹路径存在，不存在则逐级创建
    /// </summary>
    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }
            currentPath = nextPath;
        }
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    private string SanitizeFileName(string name)
    {
        string sanitized = Regex.Replace(name, @"[<>:""/\\|?*]", "_");
        sanitized = sanitized.Trim().Trim('.');
        if (string.IsNullOrEmpty(sanitized))
            sanitized = "Untitled";
        return sanitized;
    }

    private string Truncate(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return "(空)";
        return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "...";
    }
}
