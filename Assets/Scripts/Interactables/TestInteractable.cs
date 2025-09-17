using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    private bool canInteract;


    [SerializeField] private string interactableText = "Test";
    public string GetText() => interactableText;


    public void SetInteractable(bool interact) => canInteract = interact;
    public bool CanInteract() => canInteract;
    

    public void Interact(PlayerData player, PlayerInteractor interactor)
    {
        Debug.Log("Tried interacting with test interactable");
    }

    public void SetText(string text)
    {
        interactableText = text;
    }
}
