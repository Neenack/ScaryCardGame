using System;
using UnityEngine;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    private bool canInteract = true;


    [SerializeField] private string interactableText = "Interact";
    public string GetText() => interactableText;
    public void SetText(string text) => interactableText = text;


    public event Action<PlayerData> OnButtonPressed;

    public bool CanInteract() => canInteract;
    public void Interact(PlayerData player, PlayerInteractor interactor)
    {
        OnButtonPressed?.Invoke(player);
    }

    public void SetInteractable(bool interact) => canInteract = interact;
}
