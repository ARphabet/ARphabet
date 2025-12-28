using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Dokunma olaylarý için bu kütüphane þart

public class CustomButtonEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Görseller")]
    public Sprite normalSprite;
    public Sprite pressedSprite;

    private Image buttonImage;

    void Start()
    {
        buttonImage = GetComponent<Image>();

        if (normalSprite == null)
        {
            normalSprite = buttonImage.sprite;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pressedSprite != null)
        {
            buttonImage.sprite = pressedSprite;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
    }
}