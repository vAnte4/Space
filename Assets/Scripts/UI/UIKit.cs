// ============================================================
//  UIKit.cs  -  Herramientas para dibujar la interfaz por código
//  (botones, textos, barras, círculos). Así NO necesitas armar
//  la UI a mano en el editor y casi no hay conflictos en Git.
//  Coordenadas: (0,0) = esquina superior izquierda, tamaño de
//  referencia 1920 x 1080.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace AstroJunior
{
    public class BarRef
    {
        public Image fill;
        public float width;
        public float cur, target;
        public void Apply()
        {
            if (fill == null) return;
            float w = Mathf.Max(0.5f, width * Mathf.Clamp01(cur));
            fill.rectTransform.sizeDelta = new Vector2(w, fill.rectTransform.sizeDelta.y);
            fill.enabled = cur > 0.002f;
        }
    }

    public static class UIKit
    {
        public static Font font;
        public static Sprite round, circle, pixel, star;
        static bool ready;

        // Paleta de colores
        public static readonly Color Navy = new Color(0.03f, 0.05f, 0.13f, 1f);
        public static readonly Color Panel = new Color(0.09f, 0.13f, 0.27f, 0.96f);
        public static readonly Color PanelLight = new Color(0.16f, 0.22f, 0.42f, 1f);
        public static readonly Color White = new Color(1f, 1f, 1f, 1f);
        public static readonly Color Gray = new Color(0.72f, 0.78f, 0.9f, 1f);
        public static readonly Color Yellow = new Color(1f, 0.82f, 0.2f, 1f);
        public static readonly Color Orange = new Color(1f, 0.55f, 0.15f, 1f);
        public static readonly Color Red = new Color(0.95f, 0.3f, 0.3f, 1f);
        public static readonly Color Green = new Color(0.3f, 0.85f, 0.45f, 1f);
        public static readonly Color Cyan = new Color(0.35f, 0.85f, 1f, 1f);
        public static readonly Color Purple = new Color(0.7f, 0.45f, 1f, 1f);
        public static readonly Color Dark = new Color(0.1f, 0.12f, 0.2f, 1f);

        public static void Init()
        {
            if (ready) return;
            ready = true;
#if UNITY_2022_2_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            round = MakeRound(64, 20);
            circle = MakeCircle(128);
            pixel = MakePixel();
            star = MakeStar(128);
        }

        // ---------------- Sprites generados por código ----------------
        static Sprite MakePixel()
        {
            Texture2D t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) t.SetPixel(x, y, Color.white);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeRound(int size, int r)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float cx = Mathf.Clamp(px, r, size - r);
                    float cy = Mathf.Clamp(py, r, size - r);
                    float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        static Sprite MakeCircle(int size)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            float rad = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(rad, rad));
                    float a = Mathf.Clamp01(rad - d);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeStar(int size)
        {
            Vector2[] p = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0 ? 0.48f : 0.20f) * size;
                p[i] = new Vector2(size * 0.5f + Mathf.Cos(a) * rad, size * 0.5f + Mathf.Sin(a) * rad);
            }
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = false;
                    float px = x + 0.5f, py = y + 0.5f;
                    for (int i = 0, j = 9; i < 10; j = i++)
                    {
                        if (((p[i].y > py) != (p[j].y > py)) &&
                            (px < (p[j].x - p[i].x) * (py - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                            inside = !inside;
                    }
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
                }
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------------- Elementos básicos ----------------
        public static RectTransform NewRect(string name, Transform parent, float x, float y, float w, float h)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Box(Transform parent, float x, float y, float w, float h, Color c, bool rounded)
        {
            RectTransform rt = NewRect("Box", parent, x, y, w, h);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? round : pixel;
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Image Box(Transform parent, float x, float y, float w, float h, Color c)
        {
            return Box(parent, x, y, w, h, c, true);
        }

        // Círculo centrado en (cx, cy) con diámetro d
        public static Image Circle(Transform parent, float cx, float cy, float d, Color c)
        {
            RectTransform rt = NewRect("Circle", parent, cx - d * 0.5f, cy - d * 0.5f, d, d);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = circle;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Image StarIcon(Transform parent, float cx, float cy, float d, Color c)
        {
            RectTransform rt = NewRect("Star", parent, cx - d * 0.5f, cy - d * 0.5f, d, d);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = star;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color col, TextAnchor anchor, FontStyle style)
        {
            RectTransform rt = NewRect("Text", parent, x, y, w, h);
            Text t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = col;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, size / 2);
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            return t;
        }

        public static Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color col)
        {
            return Label(parent, text, x, y, w, h, size, col, TextAnchor.UpperLeft, FontStyle.Normal);
        }

        public static Button Btn(Transform parent, string text, float x, float y, float w, float h, Color bg, UnityAction onClick, int size)
        {
            Image img = Box(parent, x, y, w, h, bg, true);
            img.raycastTarget = true;
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            cb.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            cb.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            Label(img.transform, text, 8f, 0f, w - 16f, h, size, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            b.onClick.AddListener(delegate { AudioFx.Click(); onClick(); });
            return b;
        }

        // Barra de progreso (el relleno se anima en GameUI.Update)
        public static BarRef Bar(Transform parent, float x, float y, float w, float h, Color fillColor)
        {
            Box(parent, x, y, w, h, new Color(1f, 1f, 1f, 0.14f), true);
            Image f = Box(parent, x, y, w, h, fillColor, true);
            BarRef b = new BarRef();
            b.fill = f;
            b.width = w;
            return b;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
