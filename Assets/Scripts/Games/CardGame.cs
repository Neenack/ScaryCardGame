using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class CardGame<T> : MonoBehaviour, ICardGame where T : TablePlayer
{
    public event Action OnGameStarted;
    public event Action OnGameEnded;

    protected int currentPlayerIndex;
    protected T currentPlayer;

    [Header("Deck Settings")]
    [SerializeField] private CardDeckSO deckSO;
    [SerializeField] protected float timeBetweenCardDeals = 0.5f;
    [SerializeField] private Transform cardSpawnTransform;
    [SerializeField] private Transform cardPileTransform;

    private CardDeck deck;
    public CardDeck Deck => deck;

    [Header("Players at the table")]
    [SerializeField] protected List<T> players = new List<T>();
    public IEnumerable<TablePlayer> GetPlayers() => players;


    protected IInteractable interactableDeck;
    public IInteractable GetCardGameInteractable() => interactableDeck;

    private void Start()
    {
        interactableDeck = GetComponentInChildren<IInteractable>();

        if (interactableDeck != null) interactableDeck.OnInteract += InteractableDeck_OnInteract;
    }

    private void OnDestroy()
    {
        interactableDeck.OnInteract -= InteractableDeck_OnInteract;
    }

    protected void InteractableDeck_OnInteract(object sender, EventArgs e)
    {
        interactableDeck.SetInteractable(false);
        interactableDeck.OnInteract -= InteractableDeck_OnInteract;

        StartGame();
    }

    public virtual void StartGame()
    {
        foreach (var player in players) player.SetGame(this);

        deck = new CardDeck(deckSO);

        currentPlayerIndex = -1;

        ResetHands();

        StartCoroutine(DealInitialCards());

        OnGameStarted?.Invoke();
    }

    protected virtual void EndGame()
    {
        Debug.Log("Game Finished!");

        ResetHands();

        interactableDeck.SetInteractable(true);
        interactableDeck.OnInteract += InteractableDeck_OnInteract;

        OnGameEnded?.Invoke();
    }

    protected virtual void ResetHands()
    {
        // Reset everyone’s hand
        foreach (var player in players)
        {
            player.ResetHand();
        }
    }

    protected abstract IEnumerator DealInitialCards();

    public virtual void NextTurn()
    {
        currentPlayer?.EndTurn();

        currentPlayerIndex++;
        if (currentPlayerIndex >= players.Count)
            currentPlayerIndex = 0;

        currentPlayer = players[currentPlayerIndex];

        // Skip inactive players
        int attempts = 0;
        while (!currentPlayer.IsPlaying() && attempts < players.Count)
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            currentPlayer = players[currentPlayerIndex];
            attempts++;
        }

        if (CheckGameEnd())
        {
            return;
        }

        currentPlayer.StartTurn();
    }

    protected int GetPlayerCount()
    {
        int count = 0;
        foreach (var player in players)
        {
            if (player.IsPlaying()) count++;
        }
        return count;
    }

    protected abstract bool CheckGameEnd();


    #region Card Dealing

    protected PlayingCard DrawNewCard()
    {
        PlayingCardSO newCardSO = deck.DrawCard();
        if (newCardSO == null)
        {
            Debug.LogWarning("Deck is empty!");
            return null;
        }

        return newCardSO.SpawnCard(cardSpawnTransform);
    }

    protected IEnumerator DealCardToPlayerHand(TablePlayer player)
    {
        PlayingCard newCard = DrawNewCard();

        player.AddCardToHand(newCard);

        yield return new WaitForSeconds(timeBetweenCardDeals);
    }

    public virtual void PullNewCard(TablePlayer player) => StartCoroutine(DealCardToPlayerHand(player));

    public void PlaceCardOnPile(PlayingCard card, bool placeFaceDown = false, float lerpSpeed = 5f)
    {
        card.SetInteractable(false);

        StartCoroutine(PlaceCardOnPileCoroutine(card, placeFaceDown, lerpSpeed));
    }

    protected IEnumerator PlaceCardOnPileCoroutine(PlayingCard card, bool placeFaceDown = false, float lerpSpeed = 5f)
    {
        // Determine target position and rotation based on the pile
        Vector3 targetPos = cardPileTransform.position;
        Quaternion targetRot = cardPileTransform.rotation;

        if (!placeFaceDown) targetRot *= Quaternion.Euler(180, 0, 0);

        // Optional: add a slight offset so stacked cards don’t perfectly overlap
        float offsetY = 0.001f * cardPileTransform.childCount;
        targetPos += Vector3.up * offsetY;

        // Move and rotate the card
        card.MoveTo(targetPos, lerpSpeed);
        card.RotateTo(targetRot, lerpSpeed);

        // Optionally parent the card to the pile for organization
        card.transform.SetParent(cardPileTransform);

        yield return new WaitForSeconds(timeBetweenCardDeals);
    }

    #endregion
}
