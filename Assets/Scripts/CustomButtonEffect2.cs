using UnityEngine;
using UnityEngine.UI;

public class CustomButtonEffect2 : MonoBehaviour
{
    [Header("Görseller")]
    public Sprite normalSprite;
    public Sprite activeSprite;

    private Image buttonImage;

    private bool active = false;

    void Start()
    {
        buttonImage = GetComponent<Image>();

        if (normalSprite == null)
        {
            normalSprite = buttonImage.sprite;
        }
    }

    public void ButonResimDegis()
    {
        active = !active;

        if (active && activeSprite != null) {
            buttonImage.sprite = activeSprite;
        } 
        else if(!active && normalSprite != null) {
            buttonImage.sprite = normalSprite;
        }
    }
}