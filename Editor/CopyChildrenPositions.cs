using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class CopyChildrenPositions : EditorWindow
{
    [MenuItem("Window/Copy children positions")]
    public static void OpenWindow()
    {
        var window = GetWindow<CopyChildrenPositions>();
        window.titleContent = new GUIContent("Copy children positions");
    }

    private Dictionary<string, Transform> originalTransforms;
    private Dictionary<string, Transform> targetTransforms;

    private Transform original;
    private Transform target;
    private void OnGUI()
    {
        original = EditorGUILayout.ObjectField("Original", original, typeof(Transform), true) as Transform;
        target = EditorGUILayout.ObjectField("Target", target, typeof(Transform), true) as Transform;
        GUI.enabled = (original != null && target!= null);
        if (GUILayout.Button("Update positions"))
        {
            originalTransforms = new Dictionary<string, Transform>();

            for(int i = 0; i < original.childCount; i++)
            {
                originalTransforms.Add(original.GetChild(i).name, original.GetChild(i));
            }


            targetTransforms = new Dictionary<string, Transform>();
            for (int i = 0; i < target.childCount; i++)
            {
                targetTransforms.Add(target.GetChild(i).name, target.GetChild(i));
            }

            foreach (var originalChild in originalTransforms)
            {
                if (targetTransforms.ContainsKey(originalChild.Key))
                {
                    targetTransforms[originalChild.Key].localPosition = originalChild.Value.localPosition;
                    targetTransforms[originalChild.Key].localRotation = originalChild.Value.localRotation;
                    targetTransforms[originalChild.Key].localScale = originalChild.Value.localScale;
                }
            }
        }
    }
}