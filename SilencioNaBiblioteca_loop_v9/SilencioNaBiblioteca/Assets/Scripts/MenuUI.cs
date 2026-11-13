using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilencioBiblioteca
{
    // Menu principal (arte pintada), menu de pausa (Esc), tela de opções e créditos.
    public class MenuUI : MonoBehaviour
    {
        const int MaxBtn = 5;

        // ---- menu principal: canaletas de 1 pixel da arte (canvas próprio, referência 1600x900) ----
        const int MainCount = 5;
        static readonly string[] MainArt = { "novo", "carregar", "config", "creditos", "sair" };
        static readonly Vector2[] MainPos =
        {
            new Vector2(3f, -91f), new Vector2(4f, -148f), new Vector2(4f, -207f),
            new Vector2(5f, -262f), new Vector2(3f, -316f),
        };
        static readonly float[] MainW = { 210f, 232f, 232f, 142f, 76f };
        static readonly Color MainSel = new Color32(246, 216, 160, 255);
        static readonly Color MainDim = new Color32(190, 118, 78, 255);

        GameObject _mmRoot, _mmCred, _mmToast;
        CanvasGroup _mmGroup;
        Text _mmToastTxt;
        RectTransform _mmPlaca, _mmOrnL, _mmOrnR;
        readonly RectTransform[] _mmHit = new RectTransform[MainCount];
        readonly Image[] _mmImg = new Image[MainCount];
        Action _mmPlay, _mmHow;
        int _mmSel;
        Vector3 _mmMouse;
        Coroutine _mmToastCo, _mmFadeCo;

        Transform _root;

        // ---- menu de botões (pausa) ----
        GameObject _bmRoot; Image _bmDim; Text _bmTitle, _bmSub, _bmFoot;
        readonly RectTransform[] _bmRt = new RectTransform[MaxBtn];
        readonly Image[] _bmBorder = new Image[MaxBtn];
        readonly Image[] _bmFill = new Image[MaxBtn];
        readonly Text[] _bmTxt = new Text[MaxBtn];
        readonly Button[] _bmBtn = new Button[MaxBtn];
        List<MenuOpt> _bmOpts = new List<MenuOpt>();
        int _bmSel; Action _bmCancel; Vector3 _lastMouse;

        // ---- opções ----
        class Row
        {
            public bool isSlider; public Func<float> get; public Action<float> set; public float min, max; public string fmt;
            public Func<bool> getB; public Action<bool> setB;
            public Slider sl; public Text val; public Image bg; public Text tgTxt; public RectTransform rt;
        }
        GameObject _opRoot; readonly List<Row> _rows = new List<Row>(); int _opSel; Action _opClose;

        public bool Active
        {
            get
            {
                return (_mmRoot != null && _mmRoot.activeSelf)
                    || (_bmRoot != null && _bmRoot.activeSelf)
                    || (_opRoot != null && _opRoot.activeSelf);
            }
        }

        // ------------------------------------------------------------------ construção
        public void Init(Transform root)
        {
            _root = root;
            BuildMainMenu();
            BuildButtons();
            BuildOptions();
        }

        static void Fix(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }
        static void TL(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, -y);
        }
        static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        static void AddOutline(Text t, float d)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.95f); o.effectDistance = new Vector2(d, -d);
        }

        // ------------------------------------------------------------------ menu principal
        void BuildMainMenu()
        {
            var go = new GameObject("Canvas_Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;   // acima do HUD
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1600, 900);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            sc.matchWidthOrHeight = 1f;   // casa a altura: a arte fica sempre inteira
            _mmRoot = go;
            _mmGroup = go.AddComponent<CanvasGroup>();
            var root = go.transform;

            // fundo: a própria arte do menu (logo, estantes, abajures, sino e etiqueta de data)
            var bg = UiKit.Place(root, "Fundo", new Vector2(0.5f, 0.5f), new Vector2(1600, 900), Vector2.zero);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = UiKit.SpriteFromArt("MenuFundo", false);
            bgImg.raycastTarget = false;

            // placa do item selecionado (9-slice: as pontas não deformam)
            _mmPlaca = UiKit.Place(root, "Placa", new Vector2(0.5f, 0.5f), new Vector2(366, 62), new Vector2(2, MainPos[0].y));
            var placa = _mmPlaca.gameObject.AddComponent<Image>();
            placa.sprite = UiKit.SpriteFromArt("MenuPlaca", false, new Vector4(34, 0, 34, 0));
            placa.type = Image.Type.Sliced;
            placa.raycastTarget = false;

            // losangos que o design original tem nas pontas da placa
            _mmOrnL = Ornamento(_mmPlaca, -1f);
            _mmOrnR = Ornamento(_mmPlaca, 1f);

            for (int i = 0; i < MainCount; i++)
            {
                var lbl = UiKit.SpriteFromArt("MenuLbl_" + MainArt[i], false);
                float w = lbl != null ? lbl.rect.width : 200f, h = lbl != null ? lbl.rect.height : 34f;
                var rt = UiKit.Place(root, "Item" + i, new Vector2(0.5f, 0.5f), new Vector2(w, h), MainPos[i]);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = lbl; img.raycastTarget = false; img.color = MainSel;
                _mmImg[i] = img;

                // área de clique/hover mais generosa que o texto
                var hit = UiKit.Place(root, "Clique" + i, new Vector2(0.5f, 0.5f), new Vector2(620, 46), new Vector2(MainPos[i].x, MainPos[i].y));
                var hitImg = hit.gameObject.AddComponent<Image>();
                hitImg.color = new Color(0f, 0f, 0f, 0f);
                hitImg.raycastTarget = true;
                var btn = hit.gameObject.AddComponent<Button>();
                btn.targetGraphic = hitImg; btn.transition = Selectable.Transition.None;
                var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
                int idx = i;
                btn.onClick.AddListener(() => { _mmSel = idx; RefreshMain(false); PickMain(idx); });
                _mmHit[i] = hit;
            }

            // dica de teclado
            var hint = UiKit.Txt(root, "Dica", "Setas: escolher   ·   Enter: confirmar   ·   H: como jogar", 19, new Color(1f, 0.92f, 0.8f, 0.42f), TextAnchor.MiddleCenter);
            Fix((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(1000, 30), new Vector2(0, 26));
            AddOutline(hint, 1.5f);

            // aviso ("nenhum jogo salvo")
            var toast = UiKit.Txt(root, "Aviso", "", 24, new Color(1f, 0.85f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)toast.transform, new Vector2(0.5f, 0f), new Vector2(1200, 34), new Vector2(0, 74));
            AddOutline(toast, 2f);
            _mmToastTxt = toast; _mmToast = toast.gameObject;
            _mmToast.SetActive(false);

            BuildCredits(root);
            _mmRoot.SetActive(false);
        }

        static RectTransform Ornamento(RectTransform placa, float lado)
        {
            var rt = UiKit.Place(placa, "Ornamento", new Vector2(0.5f, 0.5f), new Vector2(32f, 30f), new Vector2(0f, 0f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiKit.SpriteFromArt("MenuOrnamento", false);
            img.raycastTarget = false;
            rt.localScale = new Vector3(lado, 1f, 1f);
            return rt;
        }

        void BuildCredits(Transform root){
            var rt = UiKit.Stretch(root, "Creditos");
            _mmCred = rt.gameObject;
            var dim = UiKit.Img(rt, "Dim", new Color(0.02f, 0.012f, 0.01f, 0.94f), true);
            var close = dim.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(CloseCredits);

            var title = UiKit.Txt(rt, "Titulo", "CRÉDITOS", 52, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(1200, 70), new Vector2(0, 320));
            AddOutline(title, 2.5f);

            var txt = UiKit.Txt(rt, "Texto",
                "Arthur Carvalho Rotkis\nCauê Iwamoto Meira\nHanna de Oliveira Melo\nSérgio de Siqueira Santos\n\n" +
                "FEI — Projeto acadêmico de Desenvolvimento de Jogos\n\n" +
                "\"Você tem o poder de mudar a situação apenas com a observação.\"",
                28, Hud.CText, TextAnchor.UpperCenter);
            Fix((RectTransform)txt.transform, new Vector2(0.5f, 0.5f), new Vector2(1300, 420), new Vector2(0, 20));

            var hint = UiKit.Txt(rt, "Dica", "E / clique para voltar >", 24, Hud.CDim, TextAnchor.MiddleCenter);
            Fix((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(800, 40), new Vector2(0, 70));
            _mmCred.SetActive(false);
        }

        void CloseCredits()
        {
            if (_mmCred == null) return;
            _mmCred.SetActive(false);
            _mmMouse = Input.mousePosition;
        }

        public void ShowMain(Action onPlay, Action onHow)
        {
            _mmPlay = onPlay; _mmHow = onHow;
            _bmRoot.SetActive(false);
            _opRoot.SetActive(false);
            if (_mmCred != null) _mmCred.SetActive(false);
            _mmRoot.SetActive(true);
            _mmSel = 0;
            RefreshMain(true);
            _mmMouse = Input.mousePosition;
            if (_mmFadeCo != null) StopCoroutine(_mmFadeCo);
            _mmFadeCo = StartCoroutine(FadeIn());
        }

        IEnumerator FadeIn()
        {
            _mmGroup.alpha = 0f;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                _mmGroup.alpha = Mathf.Clamp01(t / 0.45f);
                yield return null;
            }
            _mmGroup.alpha = 1f;
            _mmFadeCo = null;
        }

        void RefreshMain(bool instant)
        {
            for (int i = 0; i < MainCount; i++)
                if (_mmImg[i] != null) _mmImg[i].color = i == _mmSel ? MainSel : MainDim;

            float y = MainPos[_mmSel].y;
            float w = MainW[_mmSel] + 156f;
            _mmPlaca.sizeDelta = new Vector2(w, 62f);
            if (_mmOrnL != null) _mmOrnL.anchoredPosition = new Vector2(-(w / 2f + 15f), 0f);
            if (_mmOrnR != null) _mmOrnR.anchoredPosition = new Vector2(w / 2f + 15f, 0f);
            _mmPlaca.anchoredPosition = instant
                ? new Vector2(2f, y)
                : new Vector2(2f, Mathf.Lerp(_mmPlaca.anchoredPosition.y, y, 0.35f));
        }

        void PickMain(int i)
        {
            switch (i)
            {
                case 0:
                    _mmRoot.SetActive(false);
                    if (_mmPlay != null) _mmPlay();
                    break;
                case 1:
                    ToastMain("Nenhum jogo salvo. Comece um novo jogo.");
                    break;
                case 2:
                    _mmRoot.SetActive(false);
                    ShowOptions(() =>
                    {
                        _mmRoot.SetActive(true);
                        _mmGroup.alpha = 1f;
                        RefreshMain(true);
                        _mmMouse = Input.mousePosition;
                    });
                    break;
                case 3:
                    _mmCred.SetActive(true);
                    break;
                case 4:
                    Quit();
                    break;
            }
        }

        void ToastMain(string text)
        {
            if (_mmToast == null) return;
            _mmToastTxt.text = text;
            _mmToast.SetActive(true);
            if (_mmToastCo != null) StopCoroutine(_mmToastCo);
            _mmToastCo = StartCoroutine(ToastMainRoutine());
        }

        IEnumerator ToastMainRoutine()
        {
            yield return new WaitForSecondsRealtime(2.4f);
            _mmToast.SetActive(false);
            _mmToastCo = null;
        }

        void TickMain()
        {
            float ty = MainPos[_mmSel].y;
            var p = _mmPlaca.anchoredPosition;
            p.y = Mathf.Lerp(p.y, ty, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            _mmPlaca.anchoredPosition = p;

            int n = MainCount;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { _mmSel = (_mmSel + 1) % n; RefreshMain(false); }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { _mmSel = (_mmSel + n - 1) % n; RefreshMain(false); }

            if (Input.mousePosition != _mmMouse)
            {
                _mmMouse = Input.mousePosition;
                for (int i = 0; i < n; i++)
                    if (_mmHit[i] != null && RectTransformUtility.RectangleContainsScreenPoint(_mmHit[i], Input.mousePosition, null) && _mmSel != i)
                    {
                        _mmSel = i; RefreshMain(false);
                    }
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
            {
                PickMain(_mmSel);
                return;
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                _mmRoot.SetActive(false);
                if (_mmHow != null) _mmHow();
            }
        }

        void TickCredits()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)
                || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                CloseCredits();
        }

        // ------------------------------------------------------------------ menu de pausa
        void BuildButtons()
        {
            var rt = UiKit.Stretch(_root, "MenuBotoes");
            _bmRoot = rt.gameObject;
            _bmDim = UiKit.Img(rt, "Dim", new Color(0.02f, 0.012f, 0.01f, 0.92f), true);

            _bmTitle = UiKit.Txt(rt, "Titulo", "", 78, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)_bmTitle.transform, new Vector2(0.5f, 0.5f), new Vector2(1760, 120), new Vector2(0, 320));
            AddOutline(_bmTitle, 3f);
            _bmSub = UiKit.Txt(rt, "Sub", "", 28, Hud.CDim, TextAnchor.MiddleCenter, FontStyle.Italic);
            Fix((RectTransform)_bmSub.transform, new Vector2(0.5f, 0.5f), new Vector2(1500, 70), new Vector2(0, 225));

            for (int i = 0; i < MaxBtn; i++)
            {
                int idx = i;
                var fr = UiKit.Frame(rt, "Btn" + i, new Vector2(0.5f, 0.5f), new Vector2(560, 66), new Vector2(0, 60 - i * 84), new Color(0.15f, 0.10f, 0.06f, 1f), Hud.CBorderDim, 3f);
                var border = fr.gameObject.GetComponent<Image>();
                var fill = fr.Find("Fundo").GetComponent<Image>();
                fill.raycastTarget = true;
                var btn = fr.gameObject.AddComponent<Button>();
                btn.targetGraphic = fill; btn.transition = Selectable.Transition.None;
                var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
                btn.onClick.AddListener(() => PickBtn(idx));
                var tx = UiKit.Txt(fr, "L", "", 30, Hud.CText, TextAnchor.MiddleCenter, FontStyle.Bold);
                FillParent((RectTransform)tx.transform);
                _bmRt[i] = fr; _bmBorder[i] = border; _bmFill[i] = fill; _bmTxt[i] = tx; _bmBtn[i] = btn;
            }

            _bmFoot = UiKit.Txt(rt, "Rodape", "", 22, Hud.CDim, TextAnchor.MiddleCenter);
            Fix((RectTransform)_bmFoot.transform, new Vector2(0.5f, 0f), new Vector2(1700, 90), new Vector2(0, 60));
            _bmRoot.SetActive(false);
        }

        void BuildOptions()
        {
            var rt = UiKit.Stretch(_root, "MenuOpcoes");
            _opRoot = rt.gameObject;
            UiKit.Img(rt, "Dim", new Color(0.02f, 0.012f, 0.01f, 0.94f), true);
            var panel = UiKit.Frame(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(1060, 900), Vector2.zero, Hud.CBg, Hud.CBorder, 5f);
            var title = UiKit.Txt(panel, "Titulo", "OPÇÕES", 50, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(900, 70), new Vector2(0, -60));
            AddOutline(title, 2f);

            AddSlider(panel, "Volume geral", () => Settings.Master, v => Settings.Master = v, 0f, 1f, "{0:0}%", 100f);
            AddSlider(panel, "Música", () => Settings.Music, v => Settings.Music = v, 0f, 1f, "{0:0}%", 100f);
            AddSlider(panel, "Efeitos e alarme", () => Settings.Sfx, v => Settings.Sfx = v, 0f, 1f, "{0:0}%", 100f);
            AddSlider(panel, "Velocidade do texto", () => Settings.TextSpeed, v => Settings.TextSpeed = v, 0.5f, 3f, "x{0:0.0}", 1f);
            AddToggle(panel, "Tremor de tela", () => Settings.Shake, v => Settings.Shake = v);
            AddToggle(panel, "Luzes de ajuda", () => Settings.Hints, v => Settings.Hints = v);
            AddToggle(panel, "Tela cheia", () => Settings.Fullscreen, v => Settings.Fullscreen = v);

            FlatBtn(panel, "RESTAURAR PADRÃO", new Vector2(0.5f, 0f), new Vector2(380, 60), new Vector2(-220, 56), () => { Settings.Reset(); RefreshRows(); });
            FlatBtn(panel, "VOLTAR", new Vector2(0.5f, 0f), new Vector2(380, 60), new Vector2(220, 56), CloseOptions);
            var hint = UiKit.Txt(panel, "Dica", "Setas: escolher e ajustar   ·   Esc: voltar", 20, Hud.CDim, TextAnchor.MiddleCenter);
            Fix((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(900, 30), new Vector2(0, 112));
            _opRoot.SetActive(false);
        }

        const float RowTop = 150f, RowH = 84f;

        Row NewRow(Transform panel, string label)
        {
            var r = new Row();
            int i = _rows.Count;
            var row = UiKit.NewRect(panel, "Linha" + i);
            TL(row, 40f, RowTop + i * RowH, 980f, 72f);
            var bg = row.gameObject.AddComponent<Image>(); bg.color = new Color(0.8f, 0.56f, 0.22f, 0f); bg.raycastTarget = false;
            var lt = UiKit.Txt(row, "Rotulo", label, 28, Hud.CText, TextAnchor.MiddleLeft);
            TL((RectTransform)lt.transform, 18f, 0f, 380f, 72f);
            r.rt = row; r.bg = bg;
            _rows.Add(r);
            return r;
        }

        void AddSlider(Transform panel, string label, Func<float> get, Action<float> set, float min, float max, string fmt, float mul)
        {
            var r = NewRow(panel, label);
            r.isSlider = true; r.get = get; r.set = set; r.min = min; r.max = max; r.fmt = fmt;
            var sl = MakeSlider(r.rt, 400f, 36f, 410f, 18f);
            sl.minValue = min; sl.maxValue = max;
            var vt = UiKit.Txt(r.rt, "Valor", "", 28, Hud.CAccent, TextAnchor.MiddleRight, FontStyle.Bold);
            TL((RectTransform)vt.transform, 840f, 0f, 120f, 72f);
            r.sl = sl; r.val = vt;
            float m = mul;
            sl.onValueChanged.AddListener(v => { r.set(v); Settings.Save(); Settings.Apply(); r.val.text = string.Format(r.fmt, v * m); });
        }

        void AddToggle(Transform panel, string label, Func<bool> get, Action<bool> set)
        {
            var r = NewRow(panel, label);
            r.isSlider = false; r.getB = get; r.setB = set;
            var fr = UiKit.Frame(r.rt, "Chave", new Vector2(0f, 0.5f), new Vector2(240, 50), new Vector2(530, 0), new Color(0.2f, 0.13f, 0.07f, 1f), Hud.CBorder, 2f);
            var fill = fr.Find("Fundo").GetComponent<Image>(); fill.raycastTarget = true;
            var btn = fr.gameObject.AddComponent<Button>(); btn.targetGraphic = fill; btn.transition = Selectable.Transition.None;
            var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
            var tx = UiKit.Txt(fr, "T", "", 26, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            FillParent((RectTransform)tx.transform);
            r.tgTxt = tx;
            btn.onClick.AddListener(() => ToggleRow(r));
        }

        void ToggleRow(Row r)
        {
            r.setB(!r.getB()); Settings.Save(); Settings.Apply(); RefreshRows();
        }

        Slider MakeSlider(Transform parent, float w, float h, float x, float yCenterOffset)
        {
            var rt = UiKit.NewRect(parent, "Slider");
            rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(0f, 0.5f); rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = new Vector2(x, 0f);

            var bgRt = UiKit.NewRect(rt, "Background");
            bgRt.anchorMin = new Vector2(0f, 0.3f); bgRt.anchorMax = new Vector2(1f, 0.7f); bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            var bgImg = bgRt.gameObject.AddComponent<Image>(); bgImg.color = new Color(0.05f, 0.035f, 0.02f, 1f); bgImg.raycastTarget = true;

            var faRt = UiKit.NewRect(rt, "FillArea");
            faRt.anchorMin = new Vector2(0f, 0.3f); faRt.anchorMax = new Vector2(1f, 0.7f); faRt.offsetMin = new Vector2(6f, 0f); faRt.offsetMax = new Vector2(-6f, 0f);
            var fillRt = UiKit.NewRect(faRt, "Fill");
            fillRt.anchorMin = new Vector2(0f, 0f); fillRt.anchorMax = new Vector2(0f, 1f); fillRt.sizeDelta = new Vector2(8f, 0f);
            var fillImg = fillRt.gameObject.AddComponent<Image>(); fillImg.color = Hud.CBorder; fillImg.raycastTarget = false;

            var haRt = UiKit.NewRect(rt, "HandleSlideArea");
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one; haRt.offsetMin = new Vector2(12f, 0f); haRt.offsetMax = new Vector2(-12f, 0f);
            var hRt = UiKit.NewRect(haRt, "Handle");
            hRt.anchorMin = new Vector2(0f, 0f); hRt.anchorMax = new Vector2(0f, 1f); hRt.sizeDelta = new Vector2(22f, 0f);
            var hImg = hRt.gameObject.AddComponent<Image>(); hImg.color = Hud.CAccent; hImg.raycastTarget = true;

            var sl = rt.gameObject.AddComponent<Slider>();
            sl.fillRect = fillRt; sl.handleRect = hRt; sl.targetGraphic = hImg;
            sl.direction = Slider.Direction.LeftToRight; sl.wholeNumbers = false;
            sl.transition = Selectable.Transition.None;
            var nav = new Navigation(); nav.mode = Navigation.Mode.None; sl.navigation = nav;
            return sl;
        }

        Button FlatBtn(Transform parent, string label, Vector2 anchor, Vector2 size, Vector2 pos, Action onClick)
        {
            var fr = UiKit.Frame(parent, "Btn", anchor, size, pos, new Color(0.2f, 0.13f, 0.07f, 1f), Hud.CBorder, 2f);
            var fill = fr.Find("Fundo").GetComponent<Image>(); fill.raycastTarget = true;
            var btn = fr.gameObject.AddComponent<Button>(); btn.targetGraphic = fill;
            var cb = btn.colors; cb.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f); cb.highlightedColor = Color.white; cb.pressedColor = new Color(1f, 0.85f, 0.5f, 1f);
            cb.selectedColor = cb.normalColor; cb.colorMultiplier = 1f; cb.fadeDuration = 0.05f; btn.colors = cb;
            var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
            btn.onClick.AddListener(() => { if (onClick != null) onClick(); });
            var t = UiKit.Txt(fr, "L", label, 24, Hud.CText, TextAnchor.MiddleCenter, FontStyle.Bold);
            FillParent((RectTransform)t.transform);
            return btn;
        }

        // ------------------------------------------------------------------ telas
        public void ShowPause(Action onResume, Action onTitle)
        {
            Action resume = onResume ?? (() => { });
            var opts = new List<MenuOpt>
            {
                new MenuOpt("CONTINUAR", resume),
                new MenuOpt("OPÇÕES", () => ShowOptions(() => ShowPause(onResume, onTitle))),
                new MenuOpt("VOLTAR AO TÍTULO", onTitle),
            };
            ShowButtons("PAUSADO", "O tempo está parado.", "Esc: continuar", opts, resume, 0.72f);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
            ToastMain("Feche a aba do navegador para sair.");
#else
            Application.Quit();
#endif
        }

        void ShowButtons(string title, string sub, string foot, List<MenuOpt> opts, Action cancel, float dim)
        {
            _bmOpts = opts; _bmCancel = cancel; _bmSel = 0;
            _bmTitle.text = title; _bmSub.text = sub; _bmFoot.text = foot;
            var c = _bmDim.color; c.a = dim; _bmDim.color = c;
            int n = Mathf.Min(opts.Count, MaxBtn);
            for (int i = 0; i < MaxBtn; i++)
            {
                if (i < n)
                {
                    _bmRt[i].gameObject.SetActive(true);
                    _bmTxt[i].text = opts[i].label;
                    _bmRt[i].anchoredPosition = new Vector2(0f, 90f - i * 84f);
                }
                else _bmRt[i].gameObject.SetActive(false);
            }
            _lastMouse = Input.mousePosition;
            RefreshSel();
            _bmRoot.SetActive(true);
        }

        void RefreshSel()
        {
            for (int i = 0; i < MaxBtn; i++)
            {
                bool sel = i == _bmSel;
                _bmBorder[i].color = sel ? Hud.CAccent : Hud.CBorderDim;
                _bmFill[i].color = sel ? new Color(0.36f, 0.22f, 0.09f, 1f) : new Color(0.15f, 0.10f, 0.06f, 1f);
                _bmTxt[i].color = sel ? Hud.CAccent : Hud.CText;
            }
        }

        void PickBtn(int i)
        {
            if (!_bmRoot.activeSelf || i >= _bmOpts.Count) return;
            var a = _bmOpts[i].act;
            _bmRoot.SetActive(false);
            if (a != null) a();
        }

        public void ShowOptions(Action onClose)
        {
            _opClose = onClose; _opSel = 0;
            _bmRoot.SetActive(false);
            RefreshRows();
            _opRoot.SetActive(true);
        }

        void CloseOptions()
        {
            _opRoot.SetActive(false);
            var cb = _opClose; _opClose = null;
            if (cb != null) cb();
        }

        void RefreshRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                r.bg.color = new Color(0.8f, 0.56f, 0.22f, i == _opSel ? 0.16f : 0f);
                if (r.isSlider)
                {
                    float v = r.get();
                    r.sl.SetValueWithoutNotify(v);
                    r.val.text = string.Format(r.fmt, v * (r.max <= 1f ? 100f : 1f));
                }
                else r.tgTxt.text = r.getB() ? "LIGADO" : "DESLIGADO";
            }
        }

        // ------------------------------------------------------------------ entrada
        public void Tick()
        {
            if (_opRoot.activeSelf) { TickOptions(); return; }
            if (_mmRoot.activeSelf)
            {
                if (_mmCred != null && _mmCred.activeSelf) { TickCredits(); return; }
                TickMain();
                return;
            }
            if (!_bmRoot.activeSelf) return;
            int n = Mathf.Min(_bmOpts.Count, MaxBtn);
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { _bmSel = (_bmSel + 1) % n; RefreshSel(); }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { _bmSel = (_bmSel + n - 1) % n; RefreshSel(); }
            if (Input.mousePosition != _lastMouse)
            {
                _lastMouse = Input.mousePosition;
                for (int i = 0; i < n; i++)
                    if (RectTransformUtility.RectangleContainsScreenPoint(_bmRt[i], Input.mousePosition, null)) { if (_bmSel != i) { _bmSel = i; RefreshSel(); } }
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) { PickBtn(_bmSel); return; }
            if (_bmCancel != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)))
            {
                var cb = _bmCancel; _bmRoot.SetActive(false); cb();
            }
        }

        void TickOptions()
        {
            int n = _rows.Count;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { _opSel = (_opSel + 1) % n; RefreshRows(); }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { _opSel = (_opSel + n - 1) % n; RefreshRows(); }
            var r = _rows[_opSel];
            if (r.isSlider)
            {
                float step = (r.max - r.min) * 0.05f;
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) r.sl.value = Mathf.Clamp(r.sl.value - step, r.min, r.max);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) r.sl.value = Mathf.Clamp(r.sl.value + step, r.min, r.max);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                ToggleRow(r);
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) CloseOptions();
        }
    }
}
