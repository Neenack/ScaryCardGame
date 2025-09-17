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

    protected IEnumerator DealCardToPlayer(T player)
    {
        // Draw a new card
        PlayingCardSO newCardSO = deck.DrawCard();
        if (newCardSO == null)
        {
            Debug.LogWarning("Deck is empty!");
            yield break;
        }

        PlayingCard newCard = newCardSO.SpawnCard(cardSpawnTransform);
        player.AddCardToHand(newCard);

        yield return new WaitForSeconds(timeBetweenCardDeals);
    }

    public void PlaceCardOnPile(PlayingCard card, float lerpSpeed = 5f)
    {
        card.SetInteractable(false);

        StartCoroutine(PlaceCardOnPileCoroutine(card, lerpSpeed));
    }

    protected IEnumerator PlaceCardOnPileCoroutine(PlayingCard card, float lerpSpeed = 5f)
    {
        // Determine target position and rotation based on the pile
        Vector3 targetPos = cardPileTransform.position;
        Quaternion targetRot = cardPileTransform.rotation;

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
}
