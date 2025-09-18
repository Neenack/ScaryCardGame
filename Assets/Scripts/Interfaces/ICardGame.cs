using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ICardGame
{
    event Action OnGameStarted;
    event Action OnGameEnded;
    public void StartGame();
    public void NextTurn();
    public IEnumerable<TablePlayer> GetPlayers();
    public IInteractable GetCardGameInteractable();

    public PlayingCard PullNewCard(TablePlayer player);
    public void PlaceCardOnPile(PlayingCard card, bool placeFaceDown = false, float lerpSpeed = 5f);
    public PlayingCard GetTopPileCard();
}
