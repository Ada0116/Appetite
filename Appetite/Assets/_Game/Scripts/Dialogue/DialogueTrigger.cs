using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueNode startNode;
    public bool triggerOnStart = false;

    void Start()
    {
        if (triggerOnStart)
        {
            DialogueManager.instance.StartDialogue(startNode);
        }
    }
}