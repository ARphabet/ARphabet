using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("UI Baðlantýlarý")]
    public Image xpBarImage;
    public TextMeshProUGUI levelText; 

    [Header("Rütbe (Rank) Ayarlarý")]
    public Image rankImageComponent; 
    public Sprite[] rankSprites;     

    [Header("Seviye Ayarlarý")]
    public int currentLevel = 1;
    public float currentXP = 0;
    public float requiredXP = 100;

    private const int levelsPerRank = 5;

    void Start()
    {
        UpdateLevelUI();
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        if (currentXP >= requiredXP)
        {
            LevelUp();
        }
        UpdateLevelUI();
    }

    void LevelUp()
    {
        currentLevel++;
        currentXP -= requiredXP;
        //requiredXP += 50;
        Debug.Log("Tebrikler! Yeni Seviye: " + currentLevel);
    }

    void UpdateLevelUI()
    {
        if (levelText != null)
            levelText.text = currentLevel.ToString();

        if (xpBarImage != null)
        {
            float fillRatio = currentXP / requiredXP;
            xpBarImage.fillAmount = fillRatio;
        }

        if (rankImageComponent != null && rankSprites.Length > 0)
        {
            int rankIndex = (currentLevel - 1) / levelsPerRank;

            rankIndex = Mathf.Clamp(rankIndex, 0, rankSprites.Length - 1);

            rankImageComponent.sprite = rankSprites[rankIndex];
        }
    }

    // TEST ÝÇÝN: 'X' tuþu XP verir
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            AddXP(100);
        }
    }
}