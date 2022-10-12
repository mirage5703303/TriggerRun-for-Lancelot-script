using System;
using System.Text;
using System.Text.RegularExpressions;
using Beamable;
using Beamable.AccountManagement;
using Michsky.UI.Shift;
using TMPro;
using TrickCore;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class LoginMenu : UIMenu
{
    private static readonly Regex EmailRegex = new Regex("^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$", RegexOptions.Compiled);

    public string LastLoginKey => $"TR_LAST_LOGIN_{GetUniqueGuid()}";
    public string LastGuestKey => $"TR_LAST_LOGIN_{GetUniqueGuid()}_SALT";
    public string LastEmailKey => $"TR_LAST_EMAIL_{GetUniqueGuid()}";
    public string LastPassKey => $"TR_LAST_EMAIL_P_{GetUniqueGuid()}";
    public string LastRememberKey => $"TR_LAST_REMEBER_{GetUniqueGuid()}";
    
    public Button GuestButton;
    public Button LoginButton;
    
    public Button RegisterButton;
    public Button RegisterCancelButton;
    public Button RegisterSignUpButton;

    public TMP_InputField LoginEmailInput;
    public TMP_InputField LoginPasswordInput;
    public SwitchManager RememberMeToggle;
    
    public TMP_InputField RegisterEmailInput;
    public TMP_InputField RegisterPasswordInput;

    public RectTransform LoginRoot;
    public RectTransform RegisterRoot;
    
    public AccountManagementSignals Signals;

    protected override void AddressablesAwake()
    {
        base.AddressablesAwake();
        
        GuestButton.onClick.AddListener(LoginAsGuest);
        LoginButton.onClick.AddListener(LoginWithCredentials);
        
        RegisterButton.onClick.AddListener(ShowRegisterPanel);
        RegisterSignUpButton.onClick.AddListener(RegisterNewAccount);
        RegisterCancelButton.onClick.AddListener(RegisterCancel);
        
        LoginEmailInput.onValueChanged.AddListener(EmailValueChanged);
        LoginPasswordInput.onValueChanged.AddListener(PassValueChanged);
        RememberMeToggle.OnEvents.AddListener(RememberMeChanged);
        RememberMeToggle.OffEvents.AddListener(RememberMeChanged);
    }

    private void RememberMeChanged()
    {
        PlayerPrefs.SetInt(LastRememberKey, RememberMeToggle.isOn ? 1 : 0);
    }

    private void RegisterCancel()
    {
        TrickVisualHelper.FadeIn(LoginRoot);
        TrickVisualHelper.FadeOut(RegisterRoot, 0.0f);
    }


    private async void RegisterNewAccount()
    {
        if (EmailRegex.IsMatch(RegisterEmailInput.text))
        {
            // is email
            if (string.IsNullOrEmpty(RegisterPasswordInput.text))
            {
                ModalPopupMenu.ShowError("Password cannot be empty.");
                return;
            }
        }
        else
        {
            // is not an email
            ModalPopupMenu.ShowError("The email address is invalid.");
            return;
        }

        RegisterCancelButton.interactable = false;
        RegisterSignUpButton.interactable = false;
        // try register
        var api = await API.Instance;

        bool emailAvailable = await api.AuthService.IsEmailAvailable(RegisterEmailInput.text);

        if (!emailAvailable)
        {
            ModalPopupMenu.ShowError("The email address is not available.");
        
            RegisterCancelButton.interactable = true;
            RegisterSignUpButton.interactable = true;
            return;
        }
        
        await api.LoginToNewUser();
        
        api.AuthService.RegisterDBCredentials(RegisterEmailInput.text, RegisterPasswordInput.text).Then(user =>
        {
            ModalPopupMenu.ShowOkModal("Successfully Registered!", "Your account has been successfully created!", "Ok",
                () =>
                {
                    // auto set what we registered into the login field (email only, password you need to enter yourself)
                    LoginEmailInput.text = RegisterEmailInput.text;
                    LoginPasswordInput.text = string.Empty;
                    RegisterEmailInput.text = string.Empty;
                    RegisterPasswordInput.text = string.Empty;
        
                    RegisterCancelButton.interactable = true;
                    RegisterSignUpButton.interactable = true;

                    TrickVisualHelper.FadeIn(LoginRoot);
                    TrickVisualHelper.FadeOut(RegisterRoot, 0.0f);
                });
        }).Error(exception =>
        {
            Debug.LogException(exception);
            ModalPopupMenu.ShowError(exception);
        
            RegisterCancelButton.interactable = true;
            RegisterSignUpButton.interactable = true;
        });
    }

    private void ShowRegisterPanel()
    {
        TrickVisualHelper.FadeOut(LoginRoot, 0.0f);
        TrickVisualHelper.FadeIn(RegisterRoot);
    }

    private void EmailValueChanged(string arg0)
    { 
        PlayerPrefs.SetString(LastEmailKey, arg0);
        PlayerPrefs.Save();
    }
    
    private void PassValueChanged(string arg0)
    { 
        PlayerPrefs.SetString(LastPassKey, Convert.ToBase64String(Encoding.UTF8.GetBytes(arg0)));
        PlayerPrefs.Save();
    }
    
    
    private async void LoginWithCredentials()
    {
        if (EmailRegex.IsMatch(LoginEmailInput.text))
        {
            if (string.IsNullOrEmpty(LoginPasswordInput.text))
            {
                ModalPopupMenu.ShowError("Password cannot be empty.");
                return;
            }
        }
        else
        {
            // is not an email
            ModalPopupMenu.ShowError("The email address is invalid.");
            return;
        }
        
        bool? result = null;
        UIManager.Instance.GetMenu<LoadingMenu>().WaitFor("Logging in...", () =>
        {
            
        }, () => result != null).Show();
        
        var api = await API.Instance;
        api.AuthService.Login(LoginEmailInput.text, LoginPasswordInput.text, false).Then(response =>
        {
            BeamContext.ForPlayer(LoginEmailInput.text).ChangeAuthorizedPlayer(response).Then(context =>
            {
                context.OnReady.Then(unit =>
                {
                    PlayerPrefs.SetString(LastLoginKey, context.PlayerCode);
                    OnLogin(context, () =>
                    {
                        result = true;
                    });                
                }).Error(exception =>
                {
                    ModalPopupMenu.ShowError(exception);
                    result = false;
                });
            }).Error(exception =>
            {
                ModalPopupMenu.ShowError(exception);
                result = false;
            });;
        }).Error(exception =>
        {
            ModalPopupMenu.ShowError(exception);
            result = false;
        });;
    }

    private async void LoginAsGuest()
    {
        bool? result = null;
        
        UIManager.Instance.GetMenu<LoadingMenu>().WaitFor("Logging in...", () =>
        {
            
        }, () => result != null).Show();
        
        
        var api = await API.Instance;
        
        // register

        if (!PlayerPrefs.HasKey(LastGuestKey) || await api.AuthService.IsEmailAvailable($"{GetUniqueGuid()}"))
        {
            Debug.Log("Available to register: " + GetUniqueGuid());
            PlayerPrefs.SetString(LastGuestKey, Guid.NewGuid().ToString());
            await api.LoginToNewUser();
            await api.AuthService.RegisterDBCredentials($"{GetUniqueGuid()}", HashUtil.CreateMD5($"{GetUniqueGuid()}_{PlayerPrefs.GetString(LastGuestKey)}"));
        }
        else
        {
            Debug.Log("Already registered: " + GetUniqueGuid());
        }
        
        LoginEmailInput.text = string.Empty;
        LoginPasswordInput.text = string.Empty;

        HandleGuestLogin(b => result = b);

        /*var ctx = BeamContext.ForPlayer(GetUniqueGuid());
        ctx.OnReady.Then(unit =>
        {
            PlayerPrefs.SetString(LastLoginKey, ctx.PlayerCode);
            OnLogin(ctx, () =>
            {
                result = true;
            });
        }).Error(exception =>
        {
            ModalPopupMenu.ShowError(exception);
            result = false;
        });*/
    }

    private async void HandleGuestLogin(Action<bool> resultCallback)
    {
        var api = await API.Instance;
        api.AuthService.Login($"{GetUniqueGuid()}", HashUtil.CreateMD5($"{GetUniqueGuid()}_{PlayerPrefs.GetString(LastGuestKey)}"), false).Then(response =>
        {
            BeamContext.ForPlayer(GetUniqueGuid()).ChangeAuthorizedPlayer(response).Then(context =>
            {
                context.OnReady.Then(unit =>
                {
                    PlayerPrefs.SetString(LastLoginKey, context.PlayerCode);
                    OnLogin(context, () =>
                    {
                        resultCallback?.Invoke(true);
                    });                
                }).Error(exception =>
                {
                    ModalPopupMenu.ShowError(exception);
                    resultCallback?.Invoke(false);
                });
            }).Error(exception =>
            {
                ModalPopupMenu.ShowError(exception);
                resultCallback?.Invoke(false);
            });;
        }).Error(exception =>
        {
            ModalPopupMenu.ShowError(exception);
            resultCallback?.Invoke(false);
        });
    }

    private void TryQuickLogin(Action<BeamContext> callback)
    {
        if (PlayerPrefs.HasKey(LastLoginKey))
        {
            var playerCode = PlayerPrefs.GetString(LastLoginKey);
            var ctx = BeamContext.ForPlayer(playerCode);
            ctx.OnReady.Then(unit =>
            {
                if (!ctx.AuthorizedUser.Value.HasDBCredentials())
                {
                    callback?.Invoke(null);
                    return;
                }
                PlayerPrefs.SetString(LastLoginKey, ctx.PlayerCode);
                OnLogin(ctx, () =>
                {
                    callback?.Invoke(ctx);
                });
            }).Error(exception =>
            {
                ModalPopupMenu.ShowError(exception);
                callback?.Invoke(null);
            });
        }
        else
        {
            callback?.Invoke(null);
        }
    }

    
    private void OnLogin(BeamContext context, Action complete)
    {
        if (IsOpen)
        {
            TriggeRunGameManager.Instance.SetBeamableContext(context, (result) =>
            {
                if (!string.IsNullOrEmpty(result))
                {
                    ModalPopupMenu.ShowError(result);
                }
                else
                {
                    FadeToBlackMenu.FadeToBlack(() =>
                    {
                        Hide();
                        complete?.Invoke();
                        UIManager.Instance.ShowOnly<MainMenu>();
                    });   
                }             
            });
        }
    }

    public override UIMenu Show()
    {
        LoginEmailInput.SetTextWithoutNotify(PlayerPrefs.GetString(LastEmailKey, ""));
        RememberMeToggle.isOn = PlayerPrefs.GetInt(LastRememberKey, 0) == 1;
        LoginPasswordInput.SetTextWithoutNotify(RememberMeToggle.isOn
            ? (PlayerPrefs.HasKey(LastPassKey) && PlayerPrefs.GetString(LastPassKey) is { } x
                ? Encoding.UTF8.GetString(Convert.FromBase64String(x))
                : string.Empty)
            : string.Empty);
        
        LoginRoot.gameObject.SetActive(true);
        RegisterRoot.gameObject.SetActive(true);
        
        TrickVisualHelper.FadeOut(LoginRoot, 0.0f);
        TrickVisualHelper.FadeOut(RegisterRoot, 0.0f);
        
        bool? result = null;
        UIManager.Instance.GetMenu<LoadingMenu>().WaitFor("Logging in...", () =>
        {
            
        }, () => result != null).Show();
        TryQuickLogin(context =>
        {
            if (context == null) TrickVisualHelper.FadeIn(LoginRoot);
            result = context != null;
        });
        return base.Show();
    }

    
    private static string GetUniqueGuid()
    {
        string guid;
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            var cloneArg = ClonesManager.GetArgument();
            var uniquePlayerPrefString = $"TR_GUEST_{HashUtil.GetHash32(cloneArg)}";
            if (!PlayerPrefs.HasKey(uniquePlayerPrefString))
            {
                PlayerPrefs.SetString(uniquePlayerPrefString, guid = Guid.NewGuid().ToString());
            }
            else
            {
                guid = PlayerPrefs.GetString(uniquePlayerPrefString);
            }
        }
        else
#endif
        {
            // just do the same for the main instance
            var uniquePlayerPrefString = $"TR_GUEST_MAIN";
            if (!PlayerPrefs.HasKey(uniquePlayerPrefString))
            {
                PlayerPrefs.SetString(uniquePlayerPrefString, guid = Guid.NewGuid().ToString());
            }
            else
            {
                guid = PlayerPrefs.GetString(uniquePlayerPrefString);
            }
        }
        return guid;
    }
}