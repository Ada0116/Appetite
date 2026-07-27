using UnityEngine;

public class InputHandler : MonoBehaviour
{
    void Update()
    {
        // 只在对话活跃时才允许推进
        if (DialogueManager.instance == null) return;
        if (!DialogueManager.instance.IsDialogueActive) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            DialogueManager.instance.AdvanceDialogue();
        }
    }
}
