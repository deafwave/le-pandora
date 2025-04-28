using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using System.Collections.Generic;
using MelonLoader;
using Il2Cpp;

namespace LastEpochPandora.Services
{
    [RegisterTypeInIl2Cpp]
    public class ESPService : MonoBehaviour
    {
        private static GameObject espRoot;
        private static readonly List<ESPElement> elements = new();
        private static bool isCleanupScheduled = false;
        private static float lastCleanupTime = 0f;
        private static float cleanupInterval = 0.5f;
        private static float drawInterval = 0.1f;
        private static float lastDrawTime = 0f;
        private static bool canDraw = true;
        private static float defaultElementDuration = 0.6f;
        private static readonly Queue<GameObject> lineObjectPool = new Queue<GameObject>();
        private static readonly Queue<GameObject> labelObjectPool = new Queue<GameObject>();
        private static int maxPoolSize = 100;
        private static Camera mainCamera;

        public ESPService(System.IntPtr ptr) : base(ptr) { }

        public static void Initialize()
        {
            if (espRoot != null) return;

            espRoot = new GameObject("Pandora_ESP_Root");
            espRoot.hideFlags = HideFlags.HideAndDontSave;
            GameObject.DontDestroyOnLoad(espRoot);

            var manager = espRoot.AddComponent<ESPService>();
            mainCamera = Camera.main;

            MelonLogger.Msg("ESPManager initialized.");
        }

        public static void ClearElements()
        {
            foreach (var element in elements)
            {
                if (element != null && element.gameObject != null)
                {
                    ReturnToPool(element.gameObject);
                }
            }
            elements.Clear();
        }

        private static GameObject GetLineObject()
        {
            if (lineObjectPool.Count > 0)
            {
                var obj = lineObjectPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            var newObj = new GameObject("ESP_Line");
            newObj.transform.SetParent(espRoot.transform, false);
            var line = newObj.AddComponent<LineRenderer>();
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            return newObj;
        }

        private static GameObject GetLabelObject()
        {
            if (labelObjectPool.Count > 0)
            {
                var obj = labelObjectPool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            var newObj = new GameObject("ESP_Label");
            newObj.transform.SetParent(espRoot.transform, false);
            var textMesh = newObj.AddComponent<TextMesh>();
            textMesh.characterSize = 0.25f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            newObj.AddComponent<BillboardLabel>();

            return newObj;
        }

        private static void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);

            if (obj.name.StartsWith("ESP_Line"))
            {
                if (lineObjectPool.Count < maxPoolSize)
                    lineObjectPool.Enqueue(obj);
                else
                    GameObject.Destroy(obj);
            }
            else if (obj.name.StartsWith("ESP_Label"))
            {
                if (labelObjectPool.Count < maxPoolSize)
                    labelObjectPool.Enqueue(obj);
                else
                    GameObject.Destroy(obj);
            }
            else
            {
                GameObject.Destroy(obj);
            }
        }

        public static void QueueLine(Vector3 start, Vector3 end, Color color, float duration = -1, float alpha = 1.0f)
        {
            if (!canDraw) return;

            if (duration < 0) duration = defaultElementDuration;

            var obj = GetLineObject();
            var line = obj.GetComponent<LineRenderer>();

            line.positionCount = 2;
            Color lineColor = new Color(color.r, color.g, color.b, color.a * alpha);
            line.startColor = line.endColor = lineColor;

            line.SetPosition(0, start);
            line.SetPosition(1, end);

            elements.Add(new ESPElement(obj, Time.time + duration));
        }

        public static void QueueLabel(Vector3 position, string text, Color color, float duration = -1, float alpha = 1.0f)
        {
            if (!canDraw) return;

            if (duration < 0) duration = defaultElementDuration;

            var obj = GetLabelObject();
            obj.transform.position = position;

            var textMesh = obj.GetComponent<TextMesh>();
            textMesh.text = text;
            Color labelColor = new Color(color.r, color.g, color.b, color.a * alpha);
            textMesh.color = labelColor;

            elements.Add(new ESPElement(obj, Time.time + duration));
        }

        public static void QueueDistanceLabel(Vector3 from, Vector3 to, Color color, float duration = -1, bool useMidpoint = true, float alpha = 1.0f)
        {
            if (!canDraw) return;

            float distance = Vector3.Distance(from, to);
            Vector3 labelPos = useMidpoint ? Vector3.Lerp(from, to, 0.5f) : to;

            QueueLabel(labelPos, $"{distance:F1}m", color, duration, alpha);
        }

        [HideFromIl2Cpp]
        void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            float now = Time.time;

            if (now - lastDrawTime >= drawInterval)
            {
                lastDrawTime = now;
                canDraw = true;
            }
            else
            {
                canDraw = false;
            }

            if (!isCleanupScheduled && now - lastCleanupTime >= cleanupInterval)
            {
                lastCleanupTime = now;
                isCleanupScheduled = true;
                CleanupElements();
            }
        }

        private void CleanupElements()
        {
            float now = Time.time;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                if (now >= elements[i].despawnTime)
                {
                    if (elements[i].gameObject != null)
                        ReturnToPool(elements[i].gameObject);

                    elements.RemoveAt(i);
                }
            }
            isCleanupScheduled = false;
        }

        private class ESPElement
        {
            public GameObject gameObject;
            public float despawnTime;

            public ESPElement(GameObject obj, float time)
            {
                gameObject = obj;
                despawnTime = time;
            }
        }
    }

    [RegisterTypeInIl2Cpp]
    public class BillboardLabel : MonoBehaviour
    {
        private Camera targetCamera;

        public BillboardLabel(System.IntPtr ptr) : base(ptr) { }

        void Start()
        {
            targetCamera = Camera.main;
        }

        void Update()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                    return;
            }

            transform.rotation = targetCamera.transform.rotation;
        }
    }
}