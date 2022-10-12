using System.Linq;
using IEdgeGames;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool to convert Text to TextMeshPro, since we don't want to use the legacy Unity Text.
/// It also stores references of the old Text object, and convert the reference to the new TextMeshProUGUI
/// </summary>
public class TextToTextMeshProConverter : Editor
{
    public class TextMeshProSettings
    {
        public bool Enabled;
        public FontStyles FontStyle;
        public float FontSize;
        public float FontSizeMin;
        public float FontSizeMax;
        public float LineSpacing;
        public bool EnableRichText;
        public bool EnableAutoSizing;
        public TextAlignmentOptions TextAlignmentOptions;
        public bool WrappingEnabled;
        public TextOverflowModes TextOverflowModes;
        public string Text;
        public Color Color;
        public bool RayCastTarget;
    }
    
    [MenuItem("Window/Utility/Text To TextMeshPro By Selection", false, 4000)]
    static void DoIt()
    {
        if(TMPro.TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog("ERROR!", "Assign a default font asset in project settings!", "OK", "");
            return;
        }

        if (Selection.activeGameObject == null) return;
        
        var convertData = Selection.activeGameObject.GetComponent<TextToTextMeshProConvertData>();
        if (convertData == null) convertData = Selection.activeGameObject.AddComponent<TextToTextMeshProConvertData>();
        if (convertData != null) convertData.StoreReferences();
        if (convertData.ReflectionDataList.Count == 0) DestroyImmediate(convertData, true);
        foreach(Text textObject in Selection.activeGameObject.GetComponentsInChildren<Text>(true).ToList())
            ConvertTextToTextMeshPro(textObject);

        EditorUtility.SetDirty(Selection.activeGameObject);
    }
    
    static void ConvertTextToTextMeshPro(Text target)
    {
        TextMeshProSettings settings = GetTextMeshProSettings(target);
        if (settings == null) return;
        var go = target.gameObject;
        DestroyImmediate(target);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.enabled = settings.Enabled;
        tmp.fontStyle = settings.FontStyle;
        tmp.fontSize = settings.FontSize;
        tmp.fontSizeMin = settings.FontSizeMin;
        tmp.fontSizeMax = settings.FontSizeMax;
        tmp.lineSpacing = settings.LineSpacing;
        tmp.richText = settings.EnableRichText;
        tmp.enableAutoSizing = settings.EnableAutoSizing;
        tmp.alignment = settings.TextAlignmentOptions;
        tmp.enableWordWrapping = settings.WrappingEnabled;
        tmp.overflowMode = settings.TextOverflowModes;
        tmp.text = settings.Text;
        tmp.color = settings.Color;
        tmp.raycastTarget = settings.RayCastTarget;
    }
 
    static TextMeshProSettings GetTextMeshProSettings(Text uiText)
    {
        if (uiText == null)
        {
            EditorUtility.DisplayDialog("ERROR!", "You must select a Unity UI Text Object to convert.", "OK", "");
            return null;
        }
 
        return new TextMeshProSettings
        {
            Enabled = uiText.enabled,
            FontStyle = FontStyleToFontStyles(uiText.fontStyle),
            FontSize = uiText.fontSize,
            FontSizeMin = uiText.resizeTextMinSize,
            FontSizeMax = uiText.resizeTextMaxSize,
            LineSpacing = uiText.lineSpacing,
            EnableRichText = uiText.supportRichText,
            EnableAutoSizing = uiText.resizeTextForBestFit,
            TextAlignmentOptions = TextAnchorToTextAlignmentOptions(uiText.alignment),
            WrappingEnabled = HorizontalWrapModeToBool(uiText.horizontalOverflow),
            TextOverflowModes = VerticalWrapModeToTextOverflowModes(uiText.verticalOverflow),
            Text = uiText.text,
            Color = uiText.color,
            RayCastTarget = uiText.raycastTarget
        };
    }
 
    static bool HorizontalWrapModeToBool(HorizontalWrapMode overflow)
    {
        return overflow == HorizontalWrapMode.Wrap;
    }
 
    static TextOverflowModes VerticalWrapModeToTextOverflowModes(VerticalWrapMode verticalOverflow)
    {
        return verticalOverflow == VerticalWrapMode.Truncate ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
    }
 
    static FontStyles FontStyleToFontStyles(FontStyle fontStyle)
    {
        switch (fontStyle)
        {
            case FontStyle.Normal:
                return FontStyles.Normal;
 
            case FontStyle.Bold:
                return FontStyles.Bold;
 
            case FontStyle.Italic:
                return  FontStyles.Italic;
 
            case FontStyle.BoldAndItalic:
                return FontStyles.Bold | FontStyles.Italic;
        }
 
        Debug.LogWarning("Unhandled font style " + fontStyle);
        return FontStyles.Normal;
    }
 
    static TextAlignmentOptions TextAnchorToTextAlignmentOptions(TextAnchor textAnchor)
    {
        switch (textAnchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
 
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
 
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
 
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;
 
            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;
 
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;
 
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
 
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
 
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
        }
 
        Debug.LogWarning("Unhandled text anchor " + textAnchor);
        return TextAlignmentOptions.TopLeft;
    }
}