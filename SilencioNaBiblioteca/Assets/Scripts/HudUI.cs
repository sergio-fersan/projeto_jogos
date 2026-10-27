using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SilencioBiblioteca
{
    // Toda a interface da demo é construída em código, sobre um Canvas.
    public class HudUI : MonoBehaviour
    {
        Image _bg;
        Image _tint;
        Transform _npcLayer;
        Transform _zoneLayer;
        Image _flash;
        Image _fade;

        Text _clock;
        Text _fast;
        Text _subtitle;
        float _tintBaseAlpha;

        GameObject _dialogRoot;
        Text _dlgName;
        Text _dlgBody;
        Queue<DialogueLine> _dlgQueue;

        GameObject _puzzleRoot;
        Text _pzTitle;
        Text _pzBook;
        Text _pzData;
        Text _pzRule;
        InputField _pzInput;
        Text _pzFeedback;
        Action _pzSuccess;
        Action _pzFail;
        float _lastCheck;

        GameObject _choiceRoot;
        Text _chTitle;
        Text _chBody;
        Button _chBtnA;
        Button _chBtnB;

        GameObject _journalRoot;
        Text _jrBody;

        GameObject _bannerRoot;
        Text _bnTitle;
        Text _bnBody;
        Text _bnHint;
        Action _bannerClosed;

        GameObject _titleRoot;
        public Action OnPlay;

        GameObject _endRoot;
        Text _endTitle;
        Text _endBody;
        Button _endBtnA;
        Button _endBtnB;

        readonly Dictionary<string, NpcView> _npcs = new Dictionary<string, NpcView>();
        readonly Dictionary<string, HotspotZone> _zones = new Dictionary<string, HotspotZone>();

        Action<string> _zoneClick;
        Action<string> _npcClick;

        Coroutine _subtitleCo;
        Coroutine _flashCo;

        public bool ModalOpen
        {
            get
            {
                return Active(_dialogRoot) || Active(_puzzleRoot) || Active(_choiceRoot) ||
                       Active(_journalRoot) || Active(_bannerRoot) || Active(_titleRoot) || Active(_endRoot);
            }
        }

        static bool Active(GameObject go) { return go != null && go.activeSelf; }

        void Start()
        {
            // O HudUI é adicionado como componente antes de BuildAll ser chamado pelo Game.
        }

        // ==================================================================
        //  CONSTRUÇÃO
        // ==================================================================

        public void BuildAll(Sprite bg)
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

            // ---- camadas (a ordem define qual fica por cima) ----
            var bgRt = UiKit.Stretch(root, "Cenario");
            _bg = UiKit.Img(bgRt, "Fundo", Color.white, false, bg, false);
            bgRt.SetAsFirstSibling();

            _tint = UiKit.Img(root, "Tint", new Color(0f, 0f, 0f, 0f), false);
            _tint.transform.SetAsLastSibling();

            var npcGo = new GameObject("Clientes", typeof(RectTransform));
            npcGo.transform.SetParent(root, false);
            var npcRt = (RectTransform)npcGo.transform;
            npcRt.anchorMin = Vector2.zero; npcRt.anchorMax = Vector2.one;
            npcRt.offsetMin = Vector2.zero; npcRt.offsetMax = Vector2.zero;
            _npcLayer = npcGo.transform;

            var zoneGo = new GameObject("Zonas", typeof(RectTransform));
            zoneGo.transform.SetParent(root, false);
            var zoneRt = (RectTransform)zoneGo.transform;
            zoneRt.anchorMin = Vector2.zero; zoneRt.anchorMax = Vector2.one;
            zoneRt.offsetMin = Vector2.zero; zoneRt.offsetMax = Vector2.zero;
            _zoneLayer = zoneGo.transform;
            BuildHotspots(zoneRt);

            _flash = UiKit.Img(root, "Flash", new Color(0.8f, 0.1f, 0.05f, 0f), false);
            _fade = UiKit.Img(root, "Fade", new Color(0f, 0f, 0f, 0f), false);

            BuildTopHud(root);
            BuildDialogue(root);
            BuildPuzzle(root);
            BuildChoice(root);
            BuildJournal(root);
            BuildBanner(root);
            BuildEnding(root);
            BuildTitle(root);
        }

        void BuildTopHud(Transform root)
        {
            var clockBack = UiKit.Place(root, "RelogioFundo", new Vector2(0.5f, 1f), new Vector2(220, 62), new Vector2(0, -16));
            UiKit.Img(clockBack, "Bg", new Color(0.05f, 0.05f, 0.08f, 0.8f), false, UiKit.Round);
            var clt = UiKit.Txt(clockBack, "Hora", "17:00", 38, new Color(1f, 0.87f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            StretchFill((RectTransform)clt.transform);
            _clock = clt;

            _fast = UiKit.Txt(root, "FastInd", "▶▶ acelerado (F)", 22, new Color(1f, 0.87f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            FillRect((RectTransform)_fast.transform, new Vector2(0.5f, 1f), new Vector2(400, 30), new Vector2(0, -54));
            _fast.gameObject.SetActive(false);

            var jrBtn = UiKit.Btn(root, "BtnDiario", "📓 Diário (J)", null, new Color(0.15f, 0.13f, 0.10f, 0.92f), new Color(1f, 0.9f, 0.6f), 22);
            FillRect((RectTransform)jrBtn.transform, new Vector2(1f, 1f), new Vector2(170, 52), new Vector2(-105, -22));
            jrBtn.onClick.AddListener(() => ToggleJournalRequested());
            JournalButton = jrBtn.gameObject;

            _subtitle = UiKit.Txt(root, "Subtitulo", "", 26, new Color(0.95f, 0.93f, 0.85f), TextAnchor.LowerCenter);
            var srt = (RectTransform)_subtitle.transform;
            srt.anchorMin = new Vector2(0.1f, 0f); srt.anchorMax = new Vector2(0.9f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(0, 70);
            srt.anchoredPosition = new Vector2(0, 24);

            var hint = UiKit.Txt(root, "Hint", "F — adiantar o tempo      J — diário      F1 — pular ao próximo acontecimento", 18, new Color(1f, 1f, 1f, 0.4f), TextAnchor.UpperLeft);
            FillRect((RectTransform)hint.transform, new Vector2(0f, 0f), new Vector2(700, 26), new Vector2(24, 10));
        }

        public GameObject JournalButton;

        void ToggleJournalRequested()
        {
            var game = Game.Instance;
            if (game != null) game.RequestToggleJournal();
        }

        static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void FillRect(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        // ==================================================================
        //  HOTSPOTS (zonas clicáveis sobre o cenário)
        // ==================================================================

        class HotspotDef
        {
            public string id, label;
            public float cx, cy, w, h;
            public int minCycle;

            public HotspotDef(string id, string label, float cx, float cy, float w, float h, int minCycle)
            {
                this.id = id; this.label = label; this.cx = cx; this.cy = cy; this.w = w; this.h = h; this.minCycle = minCycle;
            }
        }

        static readonly HotspotDef[] Hotspots =
        {
            new HotspotDef("entrada",      "Entrada principal",            0.168f, 0.84f, 0.11f, 0.16f, 1),
            new HotspotDef("porta_fundos", "Porta dos Fundos",             0.876f, 0.846f,0.08f, 0.16f, 1),
            new HotspotDef("relogio",      "Relógio",                      0.632f, 0.92f, 0.06f, 0.10f, 1),
            new HotspotDef("computador",   "Computador do Balcão",         0.473f, 0.72f, 0.08f, 0.08f, 1),
            new HotspotDef("balcao",       "Balcão de Devoluções",         0.50f,  0.66f, 0.22f, 0.07f, 1),
            new HotspotDef("estante_esq",  "Estante Geral",                0.289f, 0.52f, 0.16f, 0.19f, 1),
            new HotspotDef("estante_hist", "Estante de História Militar",  0.715f, 0.52f, 0.16f, 0.19f, 1),
            new HotspotDef("mesa",         "Mesa de Leitura",              0.103f, 0.45f, 0.16f, 0.20f, 1),
            new HotspotDef("aquecedor",    "Aquecedor do Canto",           0.93f,  0.17f, 0.07f, 0.13f, 2),
        };

        void BuildHotspots(Transform parent)
        {
            foreach (var d in Hotspots)
            {
                var rt = UiKit.Anchored(parent, "hs_" + d.id,
                    new Vector2(d.cx - d.w / 2f, Mathf.Max(d.cy - d.h / 2f, 0.01f)),
                    new Vector2(d.cx + d.w / 2f, Mathf.Min(d.cy + d.h / 2f, 0.99f)));

                var hover = UiKit.Img(rt, "Hover", new Color(1f, 0.95f, 0.4f, 0f), false);
                StretchFill((RectTransform)hover.transform);

                var lt = UiKit.Txt(rt, "Rotulo", d.label, 22, new Color(1f, 0.93f, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
                var lrt = (RectTransform)lt.transform;
                lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 1f);
                lrt.pivot = new Vector2(0.5f, 0f);
                lrt.sizeDelta = new Vector2(360, 30);
                lrt.anchoredPosition = new Vector2(0, 6);
                lt.enabled = false;

                var zone = rt.gameObject.AddComponent<HotspotZone>();
                zone.id = d.id;
                zone.hover = hover;
                zone.label = lt;
                zone.onClick = HandleZoneClick;
                _zones[d.id] = zone;
            }
        }

        void HandleZoneClick(string id)
        {
            if (_zoneClick != null) _zoneClick(id);
        }

        public void SetZoneHandler(Action<string> cb) { _zoneClick = cb; }
        public void SetNpcClickHandler(Action<string> cb) { _npcClick = cb; }

        public void SetHotspotsVisible(int cycle)
        {
            foreach (var d in Hotspots)
            {
                var z = _zones[d.id];
                if (z != null) z.gameObject.SetActive(cycle >= d.minCycle);
            }
        }

        // ==================================================================
        //  NPCs
        // ==================================================================

        public class NpcView : MonoBehaviour, IPointerClickHandler
        {
            public string kind;
            public RectTransform rt;
            public Action<string> on_click;

            public void OnPointerClick(PointerEventData e)
            {
                if (on_click != null) on_click(kind);
            }
        }

        public NpcView SpawnNpc(string kind)
        {
            if (_npcs.TryGetValue(kind, out var old) && old != null) return old;
            var go = new GameObject("npc_" + kind, typeof(RectTransform));
            go.transform.SetParent(_npcLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(95, 150);
            var img = go.AddComponent<Image>();
            img.sprite = UiKit.PersonSprite(kind);
            img.preserveAspect = true;
            img.raycastTarget = true;
            var view = go.AddComponent<NpcView>();
            view.kind = kind;
            view.rt = rt;
            view.on_click = k => { if (_npcClick != null) _npcClick(k); };
            _npcs[kind] = view;
            return view;
        }

        public NpcView FindNpc(string kind)
        {
            _npcs.TryGetValue(kind, out var v);
            return v == null ? null : v;
        }

        public void DespawnNpc(string kind)
        {
            if (_npcs.TryGetValue(kind, out var v))
            {
                if (v != null) UnityEngine.Object.Destroy(v.gameObject);
                _npcs.Remove(kind);
            }
        }

        public void ClearNpcs()
        {
            foreach (var kv in _npcs)
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value.gameObject);
            _npcs.Clear();
        }

        // ==================================================================
        //  RELÓGIO / CLIMA
        // ==================================================================

        public void SetClock(string text)
        {
            if (_clock != null) _clock.text = text;
        }

        public void SetFastIndicator(bool on)
        {
            if (_fast != null) _fast.gameObject.SetActive(on);
        }

        public void SetTintForCycle(int cycle)
        {
            switch (cycle)
            {
                case 1:
                    _tintBaseAlpha = 0f;
                    _tint.color = new Color(0f, 0f, 0f, 0f);
                    break;
                case 2:
                    _tintBaseAlpha = 0.16f;
                    _tint.color = new Color(0.03f, 0.0f, 0.06f, _tintBaseAlpha);
                    break;
                default:
                    _tintBaseAlpha = 0.30f;
                    _tint.color = new Color(0.02f, 0.0f, 0.04f, _tintBaseAlpha);
                    break;
            }
        }

        public void UpdateFlicker(float time)
        {
            float a = _tintBaseAlpha + (Mathf.PerlinNoise(time * 9f, 1.23f) > 0.86f ? 0.2f : 0f);
            var c = _tint.color;
            c.a = a;
            _tint.color = c;
        }

        public void FlashReset()
        {
            if (_flashCo != null) { StopCoroutine(_flashCo); _flashCo = null; }
            _flash.color = new Color(_flash.color.r, _flash.color.g, _flash.color.b, 0f);
            _fade.color = new Color(0f, 0f, 0f, 0f);
        }

        // alias antigo
        public void FadeReset() { FlashReset(); }

        public void FlashRed()
        {
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                var c = _flash.color;
                c.a = Mathf.Clamp01(t / 0.4f) * 0.6f;
                _flash.color = c;
                yield return null;
            }
        }

        public void FadeToBlack()
        {
            StartCoroutine(FadeRoutine());
        }

        IEnumerator FadeRoutine()
        {
            float t = 0f;
            float start = _fade.color.a;
            while (t < 1.2f)
            {
                t += Time.unscaledDeltaTime;
                var c = _fade.color;
                c.a = Mathf.Lerp(start, 1f, Mathf.Clamp01(t / 1.2f));
                _fade.color = c;
                yield return null;
            }
        }

        // ==================================================================
        //  SUBTÍTULO
        // ==================================================================

        public void ShowSubtitle(string text, float dur)
        {
            if (_subtitleCo != null) StopCoroutine(_subtitleCo);
            _subtitleCo = StartCoroutine(SubtitleRoutine(text, dur));
        }

        IEnumerator SubtitleRoutine(string text, float dur)
        {
            _subtitle.text = text;
            var c = _subtitle.color;
            c.a = 1f;
            _subtitle.color = c;
            yield return new WaitForSeconds(dur);
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                var cc = _subtitle.color;
                cc.a = 1f - t / 0.6f;
                _subtitle.color = cc;
                yield return null;
            }
            _subtitle.text = "";
        }

        // ==================================================================
        //  DIÁLOGO
        // ==================================================================

        void BuildDialogue(Transform root)
        {
            var rt = UiKit.Stretch(root, "Dialogo");
            _dialogRoot = rt.gameObject;
            var block = UiKit.Img(rt, "Bloqueio", new Color(0f, 0f, 0f, 0.25f), true);

            // clicar em qualquer lugar avança
            var btn = _dialogRoot.AddComponent<Button>();
            btn.targetGraphic = block;
            btn.onClick.AddListener(AdvanceDialogue);

            var sheet = UiKit.Place(rt, "Ficha", new Vector2(0.5f, 0f), new Vector2(1240, 240), new Vector2(0, 130));
            UiKit.Img(sheet, "Bg", new Color(0.12f, 0.09f, 0.07f, 0.95f), false, UiKit.Round);

            _dlgName = UiKit.Txt(sheet, "Nome", "", 24, new Color(1f, 0.8f, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold);
            var nrt = (RectTransform)_dlgName.transform;
            StretchFill(nrt);
            nrt.offsetMin = new Vector2(36, 150); nrt.offsetMax = new Vector2(-36, -18);

            _dlgBody = UiKit.Txt(sheet, "Texto", "", 28, Color.white, TextAnchor.UpperLeft);
            var brt = (RectTransform)_dlgBody.transform;
            StretchFill(brt);
            brt.offsetMin = new Vector2(36, 44); brt.offsetMax = new Vector2(-36, -140);

            var hint = UiKit.Txt(sheet, "Hint", "clique para continuar ▸", 18, new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerRight);
            StretchFill((RectTransform)hint.transform);
            ((RectTransform)hint.transform).offsetMin = new Vector2(36, 12); ((RectTransform)hint.transform).offsetMax = new Vector2(-24, -8);

            _dialogRoot.SetActive(false);
        }

        public void ShowDialogue(DialogueLine[] lines)
        {
            _dlgQueue = new Queue<DialogueLine>(lines);
            _dialogRoot.SetActive(true);
            AdvanceDialogue();
        }

        public void AdvanceDialogue()
        {
            if (!Active(_dialogRoot)) return;
            if (_dlgQueue != null && _dlgQueue.Count > 0)
            {
                var line = _dlgQueue.Dequeue();
                bool hasWho = !string.IsNullOrEmpty(line.who);
                _dlgName.gameObject.SetActive(hasWho);
                _dlgName.text = line.who;
                _dlgBody.text = line.text;
            }
            else
            {
                _dlgQueue = null;
                _dialogRoot.SetActive(false);
            }
        }

        // ==================================================================
        //  PUZZLE DE CATALOGAÇÃO
        // ==================================================================

        void BuildPuzzle(Transform root)
        {
            var rt = UiKit.Stretch(root, "Puzzle");
            _puzzleRoot = rt.gameObject;
            UiKit.Img(rt, "Bloqueio", new Color(0f, 0f, 0f, 0.55f), true);

            var panel = UiKit.Place(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(900, 640), Vector2.zero);
            UiKit.Img(panel, "Bg", new Color(0.16f, 0.12f, 0.08f, 0.97f), false, UiKit.Round);

            _pzTitle = UiKit.Txt(panel, "Titulo", "Catalogação do dia", 32, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperCenter, FontStyle.Bold);
            FillRect((RectTransform)_pzTitle.transform, new Vector2(0.5f, 1f), new Vector2(840, 46), new Vector2(0, -34));

            _pzBook = UiKit.Txt(panel, "Livro", "", 26, new Color(0.95f, 0.9f, 0.8f), TextAnchor.UpperCenter, FontStyle.Bold);
            FillRect((RectTransform)_pzBook.transform, new Vector2(0.5f, 1f), new Vector2(840, 70), new Vector2(0, -120));

            _pzData = UiKit.Txt(panel, "Dados", "", 24, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperCenter);
            FillRect((RectTransform)_pzData.transform, new Vector2(0.5f, 1f), new Vector2(840, 70), new Vector2(0, -200));

            _pzRule = UiKit.Txt(panel, "Regra", "", 24, new Color(0.6f, 0.85f, 0.95f), TextAnchor.UpperCenter);
            FillRect((RectTransform)_pzRule.transform, new Vector2(0.5f, 1f), new Vector2(840, 110), new Vector2(0, -300));

            _pzInput = UiKit.MakeInput(panel, "Entrada", "código", 30);
            FillRect((RectTransform)_pzInput.transform, new Vector2(0.5f, 0.5f), new Vector2(300, 56), new Vector2(0, 30));

            var ok = UiKit.Btn(panel, "Confirmar", "Catalogar", null, new Color(0.24f, 0.4f, 0.2f), Color.white, 26);
            FillRect((RectTransform)ok.transform, new Vector2(0.5f, 0.5f), new Vector2(220, 56), new Vector2(-130, -60));

            var close = UiKit.Btn(panel, "Fechar", "Agora não", null, new Color(0.35f, 0.2f, 0.15f), Color.white, 26);
            FillRect((RectTransform)close.transform, new Vector2(0.5f, 0.5f), new Vector2(220, 56), new Vector2(130, -60));

            _pzFeedback = UiKit.Txt(panel, "Feedback", "", 24, Color.white, TextAnchor.UpperCenter);
            FillRect((RectTransform)_pzFeedback.transform, new Vector2(0.5f, 0.5f), new Vector2(840, 70), new Vector2(0, -250));

            _puzzleRoot.SetActive(false);
        }

        public void ShowPuzzle(PuzzleDef def, Action onSuccess, Action onFail)
        {
            _pzSuccess = onSuccess;
            _pzFail = onFail;
            _pzTitle.text = def.title;
            _pzBook.text = def.book;
            _pzData.text = def.data;
            _pzRule.text = def.rule;
            _pzFeedback.text = "";
            _pzInput.text = "";
            _pzInput.ActivateInputField();

            _pzInput.onEndEdit.RemoveAllListeners();
            _pzInput.onEndEdit.AddListener(_ => CheckPuzzleAnswer(def));

            var buttons = _puzzleRoot.GetComponentsInChildren<Button>(true);
            // reconstrói listeners dos dois botões
            foreach (var b in buttons)
            {
                b.onClick.RemoveAllListeners();
                if (b.name == "Confirmar") b.onClick.AddListener(() => CheckPuzzleAnswer(def));
                if (b.name == "Fechar") b.onClick.AddListener(HidePuzzle);
            }

            _puzzleRoot.SetActive(true);
        }

        void CheckPuzzleAnswer(PuzzleDef def)
        {
            if (!Active(_puzzleRoot)) return;
            if (_pzInput == null) return;
            if (Time.unscaledTime - _lastCheck < 0.25f) return; // evita duplo gatilho (Enter + clique)
            _lastCheck = Time.unscaledTime;
            string typed = _pzInput.text.Trim();
            _pzInput.text = "";
            if (typed.Length == 0)
            {
                _pzFeedback.color = new Color(1f, 0.9f, 0.5f);
                _pzFeedback.text = "Digite o código calculado.";
                _pzInput.ActivateInputField();
                return;
            }
            int.TryParse(typed, out int value);
            if (value == def.answer)
            {
                _pzFeedback.color = new Color(0.5f, 1f, 0.6f);
                _pzFeedback.text = "Código correto!";
                StartCoroutine(PuzzleSuccess(def));
            }
            else
            {
                _pzFeedback.color = new Color(1f, 0.45f, 0.4f);
                _pzFeedback.text = "Código não confere. O relógio na parede andou alguns minutos…";
                if (_pzFail != null) _pzFail();
            }
            _pzInput.ActivateInputField();
        }

        IEnumerator PuzzleSuccess(PuzzleDef def)
        {
            yield return new WaitForSeconds(1.0f);
            _puzzleRoot.SetActive(false);
            if (_pzSuccess != null) _pzSuccess();
        }

        public void HidePuzzle()
        {
            _puzzleRoot.SetActive(false);
        }

        // ==================================================================
        //  ESCOLHA (menus de "sim / não")
        // ==================================================================

        void BuildChoice(Transform root)
        {
            var rt = UiKit.Stretch(root, "Escolha");
            _choiceRoot = rt.gameObject;
            UiKit.Img(rt, "Bloqueio", new Color(0f, 0f, 0f, 0.5f), true);

            var panel = UiKit.Place(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(760, 420), Vector2.zero);
            UiKit.Img(panel, "Bg", new Color(0.16f, 0.12f, 0.08f, 0.98f), false, UiKit.Round);

            _chTitle = UiKidWrap(panel, "Titulo", 30, new Color(1f, 0.85f, 0.4f));
            _chBody = UiKidWrap(panel, "Texto", 26, Color.white);

            _chBtnA = UiKit.Btn(panel, "OpA", "", null, new Color(0.24f, 0.4f, 0.2f), Color.white, 26);
            FillRect((RectTransform)_chBtnA.transform, new Vector2(0.5f, 0.15f), new Vector2(260, 58), new Vector2(0, 46));

            _chBtnB = UiKit.Btn(panel, "OpB", "", null, new Color(0.35f, 0.2f, 0.15f), Color.white, 26);
            FillRect((RectTransform)_chBtnB.transform, new Vector2(0.5f, 0.15f), new Vector2(260, 58), new Vector2(0, -40));

            _choiceRoot.SetActive(false);
        }

        Text UiKidWrap(Transform parent, string name, int size, Color color)
        {
            var t = UiKit.Txt(parent, name, "", size, color, TextAnchor.UpperCenter);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(30, 100);
            rt.offsetMax = new Vector2(-30, -20);
            return t;
        }

        public void ShowChoice(string title, string body, string labelA, Action onA, string labelB, Action onB)
        {
            _chTitle.text = title;
            _chBody.text = body;
            _chBtnA.GetComponentInChildren<Text>(true).text = labelA;
            _chBtnA.onClick.RemoveAllListeners();
            _chBtnA.onClick.AddListener(() => { _choiceRoot.SetActive(false); if (onA != null) onA(); });
            bool hasB = !string.IsNullOrEmpty(labelB);
            _chBtnB.gameObject.SetActive(hasB);
            if (hasB)
            {
                _chBtnB.GetComponentInChildren<Text>(true).text = labelB;
                _chBtnB.onClick.RemoveAllListeners();
                _chBtnB.onClick.AddListener(() => { _choiceRoot.SetActive(false); if (onB != null) onB(); });
            }
            _choiceRoot.SetActive(true);
        }

        // ==================================================================
        //  DIÁRIO (journal de pistas)
        // ==================================================================

        void BuildJournal(Transform root)
        {
            var rt = UiKit.Stretch(root, "Diario");
            _journalRoot = rt.gameObject;
            UiKit.Img(rt, "Bloqueio", new Color(0f, 0f, 0f, 0.5f), true);

            var panel = UiKit.Place(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(860, 560), Vector2.zero);
            UiKit.Img(panel, "Bg", new Color(0.2f, 0.16f, 0.1f, 0.98f), false, UiKit.Round);

            var title = UiKit.Txt(panel, "Titulo", "📓  Caderno de anotações", 30, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperLeft, FontStyle.Bold);
            FillRect((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(800, 44), new Vector2(0, -36));

            _jrBody = UiKit.Txt(panel, "Conteudo", "", 24, new Color(0.95f, 0.92f, 0.8f), TextAnchor.UpperLeft);
            var brt = (RectTransform)_jrBody.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(30, 80); brt.offsetMax = new Vector2(-30, -90);

            var close = UiKit.Btn(panel, "Fechar", "Fechar (Esc)", null, new Color(0.35f, 0.2f, 0.15f), Color.white, 24);
            FillRect((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(220, 54), new Vector2(0, 50));
            close.onClick.AddListener(CloseJournal);

            _journalRoot.SetActive(false);
        }

        public void ShowJournalText(string text)
        {
            _jrBody.text = text;
            _journalRoot.SetActive(true);
        }

        public void CloseJournal()
        {
            _journalRoot.SetActive(false);
        }

        public bool IsJournalOpen
        {
            get { return Active(_journalRoot); }
        }

        // ==================================================================
        //  BANNER (aberturas de ciclo)
        // ==================================================================

        void BuildBanner(Transform root)
        {
            var rt = UiKit.Stretch(root, "Banner");
            _bannerRoot = rt.gameObject;
            var bg = UiKit.Img(rt, "Bloqueio", new Color(0f, 0f, 0f, 0.85f), true);

            var btn = _bannerRoot.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(CloseBanner);

            _bnTitle = UiKidWrap(rt, "Titulo", 46, new Color(1f, 0.85f, 0.4f));
            var trt = (RectTransform)_bnTitle.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(1400, 200);
            trt.anchoredPosition = new Vector2(0, 120);

            _bnBody = UiKit.Txt(rt, "Texto", "", 30, Color.white, TextAnchor.UpperCenter);
            var brt = (RectTransform)_bnBody.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(1300, 260);
            brt.anchoredPosition = new Vector2(0, -20);

            _bnHint = UiKit.Txt(rt, "Hint", "clique para começar ▸", 22, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerCenter);
            var hrt = (RectTransform)_bnHint.transform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0f);
            hrt.sizeDelta = new Vector2(600, 40);
            hrt.anchoredPosition = new Vector2(0, 60);

            _bannerRoot.SetActive(false);
        }

        public void ShowBanner(string title, string body, string hint, Action onClosed)
        {
            _bnTitle.text = title;
            _bnBody.text = body;
            _bnHint.text = string.IsNullOrEmpty(hint) ? "clique para continuar ▸" : hint;
            _bannerClosed = onClosed;
            _bannerRoot.SetActive(true);
        }

        void CloseBanner()
        {
            var cb = _bannerClosed;
            _bannerClosed = null;
            _bannerRoot.SetActive(false);
            if (cb != null) cb();
        }

        // ==================================================================
        //  ENDING (finais)
        // ==================================================================

        void BuildEnding(Transform root)
        {
            var rt = UiKit.Stretch(root, "Final");
            _endRoot = rt.gameObject;
            UiKit.Img(rt, "Bloqueio", new Color(0f, 0f, 0f, 0.8f), true);

            var panel = UiKit.Place(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(1000, 660), Vector2.zero);
            UiKit.Img(panel, "Bg", new Color(0.1f, 0.08f, 0.07f, 0.97f), false, UiKit.Round);

            _endTitle = UiKit.Txt(panel, "Titulo", "", 52, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperCenter, FontStyle.Bold);
            FillRect((RectTransform)_endTitle.transform, new Vector2(0.5f, 1f), new Vector2(920, 80), new Vector2(0, -50));

            _endBody = UiKit.Txt(panel, "Texto", "", 27, Color.white, TextAnchor.UpperCenter);
            var brt = (RectTransform)_endBody.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(40, 120); brt.offsetMax = new Vector2(-40, -150);

            _endBtnA = UiKit.Btn(panel, "OpA", "", null, new Color(0.24f, 0.4f, 0.2f), Color.white, 26);
            FillRect((RectTransform)_endBtnA.transform, new Vector2(0.5f, 0f), new Vector2(340, 60), new Vector2(-190, 44));

            _endBtnB = UiKit.Btn(panel, "OpB", "", null, new Color(0.35f, 0.2f, 0.15f), Color.white, 26);
            FillRect((RectTransform)_endBtnB.transform, new Vector2(0.5f, 0f), new Vector2(340, 60), new Vector2(190, 44));

            _endRoot.SetActive(false);
        }

        public void ShowEnding(string title, string body, string labelA, Action onA, string labelB, Action onB)
        {
            _endTitle.text = title;
            _endBody.text = body;
            _endBtnA.GetComponentInChildren<Text>(true).text = labelA;
            _endBtnA.onClick.RemoveAllListeners();
            _endBtnA.onClick.AddListener(() => { _endRoot.SetActive(false); if (onA != null) onA(); });
            bool hasB = !string.IsNullOrEmpty(labelB);
            _endBtnB.gameObject.SetActive(hasB);
            if (hasB)
            {
                _endBtnB.GetComponentInChildren<Text>(true).text = labelB;
                _endBtnB.onClick.RemoveAllListeners();
                _endBtnB.onClick.AddListener(() => { _endRoot.SetActive(false); if (onB != null) onB(); });
            }
            _endRoot.SetActive(true);
        }

        public void HideEnding()
        {
            _endRoot.SetActive(false);
        }

        // ==================================================================
        //  TITLE
        // ==================================================================

        void BuildTitle(Transform root)
        {
            var rt = UiKit.Stretch(root, "Titulo");
            _titleRoot = rt.gameObject;
            UiKit.Img(rt, "Bloqueio", new Color(0.02f, 0.01f, 0.02f, 0.94f), true);

            var big = UiKit.Txt(rt, "Nome", "SILÊNCIO NA BIBLIOTECA", 74, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            FillRect((RectTransform)big.transform, new Vector2(0.5f, 0.5f), new Vector2(1600, 100), new Vector2(0, 190));

            var sub = UiKit.Txt(rt, "Sub", "demo jogável — Jogos Digitais · FEI", 26, new Color(0.8f, 0.78f, 0.7f), TextAnchor.MiddleCenter);
            FillRect((RectTransform)sub.transform, new Vector2(0.5f, 0.5f), new Vector2(1200, 40), new Vector2(0, 118));

            var pitch = UiKit.Txt(rt, "Pitch",
                "Você é o funcionário da biblioteca municipal. Aos 17h, noite adentro,\n" +
                "o expediente corre como sempre: clientes, devoluções, catalogação…\n\n" +
                "Mas às 18h, você desmaia — e acorda às 17h de novo.\n\n" +
                "Observe. Anote. Descubra o que acontece antes do relógio bater.",
                28, new Color(0.92f, 0.9f, 0.82f), TextAnchor.UpperCenter);
            FillRect((RectTransform)pitch.transform, new Vector2(0.5f, 0.5f), new Vector2(1100, 240), new Vector2(0, -20));

            var play = UiKit.Btn(rt, "Jogar", "▶  COMEÇAR O EXPEDIENTE", null, new Color(0.24f, 0.4f, 0.2f), Color.white, 30);
            FillRect((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(420, 70), new Vector2(0, -180));
            play.onClick.AddListener(() => { if (OnPlay != null) OnPlay(); });

            var hint = UiKit.Txt(rt, "Hint", "Mouse — clique nos objetos   ·   F — adiantar o tempo   ·   J — caderno de anotações   ·   F1 — pular evento",
                20, new Color(1f, 1f, 1f, 0.45f), TextAnchor.LowerCenter);
            FillRect((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(1200, 40), new Vector2(0, 90));

            var credits = UiKit.Txt(rt, "Creditos",
                "Arthur Carvalho Rotkis · Cauê Iwamoto Meira · Hanna de Oliveira Melo · Sérgio de Siqueira Santos",
                20, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter);
            FillRect((RectTransform)credits.transform, new Vector2(0.5f, 0f), new Vector2(1400, 30), new Vector2(0, 30));

            _titleRoot.SetActive(true);
        }

        public void ShowTitle()
        {
            _titleRoot.SetActive(true);
        }

        public void HideTitle()
        {
            _titleRoot.SetActive(false);
        }
    }

    // ==================================================================
    //  HotspotZone — zona clicável invisible com destaque no hover
    // ==================================================================

    public class HotspotZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public string id;
        public Image hover;
        public Text label;
        public Action<string> onClick;

        public void OnPointerEnter(PointerEventData e)
        {
            var c = hover.color; c.a = 0.12f; hover.color = c;
            label.enabled = true;
        }

        public void OnPointerExit(PointerEventData e)
        {
            var c = hover.color; c.a = 0f; hover.color = c;
            label.enabled = false;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (onClick != null) onClick(id);
        }
    }
}
