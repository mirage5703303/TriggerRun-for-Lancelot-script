using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Api;
using BeauRoutine;
using Michsky.UI.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InputPopupMenu : UIMenu
{
    public RectTransform Content;
    
    [Header("Texts")]
    [SerializeField] public TextMeshProUGUI m_Text1;

    [SerializeField] public TMP_InputField InputField;
    
    [Header("Buttons")]
    [SerializeField] public Button m_ConfirmButton;
    [SerializeField] public MainButton m_ConfirmButtonText;
    [SerializeField] public Button m_Confirm2Button;
    [SerializeField] public MainButton m_Confirm2ButtonText;
    [SerializeField] public Button m_CancelButton;
    [SerializeField] public MainButton m_CancelButtonText;
    
    private UnityAction<string> confirmAction;
    private UnityAction confirm2Action;
    private UnityAction cancelAction;

    public bool HideOnResponseClicked = true;
    private int MaxTextLength = 2500;

    protected override void AddressablesAwake()
    {
        m_ConfirmButton.onClick.AddListener(() =>
        {
            if (HideOnResponseClicked) Hide();
            confirmAction?.Invoke(InputField.text);
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
        public UnityAction<string> OkAction { get; set; }
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
                ShowInputModal(data.Title, data.Description, data.OkText, data.OkAction, data.DescriptionUpdater);
                break;
        }
    }
    
    private static readonly Queue<PopupQueueData> PopupQueueDatas = new Queue<PopupQueueData>();
    private static Routine? _updater;

    public static void ShowInputModal(string title, string defaultInput, string okText, UnityAction<string> okAction, Func<string, IEnumerator> descriptionUpdater = null)
    {
        var instance = UIManager.Instance.GetMenu<InputPopupMenu>();

        if (instance.IsOpen)
        {
            PopupQueueDatas.Enqueue(new PopupQueueData()
            {
                Title = title,
                Description = defaultInput,
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
        instance.InputField.text = defaultInput;

        instance.Show();
        
        instance.m_CancelButton.interactable = true;
        instance.m_ConfirmButton.interactable = true;
        instance.m_Confirm2Button.interactable = true;

        if (_updater != null)
        {
            _updater.Value.Stop();
            _updater = null;
        }
        if (descriptionUpdater != null) _updater = Routine.Start(descriptionUpdater?.Invoke(defaultInput));
    }

    public struct ModalPopupData
    {
        public string Text;
        public UnityAction Action;
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

    public static void EnableButton(int index, bool enable)
    {
        var instance = UIManager.Instance.GetMenu<InputPopupMenu>();
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
}