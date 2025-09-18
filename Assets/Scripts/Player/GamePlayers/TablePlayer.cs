using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class TablePlayer : MonoBehaviour
{
    [SerializeField] private PlayerData playerData = null;

    private PlayerHand hand;
    private ICardGame game;
    protected IInteractable interactableDeck;

    [Header("Card Positions")]
    [SerializeField] protected Vector3 cardSpacing = new Vector3(0.3f, 0, 0);
    [SerializeField] protected float yOffset = 0;
    [SerializeField] private float fanAngle = 0f;
    [SerializeField] private float dealingXRotation = 110f;

    protected bool isPlayerTurn = false;

    public PlayerHand Hand => hand;
    public ICardGame Game => game;
    public bool IsAI => playerData == null;

    public abstract bool IsPlaying();


    public void SetGame(ICardGame game)
    {
        this.game = game;
        interactableDeck = game.GetCardGameInteractable();

        game.OnGameStarted += Game_OnGameStarted;
        game.OnGameEnded += Game_OnGameEnded;
    }

    protected abstract void Game_OnGameEnded();
    protected abstract void Game_OnGameStarted();

    protected virtual void OnDestroy()
    {
        if (hand != null)
        {
            hand.OnShowAnyCard -= Hand_OnShowAnyCard;
            hand.OnHandUpdated -= Hand_OnHandUpdated;
        }
    }

    public string GetName()
    {
        if (playerData) return playerData.GetName();
        return gameObject.name;
    }

    #region Card Hand

    public void AddCardToHand(PlayingCard card)
    {
        hand.AddCard(card);
    }

    public void InsertCardToHand(PlayingCard card, int index)
    {
        hand.InsertCard(card, index);
    }

    public bool RemoveCardFromHand(PlayingCard card)
    {
        return hand.RemoveCard(card);
    }

    public virtual void ResetHand()
    {
        if (hand == null)
        {
            hand = new PlayerHand(name);
            hand.OnShowAnyCard += Hand_OnShowAnyCard;
            hand.OnHandUpdated += Hand_OnHandUpdated;
        }

        hand.ClearHand();
    }

    public void UpdateHand() => Hand.UpdateHand();

    protected virtual void Hand_OnShowAnyCard(PlayingCard obj) { }
    protected virtual void Hand_OnHandUpdated()
    {
        RecentreCards();
    }

    public void RecentreCards(float lerpSpeed = 5f)
    {
        int totalCards = hand.Cards.Count;

        for (int i = 0; i < totalCards; i++)
        {
            PlayingCard card = hand.Cards[i];

            // Get target position and rotation from the player
            Vector3 targetPos = GetCardPosition(i, totalCards);
            Quaternion targetRot = GetCardRotation(i, totalCards);

            // Move and rotate the card smoothly
            card.MoveTo(targetPos, lerpSpeed);
            card.RotateTo(targetRot, lerpSpeed);
        }
    }

    public virtual void SortHand()
    {
        PlayerHand sortedHand = new PlayerHand(name);

        List<PlayingCard> sortedCards = new List<PlayingCard>(hand.Cards);
        sortedCards.Sort();

        foreach (var card in sortedCards)
        {
            sortedHand.AddCard(card);
        }

        hand = sortedHand;
    }

    public virtual Vector3 GetCardPosition(int cardIndex, int totalCards)
    {
        // Centered base position at this transform
        Vector3 basePos = transform.position + new Vector3(0, yOffset, 0);

        // Spread cards evenly around the middle
        float offsetFactor = cardIndex - (totalCards - 1) / 2f;

        // Offset sideways using local right
        Vector3 sideOffset = transform.right * offsetFactor * cardSpacing.x;
        Vector3 upOffset = transform.up * offsetFactor * cardSpacing.y;
        Vector3 forwardOffset = transform.forward * offsetFactor * cardSpacing.z;

        return basePos + forwardOffset + upOffset + sideOffset;
    }

    public virtual Quaternion GetCardRotation(int cardIndex, int totalCards)
    {
        // How much to angle cards apart (tweak this in inspector)
        float angleStep = fanAngle / Mathf.Max(1, totalCards - 1);

        // Rotate around local up axis, centered on middle card
        float angle = (cardIndex - (totalCards - 1) / 2f) * angleStep;

        return transform.rotation * Quaternion.Euler(dealingXRotation, angle, 0f);
    }

    #endregion

    public virtual void StartTurn()
    {
        Debug.Log($"It is {GetName()}'s turn!");

        isPlayerTurn = true;
    }

    public virtual void EndTurn() 
    {
        isPlayerTurn = false;
    }

}
