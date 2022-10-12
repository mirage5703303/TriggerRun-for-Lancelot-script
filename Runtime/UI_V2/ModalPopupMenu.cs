using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Api;
using BeauRoutine;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ModalPopupMenu : UIMenu
{
    public RectTransform Content;
    
    [Header("Texts")]
    [SerializeField] public TextMeshProUGUI m_Text1;
    [SerializeField] public TextMeshProUGUI m_Text2;
    [Header("Buttons")]
    [SerializeField] public Button m_ConfirmButton;
    [SerializeField] public MainButton m_ConfirmButtonText;
    [SerializeField] public Button m_Confirm2Button;
    [SerializeField] public MainButton m_Confirm2ButtonText;
    [SerializeField] public Button m_CancelButton;
    [SerializeField] public MainButton m_CancelButtonText;
    private UnityAction confirmAction;
    private UnityAction confirm2Action;
    private UnityAction cancelAction;

    public bool HideOnResponseClicked = true;
    private int MaxTextLength = 2500;

    protected override void AddressablesAwake()
    {
        m_ConfirmButton.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            confirmAction?.Invoke();
            ExecutePopupQueue();
        });
        m_Confirm2Button.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            confirm2Action?.Invoke();
            ExecutePopupQueue();
        });
        m_CancelButton.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            cancelAction?.Invoke();
            ExecutePopupQueue();
        });
    }

    public override UIMenu Show()
    {
        base.Show();
        Content.gameObject.SetActive(true);
        return this;
    }

    public override void Hide()
    {
        base.Hide();
        Content.gameObject.SetActive(false);

        if (_updater != null)
        {
            _updater.Value.Stop();
            _updater = null;
        }
    }

    private class PopupQueueData
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string OkText { get; set; }
        public UnityAction OkAction { get; set; }
        public string YesText { get; set; }
        public string NoText { get; set; }
        public UnityAction YesAction { get; set; }
        public UnityAction NoAction { get; set; }
        public PopupType Popup { get; set; }
        public ModalPopupData Button1 { get; set; }
        public ModalPopupData Button2 { get; set; }
        public ModalPopupData Button3 { get; set; }
        public Func<string, IEnumerator> DescriptionUpdater { get; set; }

        public enum PopupType
        {
            Ok,
            YesNo,
            ButtonModal,
        }
    }

    private static void ExecutePopupQueue()
    {
        if (PopupQueueDatas.Count <= 0) return;
        var data = PopupQueueDatas.Dequeue();
        switch (data.Popup)
        {
            case PopupQueueData.PopupType.Ok:
                ShowOkModal(data.Title, data.Description, data.OkText, data.OkAction, data.DescriptionUpdater);
                break;
            case PopupQueueData.PopupType.YesNo:
                ShowYesNoModal(data.Title, data.Description, data.YesText, data.NoText, data.YesAction, data.NoAction, data.DescriptionUpdater);
                break;
            case PopupQueueData.PopupType.ButtonModal:
                ShowButtonsModal(data.Title, data.Description, data.Button1, data.Button2, data.Button3, data.DescriptionUpdater);
                break;
        }
    }
    
    private static readonly Queue<PopupQueueData> PopupQueueDatas = new Queue<PopupQueueData>();
    private static Routine? _updater;

    public static void ShowOkModal(string title, string description, string okText, UnityAction okAction, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();

        if (instance.IsOpen)
        {
            PopupQueueDatas.Enqueue(new PopupQueueData()
            {
                Title = title,
                Description = description,
                OkText = okText,
                OkAction = okAction,
                Popup = PopupQueueData.PopupType.Ok,
                DescriptionUpdater = descriptionUpdater,
            });
            return;
        }
        
        instance.SetConfirmButtonText(okText);
        instance.SetCancelButtonText(default);
        instance.SetConfirm2ButtonText(default);
        instance.confirmAction = okAction;
        instance.confirm2Action = null;
        instance.cancelAction = null;
        instance.SetText1(title);
        instance.SetText2(description);

        instance.Show();
        
        instance.m_CancelButton.interactable = true;
        instance.m_ConfirmButton.interactable = true;
        instance.m_Confirm2Button.interactable = true;

        if (_updater != null)
        {
            _updater.Value.Stop();
            _updater = null;
        }
        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
    }

    public static void ShowYesNoModal(string title, string description, string yesText, string noText, UnityAction yesAction, UnityAction noAction, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        
        if (instance.IsOpen)
        {
            PopupQueueDatas.Enqueue(new PopupQueueData()
            {
                Title = title,
                Description = description,
                YesText = yesText,
                NoText = noText,
                YesAction = yesAction,
                NoAction = noAction,
                Popup = PopupQueueData.PopupType.YesNo,
                DescriptionUpdater = descriptionUpdater,
            });
            return;
        }

        instance.SetConfirmButtonText(yesText);
        instance.SetCancelButtonText(noText);
        instance.SetConfirm2ButtonText(default);
        instance.confirmAction = yesAction;
        instance.confirm2Action = null;
        instance.cancelAction = noAction;
        instance.SetText1(title);
        instance.SetText2(description);

        instance.Show();

        instance.m_CancelButton.interactable = true;
        instance.m_ConfirmButton.interactable = true;
        instance.m_Confirm2Button.interactable = true;
        
        if (_updater != null)
        {
            _updater.Value.Stop();
            _updater = null;
        }
        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
    }

    public struct ModalPopupData
    {
        public string Text;
        public UnityAction Action;
    }
    
    public static void ShowButtonsModal(string title, string description, ModalPopupData button1, ModalPopupData button2, ModalPopupData button3, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        
        if (instance.IsOpen)
        {
            PopupQueueDatas.Enqueue(new PopupQueueData()
            {
                Title = title,
                Description = description,
                Button1 = button1,
                Button2 = button2,
                Button3 = button3,
                Popup = PopupQueueData.PopupType.ButtonModal,
                DescriptionUpdater = descriptionUpdater,
            });
            return;
        }

        instance.SetCancelButtonText(button1.Text);
        instance.cancelAction = button1.Action;
        
        instance.SetConfirmButtonText(button2.Text);
        instance.confirmAction = button2.Action;
        
        instance.SetConfirm2ButtonText(button3.Text);
        instance.confirm2Action = button3.Action;
        instance.SetText1(title);
        instance.SetText2(description);

        instance.Show();
        
        instance.m_CancelButton.interactable = true;
        instance.m_ConfirmButton.interactable = true;
        instance.m_Confirm2Button.interactable = true;

        if (_updater != null)
        {
            _updater.Value.Stop();
            _updater = null;
        }

        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(description));
    }

    /// <summary>
    /// Sets the text on the first line.
    /// </summary>
    /// <param name="text"></param>
    public void SetText1(string pair)
    {
        if (this.m_Text1 != null)
        {
            this.m_Text1.text = pair;
            this.m_Text1.gameObject.SetActive(!string.IsNullOrEmpty(pair));
        }
    }

    /// <summary>
    /// Sets the text on the second line.
    /// </summary>
    /// <param name="text"></param>
    public void SetText2(string pair)
    {
        if (this.m_Text2 != null)
        {
            var str = pair;
            if (str != null && str.Length > MaxTextLength)
                str = str.Substring(0, MaxTextLength);
            
            this.m_Text2.text = str;

            this.m_Text2.gameObject.SetActive(!string.IsNullOrEmpty(str));
        }
    }

    /// <summary>
    /// Sets the confirm button text.
    /// </summary>
    /// <param name="text">The confirm button text.</param>
    public void SetConfirmButtonText(string pair)
    {
        if (pair != null)
        {
            this.m_ConfirmButtonText.SetText(pair);
            m_ConfirmButton.gameObject.SetActive(true);
        }
        else
        {
            m_ConfirmButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets the confirm button text.
    /// </summary>
    /// <param name="text">The confirm button text.</param>
    public void SetConfirm2ButtonText(string pair)
    {
        if (pair != null)
        {
            this.m_Confirm2ButtonText.SetText(pair);
            m_Confirm2Button.gameObject.SetActive(true);
        }
        else
        {
            m_Confirm2Button.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets the cancel button text.
    /// </summary>
    /// <param name="text">The cancel button text.</param>
    public void SetCancelButtonText(string pair)
    {
        if (pair != null)
        {
            this.m_CancelButtonText.SetText(pair);
            m_CancelButton.gameObject.SetActive(true);
        }
        else
        {
            m_CancelButton.gameObject.SetActive(false);
        }
    }

    public static void SetDescription(string s)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        instance.m_Text2.text = s;
    }

    public static void EnableButton(int index, bool enable)
    {
        var instance = UIManager.Instance.GetMenu<ModalPopupMenu>();
        switch (index)
        {
            case 0:
                instance.m_CancelButton.interactable = enable;
                break;
            case 1:
                instance.m_ConfirmButton.interactable = enable;
                break;
            case 2:
                instance.m_Confirm2Button.interactable = enable;
                break;
        }
    }

    public static void ShowError(string result, UnityAction okAction)
    {
        ShowOkModal("Error", result, "Ok", okAction);
    }

    public static void ShowError(Exception exception, UnityAction okAction)
    {
        ShowError(exception.Message, okAction);
    }

    public static void ShowError(string result)
    {
        ShowOkModal("Error", result, "Ok", null);
    }

    public static void ShowError(Exception exception)
    {
        if (exception is PlatformRequesterException platformRequesterException)
        {
            ShowError(platformRequesterException.Error.message, null);
        }
        else
        {
            ShowError(exception.Message, null);
        }
    }
}