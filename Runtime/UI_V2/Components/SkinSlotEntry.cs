using System;
using IEdgeGames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinSlotEntry : MonoBehaviour
{
    public Image SkinImage;

    public RectTransform CurrencyRoot;
    public Image CurrencyImage;
    public TextMeshProUGUI CurrencyText;

    public Image ItemLevelImage;
    public Image LockImage;

    private Action<SkinSlotEntry> _callback;

    public int CoinCosts { get; set; }
    public int GemCosts { get; set; }
    public bool HasSkin { get; set; }

    public CharacterDefinition Hero { get; set; }
    public ItemDefinition Item { get; set; }
    public int SkinIndex { get; set; }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        UIManager.Instance.PlayButtonAudio();
        _callback?.Invoke(this);
    }

    public void SetHero(CharacterDefinition characterDefinition, HeroSkinData heroSkinData, int skinIndex,
        Action<SkinSlotEntry> callback)
    {
        _callback = callback;

        GemCosts = heroSkinData.GemCosts;
        CoinCosts = heroSkinData.CoinCosts;
        SkinIndex = skinIndex;
        HasSkin = heroSkinData.HasSkin(characterDefinition, skinIndex);

        Hero = characterDefinition;
        Item = null;

        if (heroSkinData.GemCosts > 0)
        {
            CurrencyText.text = heroSkinData.GemCosts.ToString("N0");
            CurrencyImage.sprite = TriggeRunGameManager.Instance.CurrencyGemSprite;
            CurrencyRoot.gameObject.SetActive(!HasSkin);
        }
        else if (heroSkinData.CoinCosts > 0)
        {
            CurrencyText.text = heroSkinData.CoinCosts.ToString("N0");
            CurrencyImage.sprite = TriggeRunGameManager.Instance.CurrencyCoinSprite;
            CurrencyRoot.gameObject.SetActive(!HasSkin);
        }
        else
        {
            CurrencyRoot.gameObject.SetActive(false);
        }

        SkinImage.sprite = heroSkinData.SkinSprite;
        
        ItemLevelImage.gameObject.SetActive(false);
        
        LockImage.gameObject.SetActive(!HasSkin);
    }



    public void SetItem(ItemDefinition itemDefinition, ItemSkinData itemSkinData, int skinIndex,
        Action<SkinSlotEntry> callback)
    {
        _callback = callback;
        
        GemCosts = itemSkinData.GemCosts;
        CoinCosts = itemSkinData.CoinCosts;
        SkinIndex = skinIndex;
        HasSkin = itemSkinData.HasSkin(itemDefinition, skinIndex);

        Hero = null;
        Item = itemDefinition;
        
        if (itemSkinData.GemCosts > 0)
        {
            CurrencyText.text = itemSkinData.GemCosts.ToString("N0");
            CurrencyImage.sprite = TriggeRunGameManager.Instance.CurrencyGemSprite;
            CurrencyRoot.gameObject.SetActive(!HasSkin);
        }
        else if (itemSkinData.CoinCosts > 0)
        {
            CurrencyText.text = itemSkinData.CoinCosts.ToString("N0");
            CurrencyImage.sprite = TriggeRunGameManager.Instance.CurrencyCoinSprite;
            CurrencyRoot.gameObject.SetActive(!HasSkin);
        }
        else
        {
            CurrencyRoot.gameObject.SetActive(false);
        }
        
        SkinImage.sprite = itemSkinData.SkinSprite;
        
        ItemLevelImage.gameObject.SetActive(true);
        
        LockImage.gameObject.SetActive(!HasSkin);
    }

}