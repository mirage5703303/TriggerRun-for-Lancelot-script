using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IEdgeGames {

    [Serializable]
    public sealed class SceneReference : ISerializationCallbackReceiver {

#pragma warning disable 0649
#if UNITY_EDITOR
        [SerializeField] private SceneAsset m_asset; // hidden by the drawer
#endif
#pragma warning restore 0649
        [SerializeField] private string m_path; // hidden by the drawer

#if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        public SceneAsset Asset => m_asset ? m_asset : (m_asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(m_path));
#endif

        /// <summary>
        /// 
        /// </summary>
        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path.Substring(Path.LastIndexOf('/') + 1));

        /// <summary>
        /// The full path of the scene.
        /// </summary>
        public string Path => m_path;

        /// <summary>
        /// Scene build index.
        /// </summary>
        public int BuildIndex => SceneUtility.GetBuildIndexByScenePath(m_path);

        /// <summary>
        /// 
        /// </summary>
        public SceneReference() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buildIndex"></param>
        public SceneReference(int buildIndex) => SetReferences(SceneUtility.GetScenePathByBuildIndex(buildIndex));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenePath"></param>
        public SceneReference(string scenePath) => SetReferences(scenePath);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenePath"></param>
        public SceneReference(Scene scene) => SetReferences(scene.path);

        void SetReferences(string scenePath) {
            m_path = scenePath;
#if UNITY_EDITOR
            m_asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(m_path);
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {
#if UNITY_EDITOR
            EditorApplication.delayCall += () => m_path = m_asset ? AssetDatabase.GetAssetPath(m_asset) : "";
#endif
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

#if UNITY_EDITOR

        [CustomPropertyDrawer(typeof(SceneReference))]
        internal sealed class SceneReferencePropertyDrawer : PropertyDrawer {

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var relative = property.FindPropertyRelative(nameof(m_asset));
                var content = EditorGUI.BeginProperty(position, label, relative);

                EditorGUI.BeginChangeCheck();

                var source = relative.objectReferenceValue;
                var target = EditorGUI.ObjectField(position, content, source, typeof(SceneAsset), false);

                if (EditorGUI.EndChangeCheck())
                    relative.objectReferenceValue = target;

                EditorGUI.EndProperty();
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
                return EditorGUIUtility.singleLineHeight;
            }
        }
#endif
    }
}
