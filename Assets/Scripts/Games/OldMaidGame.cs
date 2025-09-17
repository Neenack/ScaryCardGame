using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class OldMaidGame : CardGame<OldMaidPlayer>
{
    private void Player_OnNoCardsLeft(OldMaidPlayer p)
    {
        //UPDATE ALL TAKE FROM PLAYERS
        foreach (var player in players)
        {
            player.SetPreviousPlayer(players);
        }
    }

    protected override IEnumerator DealInitialCards()
    {
        int playerIndex = 0;
        int cardCount = Deck.Count;

        for (int i = 0; i < cardCount; i++)
        {
            yield return StartCoroutine(DealCardToPlayer(players[playerIndex]));

            playerIndex++;
            if (playerIndex >= players.Count) playerIndex = 0;
        }

        RemoveAllPairs();

        foreach (var player in players)
        {
            player.SetPreviousPlayer(players);
            player.OnNoCardsLeft += Player_OnNoCardsLeft;
        }

        yield return new WaitForSeconds(1f);

        NextTurn();
    }

    protected override bool CheckGameEnd()
    {
        if (GetPlayerCount() <= 1)
        {
            EndGame();
            return true;
        }

        return false;
    }

    protected override void EndGame()
    {
        foreach (var player in players) player.OnNoCardsLeft -= Player_OnNoCardsLeft;

        OldMaidPlayer loser = players.FirstOrDefault(p => p.IsPlaying());

        Debug.Log($"{loser.GetName()} loses!");

        base.EndGame();
    }

    private void RemoveAllPairs()
    {
        foreach (var player in players)
        {
            List<PlayingCard> cards = player.GetAllPairs();

            foreach (var card in cards)
            {
                if (player.RemoveCardFromHand(card))
                {
                    PlaceCardOnPile(card);
                }
            }
        }
    }
}
