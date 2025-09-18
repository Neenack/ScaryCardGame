using System;
using System.Collections;
using UnityEngine;
using static Interactable;

public class PlayingCard : MonoBehaviour, IInteractable
{
    public event EventHandler OnInteract;

    public event Action<PlayingCard> OnShowCard;
    private Action onMoveComplete;

    private SpriteRenderer frontFace;
    private SpriteRenderer backFace;
    private Transform card;

    private PlayingCardSO cardSO;

    private Vector3 targetPos;

    private float speed;
    private bool moving = false;
    private bool isFaceDown = true;

    private Quaternion targetRot;
    private bool rotating = false;
    private float rotSpeed;

    public int GetValue(bool isAceHigh) => (isAceHigh && cardSO.Value == 1) ? cardSO.Value + 13 : cardSO.Value;

    public PlayingCardSO CardSO => cardSO;
    public Suit Suit => cardSO.Suit;
    public bool IsFaceDown => isFaceDown;

    private void Awake()
    {
        card = transform.GetChild(0);

        frontFace = card.Find("Front").GetComponent<SpriteRenderer>();
        backFace = card.Find("Back").GetComponent<SpriteRenderer>();
    }

    public void SetCard(PlayingCardSO card)
    {
        cardSO = card;
        SetFrontFace();
    }

    public void HideFrontFace()
    {
        frontFace.sprite = backFace.sprite;
    }

    public void SetFrontFace()
    {
        frontFace.sprite = cardSO.GetSprite();
    }

    public void FlipCard(bool waitForMovement = true, float flipSpeed = 3f)
    {
        if (!isFaceDown) return;

        StartCoroutine(FlipRoutine(flipSpeed, waitForMovement));
    }

    private IEnumerator FlipRoutine(float speed, bool afterMovement)
    {
        if (afterMovement) yield return new WaitUntil(() => !moving);

        // Determine start and end angles
        float startAngle = card.localEulerAngles.x;

        // Convert >180 to negative for smooth lerp
        if (startAngle > 180f) startAngle -= 360f;

        float endAngle = startAngle + 180f; // just flip

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            float x = Mathf.Lerp(startAngle, endAngle, t);
            card.localEulerAngles = new Vector3(x, card.localEulerAngles.y, card.localEulerAngles.z);
            yield return null;
        }

        // Ensure exact final angle
        card.localEulerAngles = new Vector3(endAngle % 360f, card.localEulerAngles.y, card.localEulerAngles.z);

        isFaceDown = !isFaceDown;
        OnShowCard?.Invoke(this);
    }

    public void MoveTo(Vector3 target, float lerpSpeed, Action onComplete = null)
    {
        targetPos = target;
        speed = lerpSpeed;
        moving = true;
        onComplete = onMoveComplete;
    }

    public void RotateTo(Quaternion targetRotation, float lerpSpeed)
    {
        targetRot = targetRotation;
        rotSpeed = lerpSpeed;
        rotating = true;
    }

    void Update()
    {
        if (moving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                transform.position = targetPos;
                moving = false;

                onMoveComplete?.Invoke();
                onMoveComplete = null;
            }
        }

        if (rotating)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetRot) < 0.5f) // close enough
            {
                transform.rotation = targetRot;
                rotating = false;
            }
        }
    }

    public override string ToString() => cardSO.GetCardName(true);
    public string CardType => cardSO.GetCardName(false);


    #region Interactable
    private bool canInteract = false;
    private string interactableText;

    public bool CanInteract() => canInteract;
    public void SetInteractable(bool interact) => canInteract = interact;
    public string GetText() => interactableText;
    public void SetText(string text) => interactableText = text;

    public void Interact(PlayerData player, PlayerInteractor interactor)
    {
        OnInteract?.Invoke(this, new InteractEventArgs(player));
    }

    #endregion
}
