using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UpgradeCardUI
{
    public Button cardButton;
    public Image cardBackground;
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
}

public class LevelUpUI : MonoBehaviour
{
    [Header("Referencias")]
    public ExperienceManager experienceManager;

    [Header("Barra Superior de XP")]
    public Image xpFillImage;
    public TextMeshProUGUI levelText;

    [Header("Panel y Tarjetas de Subida de Nivel")]
    public GameObject levelUpPanel;
    public List<UpgradeCardUI> cards = new List<UpgradeCardUI>();

    private void OnEnable()
    {
        if (experienceManager != null)
        {
            experienceManager.OnXpChanged += UpdateXpBar;
            experienceManager.OnLevelUpChoicesReady += ShowUpgradeCards;
        }
    }

    private void OnDisable()
    {
        if (experienceManager != null)
        {
            experienceManager.OnXpChanged -= UpdateXpBar;
            experienceManager.OnLevelUpChoicesReady -= ShowUpgradeCards;
        }
    }

    private void Start()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    private void UpdateXpBar(float currentXp, float maxXp, int level)
    {
        if (xpFillImage != null)
            xpFillImage.fillAmount = maxXp > 0f ? currentXp / maxXp : 0f;

        if (levelText != null)
            levelText.text = $"LVL {level}";
    }

    private void ShowUpgradeCards(List<RolledUpgradeOption> options)
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(true);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < options.Count)
            {
                RolledUpgradeOption option = options[i];
                UpgradeCardUI card = cards[i];

                card.cardButton.gameObject.SetActive(true);
                if (card.titleText != null) card.titleText.text = option.formattedTitle;
                if (card.descriptionText != null) card.descriptionText.text = option.formattedDescription;

                if (card.iconImage != null)
                {
                    card.iconImage.enabled = option.data.icon != null;
                    card.iconImage.sprite = option.data.icon;
                }

                if (card.cardBackground != null)
                {
                    card.cardBackground.color = GetRarityColor(option.rarity);
                }

                card.cardButton.onClick.RemoveAllListeners();
                card.cardButton.onClick.AddListener(() => OnCardClicked(option));
            }
            else
            {
                cards[i].cardButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnCardClicked(RolledUpgradeOption chosen)
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        experienceManager.SelectUpgrade(chosen);
    }

    private Color GetRarityColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Raro => new Color(0.22f, 0.45f, 0.85f),
            UpgradeRarity.Epico => new Color(0.58f, 0.25f, 0.85f),
            UpgradeRarity.Legendario => new Color(0.92f, 0.65f, 0.15f),
            _ => new Color(0.22f, 0.24f, 0.28f)
        };
    }
}
