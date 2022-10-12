using System.Linq;
using Beamable;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : UIMenu
{
    public Button ResetButton;
    public Button LogoutButton;

    protected override void AddressablesAwake()
    {
        base.AddressablesAwake();
        
            LogoutButton.onClick.AddListener(Logout);
        ResetButton.onClick.AddListener(ResetAccount);
    }

    private void ResetAccount()
    {
        ModalPopupMenu.ShowYesNoModal("Reset Account", "Are you sure you want to reset your account?", "RESET", "CANCEL",
            () =>
            {
                Hide();
                BeamContext.All.ToList().ForEach(context => context.ClearPlayerAndStop());
                PlayerPrefs.DeleteKey(UIManager.Instance.GetMenu<LoginMenu>().LastLoginKey);
                UIManager.Instance.ShowOnly<LoginMenu>();
            }, () =>
            {
                
            });
    }

    private void Logout()
    {
        ModalPopupMenu.ShowYesNoModal("Logout", "Are you sure you want to logout?", "LOGOUT", "CANCEL",
            () =>
            {
                Hide();
                //BeamContext.All.ToList().ForEach(context => context.ClearPlayerAndStop());
                PlayerPrefs.DeleteKey(UIManager.Instance.GetMenu<LoginMenu>().LastLoginKey);
                TriggeRunGameManager.Instance.Logout();
                UIManager.Instance.ShowOnly<LoginMenu>();
            }, () =>
            {
                
            });
        
    }

    public override UIMenu Show()
    {
        return base.Show();
    }

    public override void Hide()
    {
        base.Hide();
        
        UIManager.Instance.TryShow<MainMenu>();
    }
}