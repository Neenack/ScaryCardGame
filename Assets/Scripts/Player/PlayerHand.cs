using System;
using System.Collections.Generic;
using System.Linq;
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

    public string playerName;
    private List<PlayingCard> cards = new List<PlayingCard>();

    public List<PlayingCard> Cards => cards
        ;
    public int GetTotalValue(bool isAceHigh)
    {
        int total = 0;
        foreach (var card in Cards)
        {
            total += card.GetValue(isAceHigh);
        }
        return total;
    }

    public PlayerHand(string name)
    {
        playerName = name;
    }

    public void AddCard(PlayingCard card)
    {
        if (card == null) return;

        cards.Add(card);
        card.OnShowCard += Card_OnShowCard;

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
