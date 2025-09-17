using UnityEngine;

public interface IInteractable
{
    public bool CanInteract();
    public void SetInteractable(bool interact);
    public void Interact(PlayerData player, PlayerInteractor interactor);
    public string GetText();
    public void SetText(string text);
}
