using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class GameStartInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ICardGame game;

    private bool isGameRunning = false;
    private bool canInteract = true;

    public void SetInteractable(bool interact) => canInteract = interact;
    public bool CanInteract() => !isGameRunning && canInteract;


    [SerializeField] private string interactableText = "Start";
    public string GetText() => interactableText;
    public void SetText(string text) => interactableText = text;

    private void Start()
    {
        game = GetComponentInParent<ICardGame>();

        game.OnGameStarted += Game_OnGameStarted;
        game.OnGameEnded += Game_OnGameFinished;
    }

    private void Game_OnGameStarted()
    {
        isGameRunning = true;
    }

    private void Game_OnGameFinished()
    {
        isGameRunning = false;
    }

    public void Interact(PlayerData player, PlayerInteractor interactor)
    {
        game.StartGame();
    }
}
