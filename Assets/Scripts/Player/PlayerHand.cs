using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerHand
{
    //public event Action<PlayingCard> OnCardAdded;
    //public event Action<PlayingCard> OnCardRemoved;

    public event Action OnHandUpdated;
    public event Action<PlayingCard> OnShowAnyCard;

    public event Action<PlayingCard> OnAddCard;
    public event Action<PlayingCard> OnRemoveCard;

    public string playerName;
    private List<PlayingCard> cards = new List<PlayingCard>();

    public List<PlayingCard> Cards => cards;

    public PlayerHand(string name)
    {
        playerName = name;
    }

    public PlayerHand Clone(string name)
    {
        PlayerHand clone = new PlayerHand(name);
        foreach (var card in cards)
            clone.Cards.Add(card);
        return clone;
    }

    public void AddCard(PlayingCard card)
    {
        if (card == null) return;

        cards.Add(card);
        card.OnShowCard += Card_OnShowCard;

        OnAddCard?.Invoke(card);
        OnHandUpdated?.Invoke();
    }

    public void InsertCard(PlayingCard card, int index)
    {
        if (card == null) return;

        cards.Insert(index, card);
        card.OnShowCard += Card_OnShowCard;

        OnAddCard?.Invoke(card);
        OnHandUpdated?.Invoke();
    }

    public bool RemoveCard(PlayingCard cardToRemove)
    {
        if (cards.Count == 0 || cardToRemove == null)
            return false;

        // Find the first matching card by instance or CardSO
        var foundCard = cards.FirstOrDefault(c => c == cardToRemove || c.CardSO == cardToRemove.CardSO);

        if (foundCard == null)
            return false;

        foundCard.OnShowCard -= Card_OnShowCard;
        cards.Remove(foundCard);

        OnRemoveCard?.Invoke(foundCard);
        OnHandUpdated?.Invoke();

        return true;
    }

    public void ClearHand()
    {
        foreach (var card in cards)
        {
            card.OnShowCard -= Card_OnShowCard;
            GameObject.Destroy(card.gameObject);
        }

        cards.Clear();

        OnHandUpdated?.Invoke();
    }

    public void UpdateHand()
    {
        OnHandUpdated?.Invoke();
    }

    public void ShowCards()
    {
        foreach (var card in cards)
        {
            card.FlipCard();
        }
    }

    public void ShowCard(int index)
    {
        if (index < 0 || index >= cards.Count) return;

        cards[index].FlipCard();
    }

    public PlayingCard GetCard(int index)
    {
        if (index < 0 || index >= cards.Count) return null;

        return cards[index];
    }

    public int GetIndexOfCard(PlayingCard card) => cards.IndexOf(card);

    public PlayingCard GetRandomCard()
    {
        return GetCard(UnityEngine.Random.Range(0, cards.Count));
    }

    private void Card_OnShowCard(PlayingCard card)
    {
        OnShowAnyCard?.Invoke(card);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{playerName}'s Hand:");

        foreach (var card in cards)
        {
            if (card != null)
            {
                sb.AppendLine(card.ToString());
            }
        }

        return sb.ToString();
    }
}
