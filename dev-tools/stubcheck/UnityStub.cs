// Stub mínimo do UnityEngine para checagem de compilação fora do Unity.
// NÃO é incluído no build do jogo — serve só para validar sintaxe/tipos via `dotnet build`.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
    }

    public class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero = new Vector2(0, 0);
        public static Vector2 one = new Vector2(1, 1);
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static float Distance(Vector2 a, Vector2 b) { return 0f; }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return a; }
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public Color(float r, float g, float b) : this(r, g, b, 1f) { }
        public static Color white = new Color(1, 1, 1, 1);
        public static Color black = new Color(0, 0, 0, 1);
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
        public static implicit operator Color32(Color c) { return new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(c.a * 255f)); }
    }

    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point, Bilinear }
    public enum SpriteMeshType { FullRect, Tight }

    public class Texture2D : Object
    {
        public FilterMode filterMode;
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixels32(Color32[] p) { }
        public void SetPixels(Color[] p) { }
        public void Apply() { }
    }

    public class Sprite : Object
    {
        public static Sprite Create(Texture2D tex, Rect r, Vector2 pivot, float ppu) { return null; }
        public static Sprite Create(Texture2D tex, Rect r, Vector2 pivot, float ppu, uint extrude, SpriteMeshType type, Vector4 border) { return null; }
    }

    public class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
    }

    public class Font : Object { }

    public static class Mathf
    {
        public static float Clamp01(float v) { return 0f; }
        public static int Clamp(int v, int a, int b) { return v; }
        public static float Clamp(float v, float a, float b) { return v; }
        public static float Lerp(float a, float b, float t) { return a; }
        public static float SmoothStep(float a, float b, float t) { return a; }
        public static float Max(float a, float b) { return a; }
        public static float Min(float a, float b) { return a; }
        public static int Max(int a, int b) { return a; }
        public static int Min(int a, int b) { return a; }
        public static float Sqrt(float v) { return v; }
        public static int FloorToInt(float v) { return 0; }
        public static float PerlinNoise(float x, float y) { return 0.5f; }
        public static float Sin(float v) { return 0f; }
    }

    public static class Time
    {
        public static float deltaTime;
        public static float time;
        public static float unscaledDeltaTime;
        public static float unscaledTime;
    }

    public enum KeyCode { F, F1, J, Escape }
    public static class Input
    {
        public static bool GetKeyDown(KeyCode k) { return false; }
    }

    public class WaitForSeconds { public WaitForSeconds(float s) { } }
    public class Coroutine { }

    public class Component
    {
        public GameObject gameObject;
        public Transform transform;
        public string name;
        public T GetComponent<T>() where T : Component { return null; }
        public T GetComponentInChildren<T>(bool includeInactive) where T : Component { return null; }
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public T GetComponent<T>() where T : Component { return null; }
        public T GetComponentInChildren<T>(bool includeInactive) where T : Component { return null; }
        public T AddComponent<T>() where T : Component, new() { return null; }
        public Component AddComponent(Type t) { return null; }
        public void SetActive(bool active) { }
        public bool activeSelf { get; }
        public string tag;
        public Transform transform { get; }
        public void SetParent(Transform parent, bool worldStays) { }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component { return null; }
    }

    public class Transform : Component
    {
        public void SetParent(Transform parent, bool worldStays) { }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, sizeDelta, anchoredPosition, pivot;
    }

    public class Behaviour : Component
    {
        public bool enabled;
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator e) { return null; }
        public void StopCoroutine(Coroutine c) { }
        public void StopAllCoroutines() { }
    }

    namespace Events
    {
        public delegate void UnityAction();
        public delegate void UnityAction<T0>(T0 arg0);

        public class UnityEventBase { }

        public class UnityEvent : UnityEventBase
        {
            public void AddListener(UnityAction call) { }
            public void RemoveAllListeners() { }
        }

        public class UnityEvent<T0> : UnityEventBase
        {
            public void AddListener(UnityAction<T0> call) { }
            public void RemoveAllListeners() { }
        }
    }

    namespace EventSystems
    {
        public class PointerEventData { }
        public interface IPointerEnterHandler { void OnPointerEnter(PointerEventData e); }
        public interface IPointerExitHandler { void OnPointerExit(PointerEventData e); }
        public interface IPointerClickHandler { void OnPointerClick(PointerEventData e); }
        public class EventSystem : UnityEngine.MonoBehaviour { }
        public class StandaloneInputModule : UnityEngine.MonoBehaviour { }
    }

    public class Camera : Behaviour
    {
        public bool orthographic;
        public float orthographicSize;
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
    }

    public enum CameraClearFlags { SolidColor, Skybox }

    public class Graphic : Behaviour
    {
        public bool raycastTarget;
        public Color color;
    }

    namespace UI
    {
        using UnityEngine.Events;

        public class Canvas : Behaviour
        {
            public RenderMode renderMode;
        }

        public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

        public class CanvasScaler : Behaviour
        {
            public ScaleMode uiScaleMode;
            public Vector2 referenceResolution;
            public ScreenMatchMode screenMatchMode;
            public float matchWidthOrHeight;
            public enum ScaleMode { ScaleWithScreenSize, ConstantPixelSize }
            public enum ScreenMatchMode { MatchWidthOrHeight }
        }

        public class GraphicRaycaster : Behaviour { }

        public class Image : Graphic
        {
            public Sprite sprite;
            public Type type;
            public bool preserveAspect;
            public enum Type { Simple, Sliced, Tiled, Filled }
        }

        public class Text : Graphic
        {
            public Font font;
            public int fontSize;
            public FontStyle fontStyle;
            public TextAnchor alignment;
            public bool supportRichText;
            public HorizontalWrapMode horizontalOverflow;
            public VerticalWrapMode verticalOverflow;
            public string text;
        }

        public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
        public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
        public enum HorizontalWrapMode { Wrap, Overflow }
        public enum VerticalWrapMode { Truncate, Overflow }

        public class Selectable : Graphic
        {
            public ColorBlock colors;
            public Graphic targetGraphic;
        }

        public class Button : Selectable
        {
            public UnityEvent onClick;
        }

        public struct ColorBlock
        {
            public Color highlightedColor;
            public Color pressedColor;
        }

        public class InputField : Selectable
        {
            public string text;
            public Text textComponent;
            public Graphic placeholder;
            public int characterLimit;
            public ContentType contentType;
            public UnityEvent<string> onEndEdit;
            public void ActivateInputField() { }
            public enum ContentType { Standard, IntegerNumber, DecimalNumber }
        }
    }
}
