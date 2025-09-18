using System.Collections;
using TMPro;
using UnityEngine;

public class CambioPlayer : TablePlayer
{
    [Header("Cambio Settings")]
    [SerializeField] private Interactable callRoundButton;
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private float rowSpacing = 2.5f;

    private bool hasCalledGame = false;
    public bool HasCalledGame => hasCalledGame;


    private void Start()
    {
        callRoundButton.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
    }

    protected override void Game_OnGameStarted()
    {
        hasCalledGame = false;
        scoreText.gameObject.SetActive(false);
    }

    protected override void Game_OnGameEnded()
    {
        
    }

    public override bool IsPlaying()
    {
        return !hasCalledGame;
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

        if (!IsAI)
        {
            interactableDeck.SetInteractable(true);
            interactableDeck.OnInteract += InteractableDeck_OnInteract;

            callRoundButton.gameObject.SetActive(true);
            callRoundButton.OnInteract += CallRoundButton_OnInteract;
        }
        else
        {
            StartCoroutine(AITurn());
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

    private IEnumerator AITurn()
    {
        yield return new WaitForSeconds(2f);

        //DO TURN

        Game.NextTurn();
    }

    public override void EndTurn()
    {
        base.EndTurn();

        interactableDeck.SetInteractable(false);
        interactableDeck.OnInteract -= InteractableDeck_OnInteract;

        callRoundButton.gameObject.SetActive(false);
        callRoundButton.OnInteract -= CallRoundButton_OnInteract;

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
            int value = card.GetValue(false);

            //IF CARD IS A BLACK KING, VALUE = -1
            if (value == 13 && (card.Suit == Suit.Clubs || card.Suit == Suit.Spades))
            {
                value = -1;
            }

            total += value;
        }

        return total;
    }

    public void SetScoreText(string text)
    {
        scoreText.gameObject.SetActive(true);
        scoreText.text = text;
    }
}
