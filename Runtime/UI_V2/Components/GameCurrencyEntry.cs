using System;
using Beamable.Common.Inventory;
using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameCurrencyEntry : MonoBehaviour
{
    public CurrencyLink Currency;
    public Image CurrencyTypeImage;
    public TextMeshProUGUI CurrencyValueText;

    private void OnEnable()
    {
        Currency.Resolve().Then(content =>
        {
            content.icon.LoadSprite().Then(sprite =>
            {
                if (CurrencyTypeImage != null) CurrencyTypeImage.sprite = sprite;
            });
        });
        
        TriggeRunGameManager.Instance.UpdateCurrency(Currency, CurrencyValueText);
        TriggeRunGameManager.Instance.CurrencyUpdateEvent.AddListener(OnCurrencyUpdate);
    }

    private void OnDisable()
    {
        TriggeRunGameManager.Instance.CurrencyUpdateEvent.RemoveListener(OnCurrencyUpdate);
    }
    
    private void OnCurrencyUpdate((CurrencyRef Currency, long PreviousValue, long NewValue) arg0)
    {
        if (Currency.GetId() == arg0.Currency.GetId())
        {
            TriggeRunGameManager.Instance.UpdateCurrency(arg0.Currency, CurrencyValueText);
            //TriggeRunGameManager.Instance.LerpCurrencyText(CurrencyValueText, arg0);
        }
    }

}