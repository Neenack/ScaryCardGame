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

    private List<PlayingCard> cardPile = new List<PlayingCard>();

    private void Start()
    {
        interactableDeck = GetComponentInChildren<IInteractable>();

        if (interactableDeck != null) interactableDeck.OnInteract += InteractableDeck_OnInteract;
    }

    private void OnDestroy()
    {
        if (interactableDeck != null) interactableDeck.OnInteract -= InteractableDeck_OnInteract;
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

        foreach (var card in cardPile) Destroy(card.gameObject);
        cardPile.Clear();

        interactableDeck.SetInteractable(true);
        interactableDeck.OnInteract += InteractableDeck_OnInteract;

        OnGameEnded?.Invoke();
    }

    protected virtual void CheckWinner()
    {
        EndGame();
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

        // If no players left, end game
        if (attempts >= players.Count || !currentPlayer.IsPlaying() || CheckGameEnd())
        {
            CheckWinner();
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

    protected PlayingCard DealCardToPlayerHand(TablePlayer player)
    {
        PlayingCard newCard = DrawNewCard();

        player.AddCardToHand(newCard);

        return newCard;
    }

    public virtual PlayingCard PullNewCard(TablePlayer player) => DealCardToPlayerHand(player);

    public virtual void PlaceCardOnPile(PlayingCard card, bool placeFaceDown = false, float lerpSpeed = 5f)
    {
        card.SetInteractable(false);

        StartCoroutine(PlaceCardOnPileCoroutine(card, placeFaceDown, lerpSpeed));
    }

    public PlayingCard GetTopPileCard()
    {
        if (cardPile.Count == 0) return null;

        return cardPile[cardPile.Count - 1];
    }

    protected IEnumerator PlaceCardOnPileCoroutine(PlayingCard card, bool placeFaceDown = false, float lerpSpeed = 5f)
    {
        // Determine target position and rotation based on the pile
        Vector3 targetPos = cardPileTransform.position;
        Quaternion targetRot = cardPileTransform.rotation;

        if (!placeFaceDown) targetRot *= Quaternion.Euler(180, 0, 0);

        // Optional: add a slight offset so stacked cards don’t perfectly overlap
        float offsetY = 0.0025f * cardPileTransform.childCount;
        targetPos += Vector3.up * offsetY;

        // Move and rotate the card
        card.MoveTo(targetPos, lerpSpeed);
        card.RotateTo(targetRot, lerpSpeed);

        // Optionally parent the card to the pile for organization
        card.transform.SetParent(cardPileTransform);

        cardPile.Add(card);

        yield return new WaitForSeconds(timeBetweenCardDeals);
    }

    #endregion

    #region Helper Functions
    protected void SwapHands(TablePlayer player1, TablePlayer player2)
    {
        List<PlayingCard> tempList = new List<PlayingCard>();

        PlayerHand currentPlayerHand = player1.Hand;
        PlayerHand otherPlayerHand = player2.Hand;

        //REMOVE ALL CARDS FROM OTHER HAND AND ADD THEM TO TEMP LIST
        int cardCount = otherPlayerHand.Cards.Count;
        for (int i = 0; i < cardCount; i++)
        {
            PlayingCard card = otherPlayerHand.GetCard(0);

            if (otherPlayerHand.RemoveCard(card))
            {
                tempList.Add(card);
            }
        }

        //SWAP ALL CARDS FROM CURRENT HAND TO OTHER HAND
        cardCount = currentPlayerHand.Cards.Count;
        for (int i = 0; i < cardCount; i++)
        {
            PlayingCard card = currentPlayerHand.GetCard(0);

            if (currentPlayerHand.RemoveCard(card))
            {
                otherPlayerHand.AddCard(card);
            }
        }

        //ADD ALL TEMP CARDS BACK
        foreach (var card in tempList) currentPlayerHand.AddCard(card);
    }

    protected void SwapCards(TablePlayer player1, PlayingCard card1, TablePlayer player2, PlayingCard card2)
    {
        if (card1 != null)
        {
            // Get indexes
            int playerCardIndex = player1.Hand.GetIndexOfCard(card1);
            int otherCardIndex = player2.Hand.GetIndexOfCard(card2);

            // Remove from hands
            if (player1.Hand.RemoveCard(card1) &&
                player2.Hand.RemoveCard(card2))
            {
                // Insert into opposite hands, keeping slot position
                player1.Hand.InsertCard(card2, playerCardIndex);
                player2.Hand.InsertCard(card1, otherCardIndex);
            }
        }
        else
        {
            Debug.LogWarning("Could not find player card!");
        }
    }

    protected TablePlayer GetPlayerWithCard(PlayingCard card)
    {
        foreach (var player in players)
        {
            foreach (var cardToCheck in player.Hand.Cards)
            {
                if (cardToCheck.CardSO == card.CardSO)
                {
                    return player;
                }
            }
        }

        Debug.LogWarning("Could not find player with card: " + card.ToString());
        return null;
    }

    #endregion
}
