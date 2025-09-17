using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class InteractablePickup : MonoBehaviour, IInteractable
{
    private bool canInteract = true;


    [SerializeField] private string interactableText = "Pickup";
    public string GetText() => interactableText;
    public void SetText(string text) => interactableText = text;


    public void SetInteractable(bool interact) => canInteract = interact;
    public bool CanInteract() => canInteract;
    public void Interact(PlayerData player, PlayerInteractor interactor)
    {
        interactor.PickUpObject(gameObject);
    }
}
