using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SilencioBiblioteca
{
    // Silêncio na Biblioteca — um único loop de 3 horas (12:00 -> 15:00, 13/10/1999) em 2min30 reais.
    // Você mantém o que aprendeu; o mundo reinicia. As pessoas esquecem tudo, menos o que você fez a elas dentro da volta.
    public class Game : MonoBehaviour
    {
        public static Game Instance;
        public static int Loop { get { return Instance != null ? Instance._loop : 1; } }
        public static int Trust(string id) { return Instance != null ? Instance.GetTrust(id) : 0; }
        public static int Susp(string id) { return Instance != null ? Instance.GetSusp(id) : 0; }

        enum Phase { Title, Intro, Play, Transition }
        Phase _phase = Phase.Title;

        const float MinPerSec = 1.2f;      // minutos de jogo por segundo real (180 min = 150 s = 2min30)
        const float NormalFire = 180f;     // 15:00
        const float ReturnMinute = 131f;   // 14:11 — Rodrigo volta pela porta dos fundos
        const float RushPerMin = 3.2f;     // Rodrigo correndo (unid./min)
        const float Speed = 3.4f;

        int _loop = 1;
        float _minute, _pending;
        int _speed = 1;          // F alterna 1x -> 3x -> 6x -> 1x
        bool _smell;
        string _lastEnd = "";

        // ---- estado do loop atual (reinicia a cada volta) ----
        bool _alerted, _alertPending, _archiveLocked, _choreTold, _choreDone;
        float _alertMinute, _fireAt = NormalFire, _expelAt = 9999f;
        Vector2 _alertPos;
        int _pressure;
        readonly HashSet<string> _shown = new HashSet<string>();
        readonly HashSet<string> _greeted = new HashSet<string>();
        readonly HashSet<string> _asked = new HashSet<string>();
        readonly HashSet<string> _believers = new HashSet<string>();
        readonly HashSet<string> _gossiped = new HashSet<string>();
        readonly Dictionary<string, int> _trust = new Dictionary<string, int>();
        readonly Dictionary<string, int> _susp = new Dictionary<string, int>();

        // ---- conhecimento (persiste entre as voltas) ----
        readonly HashSet<string> _clues = new HashSet<string>();
        bool _codeEntered, _terminalUnlocked;

        Hud _hud;
        MiniGames _mini;
        MenuUI _menu;
        AudioManager _audio;
        Camera _cam;
        Actor _player;
        readonly Dictionary<string, Actor> _npcs = new Dictionary<string, Actor>();

        class Spot { public string id, label; public Vector2 pos; public float radius; }
        readonly List<Spot> _spots = new List<Spot>();

        int GetTrust(string id) { int v; return _trust.TryGetValue(id, out v) ? v : 0; }
        int GetSusp(string id) { int v; return _susp.TryGetValue(id, out v) ? v : 0; }

        // ------------------------------------------------------------------ início
        void Awake() { Instance = this; }

        void Start()
        {
            SetupCamera();
            BuildWorld();
            BuildGlows();

            _hud = gameObject.AddComponent<Hud>();
            Settings.Load(); Settings.Apply();
            _hud.BuildAll();
            _menu = gameObject.AddComponent<MenuUI>();
            _menu.Init(_hud.Root);
            _mini = gameObject.AddComponent<MiniGames>();
            _mini.Init(_hud.Root);
            _audio = new GameObject("Audio").AddComponent<AudioManager>();
            _audio.Init();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetAsLastSibling();
            }
            GoTitle();
        }

        void SetupCamera()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            _cam.transform.position = new Vector3(0f, 0f, -10f);
            _cam.transform.rotation = Quaternion.identity;
        }

        void BuildWorld()
        {
            var bgGo = new GameObject("Fundo");
            var sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -1000;
            var tex = Resources.Load<Texture2D>("Art/BgBiblioteca");
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / WorldMap.W, 0, SpriteMeshType.FullRect);
            }
            else Debug.LogWarning("Fundo não encontrado em Resources/Art/BgBiblioteca.");

            var pGo = new GameObject();
            _player = pGo.AddComponent<Actor>();
            _player.Init("player", "Jogadora");

            foreach (var d in Story.Npcs)
            {
                var go = new GameObject();
                var a = go.AddComponent<Actor>();
                a.Init(d.id, "npc_" + d.id);
                go.SetActive(false);
                _npcs[d.id] = a;
            }

            AddSpot("estante", "Estante inferior esquerda", 470, 610, 2.2f);
            AddSpot("carrinho", "Carrinho de devolução", 1113, 292, 1.7f);
            AddSpot("registro", "Livro de registros do balcão", 590, 300, 1.5f);
            AddSpot("computador", "Computador do balcão", 815, 300, 1.5f);
            AddSpot("arquivo", "Gaveta do arquivo", 92, 262, 1.7f);
            AddSpot("mesa", "Mesa de leitura (ler o caderno)", 140, 445, 1.8f);
            AddSpot("porta_entrada", "Porta de entrada", 245, 170, 1.4f);
            AddSpot("porta_fundos", "Porta dos fundos", 1255, 170, 1.4f);
        }

        void AddSpot(string id, string label, float px, float py, float r)
        {
            _spots.Add(new Spot { id = id, label = label, pos = WorldMap.Px(px, py), radius = r });
        }

        // ------------------------------------------------------------------ brilhos (só tarefas concretas)
        readonly GameObject[] _glows = new GameObject[4];
        readonly GameObject[] _auxGlows = new GameObject[9];   // luzes azuis: onde há algo novo a descobrir

        static Sprite MakeGlowSprite()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d); a *= a;
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect);
        }

        void BuildGlows()
        {
            var sp = MakeGlowSprite();
            for (int i = 0; i < _glows.Length; i++)
            {
                var go = new GameObject("Brilho_Tarefa" + i);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp; sr.color = new Color(1f, 0.85f, 0.3f, 0.7f); sr.sortingOrder = -900;
                go.SetActive(false);
                _glows[i] = go;
            }
            for (int i = 0; i < _auxGlows.Length; i++)
            {
                var go = new GameObject("Brilho_Azul" + i);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp; sr.color = new Color(0.4f, 0.9f, 1f, 0.5f); sr.sortingOrder = -900;
                go.SetActive(false);
                _auxGlows[i] = go;
            }
        }

        void HideGlows()
        {
            for (int i = 0; i < _glows.Length; i++) if (_glows[i] != null) _glows[i].SetActive(false);
            for (int i = 0; i < _auxGlows.Length; i++) if (_auxGlows[i] != null) _auxGlows[i].SetActive(false);
        }

        // provas fortes já válidas e ainda não mostradas
        int StrongReady()
        {
            int n = 0;
            foreach (var e in Story.Evidences)
                if (e.strong && _clues.Contains(e.id) && !_shown.Contains(e.id) && _minute >= e.validFrom) n++;
            return n;
        }

        Spot FindSpot(string id) { foreach (var s in _spots) if (s.id == id) return s; return null; }

        // Brilho dourado nas tarefas de assistente (nunca em pistas, para não entregar nada).
        void UpdateGlows()
        {
            if (!Settings.Hints) { HideGlows(); return; }
            var targets = new List<string>();
            if (_choreTold && !_choreDone) targets.Add("carrinho");
            if (_choreTold && !_clues.Contains("CODE")) targets.Add("estante");
            if (_choreTold && !_terminalUnlocked) targets.Add("computador");
            if (_clues.Contains("CODE") && !_clues.Contains("DOC") && !_archiveLocked) targets.Add("arquivo");
            float pulse = Mathf.Sin(Time.time * 4f);
            for (int i = 0; i < _glows.Length; i++)
            {
                if (i < targets.Count)
                {
                    var s = FindSpot(targets[i]);
                    _glows[i].SetActive(true);
                    _glows[i].transform.position = new Vector3(s.pos.x, s.pos.y, 0f);
                    float sc = 2.6f + pulse * 0.25f;
                    _glows[i].transform.localScale = new Vector3(sc, sc, 1f);
                    var sr = _glows[i].GetComponent<SpriteRenderer>();
                    var c = sr.color; c.a = 0.55f + pulse * 0.2f; sr.color = c;
                }
                else _glows[i].SetActive(false);
            }

            // luzes azuis: embaixo de quem tem algo novo a dizer / de objetos com informação nova
            var aux = new List<Vector2>();
            if (_minute >= 66f && !_clues.Contains("REG")) aux.Add(FindSpot("registro").pos);
            if (_terminalUnlocked && _minute >= ReturnMinute && !_clues.Contains("ACESSO")) aux.Add(FindSpot("computador").pos);
            foreach (var def in Story.Npcs)
            {
                var a = _npcs[def.id];
                if (!a.gameObject.activeSelf || Hostile(def.id)) continue;
                bool news = false;
                foreach (var o in Story.Opts(def.id))
                    if (o.clue != null && o.effect == null && !_clues.Contains(o.clue) && OptAvailable(def.id, o)) { news = true; break; }
                if (def.id == "rodrigo" && !_alerted && StrongReady() >= 3) news = true;   // pronto para acusar
                if (news) aux.Add(a.Pos);
            }
            for (int i = 0; i < _auxGlows.Length; i++)
            {
                if (i < aux.Count)
                {
                    _auxGlows[i].SetActive(true);
                    _auxGlows[i].transform.position = new Vector3(aux[i].x, aux[i].y, 0f);
                    float sc = 1.5f + pulse * 0.15f;
                    _auxGlows[i].transform.localScale = new Vector3(sc, sc, 1f);
                }
                else _auxGlows[i].SetActive(false);
            }
        }

        // ------------------------------------------------------------------ título / loops
        void GoTitle()
        {
            _phase = Phase.Title;
            _hud.HideScene();
            foreach (var kv in _npcs) kv.Value.gameObject.SetActive(false);
            _player.SetPos(WorldMap.Free(WorldMap.Px(245, 268)));
            HideGlows();
            _hud.SetFade(0f); _hud.SetTint(0f); _hud.SetSpeed(1); _hud.SetPrompt("");
            _hud.SetCycle(""); _hud.SetObjective(""); _hud.SetClock("12:00", false);
            _audio.StopAlarm(0f);
            _audio.PlayMusic("blues", 0.55f, true, 1.5f);
            _menu.ShowMain(NewGame, ShowHow);
        }

        void ShowHow()
        {
            _hud.ShowBanner("COMO JOGAR",
                "13 de outubro de 1999. Às 15h um alarme dispara e você desmaia. Então acorda às 12h, no mesmo dia. Só você lembra.\n\n" +
                "WASD/Setas: mover   ·   E/Espaço: interagir   ·   1-9: escolher fala   ·   Tab: caderno   ·   F: velocidade (1x / 3x / 6x)   ·   Esc: pausa\n\n" +
                "Cada volta dura 2min30 e conversar custa tempo. As pessoas esquecem tudo ao fim da volta, mas dentro dela lembram de como você as trata.\n" +
                "Seja educada: cumprimente antes de perguntar. Dizer o que você não devia saber gera desconfiança.\n\n" +
                "Para acusar alguém: cumprimente a pessoa primeiro, depois escolha ACUSAR e mostre as provas.\n" +
                "Brilho dourado: tarefas.  Brilho azul: algo novo a descobrir.",
                "E / clique para voltar >", () => _menu.ShowMain(NewGame, ShowHow));
        }

        void OpenPause()
        {
            HideGlows();
            _menu.ShowPause(null, GoTitle);
        }

        void NewGame()
        {
            _clues.Clear(); _codeEntered = false; _terminalUnlocked = false;
            _loop = 1; _lastEnd = "";
            StartLoop();
        }

        void StartLoop()
        {
            _hud.HideScene();
            _minute = 0f; _pending = 0f; _speed = 1; _smell = false;
            _alerted = false; _alertPending = false; _archiveLocked = false; _choreTold = false; _choreDone = false;
            _fireAt = NormalFire; _expelAt = 9999f; _pressure = 0;
            _shown.Clear(); _greeted.Clear(); _asked.Clear(); _believers.Clear(); _gossiped.Clear();
            _trust.Clear(); _susp.Clear();
            _hud.SetSpeed(1);
            _audio.StopAlarm(0.6f);
            _audio.PlayMusic("blues", 0.5f, true, 1.5f);
            _player.SetPos(WorldMap.Free(WorldMap.Px(245, 268)));
            _player.Animate(Vector2.zero, 0f);
            UpdateNpcs(true);
            HideGlows();
            _hud.SetCycle("VOLTA Nº " + _loop + "  ·  13/10/1999");
            _hud.SetObjective(Objective());
            _hud.SetClock("12:00", false);
            _hud.SetTint(0f);
            _hud.SetFade(1f);
            _phase = Phase.Intro;
            if (_loop == 1)
            {
                _hud.ShowBanner("13 DE OUTUBRO DE 1999", "12:00\n\nA biblioteca abre as portas.\nSeu turno de assistente começa agora.", "E / clique para começar >", () =>
                {
                    _hud.FadeTo(0f, 0.8f);
                    ShowWake();
                });
            }
            else
            {
                _hud.FadeTo(0f, 1.6f);
                StartCoroutine(WakeDelayed());
            }
        }

        IEnumerator WakeDelayed()
        {
            yield return new WaitForSecondsRealtime(1.8f);
            ShowWake();
        }

        void ShowWake()
        {
            _hud.ShowDialogue(WakeLines(), () =>
            {
                _phase = Phase.Play;
                _audio.PlayMusic("jazz", 0.5f, true, 2f);   // jazz durante o jogo
            });
        }

        DialogueLine[] WakeLines()
        {
            var l = new List<DialogueLine>();
            if (_loop == 1)
            {
                l.Add(L.N("12:00. Seu turno na biblioteca começa."));
                l.Add(L.Me("Mais um dia comum. Dona Marta no balcão, Seu Jorge com o esfregão..."));
                l.Add(L.Me("Melhor começar falando com a Dona Marta. Ela sempre tem uma tarefa para a assistente."));
                return l.ToArray();
            }
            if (_loop == 2)
            {
                l.Add(L.N("Um zumbido nos ouvidos. Depois, silêncio."));
                l.Add(L.Me("Ué...? Eu estava no chão. A fumaça... o alarme... aquele vulto..."));
                l.Add(L.N("Você está de pé, diante do balcão. A luz de outubro atravessa as janelas. O relógio marca 12:00."));
                l.Add(L.Me("Doze horas?! Isso não faz sentido. O que aconteceu? O tempo... voltou?"));
                l.Add(L.N("Dona Marta ergue os olhos, sorri e repete, palavra por palavra, o que disse há duas horas e meia."));
                l.Add(L.Me("Eu estou sonhando. Só pode ser um sonho... Mas por que eu ainda sinto o gosto da fumaça?"));
                if (_lastEnd == "alerted") l.Add(L.Me("E o fogo veio cedo demais. O homem do casaco notou que eu o observava..."));
                if (_lastEnd == "expelled") l.Add(L.Me("Me jogaram na rua antes das 15h. Eles têm medo de mim. Preciso ser mais cuidadosa."));
                return l.ToArray();
            }
            if (_lastEnd == "alerted")
            {
                l.Add(L.N("12:00. O mesmo relógio. A mesma luz. O mesmo silêncio."));
                l.Add(L.Me("O fogo veio cedo demais. Ele percebeu que eu sabia... Preciso ter mais cuidado com o que pergunto, e a quem."));
                return l.ToArray();
            }
            if (_lastEnd == "expelled")
            {
                l.Add(L.N("12:00. Você está de volta ao balcão. O ar ainda cheira a asfalto molhado da calçada onde foi jogada."));
                l.Add(L.Me("Me expulsaram. Do outro lado da rua eu vi a fumaça subir... e ninguém pôde me ouvir."));
                l.Add(L.Me("As pessoas lembram do que eu faço, e do que eu digo. Preciso conquistar a confiança delas antes de assustá-las."));
                return l.ToArray();
            }
            l.Add(L.N("12:00. Mais uma volta."));
            if (_loop == 3) l.Add(L.Me("De novo. Não é sonho. Alguém provocou aquele incêndio, e eu sou a única que sabe que ele vai acontecer."));
            else if (_clues.Contains("DOC")) l.Add(L.Me("Uma pasta sumiu. Alguém voltou por uma porta trancada. Preciso ligar os pontos, sem acusar a pessoa errada."));
            else l.Add(L.Me("Cada volta me ensina um pouco mais. O que eu ainda não vi? O que eu ainda não perguntei?"));
            return l.ToArray();
        }

        // ------------------------------------------------------------------ loop principal
        void Update()
        {
            if (_hud == null) return;

            if (_cam != null && _phase != Phase.Transition)
                _cam.orthographicSize = Mathf.Max(WorldMap.H / 2f, WorldMap.W / 2f / Mathf.Max(0.1f, _cam.aspect));

            if (_menu != null && _menu.Active)
            {
                _hud.SetPrompt("");
                HideGlows();
                _menu.Tick();
                return;
            }
            if (_mini != null && _mini.Active)
            {
                _hud.SetPrompt("");
                HideGlows();
                _player.Animate(Vector2.zero, 0f);
                UpdateNpcsIdle();
                _mini.Tick();
                return;
            }
            if (_hud.ModalOpen)
            {
                _hud.SetPrompt("");
                HideGlows();
                _player.Animate(Vector2.zero, 0f);
                UpdateNpcsIdle();
                _hud.UpdateInput();
                return;
            }
            if (_phase != Phase.Play) { HideGlows(); return; }

            if (Input.GetKeyDown(KeyCode.Escape)) { OpenPause(); return; }
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I)) { _hud.ShowNotebook(NotebookText()); return; }
            if (Input.GetKeyDown(KeyCode.F)) { _speed = _speed == 1 ? 3 : (_speed == 3 ? 6 : 1); _hud.SetSpeed(_speed); }

            float dt = Time.deltaTime;
            _minute += dt * MinPerSec * _speed;   // F: 1x -> 3x -> 6x -> 1x
            if (_pending > 0f)
            {
                float extra = Mathf.Min(_pending, 3f * dt);   // o tempo das conversas passa aos poucos
                _pending -= extra; _minute += extra;
            }

            MovePlayer(dt);
            UpdateNpcs(false);
            UpdateInteraction();
            UpdateGlows();
            UpdateAmbience();

            if (!_smell && _minute >= _fireAt - 6f)
            {
                _smell = true;
                _hud.Toast("Um cheiro de queimado no ar...", 2.5f);
            }
            if (_minute >= _expelAt && _minute < _fireAt) { StartCoroutine(ExpelRoutine()); return; }
            if (_minute >= _fireAt) StartCoroutine(FireRoutine());
        }

        void MovePlayer(float dt)
        {
            Vector2 d = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) d.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) d.x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) d.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) d.y -= 1f;
            Vector2 p = _player.Pos;
            Vector2 start = p;
            if (d.sqrMagnitude > 0f)
            {
                Vector2 step = d.normalized * Speed * dt;
                var nx = new Vector2(p.x + step.x, p.y);
                if (!WorldMap.Blocked(nx)) p.x = nx.x;
                var ny = new Vector2(p.x, p.y + step.y);
                if (!WorldMap.Blocked(ny)) p.y = ny.y;
            }
            _player.SetPos(p);
            Vector2 vel = dt > 0f ? (p - start) / dt : Vector2.zero;
            if (vel.sqrMagnitude < 0.01f && d.sqrMagnitude > 0f) vel = d.normalized * 0.2f;
            _player.Animate(vel, dt);
        }

        // Os NPCs seguem o horário, mas SEMPRE andando (velocidade limitada): nada de teleporte depois de conversas longas.
        void UpdateNpcs(bool snap)
        {
            Vector2 archive = WorldMap.Px(100, 268);
            foreach (var def in Story.Npcs)
            {
                Actor a = _npcs[def.id];
                Vector2 target;
                bool present;
                bool rush = def.id == "rodrigo" && _alerted;
                if (rush)
                {
                    target = Vector2.MoveTowards(_alertPos, archive, Mathf.Max(0f, _minute - _alertMinute) * RushPerMin);
                    present = true;
                }
                else present = Story.TryPos(def, _minute, out target);

                if (!present) { if (a.gameObject.activeSelf) a.gameObject.SetActive(false); continue; }
                if (snap || !a.gameObject.activeSelf)
                {
                    if (!a.gameObject.activeSelf) a.gameObject.SetActive(true);
                    a.SetPos(target);
                    a.Animate(Vector2.zero, 0f);
                    continue;
                }
                float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                float dist = Vector2.Distance(a.Pos, target);
                float maxV = rush ? 5f : Mathf.Lerp(2.1f, 4.6f, Mathf.Clamp01((dist - 0.8f) / 4f));
                if (_speed > 1) maxV *= _speed * 0.7f;   // NPCs acompanham o tempo acelerado
                Vector2 np = Vector2.MoveTowards(a.Pos, target, maxV * dt);
                Vector2 vel = (np - a.Pos) / dt;
                a.SetPos(np);
                a.Animate(vel, dt);
            }
        }

        void UpdateNpcsIdle()
        {
            foreach (var kv in _npcs) if (kv.Value.gameObject.activeSelf) kv.Value.Animate(Vector2.zero, 0f);
        }

        void UpdateAmbience()
        {
            _hud.SetClock(ClockString(), _minute >= _fireAt - 30f);
            float a = 0.03f * Mathf.Min(_loop - 1, 4);
            float left = _fireAt - _minute;
            if (left < 20f) a += (20f - left) / 20f * 0.22f;
            if (left < 25f && Mathf.PerlinNoise(Time.time * 7f, 3.1f) > 0.84f) a += 0.1f;
            _hud.SetTint(a);
            _hud.SetObjective(Objective());
        }

        // ------------------------------------------------------------------ interação
        void UpdateInteraction()
        {
            Vector2 foot = _player.Pos;
            string best = null; string label = null; float bestD = 999f;
            foreach (var s in _spots)
            {
                float d = Vector2.Distance(foot, s.pos);
                if (d < s.radius && d < bestD) { bestD = d; best = s.id; label = s.label; }
            }
            foreach (var def in Story.Npcs)
            {
                var a = _npcs[def.id];
                if (!a.gameObject.activeSelf) continue;
                float d = Vector2.Distance(foot, a.Pos);
                if (d < 1.5f && d < bestD) { bestD = d; best = "npc:" + def.id; label = "Falar com " + def.name; }
            }
            _hud.SetPrompt(best == null ? "" : "[E] " + label);
            if (best != null && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))) Interact(best);
        }

        void Interact(string id)
        {
            if (id.StartsWith("npc:")) { TalkTo(id.Substring(4)); return; }
            switch (id)
            {
                case "estante": OpenBooks(); break;
                case "carrinho": OpenCart(); break;
                case "registro": OpenRegistro(); break;
                case "computador": OpenComputer(); break;
                case "arquivo": OpenArchive(); break;
                case "mesa":
                    _hud.ShowNotebook(NotebookText());
                    break;
                case "porta_entrada":
                    Say(L.N("A porta de entrada. Lá fora, um dia comum de outubro."),
                        _loop > 1 ? L.Me("Eu poderia simplesmente sair e fugir. Mas e todos aqui dentro?") : L.Me("Tudo parece em ordem."));
                    break;
                case "porta_fundos":
                    if (_minute >= ReturnMinute) Say(L.N("A porta de serviço está entreaberta. Alguém acabou de passar por ela."));
                    else Say(L.N("A porta de serviço dos fundos. Trancada."), L.Me("Só quem tem a chave da zeladoria entra por aqui."));
                    break;
            }
        }

        void Say(params DialogueLine[] lines) { _hud.ShowDialogue(lines, null); }

        // ------------------------------------------------------------------ disposição dos NPCs
        void AddTrust(string id, int d) { _trust[id] = Mathf.Clamp(GetTrust(id) + d, -4, 4); }

        void SetSusp(string id, int v) { _susp[id] = Mathf.Clamp(v, 0, 4); }

        void AddSusp(string id, int d)
        {
            int before = GetSusp(id);
            _susp[id] = Mathf.Clamp(before + d, 0, 4);
            int s = GetSusp(id);
            string nm = Story.Npc(id).name;
            if (s >= 2 && before < 2)
            {
                AddClue("SUSP_" + id, false);
                if (id == "marta")
                {
                    _archiveLocked = true;
                    _hud.Toast("Dona Marta desconfia de você e trancou o arquivo.", 3.5f);
                }
                else
                {
                    _hud.Toast(nm + " começou a desconfiar de você.", 3f);
                    if (id != "rodrigo" && !_gossiped.Contains(id)) { _gossiped.Add(id); AddSusp("marta", 1); } // o boato chega à Marta
                }
            }
            if (s >= 3 && before < 3)
            {
                if (id == "rodrigo") _alertPending = true;
                else
                {
                    _expelAt = Mathf.Min(_expelAt, _minute + 30f);
                    _hud.Toast("Alguém está chamando a segurança...", 3.5f);
                }
            }
        }

        bool Hostile(string id)
        {
            if (GetTrust(id) <= -3) return true;
            return id == "rodrigo" ? GetSusp(id) >= 2 : GetSusp(id) >= 3;
        }

        // ------------------------------------------------------------------ conversas (estilo BG3: uma fala constrói a próxima)
        bool Asked(string npc, string optId) { return _asked.Contains(npc + ":" + optId); }

        bool OptAvailable(string npc, Opt o)
        {
            if (_minute < o.fromMin || _minute >= o.toMin) return false;
            if (_loop < o.minLoop) return false;
            foreach (var n in o.need) if (!_clues.Contains(n)) return false;
            if (o.needAny.Length > 0)
            {
                bool any = false;
                foreach (var n in o.needAny) if (_clues.Contains(n)) any = true;
                if (!any) return false;
            }
            foreach (var n in o.hideIf) if (_clues.Contains(n)) return false;
            int t = GetTrust(npc), s = GetSusp(npc);
            if (t < o.minTrust || t > o.maxTrust) return false;
            if (s < o.minSusp || s > o.maxSusp) return false;
            if (o.once && Asked(npc, o.id)) return false;
            foreach (var n in o.notAsked) if (Asked(npc, n)) return false;
            foreach (var n in o.after) if (!Asked(npc, n)) return false;
            if (o.afterAny.Length > 0)
            {
                bool any = false;
                foreach (var n in o.afterAny) if (Asked(npc, n)) any = true;
                if (!any) return false;
            }
            if (o.effect == "ACCUSE" && !HasEvidence()) return false;
            return true;
        }

        bool HasEvidence()
        {
            foreach (var e in Story.Evidences) if (_clues.Contains(e.id) && !_shown.Contains(e.id)) return true;
            return false;
        }

        void TalkTo(string npcId)
        {
            var def = Story.Npc(npcId);
            if (def == null) return;
            if (Hostile(npcId)) { Say(Story.Refusal(npcId)); return; }
            string gk = npcId + (_minute >= 100f ? "_b" : "_a");
            if (!_greeted.Contains(gk))
            {
                _greeted.Add(gk);
                if (npcId == "marta") _choreTold = true;
                var g = new List<DialogueLine>(Story.Greeting(npcId, _minute));
                if (_loop >= 2) g.Add(Story.LoopReaction(npcId));
                _hud.ShowDialogue(g.ToArray(), () => ShowHub(def));
                return;
            }
            ShowHub(def);
        }

        void ShowHub(NpcDef def)
        {
            var opts = new List<MenuOpt>();
            MenuOpt accuse = null;
            foreach (var o in Story.Opts(def.id))
            {
                if (!OptAvailable(def.id, o)) continue;
                var oo = o;
                var mo = new MenuOpt(o.label, () => RunOpt(def, oo));
                if (o.effect == "ACCUSE") { accuse = mo; continue; }
                if (opts.Count < 7) opts.Add(mo);
            }
            if (accuse != null) { if (def.id == "rodrigo") opts.Insert(0, accuse); else opts.Add(accuse); }
            opts.Add(new MenuOpt("Sair da conversa.", null));
            _hud.ShowMenu(def.name, Story.Mood(def.id, GetTrust(def.id), GetSusp(def.id)), opts, () => { }, def.id + "_p0");
        }

        void RunOpt(NpcDef def, Opt o)
        {
            string id = def.id;
            _asked.Add(id + ":" + o.id);
            if (o.effect == "ACCUSE") { ShowEvidenceMenu(def, 0); return; }

            int trustBefore = GetTrust(id);
            bool success = true;
            if (o.effect == "PROVE") success = trustBefore >= 0 && GetSusp(id) < 2;
            else if (o.effect == "TELL") success = _believers.Contains(id) || trustBefore >= 2;

            var lines = new List<DialogueLine>();
            lines.Add(L.Me(o.label));
            lines.AddRange((success || o.failLines == null) ? o.lines() : o.failLines());

            string gain = null;
            if (o.effect == "PROVE")
            {
                if (success) { _believers.Add(id); AddTrust(id, 2); SetSusp(id, 0); gain = o.clue; }
                else AddSusp(id, 1);
            }
            else if (o.effect == "TELL")
            {
                if (success) AddTrust(id, 1);
                else AddSusp(id, trustBefore < 0 ? 2 : 1);
            }
            else
            {
                if (o.dTrust != 0) AddTrust(id, o.dTrust);
                if (o.dSusp != 0) AddSusp(id, o.dSusp);
                if (o.clue != null) gain = o.clue;
            }

            if (gain != null && !_clues.Contains(gain)) AddClue(gain, true);
            _pending += o.cost;
            _hud.ShowDialogue(lines.ToArray(), () => AfterTalk(def, false));
        }

        void AfterTalk(NpcDef def, bool toEvidence)
        {
            if (_alertPending) { _alertPending = false; TriggerAlert(); return; }
            if (Hostile(def.id)) { Say(Story.Refusal(def.id)); return; }
            if (toEvidence) ShowEvidenceMenu(def, 0); else ShowHub(def);
        }

        // Rodrigo percebe que você sabe: corre para o arquivo e o fogo vem mais cedo.
        void TriggerAlert()
        {
            if (_alerted) return;
            _alerted = true;
            _alertMinute = _minute;
            Actor r = _npcs["rodrigo"];
            _alertPos = r.gameObject.activeSelf ? r.Pos : WorldMap.Px(100, 268);
            float dist = Vector2.Distance(_alertPos, WorldMap.Px(100, 268));
            _fireAt = Mathf.Min(_fireAt, _minute + Mathf.Max(30f, dist / RushPerMin + 12f));
            _lastEnd = "alerted";
            _smell = false;
            _hud.Toast("Rodrigo corre em direção ao arquivo!", 3.5f);
        }

        // ----- confronto com provas -----
        static DialogueLine RoA(string t) { return L.D("Rodrigo", "rodrigo_p4", t); }

        void ShowEvidenceMenu(NpcDef def, int page)
        {
            var known = new List<Ev>();
            foreach (var e in Story.Evidences)
                if (_clues.Contains(e.id) && !_shown.Contains(e.id)) known.Add(e);
            const int per = 6;
            int start = Mathf.Clamp(page, 0, Mathf.Max(0, (known.Count - 1) / per)) * per;
            var opts = new List<MenuOpt>();
            for (int i = start; i < known.Count && i < start + per; i++)
            {
                var ev = known[i];
                opts.Add(new MenuOpt("Mostrar: " + Story.ClueText[ev.id], () => PresentEvidence(def, ev)));
            }
            if (known.Count > start + per) opts.Add(new MenuOpt("Mais coisas no caderno...", () => ShowEvidenceMenu(def, page + 1)));
            if (def.id == "rodrigo") opts.Add(new MenuOpt("\"Eu sei o que o senhor vai fazer às 15h.\"", Bluff));
            opts.Add(new MenuOpt("Voltar.", () => ShowHub(def)));
            _hud.ShowMenu("ACUSANDO: " + def.name,
                "Você está acusando " + def.name + " de provocar o incêndio. Mostre as provas direto a essa pessoa. Provas que apontam para quem de fato fez isso a abalam (3 bastam); as que não apontam a deixam furiosa. Provas de fatos que ainda não aconteceram levantam suspeitas.",
                opts, () => ShowHub(def), def.id + "_p0");
        }

        void PresentEvidence(NpcDef def, Ev ev)
        {
            string id = def.id;
            var lines = new List<DialogueLine>();
            string face = id == "rodrigo" ? "rodrigo_p4" : id + "_p0";
            _pending += 2f;
            lines.Add(L.Me("Veja isto: " + Story.ClueText[ev.id]));
            if (_minute < ev.validFrom)
            {
                // vazamento de futuro
                lines.Add(L.D(def.name, face, "Isso... ainda nem aconteceu. Como você sabe disso?!"));
                lines.Add(L.N("O rosto de " + def.name + " muda. Aquilo foi um erro."));
                AddSusp(id, 2); AddTrust(id, -1);
            }
            else if (id == "rodrigo")
            {
                if (ev.strong)
                {
                    _shown.Add(ev.id); _pressure++;
                    var r = Story.EvidRodrigo(ev.id);
                    for (int i = 1; i < r.Length; i++) lines.Add(r[i]); // pula a fala da jogadora (já adicionada)
                    if (_pressure == 2) lines.Add(L.N("O suor escorre pela têmpora de Rodrigo. A mão dele desaparece dentro do casaco..."));
                }
                else
                {
                    lines.Add(L.D(def.name, face, "E o que isso tem a ver comigo?"));
                    lines.Add(L.N("Ele sorri de canto. Você deu um passo em falso."));
                    AddSusp(id, 1); AddTrust(id, -1);
                }
            }
            else
            {
                int dT, dS; string gives;
                var r = Story.EvidOther(id, ev.id, out dT, out dS, out gives);
                lines.AddRange(r);
                AddTrust(id, dT);
                if (dS > 0) AddSusp(id, dS);
                if (gives != null && !_clues.Contains(gives)) AddClue(gives, true);
            }
            _hud.ShowDialogue(lines.ToArray(), () =>
            {
                if (id == "rodrigo" && _pressure >= 3 && !_alerted) { BreakRodrigo(); return; }
                AfterTalk(def, true);
            });
        }

        void Bluff()
        {
            if (_minute >= ReturnMinute && _pressure >= 2)
            {
                _hud.ShowDialogue(L.Lines(
                    L.Me("Eu sei o que o senhor vai fazer às 15h. Eu já vi o fogo. Mais de uma vez."),
                    L.N("O rosto de Rodrigo perde a cor. Os lábios tremem sem emitir som.")), BreakRodrigo);
                return;
            }
            AddSusp("rodrigo", 2);
            _hud.ShowDialogue(L.Lines(
                L.Me("Eu sei o que o senhor vai fazer às 15h."),
                RoA("Você... não sabe de nada!"),
                L.N("Ele empurra você contra a estante e corre em direção ao arquivo.")), () =>
                {
                    _alertPending = false;
                    TriggerAlert();
                });
        }

        void BreakRodrigo()
        {
            _phase = Phase.Transition;
            var lines = L.Lines(
                RoA("...Cala a boca."),
                L.N("A voz dele falha. As mãos tremem."),
                L.D("Rodrigo", "rodrigo_p5", "Aquele relatório... meu pai assinou a aprovação da fiação sabendo que estava podre."),
                L.D("Rodrigo", "rodrigo_p5", "Se alguém lesse aquilo, meu nome, a minha carreira... tudo acabaria."),
                L.D("Rodrigo", "rodrigo_p5", "Eu só queria que o arquivo desaparecesse. Ninguém ia se machucar. Ninguém..."),
                L.Me("A Dona Marta está no balcão! O Lucas, o Seu Jorge... todos estão aqui dentro!"),
                RoA("...Eu não pensei nisso."),
                L.N("Devagar, ele tira do casaco um frasco de solvente e um isqueiro. Deposita os dois sobre a mesa."));
            _hud.ShowDialogue(lines, () => StartCoroutine(Victory()));
        }

        // ------------------------------------------------------------------ pistas e tarefas
        void AddClue(string id, bool announce)
        {
            if (_clues.Contains(id)) return;
            _clues.Add(id);
            if (announce) _hud.Toast("Caderno atualizado.", 2f);   // sem dizer o que foi anotado: nada de dicas
        }

        // Tarefa de assistente: devolver os livros do carrinho (bullet hell). Rende a confiança da Dona Marta.
        void OpenCart()
        {
            if (!_choreTold)
            {
                Say(L.N("Um carrinho cheio de livros devolvidos."), L.Me("Melhor falar com a Dona Marta antes de mexer."));
                return;
            }
            if (_choreDone)
            {
                Say(L.N("O carrinho está vazio. Todos os livros voltaram às prateleiras."));
                return;
            }
            _hud.ShowDialogue(L.Lines(
                L.N("Você empurra o carrinho até a estante. As prateleiras velhas rangem e de repente os livros começam a despencar!"),
                L.Me("Eu preciso guardar todos... sem ser atingida!")), () =>
                {
                    _mini.StartHell(win =>
                    {
                        if (win)
                        {
                            _choreDone = true; _asked.Add("marta:@chore"); _pending += 6f;
                            Say(L.N("O último livro encaixa na prateleira. Suas mãos tremem, mas a estante firmou."),
                                L.Me("Pronto. A Dona Marta vai gostar de saber."));
                        }
                        else Say(L.N("Os livros se espalharam pelo chão."), L.Me("Respira. Eu consigo. Mais uma vez."));
                    });
                });
        }

        // Puzzle: estante de empréstimos (colocar os livros na ordem certa)
        void OpenBooks()
        {
            if (_clues.Contains("CODE"))
            {
                Say(L.N("Os livros estão em ordem. O bilhete que caiu de trás do livro de Literatura diz: 4-7-2."),
                    L.Me("Os números de uma gaveta. Onde estaria essa gaveta?"));
                return;
            }
            if (!_choreTold)
            {
                Say(L.N("A estante inferior esquerda. Os livros estão todos fora do lugar."), L.Me("Melhor falar com a Dona Marta antes de mexer."));
                return;
            }
            _mini.StartShelf(win =>
            {
                if (win)
                {
                    AddClue("CODE", true);
                    _pending += 4f;
                    Say(L.N("Ao encaixar o último livro, algo cai de trás dele: um bilhete dobrado."),
                        L.N("Três números, escritos à caneta: 4-7-2."),
                        L.Me("Parece o código de alguma gaveta. Do arquivo, talvez?"));
                }
                else Say(L.Me("Ainda não está certo. Eu volto a isso."));
            });
        }

        // Registro e computador do balcão: só fatos soltos, sem conclusões.
        void OpenRegistro()
        {
            var sb = new StringBuilder();
            sb.Append("Entradas e saídas registradas até agora (" + ClockString() + "):\n\n");
            int shown = 0;
            for (int i = 0; i < Story.RegLines.Length; i++)
                if (_minute >= Story.RegMinutes[i]) { sb.Append(Story.RegLines[i]).Append("\n"); shown++; }
            if (shown == 0) sb.Append("(nenhum registro ainda)\n");
            if (_minute >= 66f && !_clues.Contains("REG")) AddClue("REG", true);
            _hud.ShowPaper("LIVRO DE REGISTROS    (E / Esc para fechar)", sb.ToString(), null);
        }

        // Puzzle: terminal protegido por Genius (disponível desde o início; depois mostra o histórico de acessos até agora)
        void OpenComputer()
        {
            if (_terminalUnlocked) { ShowTerminalLog(); return; }
            _hud.ShowDialogue(L.Lines(
                L.N("O terminal do balcão pede uma verificação por luzes: é preciso repetir a sequência de cores."),
                L.Me("Parece um desses jogos de memória... Vamos ver.")), () =>
            {
                _mini.StartGenius(win =>
                {
                    if (win)
                    {
                        _terminalUnlocked = true;
                        _pending += 4f;
                        Say(L.N("A tela pisca em verde: acesso liberado."));
                        _hud.Toast("Terminal destravado.", 2f);
                    }
                    else Say(L.Me("O terminal travou. Eu tento de novo."));
                });
            });
        }

        void ShowTerminalLog()
        {
            if (_minute >= ReturnMinute)
            {
                AddClue("ACESSO", true);
                Say(L.N("Sistema de acessos — porta de serviço:"),
                    L.N("14:11 — porta aberta com a chave ZELADORIA 02."),
                    L.Me("A chave que abre aquela porta. Quem a usou?"));
            }
            else Say(L.N("Sistema de acessos: nenhuma abertura da porta de serviço registrada até agora (" + ClockString() + ")."));
        }

        // Puzzle: gaveta do arquivo
        void OpenArchive()
        {
            if (_archiveLocked)
            {
                Say(L.N("A gaveta está trancada. Dona Marta levou a chave consigo."), L.Me("Ela não confia mais em mim."));
                return;
            }
            if (!_clues.Contains("CODE"))
            {
                Say(L.N("Uma gaveta de arquivo com teclado numérico de 3 dígitos."), L.Me("Preciso descobrir o código."));
                return;
            }
            if (_codeEntered) { ShowArchiveContents(); return; }
            _hud.ShowCode("Gaveta do arquivo", "Digite o código de 3 dígitos.", 3, c => c == "472", () =>
            {
                _codeEntered = true;
                ShowArchiveContents();
            }, null);
        }

        void ShowArchiveContents()
        {
            _pending += 2f;
            if (_minute < 28f)
            {
                AddClue("PASTA_ANTES", true);
                Say(L.N("A gaveta abre. Dentro, uma pasta amarelada: \"Relatório de Vistoria Predial — 1989\"."),
                    L.N("Reprovação da fiação. Várias páginas de anotações técnicas."),
                    L.Me("Está tudo aqui. Por enquanto."));
            }
            else if (_clues.Contains("DOC"))
            {
                Say(L.N("A pasta vazia continua ali: Relatório de Vistoria 1989 — retirado por R. Alves."));
            }
            else
            {
                AddClue("DOC", true);
                Say(L.N("A gaveta abre. Dentro, só uma pasta vazia: \"Relatório de Vistoria Predial — 1989\"."),
                    L.N("Na ficha de retirada: \"R. Alves\"."),
                    L.Me("Alguém retirou o relatório. R. Alves..."));
            }
        }

        // ------------------------------------------------------------------ caderno / objetivo
        string ClockString()
        {
            int m = Mathf.FloorToInt(_minute);
            return (12 + m / 60).ToString("00") + ":" + (m % 60).ToString("00");
        }

        string Objective()
        {
            if (!_clues.Contains("FOGO"))
            {
                if (!_choreTold) return "Objetivo: apresente-se à Dona Marta, no balcão.";
                var tasks = new List<string>();
                if (!_choreDone) tasks.Add("devolva os livros do carrinho (à direita)");
                if (!_clues.Contains("CODE")) tasks.Add("arrume a estante inferior esquerda");
                if (!_terminalUnlocked) tasks.Add("destrave o terminal do balcão");
                if (tasks.Count > 0) return "Objetivo: " + string.Join(" e ", tasks.ToArray()) + ".";
                return "Objetivo: termine seu turno. Converse com as pessoas.";
            }
            return "Objetivo: descubra quem vai provocar o incêndio. Para acusar alguém, cumprimente a PRÓPRIA pessoa e depois escolha ACUSAR.";
        }

        string NotebookText()
        {
            var sb = new StringBuilder();
            sb.Append("Volta nº ").Append(_loop).Append("  ·  13/10/1999  ·  ").Append(ClockString()).Append("\n");
            sb.Append(Objective()).Append("\n\nO QUE EU SEI:\n");
            int n = 0;
            foreach (var id in Story.ClueOrder)
                if (_clues.Contains(id)) { sb.Append(" - ").Append(Story.ClueText[id]).Append("\n"); n++; }
            if (n == 0) sb.Append(" (nada ainda — converse com as pessoas e examine os objetos)\n");
            sb.Append("\nQuem fez isto? O caderno só guarda fatos. As conclusões são suas.\nPara acusar alguém, cumprimente a PRÓPRIA pessoa primeiro; depois escolha ACUSAR e mostre 3 provas que apontem para ela.");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ fim do loop
        IEnumerator FireRoutine()
        {
            _phase = Phase.Transition;
            _speed = 1; _hud.SetSpeed(1); _hud.SetPrompt(""); HideGlows();
            _lastEnd = _alerted ? "alerted" : "alarm";
            _audio.StopMusic(0.4f);
            _audio.PlayAlarm(0.7f);
            _hud.Toast(_alerted ? "FOGO! Rodrigo foi mais rápido..." : "15:00 — O ALARME DISPARA!", 3.5f);
            _hud.FlashRed();
            StartCoroutine(Shake(1.6f));
            yield return new WaitForSecondsRealtime(1.4f);
            if (!_clues.Contains("FOGO"))
            {
                AddClue("FOGO", false); AddClue("LOOP", false);
                _hud.ShowDialogue(L.Lines(
                    L.N("O alarme berra. Fumaça espessa sai do arquivo, na parede esquerda."),
                    L.N("Entre a fumaça, um vulto de casaco se afasta... e some."),
                    L.Me("O que...? Eu não consigo respirar. Tudo está... escurecendo..."),
                    L.Me("Se eu descobrir quem foi, vou ter que olhar a pessoa nos olhos e acusá-la com provas.")), null);
                yield return new WaitUntil(() => !_hud.ModalOpen);
            }
            // a biblioteca em chamas: última imagem do loop antes de acordar de novo
            _hud.ShowScene("CenaFogo", 0.9f);
            yield return new WaitForSecondsRealtime(3.4f);
            _hud.FadeTo(1f, 1.4f);
            yield return new WaitForSecondsRealtime(1.8f);
            _hud.HideScene();
            _loop++;
            StartLoop();
        }

        IEnumerator ExpelRoutine()
        {
            _phase = Phase.Transition;
            _speed = 1; _hud.SetSpeed(1); _hud.SetPrompt(""); HideGlows();
            _lastEnd = "expelled";
            if (!_clues.Contains("FOGO")) { AddClue("FOGO", false); AddClue("LOOP", false); }
            _audio.StopMusic(0.5f);
            _hud.ShowScene("CenaExpulsa", 0.8f);
            _hud.Toast("Dona Marta e Seu Jorge te escoltam para fora.", 3.5f);
            yield return new WaitForSecondsRealtime(0.8f);
            _hud.ShowDialogue(L.Lines(
                L.D("Dona Marta", "marta_p0", "Você ficou estranha o dia todo. Chega. Vou pedir que se retire, moça."),
                L.D("Seu Jorge", "jorge_p0", "Vamos, filha. Sem escândalo."),
                L.N("A porta se fecha atrás de você. Na calçada, o relógio da praça marca 14h58."),
                L.N("Às 15h, o alarme toca e uma coluna de fumaça sobe pelas janelas do arquivo."),
                L.Me("Eu estava certa. E ninguém me ouviu. Ninguém...")), null);
            yield return new WaitUntil(() => !_hud.ModalOpen);
            _hud.FadeTo(1f, 1.6f);
            yield return new WaitForSecondsRealtime(2.0f);
            _hud.HideScene();
            _loop++;
            StartLoop();
        }

        IEnumerator Shake(float dur)
        {
            if (!Settings.Shake) yield break;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / dur;
                _cam.transform.position = new Vector3(UnityEngine.Random.Range(-1f, 1f) * 0.14f * k, UnityEngine.Random.Range(-1f, 1f) * 0.14f * k, -10f);
                yield return null;
            }
            _cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        IEnumerator Victory()
        {
            _phase = Phase.Transition;
            _speed = 1; _hud.SetSpeed(1); _hud.SetPrompt(""); HideGlows();
            _hud.Toast("O relógio avança...", 3f);
            _audio.StopMusic(1.0f);
            _audio.StopAlarm(1.0f);
            yield return new WaitForSecondsRealtime(1.8f);
            _hud.FadeTo(1f, 1.4f);
            yield return new WaitForSecondsRealtime(1.8f);
            _audio.PlayMusic("vitoria", 0.7f, false, 0.6f);
            _hud.ShowFinal(GoTitle);
            _hud.FadeTo(0f, 1.6f);
        }
    }
}
