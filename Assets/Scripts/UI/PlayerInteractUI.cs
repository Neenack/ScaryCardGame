using TMPro;
using UnityEngine;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private TextMeshProUGUI interactText;

    private void Update()
    {
        IInteractable interactable = interactor.GetCurrentInteractable();

        if (interactable != null)
        {
            interactText.gameObject.SetActive(true);
            interactText.text = interactable.GetText();
        }
        else interactText.gameObject.SetActive(false);
    }
}
