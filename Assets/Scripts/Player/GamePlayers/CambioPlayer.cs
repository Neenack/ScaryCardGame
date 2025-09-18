using System.Collections;
using UnityEngine;

public class CambioPlayer : TablePlayer
{
    [SerializeField] private float rowSpacing = 2.5f;


    public override bool IsPlaying()
    {
        return true;
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
        }
        else
        {
            StartCoroutine(AITurn());
        }
    }

    private void InteractableDeck_OnInteract(object sender, System.EventArgs e)
    {
        Game.PullNewCard(this);

        interactableDeck.SetInteractable(false);
        interactableDeck.OnInteract -= InteractableDeck_OnInteract;
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

        foreach (var card in Hand.Cards)
        {
            card.SetInteractable(false);
        }
    }

    public void SetHandInteractable(bool interactable)
    {
        foreach (var card in Hand.Cards)
        {
            card.SetInteractable(interactable);
        }
    }
}
