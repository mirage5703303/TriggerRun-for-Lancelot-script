using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using uObject = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IEdgeGames {

    public static class UnityEngineExtensions {

        // ========================================================================================
        // Behaviour Extensions
        // ========================================================================================

        /// <summary>
        /// Removes a GameObject, component or asset.
        /// </summary>
        /// <param name="obj"></param>
        public static void Destroy(this uObject obj) {
            if (obj) uObject.Destroy(obj);
        }

        /// <summary>
        /// Destroys the object obj immediately. You are strongly recommended to use Destroy instead.
        /// </summary>
        /// <param name="obj"></param>
        public static void DestroyImmediate(this uObject obj) {
            if (obj) uObject.DestroyImmediate(obj);
        }

        /// <summary>
        /// Destroy a GameObject, component or asset collection.
        /// </summary>
        /// <param name="objects"></param>
        public static void Destroy(this IEnumerable<uObject> objects) {
            foreach (var obj in objects) uObject.Destroy(obj);
        }

        /// <summary>
        /// Destroy a GameObject, component or asset collection immediately.
        /// </summary>
        /// <param name="objects"></param>
        public static void DestroyImmediate(this IEnumerable<uObject> objects) {
            foreach (var obj in objects) uObject.DestroyImmediate(obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="seconds"></param>
        /// <param name="onAwake"></param>
        /// <returns></returns>
        public static Coroutine Sleep(this MonoBehaviour behaviour, float seconds, Action onAwake)
            => behaviour.StartCoroutine(Sleep_Coroutine(seconds, onAwake));

        private static IEnumerator Sleep_Coroutine(float seconds, Action onAwake) {
            var time = 0f;

            while (time < seconds) {
                time += Time.deltaTime;
                yield return null;
            }

            onAwake?.Invoke();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="behaviour"></param>
        /// <param name="onSkipFrame"></param>
        /// <returns></returns>
        public static Coroutine SkipFrame(this MonoBehaviour behaviour, Action onSkipFrame)
            => behaviour.StartCoroutine(SkipFrame_Coroutine(onSkipFrame));

        private static IEnumerator SkipFrame_Coroutine(Action onSkipFrame) {
            yield return null;
            onSkipFrame?.Invoke();
        }

        // ========================================================================================
        // GameObject Extensions
        // ========================================================================================

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="active"></param>
        public static void SetActive(this IEnumerable<GameObject> objects, bool active) {
            foreach (var go in objects)
                go.SetActive(active);
        }

        /// <summary>
        /// Returns <see cref="GameObject"/> childs.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="recursively"></param>
        /// <returns></returns>
        public static GameObject[] GetChilds(this GameObject gameObject, bool recursively = false) {
            var result = new List<GameObject>();

            if (recursively)
                GetChildRecursive(gameObject, ref result);
            else
                foreach (Transform child in gameObject.transform)
                    result.Add(child.gameObject);

            return result.ToArray();
        }

        private static void GetChildRecursive(GameObject parent, ref List<GameObject> list) {
            if (!parent)
                return;

            foreach (Transform child in parent.transform) {
                if (!child)
                    continue;

                list.Add(child.gameObject);
                GetChildRecursive(child.gameObject, ref list);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="newLayerName"></param>
        public static void SetLayerRecursively(this GameObject root, string newLayerName) {
            var newLayer = LayerMask.NameToLayer(newLayerName);
            recurseSetLayer(root.transform);

            void recurseSetLayer(Transform parent) {
                parent.gameObject.layer = newLayer != -1 ? newLayer : parent.gameObject.layer;
                foreach (Transform child in parent)
                    recurseSetLayer(child);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="methodName"></param>
        /// <param name="options"></param>
        public static void SendMessage(this IEnumerable<GameObject> objects, string methodName, SendMessageOptions options = SendMessageOptions.DontRequireReceiver) {
            foreach (var child in objects.Where(go => go.activeInHierarchy && go.activeSelf))
                child.SendMessage(methodName, options);
        }

        // ========================================================================================
        // Transform Extensions
        // ========================================================================================

        /// <summary>
        /// Returns <see cref="Transform"/> childs.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="recursively"></param>
        /// <returns></returns>
        public static Transform[] GetChilds(this Transform transform, bool recursively = false) {
            var result = new List<GameObject>();

            if (recursively)
                GetChildRecursive(transform.gameObject, ref result);
            foreach (Transform child in transform)
                result.Add(child.gameObject);

            return result.Select(g => g.transform).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transforms"></param>
        /// <param name="parent"></param>
        public static void SetParent(this IEnumerable<Transform> transforms, Transform parent) {
            foreach (var t in transforms)
                t.SetParent(parent);
        }

        /// <summary>
        /// Adds a <see cref="RectTransform"/> expanded to the given <see cref="Transform"/>.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static RectTransform AddRectTransformExpanded(this Transform transform) {
            var result = transform.gameObject.AddComponent<RectTransform>();

            result.localScale = new Vector3(1, 1, 1);
            result.anchorMin = Vector2.zero;
            result.anchorMax = new Vector2(1, 1);
            result.anchoredPosition = Vector2.zero;
            result.sizeDelta = Vector2.zero;

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static Bounds GetLocalBounds(this Transform transform) {
            var rotation = transform.rotation;
            transform.rotation = Quaternion.identity;
            var bounds = new Bounds(transform.position, Vector3.zero);

            ExtendBounds(transform, ref bounds);

            bounds.center -= transform.position;
            transform.rotation = rotation;

            return bounds;
        }

        // ========================================================================================
        // RectTransform Extensions
        // ========================================================================================

        private enum ScreenBorder {
            Left,
            Right,
            Top,
            Bottom
        }

        /// <summary>
        /// Check if rect containns point.
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        public static bool ContainsPoint(this RectTransform rt, Vector3 point) => rt.rect.Contains(rt.InverseTransformPoint(point));

        /// <summary>
        /// Returns the <see cref="Rect"/> offset based on the current <see cref="Screen"/> size.
        /// </summary>
        /// <param name="rt"></param>
        /// <returns></returns>
        public static Rect GetRectOffset(this RectTransform rt) {
            return new Rect(rt.rect) {
                xMin = rt.GetCoordinateOffset(ScreenBorder.Left),
                xMax = rt.GetCoordinateOffset(ScreenBorder.Right),
                yMin = rt.GetCoordinateOffset(ScreenBorder.Bottom),
                yMax = rt.GetCoordinateOffset(ScreenBorder.Top)
            };
        }

        private static float GetCoordinateOffset(this RectTransform rt, ScreenBorder border) {
            var originalPosition = rt.anchoredPosition;
            var corners = new Vector3[4];
            var result = .0f;
            var flag = false;

            rt.anchoredPosition = Vector2.zero;

            while (!flag)
                switch (border) {
                    case ScreenBorder.Right:
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x + 1, 0);
                        rt.GetWorldCorners(corners);

                        if (corners[2].x > Screen.width) {
                            flag = true;
                            result = Convert.ToInt32(rt.position.x);
                        }
                        break;

                    case ScreenBorder.Left:
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - 1, 0);
                        rt.GetWorldCorners(corners);

                        if (corners[0].x < 0) {
                            flag = true;
                            result = Convert.ToInt32(rt.position.x);
                        }
                        break;

                    case ScreenBorder.Top:
                        rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y + 1);
                        rt.GetWorldCorners(corners);

                        if (corners[1].y > Screen.height) {
                            flag = true;
                            result = Convert.ToInt32(rt.position.y);
                        }
                        break;

                    case ScreenBorder.Bottom:
                        rt.anchoredPosition = new Vector2(0, rt.anchoredPosition.y - 1);
                        rt.GetWorldCorners(corners);

                        if (corners[3].y < 0) {
                            flag = true;
                            result = Convert.ToInt32(rt.position.y);
                        }
                        break;

                    default: flag = true; break;
                }

            rt.anchoredPosition = originalPosition;
            return result;
        }

        // ========================================================================================
        // BoxCollider Extensions
        // ========================================================================================

        static void ExtendBounds(Transform t, ref Bounds b) {
            var renderer = t.GetComponent<Renderer>();
            
            if (renderer) {
                b.Encapsulate(renderer.bounds.min);
                b.Encapsulate(renderer.bounds.max);
            }

            foreach (Transform t2 in t)
                ExtendBounds(t2, ref b);
        }

#if UNITY_EDITOR

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("Tools/Editor Extensions/Fit To Children", false, 100)]
        static void FitToChildren() {
            var boxCollider = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<BoxCollider>() : null;

            if (boxCollider)
                FitToChildren(boxCollider);
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="boxCollider"></param>
        public static void FitToChildren(this BoxCollider boxCollider) {
            var rotation = boxCollider.transform.rotation;
            boxCollider.transform.rotation = Quaternion.identity;
            var bounds = new Bounds(boxCollider.transform.position, Vector3.zero);

            ExtendBounds(boxCollider.transform, ref bounds);

            boxCollider.center = bounds.center - boxCollider.transform.position;
            boxCollider.size = bounds.size;

            boxCollider.transform.rotation = rotation;
        }

        // ========================================================================================
        // Vector Extensions
        // ========================================================================================

        /// <summary>
        /// Returns a new <see cref="ToVector2Int"/>.
        /// </summary>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector2Int ToVector2Int(this Vector2 v2)
            => new Vector2Int(Convert.ToInt32(v2.x), Convert.ToInt32(v2.y));

        /// <summary>
        /// Returns a new <see cref="ToVector3Int"/>.
        /// </summary>
        /// <param name="v3"></param>
        /// <returns></returns>
        public static Vector3Int ToVector3Int(this Vector3 v3)
            => new Vector3Int(Convert.ToInt32(v3.x), Convert.ToInt32(v3.y), Convert.ToInt32(v3.z));

        // ========================================================================================
        // Texture2D Extensions
        // ========================================================================================

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Texture Copy(this Texture src) {
            var newTexture = new Texture2D(src.width, src.height, (src as Texture2D).format, src.mipmapCount, false);
            Graphics.CopyTexture(src, newTexture);
            return newTexture;
        }
    }
}
