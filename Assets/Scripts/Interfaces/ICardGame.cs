using System;
using System.Collections.Generic;
using UnityEngine;

public interface ICardGame
{
    event Action OnGameStarted;
    event Action OnGameEnded;
    public void StartGame();
    public void NextTurn();
    public IEnumerable<TablePlayer> GetPlayers();
    public void PlaceCardOnPile(PlayingCard card, float lerpSpeed = 5f);
}
