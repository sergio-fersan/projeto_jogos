using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SilencioBiblioteca
{
    // Minigames em tela cheia (sobre o Canvas): estante de livros, Genius e "bullet hell" de livros.
    // O Game chama Tick() enquanto Active = true.
    public class MiniGames : MonoBehaviour
    {
        enum Kind { None, Shelf, Genius, Hell }
        Kind _kind = Kind.None;
        Transform _root; GameObject _go; Action<bool> _done;
        AudioSource _aud; AudioClip[] _tones;

        public bool Active { get { return _kind != Kind.None; } }

        public void Init(Transform root)
        {
            _root = root;
            _aud = gameObject.AddComponent<AudioSource>();
            _aud.playOnAwake = false;
            float[] f = { 262f, 330f, 392f, 523f };
            _tones = new AudioClip[4];
            for (int i = 0; i < 4; i++) _tones[i] = Tone(f[i]);
        }

        static AudioClip Tone(float freq)
        {
            int sr = 44100, n = (int)(sr * 0.4f);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Clamp01(i / (sr * 0.02f)) * Mathf.Clamp01((n - i) / (sr * 0.08f));
                d[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sr) * 0.3f * env;
            }
            var c = AudioClip.Create("tone" + (int)freq, n, 1, sr, false);
            c.SetData(d, 0);
            return c;
        }

        public void Tick()
        {
            switch (_kind)
            {
                case Kind.Shelf: TickShelf(); break;
                case Kind.Genius: TickGenius(); break;
                case Kind.Hell: TickHell(); break;
            }
        }

        RectTransform Open(Kind k, Action<bool> done, string name)
        {
            if (_go != null) Destroy(_go);
            var rt = UiKit.Stretch(_root, name);
            _go = rt.gameObject; _kind = k; _done = done;
            UiKit.Img(rt, "Dim", new Color(0f, 0f, 0f, 0.82f), true);
            return rt;
        }

        void Close(bool ok)
        {
            var cb = _done; _done = null;
            _kind = Kind.None;
            if (_go != null) { Destroy(_go); _go = null; }
            _bul.Clear(); _pk.Clear();
            if (cb != null) cb(ok);
        }

        // botão quadrado
        Button FlatBtn(Transform parent, string label, Vector2 anchor, Vector2 size, Vector2 pos, Action onClick)
        {
            var fr = UiKit.Frame(parent, "Btn", anchor, size, pos, new Color(0.2f, 0.13f, 0.07f, 1f), Hud.CBorder, 2f);
            var img = fr.gameObject.GetComponent<Image>();
            img.raycastTarget = true;
            var btn = fr.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = new Color(0.75f, 0.75f, 0.75f, 1f); cb.highlightedColor = Color.white; cb.pressedColor = new Color(1f, 0.85f, 0.5f, 1f);
            cb.selectedColor = cb.normalColor; cb.colorMultiplier = 1f; cb.fadeDuration = 0.05f;
            btn.colors = cb;
            var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
            btn.onClick.AddListener(() => { if (onClick != null) onClick(); });
            var t = UiKit.Txt(fr, "L", label, 26, Hud.CText, TextAnchor.MiddleCenter, FontStyle.Bold);
            var trt = (RectTransform)t.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            return btn;
        }

        static void Fix(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        // =============================================================== ESTANTE
        static readonly string[] ShTitles =
        {
            "Cálculo Diferencial", "Química Orgânica", "Código Civil Comentado", "Direito Constitucional",
            "Brasil Colônia", "Guerra do Paraguai", "Dom Casmurro", "Memórias Póstumas"
        };
        static readonly Color[] ShColors =
        {
            new Color(0.45f, 0.12f, 0.10f), new Color(0.12f, 0.30f, 0.18f), new Color(0.14f, 0.20f, 0.40f), new Color(0.55f, 0.40f, 0.10f),
            new Color(0.35f, 0.15f, 0.35f), new Color(0.10f, 0.35f, 0.35f), new Color(0.38f, 0.24f, 0.12f), new Color(0.25f, 0.27f, 0.32f)
        };
        const float SpW = 130f, SpGap = 14f, SpH = 330f;
        int[] _sh; RectTransform[] _spRt; Image[] _spImg; Text[] _spTxt; RectTransform _shCursor;
        int _shCur, _shSel; Text _shMsg;

        public void StartShelf(Action<bool> done)
        {
            var rt = Open(Kind.Shelf, done, "Mini_Estante");
            _sh = new[] { 6, 2, 5, 0, 7, 3, 1, 4 }; // embaralhado (a ordem certa é 0..7)
            _shCur = 0; _shSel = -1;

            var panel = UiKit.Frame(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(1480, 800), Vector2.zero, new Color(0.10f, 0.065f, 0.045f, 1f), Hud.CBorder, 5f);
            var title = UiKit.Txt(panel, "Titulo", "ESTANTE DE EMPRÉSTIMOS", 36, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(1300, 50), new Vector2(0, -40));

            var plaque = UiKit.Frame(panel, "Placa", new Vector2(0.5f, 1f), new Vector2(1180, 100), new Vector2(0, -120), new Color(0.78f, 0.62f, 0.32f, 1f), new Color(0.35f, 0.22f, 0.08f, 1f), 4f);
            var pt = UiKit.Txt(plaque, "T", "SEÇÕES:  CIÊNCIAS  ·  DIREITO  ·  HISTÓRIA  ·  LITERATURA\nDentro de cada seção, em ordem alfabética pelo título.", 26, new Color(0.18f, 0.09f, 0.03f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var ptr = (RectTransform)pt.transform; ptr.anchorMin = Vector2.zero; ptr.anchorMax = Vector2.one; ptr.offsetMin = Vector2.zero; ptr.offsetMax = Vector2.zero;

            float total = 8 * SpW + 7 * SpGap;
            var shelf = UiKit.Place(panel, "Prateleira", new Vector2(0.5f, 0.5f), new Vector2(total, SpH + 50), new Vector2(0, -35));
            var plank = UiKit.Frame(panel, "Tabua", new Vector2(0.5f, 0.5f), new Vector2(total + 60, 26), new Vector2(0, -35 - (SpH + 50) / 2f + 6f), new Color(0.35f, 0.21f, 0.10f, 1f), new Color(0.18f, 0.10f, 0.04f, 1f), 3f);

            var cur = UiKit.Place(shelf, "Cursor", new Vector2(0f, 0f), new Vector2(SpW + 18, SpH + 18), Vector2.zero);
            var curImg = cur.gameObject.AddComponent<Image>(); curImg.color = new Color(1f, 0.8f, 0.35f, 0.45f); curImg.raycastTarget = false;
            _shCursor = cur;

            _spRt = new RectTransform[8]; _spImg = new Image[8]; _spTxt = new Text[8];
            for (int i = 0; i < 8; i++)
            {
                int slot = i;
                var sp = UiKit.Place(shelf, "Livro" + i, new Vector2(0f, 0f), new Vector2(SpW, SpH), new Vector2(i * (SpW + SpGap) + SpW / 2f, SpH / 2f + 10f));
                var img = sp.gameObject.AddComponent<Image>(); img.raycastTarget = true;
                var btn = sp.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
                var cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.pressedColor = Color.white; cb.selectedColor = Color.white; cb.colorMultiplier = 1f; btn.colors = cb;
                var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
                btn.onClick.AddListener(() => { _shCur = slot; ShelfSelect(); });
                // faixas decorativas da lombada
                var band1 = UiKit.Img(sp, "F1", new Color(0f, 0f, 0f, 0.28f), false); var b1 = (RectTransform)band1.transform; b1.offsetMin = new Vector2(0, SpH - 36); b1.offsetMax = Vector2.zero;
                var band2 = UiKit.Img(sp, "F2", new Color(0f, 0f, 0f, 0.28f), false); var b2 = (RectTransform)band2.transform; b2.offsetMin = Vector2.zero; b2.offsetMax = new Vector2(0, -(SpH - 36));
                var tx = UiKit.Txt(sp, "T", "", 24, new Color(0.96f, 0.92f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
                var trt = (RectTransform)tx.transform;
                Fix(trt, new Vector2(0.5f, 0.5f), new Vector2(SpH - 80, SpW - 16), Vector2.zero);
                trt.localEulerAngles = new Vector3(0, 0, 90);
                _spRt[i] = sp; _spImg[i] = img; _spTxt[i] = tx;
            }

            _shMsg = UiKit.Txt(panel, "Msg", "Troque a posição dos livros: clique em um e depois em outro. Setas movem, E seleciona/troca, Enter confere.", 24, Hud.CText, TextAnchor.MiddleCenter);
            Fix((RectTransform)_shMsg.transform, new Vector2(0.5f, 0f), new Vector2(1350, 64), new Vector2(0, 150));
            FlatBtn(panel, "Conferir ordem", new Vector2(0.5f, 0f), new Vector2(300, 60), new Vector2(-190, 70), ShelfCheck);
            FlatBtn(panel, "Desistir", new Vector2(0.5f, 0f), new Vector2(300, 60), new Vector2(190, 70), () => Close(false));
            ShelfRefresh();
        }

        void ShelfRefresh()
        {
            for (int s = 0; s < 8; s++)
            {
                _spImg[s].color = ShColors[_sh[s]];
                _spTxt[s].text = ShTitles[_sh[s]];
                float y = SpH / 2f + 10f + (s == _shSel ? 34f : 0f);
                _spRt[s].anchoredPosition = new Vector2(s * (SpW + SpGap) + SpW / 2f, y);
            }
            _shCursor.anchoredPosition = new Vector2(_shCur * (SpW + SpGap) + SpW / 2f, SpH / 2f + 10f);
        }

        void ShelfSelect()
        {
            if (_shSel < 0) _shSel = _shCur;
            else if (_shSel == _shCur) _shSel = -1;
            else { int t = _sh[_shSel]; _sh[_shSel] = _sh[_shCur]; _sh[_shCur] = t; _shSel = -1; }
            ShelfRefresh();
        }

        void ShelfCheck()
        {
            int ok = 0;
            for (int i = 0; i < 8; i++) if (_sh[i] == i) ok++;
            if (ok == 8) { Close(true); return; }
            _shMsg.text = ok + " de 8 livros estão no lugar certo. Continue tentando.";
        }

        void TickShelf()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(false); return; }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) { _shCur = Mathf.Max(0, _shCur - 1); ShelfRefresh(); }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) { _shCur = Mathf.Min(7, _shCur + 1); ShelfRefresh(); }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) ShelfSelect();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ShelfCheck();
        }

        // =============================================================== GENIUS
        Image[] _gPad; Color[] _gBase, _gLitC; int[] _gSeq; int _gRound, _gIn, _gShow, _gState, _gLitPad;
        float _gT, _gLitT; Text _gStatus;

        public void StartGenius(Action<bool> done)
        {
            var rt = Open(Kind.Genius, done, "Mini_Genius");
            _gSeq = new int[5];
            for (int i = 0; i < _gSeq.Length; i++) _gSeq[i] = UnityEngine.Random.Range(0, 4);
            _gRound = 1; _gIn = 0; _gShow = 0; _gState = 0; _gT = 0.9f; _gLitPad = -1; _gLitT = 0f;
            _gBase = new[] { new Color(0.55f, 0.10f, 0.10f), new Color(0.10f, 0.22f, 0.55f), new Color(0.10f, 0.45f, 0.18f), new Color(0.60f, 0.50f, 0.08f) };
            _gLitC = new[] { new Color(1f, 0.30f, 0.28f), new Color(0.35f, 0.55f, 1f), new Color(0.35f, 1f, 0.50f), new Color(1f, 0.92f, 0.30f) };

            var panel = UiKit.Frame(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(900, 820), Vector2.zero, new Color(0.08f, 0.07f, 0.06f, 1f), new Color(0.55f, 0.55f, 0.5f, 1f), 5f);
            var title = UiKit.Txt(panel, "Titulo", "TERMINAL DO BALCÃO — VERIFICAÇÃO", 32, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(840, 50), new Vector2(0, -42));
            _gStatus = UiKit.Txt(panel, "Status", "Observe a sequência...", 30, Hud.CText, TextAnchor.MiddleCenter);
            Fix((RectTransform)_gStatus.transform, new Vector2(0.5f, 1f), new Vector2(840, 50), new Vector2(0, -100));

            _gPad = new Image[4];
            Vector2[] pos = { new Vector2(-125, 130), new Vector2(125, 130), new Vector2(-125, -120), new Vector2(125, -120) };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var pd = UiKit.Frame(panel, "Pad" + i, new Vector2(0.5f, 0.5f), new Vector2(236, 236), pos[i] + new Vector2(0, -20), _gBase[i], new Color(0.05f, 0.05f, 0.05f, 1f), 4f);
                var bg = pd.Find("Fundo").GetComponent<Image>();
                bg.raycastTarget = true;
                var btn = pd.gameObject.AddComponent<Button>(); btn.targetGraphic = bg;
                var cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = Color.white; cb.pressedColor = Color.white; cb.selectedColor = Color.white; cb.colorMultiplier = 1f; btn.colors = cb;
                var nav = new Navigation(); nav.mode = Navigation.Mode.None; btn.navigation = nav;
                btn.onClick.AddListener(() => GeniusPress(idx));
                var nt = UiKit.Txt(pd, "N", (i + 1).ToString(), 40, new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
                var nrt = (RectTransform)nt.transform; nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one; nrt.offsetMin = Vector2.zero; nrt.offsetMax = Vector2.zero;
                _gPad[i] = bg;
            }
            var hint = UiKit.Txt(panel, "Dica", "Repita a sequência de luzes. Clique ou use as teclas 1-4. Esc sai.", 22, Hud.CDim, TextAnchor.MiddleCenter);
            Fix((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(840, 40), new Vector2(0, 40));
        }

        void GeniusLight(int i, float dur)
        {
            if (_gLitPad >= 0) _gPad[_gLitPad].color = _gBase[_gLitPad];
            _gLitPad = i; _gLitT = dur;
            _gPad[i].color = _gLitC[i];
            if (_aud != null) _aud.PlayOneShot(_tones[i], Settings.Sfx);
        }

        void GeniusPress(int i)
        {
            if (_gState != 2) return;
            GeniusLight(i, 0.25f);
            if (i == _gSeq[_gIn])
            {
                _gIn++;
                if (_gIn >= _gRound)
                {
                    _gRound++;
                    if (_gRound > _gSeq.Length) { _gState = 4; _gT = 1.0f; _gStatus.text = "Acesso liberado."; _gStatus.color = new Color(0.5f, 1f, 0.6f); }
                    else { _gState = 0; _gT = 0.9f; _gStatus.text = "Muito bem... Observe de novo."; }
                }
            }
            else
            {
                _gStatus.text = "Errou! Observe de novo."; _gStatus.color = new Color(1f, 0.5f, 0.45f);
                _gState = 3; _gT = 1.2f; _gIn = 0;
            }
        }

        void TickGenius()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(false); return; }
            if (_gLitPad >= 0)
            {
                _gLitT -= dt;
                if (_gLitT <= 0f) { _gPad[_gLitPad].color = _gBase[_gLitPad]; _gLitPad = -1; }
            }
            switch (_gState)
            {
                case 0:
                    _gT -= dt;
                    if (_gT <= 0f) { _gState = 1; _gShow = 0; _gT = 0f; _gStatus.color = Hud.CText; _gStatus.text = "Rodada " + _gRound + " de " + _gSeq.Length + " — observe..."; }
                    break;
                case 1:
                    _gT -= dt;
                    if (_gT <= 0f)
                    {
                        if (_gShow < _gRound) { GeniusLight(_gSeq[_gShow], 0.45f); _gShow++; _gT = 0.72f; }
                        else { _gState = 2; _gIn = 0; _gStatus.text = "Sua vez!"; }
                    }
                    break;
                case 2:
                    for (int i = 0; i < 4; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) GeniusPress(i);
                    break;
                case 3:
                    _gT -= dt;
                    if (_gT <= 0f) { _gState = 0; _gT = 0.6f; _gStatus.color = Hud.CText; }
                    break;
                case 4:
                    _gT -= dt;
                    if (_gT <= 0f) Close(true);
                    break;
            }
        }

        // =============================================================== BULLET HELL DE LIVROS
        class Bul { public RectTransform rt; public Vector2 pos, vel; }
        class Pk { public RectTransform rt; public Image img; public Vector2 pos; public float life; }
        readonly List<Bul> _bul = new List<Bul>();
        readonly List<Pk> _pk = new List<Pk>();
        RectTransform _arena, _hp; Image _hpImg; Vector2 _pp; Text _hHud;
        float _hLeft, _hElapsed, _hSpawn, _hSweep, _hAim, _hPick, _hInv; int _hLives, _hGot;
        const int HNeed = 6;
        const float AW = 1180f, AH = 620f;

        public void StartHell(Action<bool> done)
        {
            var rt = Open(Kind.Hell, done, "Mini_Livros");
            _bul.Clear(); _pk.Clear();
            _pp = Vector2.zero; _hLeft = 34f; _hElapsed = 0f; _hSpawn = 0.6f; _hSweep = 4f; _hAim = 2f; _hPick = 0.5f; _hInv = 0f; _hLives = 3; _hGot = 0;

            var panel = UiKit.Frame(rt, "Painel", new Vector2(0.5f, 0.5f), new Vector2(1300, 860), Vector2.zero, new Color(0.10f, 0.065f, 0.045f, 1f), Hud.CBorder, 5f);
            var title = UiKit.Txt(panel, "Titulo", "DEVOLVER OS LIVROS — A ESTANTE ESTÁ DESABANDO!", 32, Hud.CAccent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fix((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(1250, 50), new Vector2(0, -38));
            _hHud = UiKit.Txt(panel, "Hud", "", 26, Hud.CText, TextAnchor.MiddleCenter);
            Fix((RectTransform)_hHud.transform, new Vector2(0.5f, 1f), new Vector2(1250, 40), new Vector2(0, -84));

            var af = UiKit.Frame(panel, "ArenaMoldura", new Vector2(0.5f, 0.5f), new Vector2(AW + 16, AH + 16), new Vector2(0, -28), new Color(0.2f, 0.12f, 0.07f, 1f), Hud.CBorderDim, 4f);
            _arena = UiKit.Place(af, "Arena", new Vector2(0.5f, 0.5f), new Vector2(AW, AH), Vector2.zero);
            var floor = _arena.gameObject.AddComponent<Image>(); floor.color = new Color(0.25f, 0.16f, 0.10f, 1f); floor.raycastTarget = false;
            _arena.gameObject.AddComponent<RectMask2D>();
            for (int k = -4; k <= 4; k++)
            {
                var pl = UiKit.Place(_arena, "Tabua" + k, new Vector2(0.5f, 0.5f), new Vector2(4, AH), new Vector2(k * 130f, 0));
                var pi = pl.gameObject.AddComponent<Image>(); pi.color = new Color(0f, 0f, 0f, 0.25f); pi.raycastTarget = false;
            }

            _hp = UiKit.Place(_arena, "Jogadora", new Vector2(0.5f, 0.5f), new Vector2(62, 86), Vector2.zero);
            _hpImg = _hp.gameObject.AddComponent<Image>();
            _hpImg.sprite = CharArt.Body("player", 0); _hpImg.preserveAspect = true; _hpImg.raycastTarget = false;

            var hint = UiKit.Txt(panel, "Dica", "WASD / setas: mover.  Pegue os livros dourados e desvie dos que caem.  Esc: desistir.", 22, Hud.CDim, TextAnchor.MiddleCenter);
            Fix((RectTransform)hint.transform, new Vector2(0.5f, 0f), new Vector2(1250, 36), new Vector2(0, 36));
            HellHud();
        }

        void HellHud()
        {
            _hHud.text = "Livros devolvidos: " + _hGot + "/" + HNeed + "        Vidas: " + _hLives + "        Tempo: " + Mathf.CeilToInt(Mathf.Max(0f, _hLeft));
        }

        static readonly Color[] HellBook =
        {
            new Color(0.55f, 0.15f, 0.12f), new Color(0.18f, 0.35f, 0.22f), new Color(0.16f, 0.24f, 0.48f), new Color(0.50f, 0.36f, 0.12f), new Color(0.42f, 0.2f, 0.4f)
        };

        void HellSpawn(Vector2 pos, Vector2 vel)
        {
            if (_bul.Count > 80) return;
            var rt = UiKit.Place(_arena, "Livro", new Vector2(0.5f, 0.5f), new Vector2(54, 34), pos);
            var img = rt.gameObject.AddComponent<Image>(); img.color = HellBook[UnityEngine.Random.Range(0, HellBook.Length)]; img.raycastTarget = false;
            var st = UiKit.Img(rt, "Faixa", new Color(0.95f, 0.9f, 0.75f, 0.9f), false);
            var srt = (RectTransform)st.transform; srt.offsetMin = new Vector2(6, 13); srt.offsetMax = new Vector2(-6, -13);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg);
            var b = new Bul(); b.rt = rt; b.pos = pos; b.vel = vel;
            _bul.Add(b);
        }

        void TickHell()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(false); return; }
            _hElapsed += dt; _hLeft -= dt; _hInv -= dt;

            // movimento da jogadora
            Vector2 d = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) d.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) d.x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) d.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) d.y -= 1f;
            if (d.sqrMagnitude > 0f) _pp += d.normalized * 440f * dt;
            _pp.x = Mathf.Clamp(_pp.x, -AW / 2f + 32f, AW / 2f - 32f);
            _pp.y = Mathf.Clamp(_pp.y, -AH / 2f + 46f, AH / 2f - 46f);
            _hp.anchoredPosition = _pp;
            bool blink = _hInv > 0f && ((int)(_hInv * 12f) % 2 == 0);
            _hpImg.color = blink ? new Color(1f, 1f, 1f, 0.3f) : Color.white;

            // padrões de livros
            float ramp = Mathf.Min(_hElapsed, 30f);
            _hSpawn -= dt;
            if (_hSpawn <= 0f)
            {
                HellSpawn(new Vector2(UnityEngine.Random.Range(-AW / 2f + 30f, AW / 2f - 30f), AH / 2f + 30f),
                          new Vector2(UnityEngine.Random.Range(-40f, 40f), -(230f + ramp * 4f + UnityEngine.Random.Range(0f, 90f))));
                _hSpawn = Mathf.Max(0.16f, 0.44f - ramp * 0.008f);
            }
            _hSweep -= dt;
            if (_hSweep <= 0f)
            {
                bool left = UnityEngine.Random.value < 0.5f;
                float gap = UnityEngine.Random.Range(-190f, 190f);
                for (float y = -AH / 2f + 40f; y <= AH / 2f - 40f; y += 66f)
                    if (Mathf.Abs(y - gap) > 100f)
                        HellSpawn(new Vector2(left ? -AW / 2f - 40f : AW / 2f + 40f, y), new Vector2(left ? 250f : -250f, 0f));
                _hSweep = 5.2f;
            }
            _hAim -= dt;
            if (_hAim <= 0f)
            {
                var from = new Vector2(UnityEngine.Random.value < 0.5f ? -AW / 2f - 30f : AW / 2f + 30f, UnityEngine.Random.Range(-AH / 2f, AH / 2f));
                HellSpawn(from, (_pp - from).normalized * 330f);
                _hAim = 2.3f;
            }

            // livros dourados (para devolver)
            _hPick -= dt;
            if (_pk.Count < 2 && _hPick <= 0f)
            {
                Vector2 p = Vector2.zero;
                for (int tries = 0; tries < 12; tries++)
                {
                    p = new Vector2(UnityEngine.Random.Range(-AW / 2f + 80f, AW / 2f - 80f), UnityEngine.Random.Range(-AH / 2f + 80f, AH / 2f - 80f));
                    if (Vector2.Distance(p, _pp) > 240f) break;
                }
                var rt = UiKit.Place(_arena, "Dourado", new Vector2(0.5f, 0.5f), new Vector2(46, 46), p);
                var border = rt.gameObject.AddComponent<Image>(); border.color = new Color(0.4f, 0.25f, 0.05f, 1f); border.raycastTarget = false;
                var inner = UiKit.Img(rt, "I", new Color(1f, 0.82f, 0.25f, 1f), false);
                var irt = (RectTransform)inner.transform; irt.offsetMin = new Vector2(5, 5); irt.offsetMax = new Vector2(-5, -5);
                var pk = new Pk(); pk.rt = rt; pk.img = inner; pk.pos = p; pk.life = 10f;
                _pk.Add(pk);
                _hPick = 1.2f;
            }
            for (int i = _pk.Count - 1; i >= 0; i--)
            {
                var pk = _pk[i];
                pk.life -= dt;
                float sc = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.12f;
                pk.rt.localScale = new Vector3(sc, sc, 1f);
                if (Vector2.Distance(pk.pos, _pp) < 48f)
                {
                    _hGot++;
                    Destroy(pk.rt.gameObject); _pk.RemoveAt(i);
                    if (_hGot >= HNeed) { Close(true); return; }
                }
                else if (pk.life <= 0f) { Destroy(pk.rt.gameObject); _pk.RemoveAt(i); }
            }

            // movimento e colisão dos livros
            for (int i = _bul.Count - 1; i >= 0; i--)
            {
                var b = _bul[i];
                b.pos += b.vel * dt;
                b.rt.anchoredPosition = b.pos;
                if (Mathf.Abs(b.pos.x) > AW / 2f + 100f || Mathf.Abs(b.pos.y) > AH / 2f + 100f)
                {
                    Destroy(b.rt.gameObject); _bul.RemoveAt(i); continue;
                }
                if (_hInv <= 0f && Mathf.Abs(b.pos.x - _pp.x) < 24f + 16f && Mathf.Abs(b.pos.y - (_pp.y - 4f)) < 17f + 26f)
                {
                    _hLives--; _hInv = 1.3f;
                    Destroy(b.rt.gameObject); _bul.RemoveAt(i);
                    if (_hLives <= 0) { Close(false); return; }
                }
            }
            if (_hLeft <= 0f) { Close(false); return; }
            HellHud();
        }
    }
}
