using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

public class CambioGame : CardGame<CambioPlayer>
{
    [Header("Cambio Settings")]
    [SerializeField] private float cardViewingTime = 3f;
    [SerializeField] private Vector3 cardPullPositionOffset = new Vector3(0f, 0.3f, 0f);

    [SerializeField] private float cardLiftHeight = 0.1f;
    [SerializeField] private float cardRevealHeight = 0.2f;

    [SerializeField] private float timeBetweenPlayerReveals = 1f;

    [Header("Stacking Settings")]
    [SerializeField] private bool cardStacking = true;
    [SerializeField] private float stackingTime = 2f;
    private bool isStacking = false;
    private bool hasStacked = false;

    [Header("AI Settings")]
    [SerializeField] private float AIThinkingTime = 1f;

    private Dictionary<PlayingCard, TablePlayer> swapEventDictionary = new Dictionary<PlayingCard, TablePlayer>();
    private PlayingCard drawnCard = null;

    protected override bool CheckGameEnd()
    {
        return false;
    }

    public override void StartGame()
    {
        base.StartGame();

        interactableDeck.SetText("Pull Card");
    }

    protected override void EndGame()
    {
        interactableDeck.SetText("Start Game");
        swapEventDictionary.Clear();

        base.EndGame();
    }

    protected override void CheckWinner()
    {
        StartCoroutine(CheckWinnerRoutine());
    }

    private IEnumerator CheckWinnerRoutine()
    {
        //REVEAL ALL CARDS
        foreach (var player in players)
        {
            foreach (var card in player.Hand.Cards)
            {
                card.FlipCard();
            }

            //GET SCORES
            player.SetScoreText(player.GetScore().ToString());

            yield return new WaitForSeconds(timeBetweenPlayerReveals);
        }

        //LOWEST SCORE WINS
        int lowestScore = int.MaxValue;
        CambioPlayer winner = null;
        foreach (var player in players)
        {
            int score = player.GetScore();

            if (score < lowestScore)
            {
                lowestScore = score;
                winner = player;
            }
        }

        Debug.Log($"{winner.GetName()} wins!");

        yield return new WaitForSeconds(3f);

        //END GAME
        EndGame();
    }

    public override void NextTurn()
    {
        StartCoroutine(NextTurnCoroutine());
    }

    private IEnumerator NextTurnCoroutine()
    {
        foreach (var player in players)
        {
            player.SetHandInteractable(false);
        }

        if (cardStacking && TryEnableStacking())
        {
            isStacking = false;
            hasStacked = false;

            yield return new WaitForSeconds(stackingTime / 2);

            //LET AI TRY STACK
            foreach (var player in players)
            {
                if (!player.IsAI) continue;

                if (player.CanStack())
                {
                    StackCard(player, player.GetCardToStack());
                    break;
                }
            }

            yield return new WaitForSeconds(stackingTime / 2);

            yield return new WaitUntil(() => !isStacking);

            DisableStacking();
        }

        base.NextTurn();
    }

    protected override IEnumerator DealInitialCards()
    {
        for (int i = 0; i < 4; i++)
        {
            foreach (var player in players)
            {
                DealCardToPlayerHand(player);
                yield return new WaitForSeconds(timeBetweenCardDeals);
            }
        }

        yield return new WaitForSeconds(1f);

        foreach (var player in players)
        {
            PlayingCard card1 = player.Hand.GetCard(0);
            PlayingCard card2 = player.Hand.GetCard(2);

            StartCoroutine(RevealSingleCard(card1, player, card1.transform.position));
            StartCoroutine(RevealSingleCard(card2, player, card2.transform.position));
        }

        yield return new WaitForSeconds(cardViewingTime);

        NextTurn();
    }

    public override void PlaceCardOnPile(PlayingCard card, bool placeFaceDown = false, float lerpSpeed = 5f)
    {
        base.PlaceCardOnPile(card, placeFaceDown, lerpSpeed);
    }

    private IEnumerator RevealSingleCard(PlayingCard card, CambioPlayer player, Vector3 basePos, Action OnComplete = null)
    {
        Vector3 originalPos = card.transform.position;
        Quaternion originalRot = card.transform.rotation;

        card.MoveTo(basePos + new Vector3(0, cardRevealHeight, 0), 5f);
        Quaternion targetUpwardsRot = Quaternion.LookRotation(player.transform.forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0);
        card.RotateTo(targetUpwardsRot, 5f);

        yield return new WaitForSeconds(cardViewingTime);

        card.MoveTo(originalPos, 5f);
        card.RotateTo(originalRot, 5f);

        player.AddSeenCard(card);

        OnComplete?.Invoke();
    }

    private void LiftCard(PlayingCard card)
    {
        card.MoveTo(card.transform.position + new Vector3(0, cardLiftHeight, 0), 5f);
    }
    private void BringCardToPlayer(PlayingCard card, CambioPlayer player, float xOffset)
    {
        card.MoveTo(player.transform.position + new Vector3(0, cardRevealHeight, 0) + (player.transform.right * xOffset), 5f);
        Quaternion targetUpwardsRot = Quaternion.LookRotation(player.transform.forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0);
        card.RotateTo(targetUpwardsRot, 5f);
    }

    public override PlayingCard PullNewCard(TablePlayer player)
    {
        // NEEDS TO PICK UP CARD
        // SHOW TO PLAYER
        // LET THEM EITHER CHOOSE ONE OF THERE OWN CARDS OR DECLINE NEW CARD


        drawnCard = DrawNewCard();

        drawnCard.MoveTo(player.transform.position + cardPullPositionOffset, 5f);
        drawnCard.RotateTo(Quaternion.LookRotation(player.transform.forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0), 5f);

        if (!player.IsAI)
        {
            drawnCard.SetInteractable(true);
            drawnCard.OnInteract += DrawnCard_OnInteract;

            foreach (var card in player.Hand.Cards)
            {
                card.SetInteractable(true);
                card.OnInteract += Card_OnInteract;
            }
        }
        else
        {
            StartCoroutine(AIPullCard(player as CambioPlayer));
        }

        return drawnCard;
    }

    private IEnumerator AIPullCard(CambioPlayer aiPlayer)
    {
        yield return new WaitForSeconds(AIThinkingTime);

        if (aiPlayer.Hand.Cards.Count == 0)
        {
            aiPlayer.CallCambio();
            yield break;
        }

        if (aiPlayer.ShouldDiscardCard(drawnCard))
        {
            PlaceCardOnPile(drawnCard);

            yield return new WaitForSeconds(AIThinkingTime);

            DoCardAbility();
        }
        else
        {
            //AI WANTS TO KEEP CARD
            PlayingCard cardToDiscard = aiPlayer.GetCardtoSwap();

            if (TrySwapCards(aiPlayer, drawnCard, cardToDiscard))
            {
                yield return new WaitForSeconds(AIThinkingTime);
            }
            else
            {
                Debug.LogWarning("Failed to swap cards");
            }

            NextTurn();
        }

        yield break;

    }

    //SWAP CARD
    private void Card_OnInteract(object sender, System.EventArgs e)
    {
        PlayingCard cardToDiscard = sender as PlayingCard;
        cardToDiscard.OnInteract -= Card_OnInteract;

        int cardIndex = currentPlayer.Hand.GetIndexOfCard(cardToDiscard);

        TrySwapCards(currentPlayer, drawnCard, cardToDiscard);

        //UNSUBSCRIBE DRAWN CARD
        drawnCard.OnInteract -= DrawnCard_OnInteract;

        //UNSUBSCRIBE FROM CARDS IN HAND
        foreach (var card in currentPlayer.Hand.Cards)
        {
            card.OnInteract -= Card_OnInteract;
        }

        NextTurn();
    }

    private bool TrySwapCards(CambioPlayer player, PlayingCard cardToAdd, PlayingCard cardToDiscard)
    {
        int index = player.Hand.GetIndexOfCard(cardToDiscard);

        if (index == -1)
        {
            Debug.LogWarning($"Index of card {cardToDiscard.ToString()} not found to swap");
            return false;
        }

        if (player.RemoveCardFromHand(cardToDiscard))
        {
            PlaceCardOnPile(cardToDiscard);
            player.InsertCardToHand(cardToAdd, index);
            player.AddSeenCard(cardToAdd);

            return true;
        }

        Debug.LogWarning("Card cannot be swapped as previous card was not removed from hand");
        return false;
    }

    //DISCARD CARD
    private void DrawnCard_OnInteract(object sender, System.EventArgs e)
    {
        //PLACE CARD ON PILE
        PlaceCardOnPile(drawnCard);

        //UNSUBSCRIBE DRAWN CARD
        drawnCard.OnInteract -= DrawnCard_OnInteract;

        //UNSUBSCRIBE FROM CARDS IN HAND
        foreach (var card in currentPlayer.Hand.Cards)
        {
            card.OnInteract -= Card_OnInteract;
        }

        DoCardAbility();
    }

    private void DoCardAbility()
    {
        currentPlayer.EnableSkipAbilityBtn();

        switch (currentPlayer.GetCardValue(drawnCard))
        {
            case < 6:

                Debug.Log("Less than 6, do nothing!");

                NextTurn();
                break;
            case 6:
            case 7:
                Debug.Log("Look at your own card!");

                if (currentPlayer.IsAI)
                {
                    RevealPersonalCardEvent_AI();
                }
                else
                {
                    currentPlayer.SetHandInteractable(true);
                    foreach (var card in currentPlayer.Hand.Cards) card.OnInteract += RevealPersonalCardEvent;
                }

                break;

            case 8:
            case 9:

                Debug.Log("Look at someone elses card!");

                if (currentPlayer.IsAI)
                {
                    RevealOtherPlayerCardEvent_AI();
                }
                else
                {
                    currentPlayer.SetHandInteractable(false);
                    foreach (var player in players)
                    {
                        if (player == currentPlayer) continue;

                        player.SetHandInteractable(true);
                        foreach (var card in player.Hand.Cards) card.OnInteract += RevealOtherPlayerCardEvent;
                    }
                }

                break;

            case 10:
                Debug.Log("Swap entire hands!");

                if (currentPlayer.IsAI)
                {
                    SwapHandEvent_AI();
                }
                else
                {
                    currentPlayer.SetHandInteractable(false);

                    foreach (var player in players)
                    {
                        player.SetHandInteractable(true);
                        foreach (var card in player.Hand.Cards) card.OnInteract += SwapHandsEvent;
                    }
                }

                break;

            case 11:
                Debug.Log("Choose 2 cards to decide to swap");

                if (currentPlayer.IsAI)
                {
                    StartCoroutine(ChooseSwapEvent_AI());
                }
                else
                {
                    currentPlayer.SetHandInteractable(true);
                    foreach (var card in currentPlayer.Hand.Cards) card.OnInteract += ChooseSwapEvent;
                }

                break;

            case 12:
                Debug.Log("Blind swap!");

                if (currentPlayer.IsAI)
                {
                    StartCoroutine(BlindSwapEvent_AI());
                }
                else
                {
                    currentPlayer.SetHandInteractable(true);
                    foreach (var card in currentPlayer.Hand.Cards) card.OnInteract += BlindSwapEvent;
                }

                break;

            case 13:
                Debug.Log("Look at all your cards!");

                RevealWholeHand();

                break;

        }
    }

    #region Player Abilities

    #region Reveal Single Card

    private void RevealPersonalCardEvent(object sender, EventArgs e)
    {
        foreach (var card in currentPlayer.Hand.Cards) card.OnInteract -= RevealPersonalCardEvent;

        PlayingCard cardToReveal = sender as PlayingCard;

        StartCoroutine(RevealSingleCard(cardToReveal, currentPlayer, cardToReveal.transform.position, () => NextTurn()));
    }

    private void RevealOtherPlayerCardEvent(object sender, EventArgs e)
    {
        PlayingCard chosenCard = sender as PlayingCard;
        TablePlayer otherPlayer = GetPlayerWithCard(chosenCard);

        //UNSUBSCRIBE FROM EVENTS AND FIND PLAYER WITH CARD
        foreach (var player in players)
        {
            foreach (var card in player.Hand.Cards)
            {
                card.OnInteract -= RevealOtherPlayerCardEvent;
            }
        }

        StartCoroutine(RevealSingleCard(chosenCard, currentPlayer, currentPlayer.transform.position, () => NextTurn()));
    }

    #endregion

    #region Swap Hand
    private void SwapHandsEvent(object sender, EventArgs e)
    {
        PlayingCard chosenCard = sender as PlayingCard;
        TablePlayer otherPlayer = GetPlayerWithCard(chosenCard);

        //UNSUBSCRIBE FROM EVENTS AND FIND PLAYER WITH CARD
        foreach (var player in players)
        {
            foreach (var card in player.Hand.Cards)
            {
                card.OnInteract -= SwapHandsEvent;
            }
        }

        //CHOSE NOT TO SWAP
        if (otherPlayer == currentPlayer)
        {
            NextTurn();
            return;
        }

        SwapHands(currentPlayer, otherPlayer);

        NextTurn();
    }

    #endregion

    #region Choose 2 Card and Decide which to keep
    private void ChooseSwapEvent(object sender, EventArgs e)
    {
        //CHOOSE 1 OF YOUR OWN CARDS TO SHOW
        //CHOOSE 1 OF SOMEONE ELSES CARDS TO SHOW
        //CHOOSE WHICH ONE TO KEEP
        //THE OTHER CARD GOES TO THE OTHER AVAILABLE SLOT

        PlayingCard chosenPlayerCard = sender as PlayingCard;
        currentPlayer.SetHandInteractable(false);

        //UNSUBSCRIBE FROM PLAYER CARDS
        foreach (var card in currentPlayer.Hand.Cards) card.OnInteract -= ChooseSwapEvent;

        //ADD SWAP TO DICTIONARY
        swapEventDictionary.Add(chosenPlayerCard, currentPlayer);

        //MOVE CARD INFRONT OF PLAYER
        LiftCard(chosenPlayerCard);

        //SET OTHER PLAYES HANDS AS INTERACTABLE AND SUBSCRIBE
        foreach (var player in players)
        {
            if (player == currentPlayer) continue;

            player.SetHandInteractable(true);
            foreach (var card in player.Hand.Cards) card.OnInteract += ChooseOtherPlayerSwapEvent;
        }
    }

    private void ChooseOtherPlayerSwapEvent(object sender, EventArgs e)
    {
        PlayingCard cardChosen = sender as PlayingCard;

        //UNSUBSCRIBE
        foreach (var player in players)
        {
            if (player == currentPlayer) continue;

            foreach (var card in player.Hand.Cards) card.OnInteract -= ChooseOtherPlayerSwapEvent;
        }

        TablePlayer otherPlayer = GetPlayerWithCard(cardChosen);

        //ADD TO DICTIONARY
        swapEventDictionary.Add(cardChosen, otherPlayer);

        BringCardsToPlayerToChoose();
    }

    private void BringCardsToPlayerToChoose()
    {
        float offset = -0.2f;
        float increment = 0.4f;
        //PLAYER CAN CHOOSE WHICH CARD TO KEEP
        foreach (var kvp in swapEventDictionary)
        {
            BringCardToPlayer(kvp.Key, currentPlayer, offset);

            if (!currentPlayer.IsAI)
            {
                kvp.Key.SetInteractable(true);
                kvp.Key.OnInteract += ChooseCardToKeepEvent;
            }

            offset += increment;
        }
    }

    private void ChooseCardToKeepEvent(object sender, EventArgs e)
    {
        //CARD PLAYER WANTS TO KEEP
        PlayingCard chosenCard = sender as PlayingCard;

        ChooseCardToKeep(chosenCard);
    }

    private void ChooseCardToKeep(PlayingCard chosenCard)
    {
        //IF PLAYER CHOSES TO KEEP THEIR OWN CARD NOTHING HAPPENS
        if (swapEventDictionary.TryGetValue(chosenCard, out TablePlayer playerWithCard) && playerWithCard == currentPlayer)
        {
            //CALL UPDATE ON CARDS SO THEY GO BACK TO ORIGINAL POSITION
            foreach (var kvp in swapEventDictionary)
            {
                kvp.Value.UpdateHand();
            }
        }
        else //SWAP CARDS!
        {
            //PLAYER CHOSE THE CARD THAT DOES NOT BELONG TO THEM
            PlayingCard playerCard = null;
            foreach (var kvp in swapEventDictionary)
            {
                if (kvp.Value == currentPlayer)
                {
                    playerCard = kvp.Key;
                    break;
                }
            }

            if (playerCard != null)
            {
                // Get indexes
                int playerCardIndex = currentPlayer.Hand.GetIndexOfCard(playerCard);
                int otherCardIndex = playerWithCard.Hand.GetIndexOfCard(chosenCard);

                // Remove from hands
                if (currentPlayer.Hand.RemoveCard(playerCard) &&
                    playerWithCard.Hand.RemoveCard(chosenCard))
                {
                    // Insert into opposite hands, keeping slot position
                    currentPlayer.Hand.InsertCard(chosenCard, playerCardIndex);
                    playerWithCard.Hand.InsertCard(playerCard, otherCardIndex);
                }
            }
        }

        //REGISTER THE CHOSEN CARD AS SEEN
        currentPlayer.AddSeenCard(chosenCard);

        if (!currentPlayer.IsAI)
        {
            foreach (var kvp in swapEventDictionary)
            {
                kvp.Key.SetInteractable(false);
                kvp.Key.OnInteract -= ChooseCardToKeepEvent;
            }
        }

        swapEventDictionary.Clear();
        NextTurn();
    }

    #endregion

    #region Blind Swap 2 Cards
    private void BlindSwapEvent(object sender, EventArgs e)
    {
        //CHOOSE 1 OF YOUR OWN CARDS
        //CHOOSE 1 OF SOMEONE ELSES CARDS
        //SWAP CARD POSITIONS

        PlayingCard chosenPlayerCard = sender as PlayingCard;
        currentPlayer.SetHandInteractable(false);

        //UNSUBSCRIBE FROM PLAYER CARDS
        foreach (var card in currentPlayer.Hand.Cards) card.OnInteract -= BlindSwapEvent;

        //ADD SWAP TO DICTIONARY
        swapEventDictionary.Add(chosenPlayerCard, currentPlayer);

        //LIFT CARD
        LiftCard(chosenPlayerCard);

        //SET OTHER PLAYES HANDS AS INTERACTABLE AND SUBSCRIBE
        foreach (var player in players)
        {
            if (player == currentPlayer) continue;

            player.SetHandInteractable(true);
            foreach (var card in player.Hand.Cards) card.OnInteract += ChooseOtherPlayerBlindSwapEvent;
        }
    }

    private void ChooseOtherPlayerBlindSwapEvent(object sender, EventArgs e)
    {
        PlayingCard cardChosen = sender as PlayingCard;

        //UNSUBSCRIBE
        foreach (var player in players)
        {
            if (player == currentPlayer) continue;

            foreach (var card in player.Hand.Cards) card.OnInteract -= ChooseOtherPlayerBlindSwapEvent;
        }

        TablePlayer otherPlayer = GetPlayerWithCard(cardChosen);

        //PLAYER SWAP CARDS!
        PlayingCard playerCard = null;
        foreach (var kvp in swapEventDictionary)
        {
            if (kvp.Value == currentPlayer)
            {
                playerCard = kvp.Key;
                break;
            }
        }

        SwapCards(currentPlayer, playerCard, otherPlayer, cardChosen);
        swapEventDictionary.Clear();

        NextTurn();
    }
    #endregion

    #region Reveal Whole Hand
    private void RevealWholeHand()
    {
        for (int i = 0; i < currentPlayer.Hand.Cards.Count; i++)
        {
            PlayingCard card = currentPlayer.Hand.GetCard(i);

            if (i == 0) StartCoroutine(RevealSingleCard(card, currentPlayer, card.transform.position, () => NextTurn()));
            else StartCoroutine(RevealSingleCard(card, currentPlayer, card.transform.position));
        }
    }

    #endregion

    #endregion

    #region AI Abilities

    #region Reveal Single Card

    private void RevealPersonalCardEvent_AI()
    {
        int cardIndex = currentPlayer.GetIndexToLookAt();

        PlayingCard cardToReveal = currentPlayer.Hand.GetCard(cardIndex);

        StartCoroutine(RevealSingleCard(cardToReveal, currentPlayer, cardToReveal.transform.position, () => NextTurn()));
    }

    private void RevealOtherPlayerCardEvent_AI()
    {
        List<CambioPlayer> playersToChoose = players.Where(p => p != currentPlayer).ToList();
        CambioPlayer randomPlayer = playersToChoose[UnityEngine.Random.Range(0, playersToChoose.Count)];
        PlayingCard randomCard = randomPlayer.Hand.GetRandomCard();

        StartCoroutine(RevealSingleCard(randomCard, currentPlayer, currentPlayer.transform.position, () => NextTurn()));
    }

    #endregion

    #region Swap Hand

    private void SwapHandEvent_AI()
    {
        if (currentPlayer.ShouldSwapHand())
        {
            List<CambioPlayer> playersToChoose = players.Where(p => p != currentPlayer).ToList();
            CambioPlayer randomPlayer = playersToChoose[UnityEngine.Random.Range(0, playersToChoose.Count)];

            SwapHands(currentPlayer, randomPlayer);
        }

        NextTurn();
    }

    #endregion

    #region Choose 2 Card and Decide which to keep

    private IEnumerator ChooseSwapEvent_AI()
    {
        //CHOOSE ONE OF YOUR OWN CARDS
        PlayingCard cardToSwap = currentPlayer.GetCardtoSwap();
        LiftCard(cardToSwap);

        swapEventDictionary.Add(cardToSwap, currentPlayer);

        yield return new WaitForSeconds(AIThinkingTime);

        //CHOOSE A RANDOM OPPONENT CARD
        List<CambioPlayer> playersToChoose = players.Where(p => p != currentPlayer).ToList();
        CambioPlayer randomPlayer = playersToChoose[UnityEngine.Random.Range(0, playersToChoose.Count)];
        PlayingCard randomCard = randomPlayer.Hand.GetRandomCard();

        swapEventDictionary.Add(randomCard, randomPlayer);

        //DECIDE WHETHER TO SWAP

        BringCardsToPlayerToChoose();

        yield return new WaitForSeconds(AIThinkingTime);

        //FIND LOWER CARD
        int lowestCardValue = int.MaxValue;
        PlayingCard choice = null;
        foreach (var kvp in swapEventDictionary)
        {
            int value = currentPlayer.GetCardValue(kvp.Key);

            if (value < lowestCardValue)
            {
                lowestCardValue = value;
                choice = kvp.Key;
            }
        }

        ChooseCardToKeep(choice);
    }

    #endregion

    #region Blind Swap 2 Cards

    private IEnumerator BlindSwapEvent_AI()
    {
        //CHOOSE ONE OF YOUR OWN CARDS
        PlayingCard cardToSwap = currentPlayer.GetCardtoSwap();
        LiftCard(cardToSwap);

        yield return new WaitForSeconds(AIThinkingTime);

        //CHOOSE A RANDOM OPPONENT CARD
        List<CambioPlayer> playersToChoose = players.Where(p => p != currentPlayer).ToList();
        CambioPlayer randomPlayer = playersToChoose[UnityEngine.Random.Range(0, playersToChoose.Count)];
        PlayingCard randomCard = randomPlayer.Hand.GetRandomCard();

        SwapCards(currentPlayer, cardToSwap, randomPlayer, randomCard);
        
        yield return new WaitForSeconds(AIThinkingTime);

        NextTurn();
    }

    #endregion

    #endregion

    #region Stacking

    private bool TryEnableStacking()
    {
        if (GetTopPileCard() == null) return false;

        Debug.Log("Stacking enabled");

        foreach (var player in players)
        {
            player.SetHandInteractable(true);

            foreach (var playerCard in player.Hand.Cards)
            {
                playerCard.OnInteract += Card_OnInteract_Stack;
            }
        }

        return true;
    }

    private void DisableStacking()
    {
        Debug.Log("Stacking disabled");

        foreach (var player in players)
        {
            player.SetHandInteractable(false);

            foreach (var playerCard in player.Hand.Cards)
            {
                playerCard.OnInteract -= Card_OnInteract_Stack;
            }
        }
    }

    private void Card_OnInteract_Stack(object sender, EventArgs e)
    {
        PlayingCard card = (PlayingCard)sender;
        TablePlayer player = GetPlayerWithCard(card);
        StackCard(player, card);
    }

    private void StackCard(TablePlayer player, PlayingCard card)
    {
        if (hasStacked) return;

        DisableStacking();
        isStacking = true;
        hasStacked = true;

        StartCoroutine(StackCardCoroutine(player, card));
    }

    private IEnumerator StackCardCoroutine(TablePlayer player, PlayingCard card)
    {
        if (!player.RemoveCardFromHand(card))
        {
            Debug.LogWarning("Card failed to be removed from hand");
            yield break;
        }

        PlayingCard topCard = GetTopPileCard();
        PlaceCardOnPile(card);

        yield return new WaitForSeconds(4f);

        //If cards are not the same value, take card back and add a new one
        if (topCard.GetValue(false) != card.GetValue(false))
        {
            player.AddCardToHand(card);

            yield return new WaitForSeconds(2f);

            DealCardToPlayerHand(player);

            yield return new WaitForSeconds(2f);
        }

        isStacking = false;
    }

    #endregion
}
