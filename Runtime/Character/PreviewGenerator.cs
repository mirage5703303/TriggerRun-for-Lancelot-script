using UnityEngine;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IEdgeGames {

    public class PreviewGenerator : MonoBehaviour {

        public Sprite icon;

#if UNITY_EDITOR
        [ShowInInspector, PreviewField]
        private Sprite m_TempPreview;

        [Button]
        void GenerateTempPreview() => m_TempPreview = RTImage();

        Sprite RTImage() {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_TriggeRun/Sprites/Temp Icons/{name}.png");

            if (sprite)
                return sprite;

            var camera = GetComponentInChildren<Camera>();

            if (!camera)
                return m_TempPreview;

            var rt = camera.targetTexture;
            var screenShot = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);

            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            screenShot.Apply();

            System.IO.File.WriteAllBytes($"Assets/_TriggeRun/Sprites/Temp Icons/{name}.png", screenShot.EncodeToPNG());
            AssetDatabase.Refresh();

            RenderTexture.active = null;

            var s = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_TriggeRun/Sprites/Temp Icons/{name}.png");

            if (s) {
                var ti = AssetImporter.GetAtPath($"Assets/_TriggeRun/Sprites/Temp Icons/{name}.png") as TextureImporter;
                var tis = new TextureImporterSettings();

                tis.ApplyTextureType(TextureImporterType.Sprite);
                tis.spriteMeshType = SpriteMeshType.FullRect;
                tis.spriteGenerateFallbackPhysicsShape = false;

                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp;

                ti.SetTextureSettings(tis);
                ti.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_TriggeRun/Sprites/Temp Icons/{name}.png");
        }
#endif
    }
}
