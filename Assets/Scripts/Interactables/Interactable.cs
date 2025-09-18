using System;
using UnityEngine;

public class Interactable : MonoBehaviour, IInteractable
{
    public event EventHandler OnInteract;

    public class InteractEventArgs : EventArgs
    {
        public PlayerData playerData;

        public InteractEventArgs(PlayerData player) { playerData = player; }
    }


    [SerializeField] private string interactableText = "Interact";

    private bool canInteract = true;


    public string GetText() => interactableText;
    public void SetText(string text) => interactableText = text;

    public bool CanInteract() => canInteract;
    public void Interact(PlayerData player, PlayerInteractor interactor)
    {
        OnInteract?.Invoke(this, new InteractEventArgs(player));
    }


    public void SetInteractable(bool interact) => canInteract = interact;
}
