using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class CopySkeletonWindow : EditorWindow
{
    [MenuItem("Window/CopySkeletonWindow")]
    public static void OpenWindow()
    {
        var window = GetWindow<CopySkeletonWindow>();
        window.titleContent = new GUIContent("Copy skeleton");
    }

    private Dictionary<string, Transform> originalBones;
    private Dictionary<string, Transform> targetBones;

    private Transform original;
    private Transform target;
    private void OnGUI()
    {
        original = EditorGUILayout.ObjectField("Original", original, typeof(Transform), true) as Transform;
        target = EditorGUILayout.ObjectField("Target", target, typeof(Transform), true) as Transform;
        GUI.enabled = (original != null && target!= null);
        if (GUILayout.Button("Update Skinned Mesh Renderer"))
        {
            originalBones = new Dictionary<string, Transform>();
            Transform[] allChildren = original.GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                int number = 1;
                if(!originalBones.ContainsKey(child.name))
                    originalBones.Add(child.name, child);
                else
                {
                    string newName = child.name + "(" + number + ")";
                    while (originalBones.ContainsKey(newName))
                    {
                        newName = child.name + "(" + number + ")";
                    }

                    originalBones.Add(newName, child);
                }
            }

            targetBones = new Dictionary<string, Transform>();
            allChildren = target.GetComponentsInChildren<Transform>();
            foreach (Transform targetBone in allChildren)
            {

                Transform originalBone;
                if (originalBones.TryGetValue(targetBone.name, out originalBone))
                {
                    if (!originalBone.parent.name.Equals(targetBone.parent.name) || targetBones.ContainsKey(originalBone.name))
                    {
                        Debug.Log(originalBone.parent.name);
                        continue;
                    }

                    targetBones.Add(targetBone.name, targetBone);
                    targetBone.localPosition = originalBone.localPosition;
                    targetBone.localPosition = originalBone.localPosition;
                    targetBone.localScale = originalBone.localScale;
                }
                else
                {
                    Debug.Log("Wrong bone " + targetBone.name);
                }
            }

            bool hasErrors = false;
            int maxTries = 5;
            int currentTries = 0;

            do
            {
                hasErrors = false;
                foreach (var originBone in originalBones)
                {
                    if (!targetBones.ContainsKey(originBone.Key))
                    {

                        if (!targetBones.ContainsKey(originBone.Value.parent.name))
                        {
                            hasErrors = true;
                            continue;
                        }

                        GameObject newBone = Instantiate(originBone.Value.gameObject);
                        newBone.transform.parent = targetBones[originBone.Value.parent.name];
                        newBone.transform.localPosition = originBone.Value.localPosition;
                        newBone.transform.localRotation = originBone.Value.localRotation;
                        newBone.transform.localScale = originBone.Value.localScale;
                        newBone.name = newBone.name.Remove(newBone.name.Length - "(Clone)".Length);

                        targetBones.Add(originBone.Key, newBone.transform);
                        Debug.Log("Creating " + originBone.Value.name);
                    }
                }

                currentTries++;
            }
            while (hasErrors && currentTries > maxTries);

            Debug.Log(currentTries);

        }
    }
}