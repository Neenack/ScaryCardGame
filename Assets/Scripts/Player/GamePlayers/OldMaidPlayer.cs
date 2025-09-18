using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class OldMaidPlayer : TablePlayer
{
    public event Action<OldMaidPlayer> OnNoCardsLeft;

    private OldMaidPlayer previousPlayer;

    public override bool IsPlaying()
    {
        if (Hand == null) return true;

        return Hand.Cards.Count > 0;
    }

    public override int GetCardValue(PlayingCard card)
    {
        return 0;
    }

    protected override void Game_OnGameStarted() { }
    protected override void Game_OnGameEnded() { }

    public void SetPreviousPlayer(List<OldMaidPlayer> players)
    {
        if (players == null || players.Count == 0)
        {
            previousPlayer = null;
            return;
        }

        int currentIndex = players.IndexOf(this);
        if (currentIndex == -1)
        {
            previousPlayer = null;
            return; // safety check
        }

        int attempts = 0;
        int index = currentIndex;

        do
        {
            index = (index - 1 + players.Count) % players.Count; // wrap around
            var candidate = players[index];

            if (candidate != null && candidate.IsPlaying())
            {
                previousPlayer = candidate;
                return;
            }

            attempts++;
        } while (attempts < players.Count);

        previousPlayer = null; // no active player found
    }

    public override void StartTurn()
    {
        base.StartTurn();

        DisableInteraction(previousPlayer);
        DisableInteraction(this);

        if (!IsAI)
        {
            // Now safely add new ones
            EnableInteraction(previousPlayer);
        }
        else
        {
            StartCoroutine(AITurn());
        }
    }

    public override void EndTurn()
    {
        base.EndTurn();

        DisableInteraction(previousPlayer);
    }

    private void Card_OnInteract(object sender, EventArgs e)
    {
        PlayingCard card = (PlayingCard)sender;

        StartCoroutine(PlayerTurn(card));
    }

    private PlayingCard TryTakeCardFromPlayer(OldMaidPlayer player, PlayingCard card = null)
    {
        if (card == null) card = player.Hand.GetRandomCard();

        if (player.RemoveCardFromHand(card))
        {
            AddCardToHand(card);
            return card;
        }

        return null;
    }

    private IEnumerator PlayerTurn(PlayingCard chosenCard)
    {
        if (TryTakeCardFromPlayer(previousPlayer, chosenCard) != null)
        {
            yield return new WaitForSeconds(2f);

            CheckNewPair(chosenCard);
        }

        Game.NextTurn();
    }

    private IEnumerator AITurn()
    {
        yield return new WaitForSeconds(3f);

        if (previousPlayer != null && previousPlayer.Hand.Cards.Count > 0)
        {
            PlayingCard card = TryTakeCardFromPlayer(previousPlayer);

            yield return new WaitForSeconds(1f);

            if (card != null) CheckNewPair(card);
        }

        Game.NextTurn();
    }

    private void DisableInteraction(OldMaidPlayer player)
    {
        foreach (var card in player.Hand.Cards)
        {
            card.SetInteractable(false);
            card.OnInteract -= Card_OnInteract;
        }
    }

    private void EnableInteraction(OldMaidPlayer player)
    {
        foreach (var card in player.Hand.Cards)
        {
            card.SetInteractable(true);
            card.OnInteract += Card_OnInteract;
        }
    }
    private void CheckNewPair(PlayingCard newCard)
    {
        List<PlayingCard> toRemove = new List<PlayingCard>();

        foreach (var card in Hand.Cards)
        {
            if (card.CardSO == newCard.CardSO) continue;

            if (card.GetValue(false) == newCard.GetValue(false))
            {
                toRemove.Add(card);
                toRemove.Add(newCard);
                break;
            }
        }

        foreach (var card in toRemove)
        {
            RemoveCardFromHand(card);
            Game.PlaceCardOnPile(card);
        }
    }

    public List<PlayingCard> GetAllPairs()
    {
        List<PlayingCard> pairs = new List<PlayingCard>();
        Dictionary<int, PlayingCard> seenValues = new Dictionary<int, PlayingCard>();

        foreach (var card in Hand.Cards)
        {
            int value = card.GetValue(true); // Ace high

            if (seenValues.TryGetValue(value, out var matchingCard))
            {
                // Found a pair
                pairs.Add(matchingCard);
                pairs.Add(card);

                // Remove the value so we don't match it again
                seenValues.Remove(value);
            }
            else
            {
                // First time seeing this value
                seenValues[value] = card;
            }
        }

        return pairs;
    }

    protected override void Hand_OnHandUpdated()
    {
        base.Hand_OnHandUpdated();

        if (Hand.Cards.Count == 0)
        {
            OnNoCardsLeft?.Invoke(this);
        }
    }
}
