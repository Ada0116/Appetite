using UnityEngine;

public class InputHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            if (DialogueManager.instance != null)
                DialogueManager.instance.AdvanceDialogue();
        }
    }
}