using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SilencioBiblioteca
{
    // Utilidades para construir UI em código. A demo não usa assets de UI:
    // painéis e sprites são gerados aqui, em runtime.
    public static class UiKit
    {
        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
#if UNITY_2022_1_OR_NEWER
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
                }
                return _font;
            }
        }

        static Sprite _round;

        // Retângulo branco com cantos arredondados + 9-slice, usado em painéis e botões.
        public static Sprite Round
        {
            get
            {
                if (_round != null) return _round;
                int s = 64, r = 14;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                {
                    for (int x = 0; x < s; x++)
                    {
                        float dx = Mathf.Max(r - x, 0);
                        float dy = Mathf.Max(r - y, 0);
                        float dx2 = Mathf.Max(x - (s - 1 - r), 0);
                        float dy2 = Mathf.Max(y - (s - 1 - r), 0);
                        float d = Mathf.Sqrt(dx * dx + dy * dy + dx2 * dx2 + dy2 * dy2);
                        float a = Mathf.Clamp01(r - d + 0.5f);
                        byte c = (byte)(a * 255f);
                        px[y * s + x] = new Color32(255, 255, 255, c);
                    }
                }
                tex.SetPixels32(px);
                tex.Apply();
                var border = new Vector4(r, r, r, r);
                _round = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect, border);
                return _round;
            }
        }

        static Sprite _personSilver;
        static Sprite _personGreen;
        static Sprite _personPink;
        static Sprite _personGray;

        // Silhueta simples de pessoa (28x44 px) para representar os clientes.
        public static Sprite PersonSprite(string kind)
        {
            switch (kind)
            {
                case "almeida":
                    if (_personPink == null) _personPink = MakePersonSprite(new Color32(176, 74, 110, 255), new Color32(201, 194, 184, 255), new Color32(176, 74, 110, 255), false);
                    return _personPink;
                case "abreu":
                    if (_personGreen == null) _personGreen = MakePersonSprite(new Color32(78, 122, 82, 255), new Color32(46, 38, 32, 255), new Color32(59, 63, 68, 255), false);
                    return _personGreen;
                default:
                    if (_personGray == null) _personGray = MakePersonSprite(new Color32(110, 115, 120, 255), new Color32(30, 27, 24, 255), new Color32(35, 38, 43, 255), true);
                    return _personGray;
            }
        }

        static Sprite MakePersonSprite(Color cloth, Color hair, Color pants, bool hat)
        {
            int w = 28, h = 44;
            var px = new Color[w * h];
            Fill(px, w, h, 11, 37, 17, 43, hair);      // cabeça (bloco)
            Fill(px, w, h, 9, 26, 19, 37, cloth);      // tronco
            Fill(px, w, h, 7, 20, 10, 37, new Color(cloth.r * 0.8f, cloth.g * 0.8f, cloth.b * 0.8f)); // braço esq
            Fill(px, w, h, 18, 20, 21, 37, new Color(cloth.r * 0.8f, cloth.g * 0.8f, cloth.b * 0.8f)); // braço dir
            Fill(px, w, h, 10, 0, 13, 21, pants);      // perna esq
            Fill(px, w, h, 15, 0, 18, 21, pants);      // perna dir
            Fill(px, w, h, 8, 0, 20, 2, new Color32(30, 30, 34, 255)); // sapatos
            if (hat)
            {
                Fill(px, w, h, 9, 40, 19, 42, new Color32(38, 34, 30, 255)); // aba
                Fill(px, w, h, 11, 42, 17, 46, new Color32(38, 34, 30, 255)); // copa
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 44f);
        }

        static void Fill(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color c)
        {
            x0 = Mathf.Clamp(x0, 0, w - 1);
            x1 = Mathf.Clamp(x1, 0, w - 1);
            y0 = Mathf.Clamp(y0, 0, h - 1);
            y1 = Mathf.Clamp(y1, 0, h - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    px[y * w + x] = c;
        }

        // ------------------------- construtores de UI -------------------------

        public static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // Preenche o pai inteiro.
        public static RectTransform Stretch(Transform parent, string name)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        // Posiciona por âncoras (frações do pai) — usado para hotspots sobre o cenário.
        public static RectTransform Anchored(Transform parent, string name, Vector2 aMin, Vector2 aMax)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(Transform parent, string name, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            var rt = NewRect(parent, name);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, bool raycast, Sprite sprite = null, bool sliced = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        public static Text Txt(Transform parent, string name, string content, int size, Color color, TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = content;
            return t;
        }

        public static Button Btn(Transform parent, string name, string label, UnityAction onClick, Color bg, Color fg, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Round;
            img.type = Image.Type.Sliced;
            img.color = bg;
            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.05f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var rt = (RectTransform)go.transform;
            var lt = Txt(go.transform, "Label", label, fontSize, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            var lrt = (RectTransform)lt.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 2);
            lrt.offsetMax = new Vector2(-8, -2);
            return btn;
        }

        public static InputField MakeInput(Transform parent, string name, string placeholder, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Round;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300, 52);

            var field = go.GetComponent<InputField>();

            var valGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            valGo.transform.SetParent(go.transform, false);
            var vrt = (RectTransform)valGo.transform;
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = new Vector2(10, 2);
            vrt.offsetMax = new Vector2(-10, -2);
            var val = valGo.GetComponent<Text>();
            val.font = Font;
            val.fontSize = fontSize;
            val.color = Color.white;
            val.alignment = TextAnchor.MiddleCenter;
            val.supportRichText = false;
            field.textComponent = val;

            var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            phGo.transform.SetParent(go.transform, false);
            var prt = (RectTransform)phGo.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(10, 2);
            prt.offsetMax = new Vector2(-10, -2);
            var ph = phGo.GetComponent<Text>();
            ph.font = Font;
            ph.fontSize = fontSize;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.alignment = TextAnchor.MiddleCenter;
            ph.text = placeholder;
            field.placeholder = ph;

            field.characterLimit = 8;
            field.contentType = InputField.ContentType.Standard;
            return field;
        }
    }
}
