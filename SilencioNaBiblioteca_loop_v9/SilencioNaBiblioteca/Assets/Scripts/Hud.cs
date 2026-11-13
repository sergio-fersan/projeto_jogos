using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilencioBiblioteca
{
    public class MenuOpt
    {
        public string label; public Action act;
        public MenuOpt(string label, Action act) { this.label = label; this.act = act; }
    }

    // Interface em código. Estilo: madeira escura + âmbar, cantos retos (combina com a pixel-art da biblioteca).
    public class Hud : MonoBehaviour
    {
        const int MaxOpts = 9;

        public static readonly Color CBg = new Color(0.07f, 0.045f, 0.03f, 0.96f);
        public static readonly Color CBorder = new Color(0.80f, 0.56f, 0.22f, 1f);
        public static readonly Color CBorderDim = new Color(0.42f, 0.28f, 0.13f, 1f);
        public static readonly Color CText = new Color(0.95f, 0.90f, 0.76f, 1f);
        public static readonly Color CDim = new Color(0.72f, 0.66f, 0.52f, 1f);
        public static readonly Color CAccent = new Color(1f, 0.80f, 0.35f, 1f);

        public Transform Root { get; private set; }

        Text _clock, _cycle, _obj, _prompt, _hint, _fastInd, _toast;
        Image _tint, _flash, _fade;
        Coroutine _toastCo, _flashCo, _fadeCo;

        // fala
        GameObject _dlgRoot, _dlgFaceFrame; Text _dlgName, _dlgBody; Image _dlgFace;
        Queue<DialogueLine> _dlgQ; Action _dlgEnd;
        string _dlgFull = ""; int _dlgIdx; float _dlgTimer;

        // escolhas (barra inferior)
        GameObject _menuRoot; RectTransform _chFrame; Text _chTitle, _chBody;
        GameObject _chFaceFrame; Image _chFace;
        readonly Button[] _menuBtns = new Button[MaxOpts];
        readonly Text[] _menuLbls = new Text[MaxOpts];
        List<MenuOpt> _menuOpts = new List<MenuOpt>(); Action _menuCancel;

        // código
        GameObject _codeRoot; Text _codeTitle, _codeBody, _codeDisp, _codeFb;
        string _code = ""; int _codeLen; Func<string, bool> _codeCheck; Action _codeOk, _codeCancel;

        // caderno / papel
        GameObject _nbRoot; Text _nbTitle, _nbBody; Action _nbClosed;

        // banner
        GameObject _banRoot; Text _banTitle, _banBody, _banHint; Action _banClosed;

        // final bom (tela com a arte do epílogo)
        GameObject _finRoot; CanvasGroup _finGroup; Action _finClosed; Coroutine _finCo;

        // cena ilustrada em tela cheia (expulsão, incêndio), atrás do HUD
        GameObject _scnRoot; CanvasGroup _scnGroup; Image _scnImg; Coroutine _scnCo;

        static bool Active(GameObject g) { return g != null && g.activeSelf; }

        public bool ModalOpen
        {
            get { return Active(_dlgRoot) || Active(_menuRoot) || Active(_codeRoot) || Active(_nbRoot) || Active(_banRoot) || Active(_finRoot); }
        }

        // ------------------------------------------------------------------ helpers
        static void Stretch(RectTransform rt, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }
        static void Fixed(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }
        // ancorado no canto superior esquerdo do pai
        static void TL(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, -y);
        }
        RectTransform Dim(Transform root, string name, float dim)
        {
            var rt = UiKit.Stretch(root, name);
            UiKit.Img(rt, "Dim", new Color(0f, 0f, 0f, dim), true);
            return rt;
        }

        static void AddOutline(Text t, float d)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.95f);
            o.effectDistance = new Vector2(d, -d);
        }

        // ------------------------------------------------------------------ construção
        public void BuildAll()
        {
            var canvasGo = new GameObject("Canvas_Hud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;
            Root = root;

            _tint = UiKit.Img(root, "Tint", new Color(0.12f, 0f, 0.02f, 0f), false);
            _flash = UiKit.Img(root, "Flash", new Color(0.85f, 0.1f, 0.05f, 0f), false);

            BuildTop(root);
            BuildDialogue(root);
            BuildChoices(root);
            BuildCode(root);
            BuildNotebook(root);
            BuildBanner(root);
            BuildScene();
            BuildFinal(root);
            _fade = UiKit.Img(root, "Fade", new Color(0f, 0f, 0f, 0f), false);
        }

        void BuildTop(Transform root)
        {
            var clockBack = UiKit.Frame(root, "RelogioFundo", new Vector2(0.5f, 1f), new Vector2(230, 62), new Vector2(0, -46), CBg, CBorder, 3f);
            _clock = UiKit.Txt(clockBack, "Hora", "12:00", 40, CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch((RectTransform)_clock.transform, Vector2.zero, Vector2.zero);

            _cycle = UiKit.Txt(root, "Ciclo", "", 30, CAccent, TextAnchor.UpperLeft, FontStyle.Bold);
            Fixed((RectTransform)_cycle.transform, new Vector2(0f, 1f), new Vector2(700, 40), new Vector2(380, -34));

            _obj = UiKit.Txt(root, "Objetivo", "", 26, CText, TextAnchor.UpperCenter);
            Fixed((RectTransform)_obj.transform, new Vector2(0.5f, 1f), new Vector2(1700, 36), new Vector2(0, -105));

            _fastInd = UiKit.Txt(root, "Acelerado", ">> acelerado x8 (F)", 22, CAccent, TextAnchor.UpperCenter, FontStyle.Bold);
            Fixed((RectTransform)_fastInd.transform, new Vector2(0.5f, 1f), new Vector2(400, 30), new Vector2(0, -145));
            _fastInd.gameObject.SetActive(false);

            _toast = UiKit.Txt(root, "Aviso", "", 30, new Color(1f, 0.93f, 0.6f), TextAnchor.UpperCenter, FontStyle.Bold);
            Fixed((RectTransform)_toast.transform, new Vector2(0.5f, 1f), new Vector2(1500, 80), new Vector2(0, -215));
            _toast.gameObject.SetActive(false);

            _prompt = UiKit.Txt(root, "Prompt", "", 32, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fixed((RectTransform)_prompt.transform, new Vector2(0.5f, 0f), new Vector2(1200, 50), new Vector2(0, 80));

            _hint = UiKit.Txt(root, "Dica", "WASD: mover   E: interagir   Tab: caderno   F: velocidade (1x/3x/6x)   Esc: pausa", 20, new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerLeft);
            Fixed((RectTransform)_hint.transform, new Vector2(0f, 0f), new Vector2(1000, 30), new Vector2(530, 24));

            // contorno preto: o texto não some no fundo do cenário
            AddOutline(_cycle, 2.5f); AddOutline(_obj, 3f); AddOutline(_fastInd, 2.5f);
            AddOutline(_toast, 3f); AddOutline(_prompt, 3f); AddOutline(_hint, 2f);
        }

        void BuildDialogue(Transform root)
        {
            var rt = UiKit.Stretch(root, "Dialogo");
            _dlgRoot = rt.gameObject;
            var box = UiKit.Frame(rt, "Caixa", new Vector2(0.5f, 0f), new Vector2(1700, 250), new Vector2(0, 150), CBg, CBorder, 4f);

            _dlgFaceFrame = UiKit.Frame(box, "RetratoQuadro", new Vector2(0f, 0.5f), new Vector2(190, 190), new Vector2(125, 0), new Color(0.13f, 0.09f, 0.06f, 1f), CBorderDim, 3f).gameObject;
            var faceRt = UiKit.NewRect(_dlgFaceFrame.transform, "Retrato");
            Stretch(faceRt, new Vector2(8, 8), new Vector2(-8, -8));
            _dlgFace = faceRt.gameObject.AddComponent<Image>();
            _dlgFace.preserveAspect = true;
            _dlgFace.raycastTarget = false;

            _dlgName = UiKit.Txt(box, "Nome", "", 28, CAccent, TextAnchor.UpperLeft, FontStyle.Bold);
            Stretch((RectTransform)_dlgName.transform, new Vector2(270, 180), new Vector2(-40, -18));
            _dlgBody = UiKit.Txt(box, "Texto", "", 30, CText, TextAnchor.UpperLeft);
            Stretch((RectTransform)_dlgBody.transform, new Vector2(270, 44), new Vector2(-40, -64));
            var h = UiKit.Txt(box, "Dica", "E / clique para continuar >", 18, CDim, TextAnchor.LowerRight);
            Stretch((RectTransform)h.transform, new Vector2(270, 12), new Vector2(-30, -10));
            _dlgRoot.SetActive(false);
        }

        // Barra de escolhas na parte de baixo da tela (o cenário continua visível acima).
        void BuildChoices(Transform root)
        {
            var rt = UiKit.Stretch(root, "Escolhas");
            _menuRoot = rt.gameObject;
            _chFrame = UiKit.Frame(rt, "Barra", new Vector2(0.5f, 0f), new Vector2(1560, 400), new Vector2(0, 220), CBg, CBorder, 4f);

            _chFaceFrame = UiKit.Frame(_chFrame, "RetratoQuadro", new Vector2(0f, 1f), new Vector2(170, 170), new Vector2(105, -105), new Color(0.13f, 0.09f, 0.06f, 1f), CBorderDim, 3f).gameObject;
            var faceRt = UiKit.NewRect(_chFaceFrame.transform, "Retrato");
            Stretch(faceRt, new Vector2(7, 7), new Vector2(-7, -7));
            _chFace = faceRt.gameObject.AddComponent<Image>();
            _chFace.preserveAspect = true; _chFace.raycastTarget = false;

            _chTitle = UiKit.Txt(_chFrame, "Titulo", "", 30, CAccent, TextAnchor.UpperLeft, FontStyle.Bold);
            _chBody = UiKit.Txt(_chFrame, "Texto", "", 22, CDim, TextAnchor.UpperLeft, FontStyle.Italic);

            for (int i = 0; i < MaxOpts; i++)
            {
                var row = UiKit.NewRect(_chFrame, "Op" + i);
                var img = row.gameObject.AddComponent<Image>();
                img.color = Color.white;
                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var cb = btn.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0f);
                cb.highlightedColor = new Color(0.80f, 0.56f, 0.22f, 0.28f);
                cb.pressedColor = new Color(0.80f, 0.56f, 0.22f, 0.5f);
                cb.selectedColor = cb.normalColor; cb.disabledColor = cb.normalColor;
                cb.colorMultiplier = 1f; cb.fadeDuration = 0.05f;
                btn.colors = cb;
                var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
                int idx = i;
                btn.onClick.AddListener(() => PickMenu(idx));

                var num = UiKit.Frame(row, "Num", new Vector2(0f, 0.5f), new Vector2(38, 38), new Vector2(26, 0), new Color(0.2f, 0.13f, 0.07f, 1f), CBorder, 2f);
                var nt = UiKit.Txt(num, "N", (i + 1).ToString(), 24, CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
                Stretch((RectTransform)nt.transform, Vector2.zero, Vector2.zero);
                var lbl = UiKit.Txt(row, "Label", "", 24, CText, TextAnchor.MiddleLeft);
                Stretch((RectTransform)lbl.transform, new Vector2(62, 0), new Vector2(-12, 0));
                _menuBtns[i] = btn; _menuLbls[i] = lbl;
            }
            _menuRoot.SetActive(false);
        }

        void BuildCode(Transform root)
        {
            var rt = Dim(root, "Codigo", 0.65f);
            _codeRoot = rt.gameObject;
            var panel = UiKit.Frame(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(860, 520), Vector2.zero, CBg, CBorder, 4f);
            _codeTitle = UiKit.Txt(panel, "Titulo", "", 36, CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fixed((RectTransform)_codeTitle.transform, new Vector2(0.5f, 1f), new Vector2(780, 60), new Vector2(0, -50));
            _codeBody = UiKit.Txt(panel, "Texto", "", 26, CText, TextAnchor.UpperCenter);
            Fixed((RectTransform)_codeBody.transform, new Vector2(0.5f, 1f), new Vector2(780, 100), new Vector2(0, -150));
            var disp = UiKit.Frame(panel, "Visor", new Vector2(0.5f, 0.5f), new Vector2(520, 130), new Vector2(0, -30), new Color(0.02f, 0.06f, 0.05f, 1f), new Color(0.3f, 0.7f, 0.55f, 1f), 3f);
            _codeDisp = UiKit.Txt(disp, "Codigo", "", 80, new Color(0.55f, 1f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch((RectTransform)_codeDisp.transform, Vector2.zero, Vector2.zero);
            _codeFb = UiKit.Txt(panel, "Feedback", "", 26, new Color(1f, 0.5f, 0.45f), TextAnchor.MiddleCenter);
            Fixed((RectTransform)_codeFb.transform, new Vector2(0.5f, 0f), new Vector2(780, 40), new Vector2(0, 110));
            var h = UiKit.Txt(panel, "Dica", "Digite os números e aperte Enter. Backspace apaga. Esc cancela.", 20, CDim, TextAnchor.MiddleCenter);
            Fixed((RectTransform)h.transform, new Vector2(0.5f, 0f), new Vector2(780, 30), new Vector2(0, 50));
            _codeRoot.SetActive(false);
        }

        void BuildNotebook(Transform root)
        {
            var rt = Dim(root, "Caderno", 0.6f);
            _nbRoot = rt.gameObject;
            var paper = UiKit.Frame(rt, "Papel", new Vector2(0.5f, 0.5f), new Vector2(1500, 900), Vector2.zero, new Color(0.84f, 0.76f, 0.58f, 1f), new Color(0.30f, 0.18f, 0.08f, 1f), 6f);
            _nbTitle = UiKit.Txt(paper, "Titulo", "CADERNO", 34, new Color(0.35f, 0.17f, 0.07f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Fixed((RectTransform)_nbTitle.transform, new Vector2(0.5f, 1f), new Vector2(1400, 60), new Vector2(0, -48));
            _nbBody = UiKit.Txt(paper, "Conteudo", "", 25, new Color(0.17f, 0.10f, 0.05f), TextAnchor.UpperLeft);
            Stretch((RectTransform)_nbBody.transform, new Vector2(60, 50), new Vector2(-60, -110));
            _nbRoot.SetActive(false);
        }

        void BuildBanner(Transform root)
        {
            var rt = UiKit.Stretch(root, "Banner");
            _banRoot = rt.gameObject;
            UiKit.Img(rt, "Dim", new Color(0.02f, 0.01f, 0.02f, 0.96f), true);

            // coluna com layout vertical: título e texto nunca se sobrepõem (espaço fixo entre eles)
            var col = UiKit.NewRect(rt, "Coluna");
            col.anchorMin = new Vector2(0.5f, 0f); col.anchorMax = new Vector2(0.5f, 1f); col.pivot = new Vector2(0.5f, 0.5f);
            col.offsetMin = new Vector2(-760f, 170f); col.offsetMax = new Vector2(760f, -90f);
            var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter; vlg.spacing = 44f;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            _banTitle = UiKit.Txt(col, "Titulo", "", 58, CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddOutline(_banTitle, 3f);
            _banBody = UiKit.Txt(col, "Texto", "", 30, CText, TextAnchor.UpperCenter);
            _banHint = UiKit.Txt(rt, "Dica", "E / clique para continuar >", 24, CDim, TextAnchor.MiddleCenter);
            Fixed((RectTransform)_banHint.transform, new Vector2(0.5f, 0f), new Vector2(900, 40), new Vector2(0, 70));
            _banRoot.SetActive(false);
        }

        // Cena ilustrada: arte em tela cheia num canvas abaixo do HUD, para os
        // diálogos e o relógio continuarem aparecendo por cima.
        void BuildScene()
        {
            var go = new GameObject("Canvas_Cena", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1;   // abaixo do HUD (0), do menu (20) e do final (30)
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _scnGroup = go.AddComponent<CanvasGroup>();
            _scnRoot = go;

            var rt = (RectTransform)go.transform;
            UiKit.Img(rt, "Preto", new Color(0.02f, 0.015f, 0.012f, 1f), true);
            var irt = UiKit.Stretch(rt, "Arte");
            _scnImg = irt.gameObject.AddComponent<Image>();
            _scnImg.raycastTarget = false;
            _scnImg.preserveAspect = true;
            _scnRoot.SetActive(false);
        }

        public void ShowScene(string art, float dur = 0.7f)
        {
            if (_scnImg == null) return;
            var sp = UiKit.SpriteFromArt(art, false);
            if (sp == null) return;
            _scnImg.sprite = sp;
            _scnRoot.SetActive(true);
            if (_scnCo != null) StopCoroutine(_scnCo);
            _scnGroup.alpha = 0f;
            _scnCo = StartCoroutine(SceneFadeIn(dur));
        }

        public void HideScene()
        {
            if (_scnCo != null) { StopCoroutine(_scnCo); _scnCo = null; }
            if (_scnRoot != null) _scnRoot.SetActive(false);
        }

        IEnumerator SceneFadeIn(float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _scnGroup.alpha = Mathf.SmoothStep(0f, 1f, t / dur);
                yield return null;
            }
            _scnGroup.alpha = 1f;
            _scnCo = null;
        }

        // Final bom: a arte do epílogo em tela cheia, em um canvas próprio acima de tudo
        // (o menu principal tem canvas com sortingOrder 20; este fica por cima).
        void BuildFinal(Transform unused)
        {
            var go = new GameObject("Canvas_Final", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _finGroup = go.AddComponent<CanvasGroup>();

            var rt = (RectTransform)go.transform;
            _finRoot = go;
            UiKit.Img(rt, "Preto", new Color(0.016f, 0.014f, 0.018f, 1f), true);

            var tex = Resources.Load<Texture2D>("Art/FinalBom");
            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                var irt = UiKit.Stretch(rt, "Epilogo");
                var img = irt.gameObject.AddComponent<Image>();
                img.sprite = sp; img.raycastTarget = false;
                img.preserveAspect = true;
            }
            else Debug.LogWarning("Arte do final não encontrada em Resources/Art/FinalBom.");

            var hint = UiKit.Txt(rt, "Dica", "E / clique para voltar ao título >", 26, new Color(0.78f, 0.73f, 0.64f, 0.9f), TextAnchor.MiddleCenter);
            Fixed((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(900, 40), new Vector2(0, 46));
            AddOutline(hint, 2f);
            _finRoot.SetActive(false);
        }

        public void ShowFinal(Action onClosed)
        {
            _finClosed = onClosed;
            _finRoot.SetActive(true);
            if (_finCo != null) StopCoroutine(_finCo);
            _finCo = StartCoroutine(FinalFadeIn());
        }

        IEnumerator FinalFadeIn()
        {
            _finGroup.alpha = 0f;
            float t = 0f;
            while (t < 1.1f)
            {
                t += Time.unscaledDeltaTime;
                _finGroup.alpha = Mathf.SmoothStep(0f, 1f, t / 1.1f);
                yield return null;
            }
            _finGroup.alpha = 1f;
            _finCo = null;
        }

        // ------------------------------------------------------------------ HUD simples
        public void SetClock(string s, bool warn) { _clock.text = s; _clock.color = warn ? new Color(1f, 0.45f, 0.35f) : CAccent; }
        public void SetCycle(string s) { _cycle.text = s; }
        public void SetObjective(string s) { _obj.text = s; }
        public void SetPrompt(string s) { _prompt.text = s; }
        public void SetSpeed(int s)
        {
            _fastInd.gameObject.SetActive(s > 1);
            if (s > 1) _fastInd.text = ">> velocidade x" + s + "  (F)";
        }
        public void SetTint(float alpha) { var c = _tint.color; c.a = alpha; _tint.color = c; }

        public void Toast(string text, float dur)
        {
            if (_toastCo != null) StopCoroutine(_toastCo);
            _toastCo = StartCoroutine(ToastRoutine(text, dur));
        }
        IEnumerator ToastRoutine(string text, float dur)
        {
            _toast.text = text; _toast.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(dur);
            _toast.gameObject.SetActive(false);
        }

        public void FlashRed()
        {
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashRoutine());
        }
        IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                var c = _flash.color; c.a = Mathf.Clamp01(t / 0.5f) * 0.55f; _flash.color = c;
                yield return null;
            }
            while (t < 1.6f)
            {
                t += Time.unscaledDeltaTime;
                var c = _flash.color; c.a = Mathf.Lerp(0.55f, 0f, (t - 0.5f) / 1.1f); _flash.color = c;
                yield return null;
            }
            var e = _flash.color; e.a = 0f; _flash.color = e;
        }

        public void SetFade(float a) { if (_fadeCo != null) { StopCoroutine(_fadeCo); _fadeCo = null; } var c = _fade.color; c.a = a; _fade.color = c; }
        public void FadeTo(float a, float dur)
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeRoutine(a, dur));
        }
        IEnumerator FadeRoutine(float target, float dur)
        {
            float start = _fade.color.a, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                var c = _fade.color; c.a = Mathf.Lerp(start, target, Mathf.Clamp01(t / dur)); _fade.color = c;
                yield return null;
            }
            var e = _fade.color; e.a = target; _fade.color = e;
        }

        // ------------------------------------------------------------------ painéis
        public void ShowBanner(string title, string body, string hint, Action onClosed)
        {
            _banTitle.text = title; _banBody.text = body;
            _banHint.text = string.IsNullOrEmpty(hint) ? "E / clique para continuar >" : hint;
            _banClosed = onClosed;
            // o fade de cena nasce por último no canvas: sem isso o cartão de abertura
            // fica atrás do preto e o jogador não lê o texto
            _banRoot.transform.SetAsLastSibling();
            _banRoot.SetActive(true);
        }

        public void ShowDialogue(DialogueLine[] lines, Action onEnd)
        {
            _dlgQ = new Queue<DialogueLine>(lines);
            _dlgEnd = onEnd;
            _dlgRoot.SetActive(true);
            AdvanceDialogue();
        }

        void AdvanceDialogue()
        {
            if (_dlgQ != null && _dlgQ.Count > 0)
            {
                var l = _dlgQ.Dequeue();
                _dlgName.text = l.who;
                _dlgFull = l.text ?? ""; _dlgIdx = 0; _dlgTimer = 0f; _dlgBody.text = "";
                Sprite sp = string.IsNullOrEmpty(l.face) ? null : CharArt.Get(l.face, true);
                _dlgFace.sprite = sp;
                _dlgFaceFrame.SetActive(sp != null);
            }
            else
            {
                _dlgQ = null;
                _dlgRoot.SetActive(false);
                var cb = _dlgEnd; _dlgEnd = null;
                if (cb != null) cb();
            }
        }

        // Barra de escolhas. face = nome do retrato (ou null). Alturas calculadas pelo texto: nada fica por cima de nada.
        public void ShowMenu(string title, string body, List<MenuOpt> opts, Action onCancel, string face = null)
        {
            _menuOpts = opts; _menuCancel = onCancel;
            int n = Mathf.Min(opts.Count, MaxOpts);
            Sprite sp = string.IsNullOrEmpty(face) ? null : CharArt.Get(face, true);
            _chFace.sprite = sp;
            _chFaceFrame.SetActive(sp != null);

            float x0 = sp != null ? 220f : 36f;
            float w = 1560f - x0 - 36f;

            _chTitle.text = title;
            TL((RectTransform)_chTitle.transform, x0, 16f, w, 36f);
            float y = 16f + 36f + 8f;                           // logo abaixo do título

            bool hasBody = !string.IsNullOrEmpty(body);
            _chBody.text = hasBody ? body : "";
            var brt = (RectTransform)_chBody.transform;
            TL(brt, x0, y, w, 10f);
            if (hasBody)
            {
                float bh = Mathf.Ceil(_chBody.preferredHeight);
                TL(brt, x0, y, w, bh);
                y += bh + 16f;
            }
            else y += 6f;

            for (int i = 0; i < MaxOpts; i++)
            {
                var b = _menuBtns[i];
                if (i < n)
                {
                    b.gameObject.SetActive(true);
                    _menuLbls[i].text = opts[i].label;
                    var rrt = (RectTransform)b.transform;
                    TL(rrt, x0, y, w, 54f);                      // define a largura primeiro
                    float rh = Mathf.Max(54f, Mathf.Ceil(_menuLbls[i].preferredHeight) + 20f);
                    TL(rrt, x0, y, w, rh);
                    y += rh + 8f;                                // espaçamento fixo entre opções
                }
                else b.gameObject.SetActive(false);
            }
            float h = Mathf.Min(Mathf.Max(y + 14f, sp != null ? 210f : 0f), 1030f);
            _chFrame.sizeDelta = new Vector2(1560f, h);
            _chFrame.anchoredPosition = new Vector2(0f, h / 2f + 24f);
            _menuRoot.SetActive(true);
        }

        void PickMenu(int idx)
        {
            if (!Active(_menuRoot) || idx >= _menuOpts.Count) return;
            var a = _menuOpts[idx].act;
            _menuRoot.SetActive(false);
            if (a != null) a();
        }

        public void HideMenu() { _menuRoot.SetActive(false); }

        public void ShowCode(string title, string body, int len, Func<string, bool> check, Action onOk, Action onCancel)
        {
            _codeTitle.text = title; _codeBody.text = body; _codeLen = len;
            _code = ""; _codeFb.text = "";
            _codeCheck = check; _codeOk = onOk; _codeCancel = onCancel;
            RefreshCode();
            _codeRoot.SetActive(true);
        }

        void RefreshCode()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _codeLen; i++)
            {
                sb.Append(i < _code.Length ? _code[i].ToString() : "_");
                if (i < _codeLen - 1) sb.Append(" ");
            }
            _codeDisp.text = sb.ToString();
        }

        public void ShowNotebook(string text) { ShowPaper("CADERNO DE PISTAS    (Tab / Esc para fechar)", text, null); }
        public void ShowPaper(string title, string text, Action onClosed)
        {
            _nbTitle.text = title; _nbBody.text = text; _nbClosed = onClosed;
            _nbRoot.SetActive(true);
        }
        void ClosePaper()
        {
            _nbRoot.SetActive(false);
            var cb = _nbClosed; _nbClosed = null;
            if (cb != null) cb();
        }

        // ------------------------------------------------------------------ entrada dos painéis
        static bool Confirm()
        {
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0);
        }

        static float CharDelay(char c)
        {
            float d;
            if (c == '.' || c == '?' || c == '!' || c == '\u2026') d = 0.17f;
            else if (c == ',' || c == ';' || c == ':') d = 0.08f;
            else if (c == ' ') d = 0.02f;
            else d = 0.024f;
            return d / Mathf.Max(0.1f, Settings.TextSpeed);
        }

        // Chamado pelo Game a cada frame enquanto ModalOpen.
        public void UpdateInput()
        {
            if (Active(_finRoot))
            {
                if (Confirm())
                {
                    var cb = _finClosed; _finClosed = null;
                    _finRoot.SetActive(false);
                    if (cb != null) cb();
                }
                return;
            }
            if (Active(_banRoot))
            {
                if (Confirm())
                {
                    var cb = _banClosed; _banClosed = null;
                    _banRoot.SetActive(false);
                    if (cb != null) cb();
                }
                return;
            }
            if (Active(_codeRoot))
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { _codeRoot.SetActive(false); if (_codeCancel != null) _codeCancel(); return; }
                foreach (char c in Input.inputString)
                {
                    if (c == '\b') { if (_code.Length > 0) _code = _code.Substring(0, _code.Length - 1); }
                    else if (c == '\n' || c == '\r')
                    {
                        if (_code.Length == _codeLen)
                        {
                            if (_codeCheck == null || _codeCheck(_code)) { _codeRoot.SetActive(false); if (_codeOk != null) _codeOk(); return; }
                            _codeFb.text = "Código incorreto.";
                            _code = "";
                        }
                        else _codeFb.text = "Digite os " + _codeLen + " números.";
                    }
                    else if (char.IsDigit(c) && _code.Length < _codeLen) { _code += c; _codeFb.text = ""; }
                }
                RefreshCode();
                return;
            }
            if (Active(_menuRoot))
            {
                for (int i = 0; i < MaxOpts && i < _menuOpts.Count; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { PickMenu(i); return; }
                if (_menuCancel != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)))
                {
                    var cb = _menuCancel; _menuRoot.SetActive(false); cb();
                }
                return;
            }
            if (Active(_dlgRoot))
            {
                if (_dlgIdx < _dlgFull.Length)
                {
                    if (Confirm()) { _dlgIdx = _dlgFull.Length; _dlgBody.text = _dlgFull; return; }
                    _dlgTimer += Time.unscaledDeltaTime;
                    while (_dlgIdx < _dlgFull.Length)
                    {
                        float dly = CharDelay(_dlgFull[_dlgIdx]);
                        if (_dlgTimer < dly) break;
                        _dlgTimer -= dly; _dlgIdx++;
                    }
                    _dlgBody.text = _dlgFull.Substring(0, _dlgIdx);
                    return;
                }
                if (Confirm()) AdvanceDialogue();
                return;
            }
            if (Active(_nbRoot))
            {
                if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Escape)
                    || Input.GetKeyDown(KeyCode.Backspace) || Confirm())
                    ClosePaper();
            }
        }
    }
}
