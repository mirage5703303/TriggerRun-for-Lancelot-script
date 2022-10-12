using UnityEngine.UI;

public class ShopMenu : UIMenu
{
    public Button CoinButton;
    public Button GemButton;

    protected override void AddressablesAwake()
    {
        base.AddressablesAwake();
        
        CoinButton.onClick.AddListener(AddCoins);
        GemButton.onClick.AddListener(AddGems);
    }

    private void AddGems()
    {
        TriggeRunGameManager.Instance.TryAddCurrency(TriggeRunGameManager.Instance.GemCurrency, 500);
    }

    private void AddCoins()
    {
        TriggeRunGameManager.Instance.TryAddCurrency(TriggeRunGameManager.Instance.CoinCurrency, 500);
    }

    public override void Hide()
    {
        base.Hide();
        
        UIManager.Instance.TryShow<MainMenu>();
    }
}