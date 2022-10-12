using System;
using IEdgeGames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemEntry : MonoBehaviour
{
    public Button ClickButton;
    public Image IconImage;
    public TextMeshProUGUI ItemNameText;
    
    public RectTransform EquippedRoot;
    public TextMeshProUGUI EquippedText;

    public RectTransform CurrencyRoot;
    public Image CurrencyTypeImage;
    public TextMeshProUGUI CurrencyValueText;

    private Action<ShopItemEntry> _clickCallback;
    
    public ItemDefinition Item { get; set; }

    private void Awake()
    {
        ClickButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        UIManager.Instance.PlayButtonAudio();
        
        _clickCallback?.Invoke(this);
        
        EquippedText.gameObject.SetActive(Item.HasOwned() && Item.HasEquipped());
    }

    public void SetData(ItemDefinition item, Action<ShopItemEntry> action)
    {
        Item = item;
        
        var hasItem = Item.HasOwned();

        _clickCallback = action;
        IconImage.sprite = item.ItemSprite;
        ItemNameText.text = item.name;
        EquippedRoot.gameObject.SetActive(hasItem);
        CurrencyRoot.gameObject.SetActive(!hasItem);
        if (item.GemCosts > 0)
        {
            CurrencyTypeImage.sprite = TriggeRunGameManager.Instance.CurrencyGemSprite;
            CurrencyValueText.text = item.GemCosts.ToString("N0");
        }
        else if (item.CoinCosts > 0)
        {
            CurrencyTypeImage.sprite = TriggeRunGameManager.Instance.CurrencyCoinSprite;
            CurrencyValueText.text = item.CoinCosts.ToString("N0");
        }
        else
        {
            CurrencyRoot.gameObject.SetActive(false);
        }
        EquippedText.gameObject.SetActive(hasItem && Item.HasEquipped());
        
    }


    public void RefreshView() => SetData(Item, _clickCallback);
}