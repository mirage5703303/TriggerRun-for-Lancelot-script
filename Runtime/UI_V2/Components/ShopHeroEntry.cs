using System;
using System.Collections;
using System.Collections.Generic;
using IEdgeGames;
using TMPro;
using TrickCore;
using UnityEngine;
using UnityEngine.UI;

public class ShopHeroEntry : MonoBehaviour
{
    public Button ClickButton;
    public Image IconImage;
    public TextMeshProUGUI HeroNameText;
    
    public RectTransform EquippedRoot;
    public TextMeshProUGUI EquippedText;

    public RectTransform CurrencyRoot;
    public Image CurrencyTypeImage;
    public TextMeshProUGUI CurrencyValueText;

    private Action<ShopHeroEntry> _clickCallback;
    
    public CharacterDefinition Hero { get; set; }
    public bool HasHero { get; set; }

    private void Awake()
    {
        ClickButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        UIManager.Instance.PlayButtonAudio();

        _clickCallback?.Invoke(this);
        
        EquippedText.gameObject.SetActive(HasHero && Hero.HasEquipped());
    }

    public void SetData(CharacterDefinition hero, bool hasHero, Action<ShopHeroEntry> action)
    {
        Hero = hero;
        HasHero = hasHero;
        _clickCallback = action;
        IconImage.sprite = hero.portrait;
        HeroNameText.text = hero.name;
        EquippedRoot.gameObject.SetActive(hasHero);
        CurrencyRoot.gameObject.SetActive(!hasHero);
        CurrencyTypeImage.sprite = TriggeRunGameManager.Instance.CurrencyGemSprite;
        CurrencyValueText.text = hero.GemCosts.ToString("N0");
        EquippedText.gameObject.SetActive(HasHero && Hero.HasEquipped());
        
    }


    public void RefreshView() => SetData(Hero, HasHero, _clickCallback);
}