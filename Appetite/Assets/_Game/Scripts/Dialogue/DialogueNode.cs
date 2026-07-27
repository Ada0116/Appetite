using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "对话系统/对话文件")]
public class DialogueNode : ScriptableObject
{
    [TextArea(3, 10)] public string text;            // 对话内容
    public string speakerName;                        // 说话者名字
    public Sprite speakerIcon;                        // 说话者头像（暂时不用）
    public List<DialogueOption> options;              // 选项列表

    public bool isAutoNext = true;                    // 是否点击继续到下一句
    public DialogueNode nextNode;                     // 普通下一句（无选项时）

    [Header("对话结束行为")]
    public DialogueEndAction endAction = DialogueEndAction.None;
    public string endActionSceneName;                 // 当 endAction 为 LoadScene 时，目标场景名
}

public enum DialogueEndAction
{
    None,               // 不做任何事
    LoadScene,          // 加载指定场景
    ReturnToPrevious    // 返回上一个场景
}

[System.Serializable]
public class DialogueOption
{
    public string optionText;           // 选项显示文字
    public DialogueNode nextNode;       // 选择后跳转的节点
    public int hungerChange;            // 饥饿值变化（可选）
}