using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public class CambioPlayer : TablePlayer
{
    [Header("Cambio Settings")]
    [SerializeField] private Interactable callRoundButton;
    [SerializeField] private Interactable skipAbilityButton;
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private float rowSpacing = 2.5f;

    private bool hasCalledGame = false;
    public bool HasCalledGame => hasCalledGame;

    private HashSet<PlayingCard> seenCards = new HashSet<PlayingCard>();


    private void Start()
    {
        callRoundButton.gameObject.SetActive(false);
        skipAbilityButton.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
    }

    protected override void Game_OnGameStarted()
    {
        hasCalledGame = false;
        scoreText.gameObject.SetActive(false);
        skipAbilityButton.gameObject.SetActive(false);

        Hand.OnRemoveCard += Hand_OnRemoveCard;
    }

    protected override void Game_OnGameEnded()
    {
        Hand.OnRemoveCard -= Hand_OnRemoveCard;
    }

    public override bool IsPlaying()
    {
        return !hasCalledGame;
    }

    public override int GetCardValue(PlayingCard card)
    {
        int value = card.GetValue(false);

        if (value == 13 && (card.Suit == Suit.Spades || card.Suit == Suit.Clubs))
        {
            return -1;
        }

        return value;
    }

    public override Vector3 GetCardPosition(int cardIndex, int totalCards)
    {
        // Grid arrangement: 2 rows (bottom + top)
        int column = cardIndex / 2;   // every 2 cards start a new column
        int row = cardIndex % 2;      // 0 = bottom row, 1 = top row

        // Base position (centered at player transform, respecting table height)
        Vector3 basePos = transform.position + new Vector3(0, yOffset, 0);

        // How many columns do we need total?
        int totalColumns = Mathf.CeilToInt(totalCards / 2f);

        // Offset so that cards are centered around transform.position
        float centerOffset = (totalColumns - 1) / 2f;

        // Offsets
        Vector3 sideOffset = transform.right * ((column - centerOffset) * cardSpacing.x);
        Vector3 rowOffset = -transform.forward * (row * rowSpacing);

        return basePos + sideOffset + rowOffset;
    }

    public override void StartTurn()
    {
        base.StartTurn();

        skipAbilityButton.gameObject.SetActive(false);

        if (!IsAI)
        {
            interactableDeck.SetInteractable(true);
            interactableDeck.OnInteract += InteractableDeck_OnInteract;

            callRoundButton.gameObject.SetActive(true);
            callRoundButton.OnInteract += CallRoundButton_OnInteract;
        }
        else
        {
            Game.PullNewCard(this);
        }
    }

    private void CallRoundButton_OnInteract(object sender, System.EventArgs e)
    {
        hasCalledGame = true;
        Game.NextTurn();
    }

    private void InteractableDeck_OnInteract(object sender, System.EventArgs e)
    {
        Game.PullNewCard(this);

        interactableDeck.SetInteractable(false);
        interactableDeck.OnInteract -= InteractableDeck_OnInteract;

        callRoundButton.gameObject.SetActive(false);
        callRoundButton.OnInteract -= CallRoundButton_OnInteract;
    }

    public override void EndTurn()
    {
        base.EndTurn();

        interactableDeck.SetInteractable(false);
        interactableDeck.OnInteract -= InteractableDeck_OnInteract;

        callRoundButton.gameObject.SetActive(false);
        callRoundButton.OnInteract -= CallRoundButton_OnInteract;

        skipAbilityButton.gameObject.SetActive(false);
        skipAbilityButton.OnInteract -= SkipAbilityButton_OnInteract;

        foreach (var card in Hand.Cards)
        {
            card.SetInteractable(false);
        }

        foreach (var player in Game.GetPlayers())
        {
            if (!player.IsPlaying())
            {
                hasCalledGame = true;
                break;
            }
        }
    }

    public void EnableSkipAbilityBtn()
    {
        skipAbilityButton.gameObject.SetActive(true);
        skipAbilityButton.SetInteractable(true);

        skipAbilityButton.OnInteract += SkipAbilityButton_OnInteract;
    }

    private void SkipAbilityButton_OnInteract(object sender, System.EventArgs e)
    {
        Game.NextTurn();
    }

    public void SetHandInteractable(bool interactable)
    {
        foreach (var card in Hand.Cards)
        {
            card.SetInteractable(interactable);
        }
    }

    public int GetScore()
    {
        int total = 0;

        foreach (var card in Hand.Cards)
        {
            total += GetCardValue(card);
        }

        return total;
    }

    public void SetScoreText(string text)
    {
        scoreText.gameObject.SetActive(true);
        scoreText.text = text;
    }

    #region Memory

    private void Hand_OnRemoveCard(PlayingCard card)
    {
        RemoveSeenCard(card);
    }

    public void AddSeenCard(PlayingCard card)
    {
        seenCards.Add(card);
    }

    public bool HasSeenCard(PlayingCard card)
    {
        return seenCards.Contains(card);
    }

    public void RemoveSeenCard(PlayingCard card)
    {
        seenCards.Remove(card);
    }

    #endregion


    #region AI

    private PlayingCard GetHighestSeenCard() => seenCards.OrderByDescending(c => GetCardValue(c)).FirstOrDefault();

    /// <summary>
    /// Asks the AI if it should discard a card
    /// </summary>
    /// /// <param name="card">The card to compare with</param>
    /// <returns>True if it does not want the card</returns>
    public bool ShouldDiscardCard(PlayingCard card)
    {
        //If card is above 5 return true
        if (GetCardValue(card) > 5)
        {
            return true;
        }

        //If AI has not seen all of its cards return false
        if (seenCards.Count < Hand.Cards.Count)
        {
            return false;
        }

        //If card is larger than all seen cards return true
        if (GetCardValue(GetHighestSeenCard()) < GetCardValue(card))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Chooses a card the AI would most like to swap out
    /// </summary>
    /// <returns>The index of the card to remove</returns>
    public int GetIndexToSwap()
    {
        if (seenCards.Count == 0)
        {
            return UnityEngine.Random.Range(0, Hand.Cards.Count);
        }

        PlayingCard highestCard = GetHighestSeenCard();

        //If highest card is above 9 or has seen all the cards then trade it out
        if (GetCardValue(highestCard) > 9)
        {
            return Hand.GetIndexOfCard(highestCard);
        }

        //Otherwise replace an unknown card
        List<PlayingCard> unseenCards = Hand.Cards.Where(c => !seenCards.Contains(c)).ToList();

        //If it has seen all the cards, get rid of the highest one
        if (unseenCards.Count == 0)
        {
            return Hand.GetIndexOfCard(highestCard);
        }

        return Hand.GetIndexOfCard(unseenCards[UnityEngine.Random.Range(0, unseenCards.Count)]);
    }

    /// <summary>
    /// Asks the AI what index card it would like to look at
    /// </summary>
    /// <returns>An index of a card that it has not seen</returns>
    public int GetIndexToLookAt()
    {
        //IF PLAYER KNOWS ALL CARDS
        if (seenCards.Count == Hand.Cards.Count)
        {
            return UnityEngine.Random.Range(0, Hand.Cards.Count);
        }

        List<PlayingCard> unseenCards = Hand.Cards.Where(c => !seenCards.Contains(c)).ToList();

        //If it has seen all the cards, look at a random card
        if (unseenCards.Count == 0)
        {
            return UnityEngine.Random.Range(0, Hand.Cards.Count);
        }

        return Hand.GetIndexOfCard(unseenCards[UnityEngine.Random.Range(0, unseenCards.Count)]);
    }

    /// <summary>
    /// Asks the AI if it would like to swap hands
    /// </summary>
    /// <returns>True if the player would like to swap hands</returns>
    public bool ShouldSwapHand()
    {
        return (seenCards.Count < Hand.Cards.Count / 2) && GetScore() > 10;
    }

#endregion
}
