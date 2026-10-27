using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilencioBiblioteca
{
    // Orquestra a demo: ciclos, relógio, acontecimentos programados,
    // resolucão das 18h e os dois finais.
    public class Game : MonoBehaviour
    {
        public static Game Instance;

        HudUI _hud;

        enum Phase { Title, Intro, Playing, Resolution, Ended }
        Phase _phase = Phase.Title;

        int _cycle = 1;
        const int MaxCycle = 3;
        const float SecondsPerGameMinute = 5f;
        const int GameHour = 17;

        float _gameMinute = 0f;
        bool _fast = false;
        bool _intervention = false;
        bool _flicker = false;

        readonly HashSet<string> _clues = new HashSet<string>();

        class GameEvent { public int minute; public Action run; public bool done; }
        readonly List<GameEvent> _events = new List<GameEvent>();

        // Pistas (ids)
        public const string ClueFire = "fogo";
        public const string ClueVisitor = "visitante";
        public const string ClueHeater = "aquecedor";

        void Awake() { Instance = this; }

        void Start()
        {
            _hud = gameObject.AddComponent<HudUI>();
            var bg = Resources.Load<Sprite>("Art/BgBiblioteca");
            if (bg == null) Debug.LogWarning("Background não encontrado em Assets/Resources/Art (Resources/Art/BgBiblioteca). Rode o menu 'Silêncio na Biblioteca > 1. Preparar projeto' no Editor.");
            _hud.BuildAll(bg);
            _hud.SetZoneHandler(OnHotspot);
            _hud.SetNpcClickHandler(OnNpcClick);
            _hud.OnPlay = () => { _hud.HideTitle(); StartCycle(1); };
            _hud.ShowTitle();
        }

        void Update()
        {
            if (_hud == null) return;

            // atalhos funcionam fora do título
            if (_phase == Phase.Playing || _phase == Phase.Intro)
            {
                if (Input.GetKeyDown(KeyCode.F) && _phase == Phase.Playing) { _fast = !_fast; _hud.SetFastIndicator(_fast); }
                if (Input.GetKeyDown(KeyCode.F1) && _phase == Phase.Playing) SkipToNextEvent();
                if (Input.GetKeyDown(KeyCode.J) && _phase == Phase.Playing) RequestToggleJournal();
                if (Input.GetKeyDown(KeyCode.Escape) && _phase == Phase.Playing) _hud.CloseJournal();
            }

            if (_phase != Phase.Playing) return;
            if (_hud.ModalOpen) return;

            float rate = (_fast ? 8f : 1f) / SecondsPerGameMinute;
            _gameMinute += Time.deltaTime * rate;
            if (_flicker) _hud.UpdateFlicker(Time.time);

            for (int i = 0; i < _events.Count; i++)
            {
                var ev = _events[i];
                if (!ev.done && _gameMinute >= ev.minute)
                {
                    ev.done = true;
                    ev.run();
                }
            }
            if (!_hud.ModalOpen)
            {
                _hud.SetClock(ClockText());
                if (_gameMinute >= 60f) Resolve18h();
            }
        }

        string ClockText()
        {
            int m = Mathf.Clamp(Mathf.FloorToInt(_gameMinute), 0, 59);
            return GameHour + ":" + m.ToString("00");
        }

        void SkipToNextEvent()
        {
            int min = int.MaxValue;
            bool any = false;
            foreach (var ev in _events)
                if (!ev.done && ev.minute > _gameMinute && ev.minute < min) { min = ev.minute; any = true; }
            _gameMinute = any ? min : 59.4f;
        }

        public void RequestToggleJournal()
        {
            if (_hud.IsJournalOpen)
            {
                _hud.CloseJournal();
            }
            else
            {
                _hud.ShowJournalText(JournalText());
            }
        }

        // ==================================================================
        //  CICLOS
        // ==================================================================

        void StartCycle(int cycle)
        {
            _cycle = cycle;
            _gameMinute = 0f;
            _fast = false;
            _hud.SetFastIndicator(false);
            _flicker = false;
            _puzzleSolvedThisCycle = false;
            if (cycle == 1) { _clues.Clear(); _intervention = false; }

            _hud.ClearNpcs();
            _hud.FlashReset();
            BuildEvents(_cycle);
            _hud.SetHotspotsVisible(_cycle);
            _hud.SetTintForCycle(_cycle);

            _phase = Phase.Intro;
            _hud.SetClock("17:00");
            _hud.ShowBanner(CycleTitle(_cycle), CycleBody(_cycle), CycleHint(), () => _phase = Phase.Playing);
        }

        bool _puzzleSolvedThisCycle;

        static string CycleTitle(int c)
        {
            if (c == 1) return "CICLO 1 — Sexta-feira, 17:00";
            if (c == 2) return "CICLO 2 — 17:00… de novo";
            return "CICLO FINAL — 17:00";
        }

        static string CycleBody(int c)
        {
            if (c == 1)
                return "Expediente tranquilo: clientes, devoluções, catalogação do dia.\nÀs 18h, você vai para casa, como sempre.";
            if (c == 2)
                return "O relógio marca 17:00. Não foi um apagão — é o mesmo dia se repetindo.\nDesta vez, observe melhor: algo mudou.";
            return "Você já conhece a rotina de cor.\nUse tudo o que aprendeu antes de o relógio chegar às 18h.";
        }

        static string CycleHint()
        {
            return "Clique onde a tela acende ao passar o mouse.";
        }

        void AddEvent(int minute, Action run)
        {
            _events.Add(new GameEvent { minute = minute, run = run });
        }

        static Vector2 N(float fx, float fy)
        {
            return new Vector2(fx * 1920f, fy * 1080f);
        }

        void BuildEvents(int c)
        {
            _events.Clear();

            AddEvent(2, () => Spawn("almeida", N(0.168f, 0.76f), N(0.50f, 0.44f), StoryData.Almeida(c)));
            AddEvent(10, () => _hud.ShowSubtitle(c == 1
                ? "A coordenação deixou um livro para catalogar hoje. (Veja o COMPUTADOR do balcão.)"
                : "De novo: a mesma solicitação de catalogação, no mesmo minuto.", 4f));
            AddEvent(13, () => Spawn("abreu", N(0.168f, 0.76f), N(0.44f, 0.44f), StoryData.Abreu(c)));
            AddEvent(12, () => Leave("almeida"));
            AddEvent(24, () => Leave("abreu"));

            if (c >= 2)
            {
                AddEvent(28, () => Spawn("visitante", N(0.876f, 0.76f), N(0.715f, 0.46f), StoryData.Visitante(c)));
                AddEvent(31, () => _hud.ShowSubtitle("O homem de casaco cinza atravessa a sala, direito à estante de História Militar.", 4f));
                AddEvent(38, () =>
                {
                    _flicker = true;
                    _hud.ShowSubtitle(c == 2
                        ? "A luz pisca uma vez. Você não lembra disso no dia de ontem."
                        : "A luz treme. Você já sabe o que vem primeiro.", 4f);
                });
                AddEvent(46, () => Leave("visitante"));
            }

            if (c == 3)
            {
                AddEvent(45, () => _hud.ShowSubtitle("17h45. Falta pouco. Falta pouco sempre.", 4f));
            }

            AddEvent(52, () => _hud.ShowSubtitle("Sua cabeça pesa um pouco. Não muito. Ainda.", 4f));
        }

        void Spawn(string kind, Vector2 from, Vector2 to, DialogueLine[] onArrive)
        {
            var view = _hud.SpawnNpc(kind);
            if (view == null) return;
            view.rt.anchoredPosition = from;
            StartCoroutine(WalkTo(view, to, () =>
            {
                if (onArrive != null) _hud.ShowDialogue(onArrive);
            }));
        }

        void Leave(string kind)
        {
            var view = _hud.FindNpc(kind);
            if (view == null) return;
            StartCoroutine(WalkTo(view, N(0.168f, 0.76f), () => _hud.DespawnNpc(kind)));
        }

        IEnumerator WalkTo(HudUI.NpcView view, Vector2 to, Action onArrive)
        {
            if (view == null) yield break;
            RectTransform rt = view.rt;
            Vector2 from = rt.anchoredPosition;
            float dur = Mathf.Clamp(Vector2.Distance(from, to) / 520f, 1.0f, 3.4f);
            float t = 0f;
            while (t < dur)
            {
                if (view == null || view.rt == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                Vector2 p = Vector2.Lerp(from, to, k);
                p.y += Mathf.Sin(t * 9f) * 2.5f;
                rt.anchoredPosition = p;
                yield return null;
            }
            if (view != null && view.rt != null)
            {
                rt.anchoredPosition = to;
                if (onArrive != null) onArrive();
            }
        }

        void OnNpcClick(string kind)
        {
            if (_phase != Phase.Playing) return;
            if (_hud.ModalOpen) return;
            switch (kind)
            {
                case "almeida":
                    _hud.ShowDialogue(L.Lines(L.D("Dona Almeida", "Só vim entregar os livros, querido. Até já!")));
                    break;
                case "abreu":
                    _hud.ShowDialogue(L.Lines(L.D("Sr. Abreu", "Fica com as revistas. E lembrando: o aquecedor do canto…" ),
                        L.N("(No canto da sala, o zumbido continua, paciente.)") ));
                    break;
                case "visitante":
                    _hud.ShowDialogue(_cycle == 2
                        ? L.Lines(L.D("Visitante", "Está tudo aqui, obrigado. Gosto de folhear antes de decidir." ),
                            L.N("(Ele não tira os olhos do relógio.)"))
                        : L.Lines(L.D("Visitante", "De novo estamos cara a cara. Ou talvez seja a primeira vez — para nós dois." )));
                    break;
            }
        }

        // ==================================================================
        //  INTERAÇÕES
        // ==================================================================

        void OnHotspot(string id)
        {
            if (_phase != Phase.Playing || _hud.ModalOpen) return;

            switch (id)
            {
                case "relogio":
                    _hud.ShowDialogue(StoryData.Flavor("relogio", _cycle, ClockText()));
                    break;
                case "computador":
                    OpenComputer();
                    break;
                case "balcao":
                    _hud.ShowDialogue(StoryData.Flavor("balcao", _cycle, null));
                    break;
                case "estante_esq":
                    _hud.ShowDialogue(StoryData.Flavor("estante_esq", _cycle, null));
                    break;
                case "estante_hist":
                    _hud.ShowDialogue(StoryData.Flavor("estante_hist", _cycle, null));
                    break;
                case "mesa":
                    _hud.ShowDialogue(StoryData.Flavor("mesa", _cycle, null));
                    break;
                case "entrada":
                    _hud.ShowDialogue(StoryData.Flavor("entrada", _cycle, null));
                    break;
                case "aquecedor":
                    OnHeater();
                    break;
                case "porta_fundos":
                    OnBackDoor();
                    break;
            }
        }

        void OpenComputer()
        {
            if (!_puzzleSolvedThisCycle)
            {
                _hud.ShowChoice("Computador do balcão",
                    "O sistema da biblioteca abre em uma janela.",
                    "Catalogar o livro do dia",
                    () => _hud.ShowPuzzle(PuzzleData.Get(_cycle),
                        () => { _puzzleSolvedThisCycle = true; OnPuzzleSolved(_cycle); },
                        () => { _gameMinute = Mathf.Min(_gameMinute + 2f, 59.5f); }),
                    "Consultar os registros do sistema",
                    () =>
                    {
                        // no ciclo 2+, a vistoria revela a possibilidade do incêndio às 18h
                        bool jaTinha = _clues.Contains(ClueFire);
                        if (_cycle >= 2 && !jaTinha) GainClue(ClueFire, silent: true);
                        _hud.ShowDialogue(StoryData.Registros(_cycle, jaTinha));
                    });
            }
            else
            {
                _hud.ShowDialogue(StoryData.Registros(_cycle, _clues.Contains(ClueFire)));
            }
        }

        void OnPuzzleSolved(int cycle)
        {
            var def = PuzzleData.Get(cycle);
            var lines = new List<DialogueLine>
            {
                L.N(def.success)
            };
            if (cycle == 1)
            {
                lines.Add(L.N("(Catalogar é fácil. Catalogar o mesmo livro, todos os dias, no mesmo minuto, é outra conversa.)"));
            }
            else if (cycle == 2)
            {
                lines.Add(L.N("(O registro do manual aponta que o mesmo cliente o guarda toda sexta.)"));
                lines.Add(L.D("Você", "O homem de casaco cinza. Estudar quem lê 'Manual de Artilharia'… não é rotina de leitor comum."));
                GainClue(ClueVisitor);
            }
            else
            {
                lines.Add(L.N("(Três boletins do mesmo problema, todos do aquecedor do canto. O clima fina aqui deve ficar caro.)"));
                if (!_clues.Contains(ClueHeater)) lines.Add(L.D("Você", "Cada detalhe reforça o que o caderno já sabia."));
            }
            _hud.ShowDialogue(lines.ToArray());
        }

        void OnHeater()
        {
            if (_cycle < 2)
            {
                _hud.ShowDialogue(L.Lines(L.N("Um aquecedor comum de bancada, ligado no canto. Sem nada estranho — por enquanto.")));
                return;
            }
            if (!_clues.Contains(ClueHeater))
            {
                GainClue(ClueHeater);
                _hud.ShowDialogue(StoryData.Flavor("aquecedor", _cycle, null));
            }
            else
            {
                _hud.ShowDialogue(L.Lines(L.N("O aquecedor do canto. O ponto derretido na tomada não é coincidência."), L.D("Você", "Se a fiação estalar perto das estantes…")));
            }
        }

        void OnBackDoor()
        {
            if (_cycle < 3)
            {
                _hud.ShowDialogue(L.Lines(
                    L.N("A porta dos fundos está trancada: \"somente autorizadas\"."),
                    L.D("Você", _cycle == 1 ? "O corredor de serviços. Você nunca teve motivo para olhar para lá." : "O corredor de serviços — onde ficam o quadro de luz e o extintor vencido."))
                );
                return;
            }

            if (_intervention)
            {
                _hud.ShowDialogue(L.Lines(L.N("Você já desligou o disjuntor atrás da porta. O zumbido do aquecedor acabou — para sempre.")));
                return;
            }

            bool ready = _clues.Contains(ClueFire) && _clues.Contains(ClueVisitor) && _clues.Contains(ClueHeater);
            if (!ready)
            {
                var lines = new List<DialogueLine> { L.N("Você fecha a mão na maçaneta… e recua. Falta contexto para agir de cabeça fria.") };
                if (!_clues.Contains(ClueFire)) lines.Add(L.D("Diário", "Falta entender: o que acontece às 18h? (Registros do computador — ciclo 2)"));
                if (!_clues.Contains(ClueVisitor)) lines.Add(L.D("Diário", "Falta entender: por que de História Militar? (Catalogação + visitante — ciclo 2)"));
                if (!_clues.Contains(ClueHeater)) lines.Add(L.D("Diário", "Falta descobrir: onde começa o incêndio? (Examine o aquecedor do canto)"));
                _hud.ShowDialogue(lines.ToArray());
                return;
            }

            _hud.ShowChoice("Porta dos Fundos",
                "Enquanto a biblioteca toda respira no ritmo de sempre, a decisão cabe a você:\ndesligar a energia do aquecedor agora?",
                "Descer a chave do disjuntor",
                () =>
                {
                    _intervention = true;
                    _flicker = false;
                    _hud.ShowDialogue(L.Lines(
                        L.N("Você abre o painel, desce a chave do circuito do canto leste — o mesmo em que o aquecedor dorme."),
                        L.D("Você", "Feito. Às 18h, nada vai acontecer. Nada do pior, digo.")));
                },
                "Dar mais uma volta antes",
                null);
        }

        void GainClue(string id, bool silent = false)
        {
            if (_clues.Contains(id)) return;
            _clues.Add(id);
            if (!silent && clueDict.TryGetValue(id, out var name))
                _hud.ShowSubtitle("📓 Anotado no caderno: " + name, 4f);
        }

        // ==================================================================
        //  FIM DO CICLO — 18:00
        // ==================================================================

        void Resolve18h()
        {
            _phase = Phase.Resolution;
            StopAllCoroutines();
            _hud.SetClock("18:00");
            if (_intervention && _cycle == MaxCycle)
                StartCoroutine(EndingSurvive());
            else
                StartCoroutine(EndingCollapse(_cycle == MaxCycle));
        }

        IEnumerator EndingCollapse(bool final)
        {
            _fast = false;
            yield return new WaitForSeconds(1.2f);
            _hud.FlashRed();
            yield return new WaitForSeconds(1.8f);
            _hud.FadeToBlack();
            yield return new WaitForSeconds(1.6f);
            if (final)
            {
                _hud.ShowEnding("18:00",
                    TragicText(),
                    "Tentar o último ciclo novamente",
                    () => StartCycle(MaxCycle),
                    "Voltar ao início",
                    GoTitle);
            }
            else
            {
                StartCycle(_cycle + 1);
            }
        }

        IEnumerator EndingSurvive()
        {
            _fast = false;
            yield return new WaitForSeconds(1.6f);
            _hud.SetTintForCycle(1);
            _hud.SetClock("18:01");
            _hud.ShowSubtitle("Silêncio. De verdade, pela primeira vez.", 3.5f);
            yield return new WaitForSeconds(3.6f);
            var sb = new StringBuilder();
            sb.Append("O relógio marca 18:01 — e continua andando.\n\n");
            sb.Append("Não houve fumaça, não houve chama. O aquecedor do canto, desligado por você, ");
            sb.Append("nunca fará o dia recomeçar.\n\n");
            sb.Append("Pela primeira vez, a biblioteca é só uma biblioteca.\n\n");
            sb.Append("CLIENTES atendidos: todos.  CICLOS: ").Append(_cycle).Append(".\n");
            sb.Append("DECISÃO: você mudou a sequência de eventos — nada mais.");
            _hud.ShowEnding("18:01",
                sb.ToString(),
                "Jogar novamente",
                GoTitle,
                "", null);
        }

        string TragicText()
        {
            var sb = new StringBuilder();
            sb.Append("O fogo começa no aquecedor do canto e salta para as estantes.");
            sb.Append("\nO expediente termina às 18:00 — e o dia pode recomeçar, se você souber o que mudar.\n\n");
            sb.Append("Pistas que faltaram:\n");
            if (!_clues.Contains(ClueFire)) sb.Append("  🔥 A possibilidade do incêndio às 18h (registros do computador — ciclo 2)\n");
            if (!_clues.Contains(ClueVisitor)) sb.Append("  👤 Por que o visitante procura livros de História Militar (catalogação — ciclo 2)\n");
            if (!_clues.Contains(ClueHeater)) sb.Append("  ⚡ Onde o fogo começa (examine o aquecedor do canto)\n");
            sb.Append("\nDica: nenhum item você perde ao recomeçar o ciclo.");
            return sb.ToString();
        }

        void GoTitle()
        {
            _cycle = 1;
            _clues.Clear();
            _intervention = false;
            _phase = Phase.Title;
            _hud.ClearNpcs();
            _hud.FlashReset();
            _hud.ShowTitle();
        }

        // ==================================================================
        //  DIÁRIO
        // ==================================================================

        static readonly Dictionary<string, string> clueDict = new Dictionary<string, string>
        {
            { ClueFire, "🔥 Biblioteca pega fogo às 18h (vistorias)" },
            { ClueVisitor, "👤 O visitante e o Manual de Artilharia (1943)" },
            { ClueHeater, "⚡ Fiação viciada no aquecedor do canto" },
        };

        string JournalText()
        {
            var sb = new StringBuilder();
            sb.Append("Você anota o que percebeu entre 17h e 18h:\n");
            sb.Append("\n— PISTAS —\n");
            sb.Append(_clues.Contains(ClueFire) ? clueDict[ClueFire] : "  (???)");
            sb.Append("\n");
            sb.Append(_clues.Contains(ClueVisitor) ? clueDict[ClueVisitor] : "(???)");
            sb.Append("\n");
            sb.Append(_clues.Contains(ClueHeater) ? clueDict[ClueHeater] : "(???)");
            sb.Append("\n\n— SOBRE O DIA —\n");
            sb.Append("Ciclo atual: ").Append(_cycle).Append(" de ").Append(MaxCycle).Append(".\n");
            sb.Append("Ciclos 1 e 2 acabam às 18h e recomeçam sozinhos.\n");
            sb.Append("No ciclo final, você decide: mudar o padrão ou repeti-lo.\n");
            sb.Append("\nDica: tudo que parece rotina se repete — os clientes dizem as mesmas linhas no mesmo minuto.");
            return sb.ToString();
        }
    }
}
