using System;
using UnityEngine;

public class EnsureSingleInstance : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectsOfType<EnsureSingleInstance>().Length > 1) DestroyImmediate(gameObject);
    }
}