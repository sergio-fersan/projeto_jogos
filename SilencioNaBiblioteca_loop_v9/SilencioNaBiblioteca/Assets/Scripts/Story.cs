using System;
using System.Collections.Generic;
using UnityEngine;

namespace SilencioBiblioteca
{
    public class Key
    {
        public float t; public Vector2 p;
        public Key(float t, Vector2 p) { this.t = t; this.p = p; }
    }

    public struct Wp
    {
        public float x, y, until;
        public Wp(float x, float y, float until) { this.x = x; this.y = y; this.until = until; }
    }

    public class NpcDef
    {
        public string id, name;
        public Key[][] stints; // trechos em que o NPC está na biblioteca (t em minutos desde 12:00)
        public NpcDef(string id, string name, params Key[][] stints) { this.id = id; this.name = name; this.stints = stints; }
    }

    // Uma fala do jogador. Só aparece conforme horário, volta, conhecimento, ordem da conversa e confiança.
    // (O tom — gentil, grosso, direto — não é mostrado: está no próprio texto da fala.)
    public class Opt
    {
        public static readonly string[] None = new string[0];
        public string id, label;
        public int fromMin = 0, toMin = 999, minLoop = 1;
        public int minTrust = -9, maxTrust = 9, minSusp = 0, maxSusp = 9;
        public int cost = 2;                   // minutos de jogo que a fala consome
        public int dTrust = 0, dSusp = 0;      // efeito oculto na confiança / desconfiança
        public string[] need = None, needAny = None, hideIf = None, notAsked = None;
        public string[] after = None;          // falas que precisam ter sido ditas antes (nesta volta)
        public string[] afterAny = None;       // ao menos uma destas
        public bool once = true;
        public string clue;                    // pista obtida
        public string effect;                  // PROVE | TELL | ACCUSE
        public Func<DialogueLine[]> lines, failLines;
        public Opt(string id, string label, Func<DialogueLine[]> lines) { this.id = id; this.label = label; this.lines = lines; }
    }

    // Prova que pode ser mostrada a alguém. validFrom = minuto a partir do qual o fato já aconteceu.
    public class Ev
    {
        public string id; public int validFrom; public bool strong;
        public Ev(string id, int validFrom, bool strong) { this.id = id; this.validFrom = validFrom; this.strong = strong; }
    }

    public static class Story
    {
        // ~1,65 u/s no tempo normal (a volta dura 150 s = 180 min de jogo -> 1,2 min/s)
        const float WalkPerMin = 1.375f;

        static Wp P(float x, float y, float until) { return new Wp(x, y, until); }
        static Wp P(float x, float y) { return new Wp(x, y, 0f); }

        // Rota em velocidade de caminhada. "until" = horário absoluto (minutos) até o qual fica parado ali.
        static Key[] Path(float t0, params Wp[] w)
        {
            var list = new List<Key>();
            float t = t0; Vector2 prev = Vector2.zero;
            for (int i = 0; i < w.Length; i++)
            {
                Vector2 p = WorldMap.Px(w[i].x, w[i].y);
                if (i > 0) t += Vector2.Distance(prev, p) / WalkPerMin;
                list.Add(new Key(t, p));
                if (w[i].until > t) { t = w[i].until; list.Add(new Key(t, p)); }
                prev = p;
            }
            return list.ToArray();
        }

        // Minutos desde 12:00 (min 131 = 14:11).
        public static readonly NpcDef[] Npcs =
        {
            new NpcDef("marta", "Dona Marta",
                Path(0, P(690,325,38), P(520,302), P(262,285,62), P(520,302), P(690,325,85), P(520,302), P(262,285,104), P(520,302), P(690,325,400))),
            new NpcDef("jorge", "Seu Jorge",
                Path(0, P(1300,262,25), P(1190,302,62), P(950,306,71), P(700,332,86), P(520,305,93), P(262,290,118), P(265,440,129), P(262,290,134), P(520,305), P(700,332), P(760,335,400))),
            new NpcDef("lucas", "Lucas",
                Path(20, P(245,215), P(258,290), P(265,440,96), P(258,290), P(245,215)),
                Path(128, P(245,215), P(262,290), P(285,548,400))),
            new NpcDef("helena", "Helena",
                Path(40, P(245,215), P(250,298), P(520,305), P(700,332), P(950,306), P(1190,302), P(1182,440,100), P(1190,302), P(950,306), P(700,332,115), P(520,305), P(250,298), P(245,215))),
            new NpcDef("rodrigo", "Rodrigo",
                // chega 12:05; arquivo até ~12:28 (pega a pasta); fica no carrinho do zelador até ~12:58 (pega as chaves); assina a saída ~13:05
                Path(5, P(245,215), P(252,290), P(100,268,28), P(262,290), P(520,305), P(700,332), P(950,306), P(1100,300,58), P(950,306), P(700,332,67), P(520,305), P(262,288), P(245,215)),
                // 14:11 volta pela porta dos fundos com a chave roubada; espreita; chega ao arquivo ~14:52
                Path(131, P(1255,215,140), P(1190,302,150), P(950,306), P(700,332,160), P(520,305), P(262,288,168), P(100,268,400))),
        };

        public static NpcDef Npc(string id)
        {
            for (int i = 0; i < Npcs.Length; i++) if (Npcs[i].id == id) return Npcs[i];
            return null;
        }

        public static bool TryPos(NpcDef d, float t, out Vector2 pos)
        {
            for (int s = 0; s < d.stints.Length; s++)
            {
                var k = d.stints[s];
                if (t < k[0].t || t > k[k.Length - 1].t) continue;
                for (int i = 0; i < k.Length - 1; i++)
                {
                    if (t <= k[i + 1].t)
                    {
                        float span = Mathf.Max(0.0001f, k[i + 1].t - k[i].t);
                        pos = Vector2.Lerp(k[i].p, k[i + 1].p, (t - k[i].t) / span);
                        return true;
                    }
                }
                pos = k[k.Length - 1].p;
                return true;
            }
            pos = Vector2.zero;
            return false;
        }

        // ------------------------------- saudação, humor e recusas -------------------------------

        public static DialogueLine[] Greeting(string id, float minute)
        {
            switch (id)
            {
                case "marta":
                    return L.Lines(
                        L.D("Dona Marta", "marta_p0", "Boa tarde, querida. O expediente começa às 12h. Silêncio na biblioteca, por favor."),
                        L.D("Dona Marta", "marta_p0", "Já que chegou: o carrinho de devolução, ali à direita, está cheio. E a estante inferior esquerda está uma bagunça."),
                        L.D("Dona Marta", "marta_p0", "Ah, e o terminal aqui do balcão está pedindo uma verificação por luzes. Veja se consegue destravar, você é boa com essas coisas."));
                case "jorge": return L.Lines(L.D("Seu Jorge", "jorge_p0", "Cuidado com o chão molhado, moça! Essa vassoura é mais velha que eu."));
                case "lucas": return L.Lines(L.D("Lucas", "lucas_p0", "Shh... Prova de química amanhã. Esse silêncio ajuda."));
                case "helena": return L.Lines(L.D("Helena", "helena_p1", "Oi. Cheguei agora há pouco. Estou pesquisando a história deste prédio."));
                default:
                    if (minute >= 100f) return L.Lines(L.D("Rodrigo", "rodrigo_p4", "Não tenho nada a dizer. Deixe-me trabalhar."));
                    return L.Lines(L.D("Rodrigo", "rodrigo_p0", "O arquivo é restrito a pessoal autorizado. Posso ajudar?"));
            }
        }

        public static DialogueLine LoopReaction(string id)
        {
            switch (id)
            {
                case "marta": return L.Me("(Palavra por palavra. A mesma entonação, a mesma pausa... Ela não faz ideia.)");
                case "jorge": return L.Me("(Até o gesto de limpar a testa é idêntico. Ele não lembra de nada.)");
                case "lucas": return L.Me("(Ele vai bocejar... agora. Bocejou. Como um relógio.)");
                case "helena": return L.Me("(O mesmo sorriso, a mesma frase. Isso é... assustador.)");
                default: return L.Me("(Ele desvia o olhar primeiro. Sempre desvia. Como se escondesse algo.)");
            }
        }

        public static string Mood(string id, int trust, int susp)
        {
            string n = Npc(id).name;
            if (susp >= 2) return n + " te observa como se você fosse uma ameaça.";
            if (susp == 1) return n + " estreita os olhos, desconfiado(a).";
            if (trust <= -1) return n + " está de cara fechada.";
            if (trust >= 2) return n + " te trata como uma amiga.";
            if (trust == 1) return n + " parece à vontade com você.";
            return n + " espera o que você vai dizer.";
        }

        public static DialogueLine[] Refusal(string id)
        {
            switch (id)
            {
                case "marta": return L.Lines(L.D("Dona Marta", "marta_p0", "Chega. Não tenho mais nada a dizer a você. Fique longe do meu balcão."));
                case "jorge": return L.Lines(L.D("Seu Jorge", "jorge_p0", "Não tenho conversa com quem me trata assim. Vá com Deus."));
                case "lucas": return L.Lines(L.D("Lucas", "lucas_p0", "Cara, me deixa em paz. Eu só quero estudar!"));
                case "helena": return L.Lines(L.D("Helena", "helena_p0", "Acho melhor você se afastar. Eu não gosto da forma como você me olha."));
                default: return L.Lines(L.D("Rodrigo", "rodrigo_p5", "Saia da minha frente."), L.N("O olhar dele está vazio. Ele não vai mais te ouvir."));
            }
        }

        // ------------------------------- falas (ordem de conversa, estilo BG3) -------------------------------

        static Dictionary<string, Opt[]> _opts;
        public static Opt[] Opts(string id)
        {
            if (_opts == null) BuildOpts();
            Opt[] r;
            return _opts.TryGetValue(id, out r) ? r : new Opt[0];
        }

        static DialogueLine Ma(string t) { return L.D("Dona Marta", "marta_p0", t); }
        static DialogueLine MaS(string t) { return L.D("Dona Marta", "marta_p1", t); }
        static DialogueLine Jo(string t) { return L.D("Seu Jorge", "jorge_p0", t); }
        static DialogueLine Lu(string t) { return L.D("Lucas", "lucas_p0", t); }
        static DialogueLine He(string t) { return L.D("Helena", "helena_p0", t); }
        static DialogueLine HeS(string t) { return L.D("Helena", "helena_p1", t); }
        static DialogueLine Ro(string t) { return L.D("Rodrigo", "rodrigo_p0", t); }
        static DialogueLine RoA(string t) { return L.D("Rodrigo", "rodrigo_p4", t); }

        static void BuildOpts()
        {
            _opts = new Dictionary<string, Opt[]>();

            // ============================================================ DONA MARTA
            // Ordem: abertura -> o dia -> quem passou -> o arquivo -> a autorização
            _opts["marta"] = new[]
            {
                new Opt("m_hello", "Bom dia, Dona Marta. Precisa de ajuda com alguma coisa?", () => L.Lines(
                    MaS("Que moça educada! Pode levar aquele carrinho de volta à estante, se quiser."),
                    L.N("Ela sorri por cima dos óculos.")))
                    { notAsked = new[] { "m_rude" }, dTrust = 1, cost = 1, maxSusp = 1 },
                new Opt("m_rude", "Pare de enrolar e me deixe ver o registro logo.", () => L.Lines(
                    Ma("Modos, moça! Aqui é uma biblioteca, não uma delegacia."),
                    L.N("Ela fecha a cara e vira o rosto.")))
                    { notAsked = new[] { "m_hello" }, minTrust = -1, dTrust = -2, cost = 1, maxSusp = 1 },
                new Opt("m_sorry", "Desculpe, Dona Marta. Eu fui grosseira.", () => L.Lines(
                    Ma("...Hmpf. Vou fingir que não ouvi. Mas continue assim, querida."),
                    L.N("O gelo na voz dela derrete um pouco.")))
                    { after = new[] { "m_rude" }, maxTrust = -1, dTrust = 2, cost = 1 },
                new Opt("m_thanks", "Dona Marta, já devolvi os livros do carrinho.", () => L.Lines(
                    Ma("Já?! Que moça trabalhadeira! Você é uma bênção, querida."),
                    L.N("Pela primeira vez no dia, ela te dá um sorriso de verdade.")))
                    { after = new[] { "@chore" }, dTrust = 2, cost = 1, maxSusp = 1 },
                new Opt("m_day", "Como tem sido o dia por aqui?", () => L.Lines(
                    Ma("Tranquilo, querida. Pouca gente. Eu fico no balcão quase o tempo todo, só saio para guardar livros na estante da esquerda."),
                    Ma("Alguém precisa vigiar este silêncio.")))
                    { afterAny = new[] { "m_hello", "m_rude" }, minTrust = 0, maxSusp = 1, clue = "S_marta" },
                new Opt("m_who", "E quem já passou por aqui hoje?", () => L.Lines(
                    Ma("Quase ninguém. Só o professor Rodrigo, que chegou e foi direto para o arquivo."),
                    Ma("Homem calado. Educado, mas calado.")))
                    { after = new[] { "m_day" }, fromMin = 20, minTrust = 0, maxSusp = 1 },
                new Opt("m_archive", "O professor costuma frequentar o arquivo?", () => L.Lines(
                    Ma("Nunca. Hoje pediu a pasta de 1989 — ninguém toca naquela pasta há dez anos."),
                    Ma("Fiquei curiosa, mas não é da minha conta."),
                    L.Me("A pasta de 1989... Por que alguém teria tanta pressa em ler algo tão velho?")))
                    { after = new[] { "m_who" }, minTrust = 1, maxSusp = 1, clue = "R1989" },
                new Opt("m_open", "A senhora deixou ele entrar no arquivo sem conferir a autorização?", () => L.Lines(
                    Ma("...Deixei. Ele disse que tinha autorização do departamento."),
                    Ma("Agora que você pergunta... nunca vi o papel. Que cabeça a minha."),
                    L.Me("Ela confiou nele sem pensar. Ninguém conferiu nada.")))
                    { after = new[] { "m_archive" }, minTrust = 2, maxSusp = 1, clue = "MARTA_ABRIU" },
                new Opt("m_warn", "Dona Marta, às 15h vai começar um incêndio no arquivo. Precisamos tirar todos daqui.", () => L.Lines(
                    Ma("Meu Deus... você está falando sério. Está bem, vou ficar atenta."),
                    Ma("Mas não posso esvaziar o prédio só com a sua palavra, querida. Preciso de algo concreto."),
                    L.Me("Ela acreditou. Pelo menos por enquanto.")))
                    { after = new[] { "m_day" }, need = new[] { "FOGO" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          Ma("Que brincadeira de péssimo gosto é essa?"),
                          L.N("Ela abre o caderninho e anota algo. Você consegue ler o próprio nome."),
                          L.Me("Eu não devia ter dito isso. Ela não vai deixar passar.")) },
                new Opt("m_loop", "Dona Marta... este dia está se repetindo. Eu já vivi isso.", () => L.Lines(
                    Ma("Palavra por palavra... eu... eu quero acreditar em você, querida."),
                    Ma("Se isso for verdade, o que a gente faz?"),
                    L.Me("Ela me escuta. Isso já é muito.")))
                    { after = new[] { "m_day" }, need = new[] { "LOOP" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          Ma("Querida... você está me assustando. Quer que eu ligue para a sua mãe?"),
                          L.N("Ela sorri, mas por baixo do sorriso a desconfiança cresce."),
                          L.Me("Ela acha que estou doente. Ou pior: que estou escondendo algo.")) },
                new Opt("m_prove", "A senhora fica no balcão quase o tempo todo. Só sai para guardar livros na estante da esquerda. E vai dizer que alguém precisa vigiar este silêncio.", () => L.Lines(
                    Ma("...Eu nunca disse isso a ninguém hoje. Como você..."),
                    L.Me("Eu já vivi este dia. Mais de uma vez."),
                    Ma("Meu Deus. Está bem. Eu te escuto.")))
                    { afterAny = new[] { "m_hello", "m_rude" }, need = new[] { "S_marta" }, notAsked = new[] { "m_day" }, minLoop = 2, effect = "PROVE", cost = 3,
                      failLines = () => L.Lines(
                          Ma("Você andou me espionando, não foi?"),
                          L.N("O olhar dela esfria. Ela cruza os braços.")) },
                new Opt("m_accuse", "ACUSAR a Dona Marta: mostrar provas de que foi ela.", null)
                    { afterAny = new[] { "m_hello", "m_rude" }, need = new[] { "FOGO" }, effect = "ACCUSE", cost = 0, once = false },
            };

            // ============================================================ SEU JORGE
            // Ordem: abertura -> o trabalho -> chaves / aflição -> carrinho
            _opts["jorge"] = new[]
            {
                new Opt("j_hello", "Posso ajudar a secar o chão, Seu Jorge?", () => L.Lines(
                    Jo("Ah, moça boa! Meu pai dizia que quem seca o chão dos outros nunca escorrega."),
                    L.N("Ele ri, e o riso o deixa dez anos mais novo.")))
                    { notAsked = new[] { "j_rude" }, dTrust = 1, cost = 1, maxSusp = 1 },
                new Opt("j_rude", "Anda logo com esse esfregão, velho.", () => L.Lines(
                    Jo("Velho?! Eu limpava esta biblioteca antes de você nascer, menina!"),
                    L.N("A mão dele aperta o cabo da vassoura até os nós dos dedos ficarem brancos.")))
                    { notAsked = new[] { "j_hello" }, minTrust = -1, dTrust = -2, cost = 1, maxSusp = 1 },
                new Opt("j_sorry", "Desculpe, Seu Jorge. Eu não devia ter falado assim.", () => L.Lines(
                    Jo("...Tá. Eu também já perdi a paciência com muito patrão. Esquece, filha."),
                    L.N("Ele volta a esfregar o chão, mas agora com menos raiva.")))
                    { after = new[] { "j_rude" }, maxTrust = -1, dTrust = 2, cost = 1 },
                new Opt("j_job", "O senhor trabalha aqui há muito tempo?", () => L.Lines(
                    Jo("Quarenta anos, filha. Já vi três diretores, duas reformas e uma goteira que nunca consertaram."),
                    Jo("Conheço cada tábua desta casa.")))
                    { afterAny = new[] { "j_hello", "j_rude" }, minTrust = 0, maxSusp = 1 },
                new Opt("j_keys", "O senhor cuida das chaves daqui?", () => L.Lines(
                    Jo("Tenho o molho inteiro no cinto, moça. A Zeladoria 02 abre a porta de serviço."),
                    Jo("É a única que abre aquela porta.")))
                    { after = new[] { "j_job" }, toMin = 60, minTrust = 0, maxSusp = 1, clue = "CHAVE02" },
                new Opt("j_lost", "O senhor parece aflito, Seu Jorge.", () => L.Lines(
                    Jo("Perdi meu molho de chaves depois do almoço. A Zeladoria 02 estava nele."),
                    Jo("Sem ela a porta de serviço fica trancada... e sou eu que vou pagar outra."),
                    L.Me("Perdeu. Ou alguém levou.")))
                    { after = new[] { "j_job" }, fromMin = 60, minTrust = 0, maxSusp = 1, clue = "S_jorge" },
                new Opt("j_cart", "O senhor viu alguém perto do seu carrinho hoje?", () => L.Lines(
                    Jo("Agora que você falou... o professor Rodrigo. Por volta das 12h50. Disse que procurava o banheiro."),
                    Jo("Banheiro? Pelo lado do carrinho? Não pensei nada na hora..."),
                    L.Me("Foi ele que levou o molho. Ou eu estou vendo coisa onde não tem?")))
                    { after = new[] { "j_lost" }, fromMin = 55, minTrust = 1, maxSusp = 1, clue = "CARRINHO" },
                new Opt("j_joke", "O senhor parece cansado desse trabalho.", () => L.Lines(
                    Jo("Ah, filha... às vezes eu penso: um dia ainda ponho fogo nisso aqui! Ha ha ha!"),
                    Jo("Brincadeira. Brincadeira..."),
                    L.N("O riso dele morre cedo. Os olhos ficam parados.")))
                    { after = new[] { "j_job" }, minTrust = 1, maxSusp = 1, clue = "JORGE_FOGO" },
                new Opt("j_warn", "Seu Jorge, às 15h vai começar um incêndio no arquivo. Fique de olho nas chaves.", () => L.Lines(
                    Jo("Incêndio?! No arquivo? ...Escuta, eu não sei por que, mas eu acredito em você, filha."),
                    Jo("Vou prestar atenção. Se eu ver qualquer fumaça, eu grito."),
                    L.Me("Ele acreditou. Isso aquece um pouco o peito.")))
                    { after = new[] { "j_job" }, need = new[] { "FOGO" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          Jo("Que conversa é essa, moça? Incêndio? Você bebeu?"),
                          L.N("Ele cruza os braços. O sorriso some completamente."),
                          L.Me("Ele vai comentar isso com a Marta. Eu sei.")) },
                new Opt("j_loop", "Seu Jorge, o tempo está voltando. Eu vivo estas três horas sem parar.", () => L.Lines(
                    Jo("Meu pai dizia que relógio de biblioteca atrasa de propósito..."),
                    Jo("Mas a sua cara não é de quem está brincando. Eu... eu te escuto."),
                    L.Me("Mais uma pessoa que acredita. Mais uma que posso perder.")))
                    { after = new[] { "j_job" }, need = new[] { "LOOP" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          Jo("Ha! Meu falecido pai também falava de coisas assim, depois da quinta cachaça..."),
                          L.N("O riso dele não chega nos olhos. Ele te olha como a uma doente."),
                          L.Me("Ele não acredita. E agora me vigia.")) },
                new Opt("j_prove", "O senhor vai perder o molho de chaves depois do almoço. A Zeladoria 02.", () => L.Lines(
                    Jo("Perder...? Eu nunca perdi as chaves em quarenta anos! Como você sabe disso?"),
                    L.Me("Porque já aconteceu. Em outro dia, que só eu lembro."),
                    Jo("...Meu Deus. Está bem. Eu te escuto, filha.")))
                    { afterAny = new[] { "j_hello", "j_rude" }, need = new[] { "S_jorge" }, toMin = 60, minLoop = 2, effect = "PROVE", cost = 3,
                      failLines = () => L.Lines(
                          Jo("Está me rogando praga, é?"),
                          L.N("Ele leva a mão ao molho de chaves, protegendo-o.")) },
                new Opt("j_accuse", "ACUSAR o Seu Jorge: mostrar provas de que foi ele.", null)
                    { afterAny = new[] { "j_hello", "j_rude" }, need = new[] { "FOGO" }, effect = "ACCUSE", cost = 0, once = false },
            };

            // ============================================================ LUCAS
            // Ordem: abertura -> estudo (manhã) | voltou do almoço -> viu algo / cheiro de cigarro
            _opts["lucas"] = new[]
            {
                new Opt("l_hello", "Boa sorte na prova! Precisa de alguma coisa?", () => L.Lines(
                    Lu("Valeu! Poucas pessoas falam comigo aqui sem mandar eu ficar quieto."),
                    L.N("Ele esboça um sorriso nervoso.")))
                    { fromMin = 20, notAsked = new[] { "l_rude" }, dTrust = 1, cost = 1, maxSusp = 1 },
                new Opt("l_rude", "Cala a boca e me escuta, moleque.", () => L.Lines(
                    Lu("Ei! Qual é o seu problema?"),
                    L.N("Ele fecha o livro com força e te encara.")))
                    { fromMin = 20, notAsked = new[] { "l_hello" }, minTrust = -1, dTrust = -2, cost = 1, maxSusp = 1 },
                new Opt("l_sorry", "Foi mal, Lucas. Eu fui uma idiota agora há pouco.", () => L.Lines(
                    Lu("Tá tranquilo. Eu também sou meio estressado."),
                    L.N("Ele relaxa os ombros.")))
                    { after = new[] { "l_rude" }, maxTrust = -1, dTrust = 2, cost = 1 },
                new Opt("l_study", "O que você está estudando?", () => L.Lines(
                    Lu("Química Orgânica. Odeio, mas o silêncio daqui ajuda."),
                    Lu("Se eu repetir de novo, meu pai me mata.")))
                    { afterAny = new[] { "l_hello", "l_rude" }, fromMin = 20, toMin = 100, minTrust = 0, maxSusp = 1 },
                new Opt("l_back", "Voltou do almoço agora?", () => L.Lines(
                    Lu("É, fui comer lá fora. Tô morto de sono."),
                    L.N("Ele boceja e esfrega os olhos.")))
                    { afterAny = new[] { "l_hello", "l_rude" }, fromMin = 128, minTrust = 0, maxSusp = 1 },
                new Opt("l_saw", "Reparou em alguma coisa quando voltou?", () => L.Lines(
                    Lu("Quando entrei, vi o Rodrigo entrando pela porta dos fundos. Por volta das 14h10."),
                    Lu("Aquela porta fica trancada! Achei estranho, mas ele tem cara de quem não gosta de perguntas."),
                    L.Me("A porta dos fundos... Alguém a abriu.")))
                    { after = new[] { "l_back" }, minTrust = 0, maxSusp = 1, clue = "S_lucas" },
                new Opt("l_smell", "Você está com cheiro de cigarro, Lucas.", () => L.Lines(
                    Lu("Shh! Foi só um... Não conta pra Dona Marta, por favor."),
                    L.N("Ele leva a mão ao bolso da jaqueta. Um isqueiro, de relance."),
                    L.Me("Um isqueiro. Interessante... ou só um garoto fugindo da bronca?")))
                    { after = new[] { "l_back" }, minTrust = 0, maxSusp = 1, dTrust = -1, clue = "LUCAS_FUMA" },
                new Opt("l_warn", "Lucas, às 15h vai começar um incêndio. Saia daqui antes disso.", () => L.Lines(
                    Lu("Incêndio?! ...Eu não sei por que, mas eu acredito em você."),
                    Lu("Tá. Às 14h50 eu estou na rua. Valeu pelo aviso."),
                    L.Me("Ele vai sair a tempo. Pelo menos um.")))
                    { afterAny = new[] { "l_hello", "l_rude" }, fromMin = 20, need = new[] { "FOGO" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          Lu("Hein? Incêndio? Você tá me zoando?"),
                          L.N("Ele olha para a porta e depois para você, mais tempo do que devia."),
                          L.Me("Agora ele acha que eu sou o perigo aqui.")) },
                new Opt("l_loop", "Lucas, o dia está se repetindo. Eu já vivi tudo isso.", () => L.Lines(
                    Lu("Cara... isso parece coisa de filme. Mas você não está rindo."),
                    Lu("Se for verdade... eu ajudo no que puder."),
                    L.Me("Ele acreditou. Um aliado.")))
                    { afterAny = new[] { "l_hello", "l_rude" }, fromMin = 20, need = new[] { "LOOP" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          Lu("Ahn... tá, cara. Foi mal, preciso estudar."),
                          L.N("Ele se afasta uns centímetros da cadeira. Está com medo de você."),
                          L.Me("Ele vai contar para alguém. Sempre contam.")) },
                new Opt("l_prove", "Você vai sair às 13h40 e voltar às 14h10. E vai ver o Rodrigo entrar pelos fundos.", () => L.Lines(
                    Lu("Eu... eu nem saí ainda. Como você sabe disso?"),
                    L.N("O sorriso de Lucas congela. O livro escorrega da mão dele."),
                    L.Me("Porque eu já vivi isso. Mais de uma vez."),
                    Lu("Isso não tem como alguém saber... Se você estiver falando a verdade, eu confirmo o que vi, pra quem precisar.")))
                    { afterAny = new[] { "l_hello", "l_rude" }, fromMin = 20, toMin = 100, need = new[] { "S_lucas" }, minLoop = 2, effect = "PROVE", clue = "TESTEMUNHA", cost = 3,
                      failLines = () => L.Lines(
                          Lu("Hã? Você tá me espionando?!"),
                          L.N("Ele recua com a cadeira.")) },
                new Opt("l_accuse", "ACUSAR o Lucas: mostrar provas de que foi ele.", null)
                    { afterAny = new[] { "l_hello", "l_rude" }, fromMin = 20, need = new[] { "FOGO" }, effect = "ACCUSE", cost = 0, once = false },
            };

            // ============================================================ HELENA
            // Ordem: abertura -> pesquisa -> segredo | quanto tempo fica
            _opts["helena"] = new[]
            {
                new Opt("h_hello", "Posso ajudar a achar algum livro?", () => L.Lines(
                    HeS("Que gentileza! Estou bem, mas obrigada. Poucos funcionários perguntam isso."),
                    L.N("Ela guarda a caneta e te olha com mais interesse.")))
                    { fromMin = 40, notAsked = new[] { "h_rude" }, dTrust = 1, cost = 1, maxSusp = 1 },
                new Opt("h_rude", "Você não devia estar xeretando por aqui.", () => L.Lines(
                    He("Perdão? Esta é uma biblioteca pública."),
                    L.N("Ela fecha o caderno de notas devagar, sem tirar os olhos de você.")))
                    { fromMin = 40, toMin = 126, notAsked = new[] { "h_hello" }, minTrust = -1, dTrust = -2, cost = 1, maxSusp = 1 },
                new Opt("h_sorry", "Desculpe, Helena. Eu fui indelicada.", () => L.Lines(
                    He("...Tudo bem. Acontece."),
                    L.N("Ela volta a anotar algo, mas o ombro relaxa.")))
                    { after = new[] { "h_rude" }, maxTrust = -1, dTrust = 2, cost = 1 },
                new Opt("h_why", "O que você pesquisa sobre este prédio?", () => L.Lines(
                    He("A história dele. Dizem que a fiação foi reprovada na vistoria de 1989..."),
                    He("E que o relatório simplesmente sumiu do arquivo. Estranho, não acha?"),
                    L.Me("Um relatório de 1989 desaparecido. E a Dona Marta falou de uma pasta de 1989...")))
                    { afterAny = new[] { "h_hello", "h_rude" }, fromMin = 40, toMin = 126, minTrust = 0, maxSusp = 1, clue = "H1989" },
                new Opt("h_secret", "Você parece saber mais sobre 1989 do que diz.", () => L.Lines(
                    He("...Sou repórter freelancer. Pedi a pasta de 1989 à Dona Marta e ela me mandou embora."),
                    He("Mas o professor Rodrigo pediu antes de mim... e ele conseguiu. Sem nenhuma pergunta."),
                    L.Me("Os dois queriam a mesma pasta. Mas por razões diferentes?")))
                    { after = new[] { "h_why" }, fromMin = 40, toMin = 126, minTrust = 1, maxSusp = 1, clue = "HELENA_PEDIU" },
                new Opt("h_leave", "Vai ficar muito tempo?", () => L.Lines(
                    He("Saio às 14h. Tenho consulta."),
                    He("Não vi nada de estranho por aqui, se é isso que você quer saber.")))
                    { afterAny = new[] { "h_hello", "h_rude" }, fromMin = 40, toMin = 126, minTrust = 0, maxSusp = 1, clue = "S_helena" },
                new Opt("h_warn", "Helena, às 15h este prédio vai pegar fogo. Vá embora cedo.", () => L.Lines(
                    He("Meu Deus... Eu confio no seu instinto. Eu saio às 14h de qualquer jeito, mas vou mais cedo."),
                    L.Me("Pelo menos ela vai estar longe.")))
                    { afterAny = new[] { "h_hello", "h_rude" }, fromMin = 40, toMin = 126, need = new[] { "FOGO" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          He("Isso é uma ameaça? Ou uma piada de muito mau gosto?"),
                          L.N("Ela anota algo no caderno. Provavelmente a sua descrição."),
                          L.Me("Uma repórter desconfiada. Péssima combinação.")) },
                new Opt("h_loop", "Helena, o tempo está voltando. Eu vivo este dia de novo e de novo.", () => L.Lines(
                    He("Isso é... fascinante. E aterrador."),
                    He("Se você sabe do relatório e do resto... então eu acredito. Conte-me o que precisar.")))
                    { afterAny = new[] { "h_hello", "h_rude" }, fromMin = 40, toMin = 126, need = new[] { "LOOP" }, effect = "TELL", cost = 3,
                      failLines = () => L.Lines(
                          He("Isso é um truque? Uma matéria? Quem te mandou?"),
                          L.N("Ela se afasta uns passos. Já está procurando uma saída."),
                          L.Me("Pior: agora ela vai observar tudo o que eu fizer.")) },
                new Opt("h_prove", "Você vai embora às 14h, por causa de uma consulta. E não viu nada de estranho.", () => L.Lines(
                    He("...Eu ainda nem disse isso a ninguém. Como..."),
                    L.Me("Eu já vivi este dia, Helena. Muitas vezes."),
                    He("Isso é impossível. Mas eu quero ouvir mais.")))
                    { afterAny = new[] { "h_hello", "h_rude" }, fromMin = 40, toMin = 100, need = new[] { "S_helena" }, notAsked = new[] { "h_leave" }, minLoop = 2, effect = "PROVE", cost = 3,
                      failLines = () => L.Lines(
                          He("Você anda me seguindo?"),
                          L.N("Ela ergue o queixo, em alerta.")) },
                new Opt("h_accuse", "ACUSAR a Helena: mostrar provas de que foi ela.", null)
                    { afterAny = new[] { "h_hello", "h_rude" }, fromMin = 40, toMin = 126, need = new[] { "FOGO" }, effect = "ACCUSE", cost = 0, once = false },
            };

            // ============================================================ RODRIGO
            // Ordem (manhã): abertura -> o que procura -> quanto tempo / a pasta.  (tarde): "o senhor por aqui?" -> "disse que não voltaria"
            _opts["rodrigo"] = new[]
            {
                new Opt("r_hello", "Boa tarde, professor. Em que posso ajudar?", () => L.Lines(
                    Ro("Silêncio e acesso ao arquivo. É tudo o que preciso."),
                    L.N("Ele nem levanta os olhos da pasta, mas o tom é um pouco menos seco.")))
                    { fromMin = 5, toMin = 82, notAsked = new[] { "r_rude" }, dTrust = 1, cost = 1, maxSusp = 1 },
                new Opt("r_rude", "O senhor não devia estar aqui. Vou chamar a Marta.", () => L.Lines(
                    RoA("Faça isso e se arrependerá."),
                    L.N("A voz dele é baixa, controlada. Muito mais perigosa que um grito.")))
                    { fromMin = 5, toMin = 82, notAsked = new[] { "r_hello" }, minTrust = -1, dTrust = -2, dSusp = 1, maxSusp = 1 },
                new Opt("r_what", "O que o senhor procura no arquivo?", () =>
                    Game.Trust("rodrigo") >= 1
                        ? L.Lines(
                            Ro("Pesquisa sobre a história do prédio. Vistorias antigas, projetos..."),
                            L.N("Ele ajeita os óculos duas vezes."))
                        : L.Lines(
                            Ro("Pesquisa acadêmica. Nada que interesse a uma assistente de biblioteca."),
                            L.N("Ele evita os seus olhos.")))
                    { after = new[] { "r_hello" }, fromMin = 5, toMin = 82, maxSusp = 1 },
                new Opt("r_leave", "O senhor vai ficar muito tempo?", () => L.Lines(
                    Ro("Saio às 13h05 e não volto mais. Tenho aula na universidade."),
                    L.Me("(Ele disse 'não volto mais' com muita convicção...)")))
                    { after = new[] { "r_what" }, fromMin = 5, toMin = 82, maxSusp = 1, clue = "S_rodrigo" },
                new Opt("r_1989", "O senhor pediu a pasta de 1989, não pediu?", () => L.Lines(
                    RoA("Quem... quem lhe disse isso?"),
                    L.N("O sorriso some. Algo muda no olhar dele — frio, calculista."),
                    L.Me("Ele percebeu que eu sei. Errei? Talvez... ele está me estudando agora.")))
                    { after = new[] { "r_what" }, fromMin = 5, toMin = 82, needAny = new[] { "R1989", "H1989" }, maxSusp = 1, dSusp = 1, dTrust = -1 },
                new Opt("r_again", "Professor? Pensei que o senhor tivesse saído.", () => L.Lines(
                    RoA("Voltei. Algum problema?"),
                    L.N("Ele não sorri. Os olhos varrem o salão antes de pousar em você.")))
                    { fromMin = 131, maxSusp = 1 },
                new Opt("r_back", "O senhor disse que não voltaria hoje.", () => L.Lines(
                    RoA("Esqueci um objeto. É crime voltar para buscar o que é meu?"),
                    L.N("Ele nem piscou ao mentir."),
                    L.Me("(Nenhum objeto. Ele não carrega nada nas mãos.)")))
                    { after = new[] { "r_again" }, fromMin = 131, need = new[] { "S_rodrigo" }, maxSusp = 1, dSusp = 1 },
                new Opt("r_warn", "Professor, às 15h vai haver um incêndio no arquivo.", () => L.Lines(
                    RoA("...Que coisa mais mórbida de se dizer."),
                    L.N("O sorriso dele some. Uma gota de suor escorre pela têmpora."),
                    L.Me("Ele ficou pálido. Eu não devia ter dito isso.")))
                    { afterAny = new[] { "r_hello", "r_again" }, need = new[] { "FOGO" }, dSusp = 2, dTrust = -1, cost = 3 },
                new Opt("r_loop", "Professor, o dia está se repetindo. Eu já vi tudo isso.", () => L.Lines(
                    RoA("Você é... estranha, moça. Muito estranha."),
                    L.N("Ele guarda a pasta devagar. Os olhos não saem de você."),
                    L.Me("Agora ele me vigia. Não... agora ele me teme.")))
                    { afterAny = new[] { "r_hello", "r_again" }, need = new[] { "LOOP" }, dSusp = 2, dTrust = -1, cost = 3 },
                new Opt("r_accuse", "ACUSAR o Rodrigo: mostrar as provas diretamente a ele.", null)
                    { afterAny = new[] { "r_hello", "r_again" }, need = new[] { "FOGO" }, effect = "ACCUSE", cost = 0, once = false },
            };
        }

        // ------------------------------- provas -------------------------------

        public static readonly Ev[] Evidences =
        {
            new Ev("R1989", 10, true),
            new Ev("DOC", 28, true),
            new Ev("CARRINHO", 58, true),
            new Ev("REG", 66, true),
            new Ev("ACESSO", 131, true),
            new Ev("S_lucas", 131, true),
            new Ev("PASTA_ANTES", 0, false),
            new Ev("H1989", 0, false),
            new Ev("HELENA_PEDIU", 0, false),
            new Ev("MARTA_ABRIU", 0, false),
            new Ev("S_jorge", 60, false),
            new Ev("CHAVE02", 0, false),
            new Ev("JORGE_FOGO", 0, false),
            new Ev("LUCAS_FUMA", 128, false),
        };

        public static DialogueLine[] EvidRodrigo(string id)
        {
            switch (id)
            {
                case "R1989":
                    return L.Lines(L.Me("A Dona Marta me disse que o senhor pediu a pasta de 1989 assim que chegou."),
                                   RoA("E daí? Sou professor. Pesquiso."));
                case "DOC":
                    return L.Lines(L.Me("A pasta de 1989 está vazia na gaveta. Na ficha de retirada: R. Alves."),
                                   RoA("Existem mil Alves nesta cidade. Isso não prova nada."),
                                   L.Me("O registro do balcão tem um Rodrigo Alves. Só um."));
                case "CARRINHO":
                    return L.Lines(L.Me("O Seu Jorge o viu perto do carrinho às 12h50. Pouco antes de perder o molho de chaves."),
                                   RoA("Aquele velho não sabe o que viu."));
                case "REG":
                    return L.Lines(L.Me("O registro diz que o senhor assinou a saída às 13h05. Nenhuma nova entrada."),
                                   RoA("Então o registro prova que eu saí. Ótimo."),
                                   L.Me("E mesmo assim, aqui está o senhor."));
                case "ACESSO":
                    return L.Lines(L.Me("A porta de serviço foi aberta às 14h11 com a chave Zeladoria 02. A que o Seu Jorge perdeu."),
                                   RoA("Eu... eu não sei nada sobre chaves."));
                default:
                    return L.Lines(L.Me("O Lucas viu o senhor entrar pelos fundos. Ele vai confirmar."),
                                   RoA("Aquele garoto... ele não pode..."));
            }
        }

        // Reação de quem NÃO é o culpado ao ser confrontado com uma prova.
        public static DialogueLine[] EvidOther(string npc, string ev, out int dTrust, out int dSusp, out string gives)
        {
            gives = null; dTrust = -2; dSusp = 1;
            switch (npc)
            {
                case "marta":
                    dTrust = -3; dSusp = 2;
                    return L.Lines(Ma("Está me acusando? Na MINHA biblioteca?!"),
                                   Ma("Eu trabalho aqui há vinte anos! Vá embora antes que eu perca a paciência."));
                case "jorge":
                    if (ev == "S_jorge" || ev == "CHAVE02") { dTrust = 0; dSusp = 0; return L.Lines(Jo("Sim, sim, as chaves... é um vexame. Mas eu não fiz nada de errado, filha.")); }
                    return L.Lines(Jo("Eu?! Eu perdi aquelas chaves, moça! Acha que eu queimaria o meu próprio emprego?!"),
                                   L.N("Ele aperta o cabo da vassoura com os dois punhos."));
                case "lucas":
                    if (ev == "LUCAS_FUMA") { dTrust = -1; dSusp = 0; return L.Lines(Lu("...Você vai contar pra Marta? Foi só um cigarro, cara!")); }
                    return L.Lines(Lu("Eu só quero estudar! Por que você está me acusando?!"),
                                   L.N("A voz dele treme de medo e de raiva."));
                default:
                    if (ev == "HELENA_PEDIU") { dTrust = -1; dSusp = 0; gives = "R1989"; return L.Lines(He("Eu quis a pasta para PUBLICAR, não para queimar! Quem a pegou foi outro: o professor Rodrigo.")); }
                    return L.Lines(He("Está insinuando que eu...? Eu sou jornalista, não incendiária!"),
                                   L.N("Ela fecha o caderno de notas e te encara."));
            }
        }

        // ------------------------------- registro do balcão -------------------------------

        public static readonly string[] RegLines =
        {
            "12:05  Rodrigo Alves — entrada — consulta ao arquivo",
            "12:20  Lucas Prado — entrada",
            "12:40  Helena Duarte — entrada — visitante",
            "13:05  Rodrigo Alves — saída",
            "13:40  Lucas Prado — saída",
            "13:53  Helena Duarte — saída",
            "14:08  Lucas Prado — entrada",
        };
        public static readonly int[] RegMinutes = { 5, 20, 40, 66, 100, 113, 128 };

        // ------------------------------- pistas -------------------------------

        public static readonly string[] ClueOrder =
        {
            "LOOP", "FOGO",
            "S_marta", "R1989", "MARTA_ABRIU",
            "CHAVE02", "S_jorge", "CARRINHO", "JORGE_FOGO",
            "S_lucas", "LUCAS_FUMA", "TESTEMUNHA",
            "S_helena", "H1989", "HELENA_PEDIU",
            "S_rodrigo", "REG",
            "CODE", "PASTA_ANTES", "DOC", "ACESSO",
            "SUSP_marta", "SUSP_jorge", "SUSP_lucas", "SUSP_helena", "SUSP_rodrigo",
        };

        public static readonly Dictionary<string, string> ClueText = new Dictionary<string, string>
        {
            { "LOOP",       "O dia 13/10/1999 se repete. Só eu me lembro." },
            { "FOGO",       "Às 15h o alarme toca. Há fumaça saindo do arquivo (parede esquerda) e um vulto de casaco." },
            { "S_marta",    "Marta fica no balcão; só sai para guardar livros." },
            { "R1989",      "Marta: Rodrigo pediu a pasta de 1989 assim que chegou." },
            { "MARTA_ABRIU","Marta deixou Rodrigo entrar no arquivo sem conferir a autorização." },
            { "CHAVE02",    "Jorge: a chave Zeladoria 02 (no molho do cinto) abre a porta de serviço." },
            { "S_jorge",    "Jorge perdeu o molho de chaves depois do almoço." },
            { "CARRINHO",   "Jorge viu Rodrigo perto do carrinho às 12h50, antes de perder o molho." },
            { "JORGE_FOGO", "Jorge brincou que 'um dia ainda põe fogo nisso aqui'." },
            { "S_lucas",    "Lucas viu Rodrigo entrar pela porta dos fundos por volta das 14h10." },
            { "LUCAS_FUMA", "Lucas fuma escondido e carrega um isqueiro." },
            { "TESTEMUNHA", "Lucas acreditou em mim e confirmará o que viu." },
            { "S_helena",   "Helena disse que sairia às 14h." },
            { "H1989",      "Helena: dizem que o relatório da vistoria de 1989 sumiu do arquivo." },
            { "HELENA_PEDIU","Helena é repórter e também pediu a pasta de 1989; Marta recusou." },
            { "S_rodrigo",  "Rodrigo disse que saía às 13h05 e não voltaria." },
            { "REG",        "Registro do balcão: Rodrigo Alves assinou a saída às 13h05; não há nova entrada dele." },
            { "CODE",       "Bilhete atrás do livro de Literatura: 4-7-2." },
            { "PASTA_ANTES","Às 12h a pasta do relatório de 1989 ainda estava na gaveta do arquivo." },
            { "DOC",        "A pasta de 1989 está vazia; ficha de retirada: R. Alves." },
            { "ACESSO",     "Acessos: porta de serviço aberta às 14h11 com a chave Zeladoria 02." },
            { "SUSP_marta", "Marta desconfiou de mim quando falei de coisas que eu não deveria saber." },
            { "SUSP_jorge", "Jorge desconfiou de mim quando falei de coisas que eu não deveria saber." },
            { "SUSP_lucas", "Lucas desconfiou de mim quando falei de coisas que eu não deveria saber." },
            { "SUSP_helena","Helena desconfiou de mim quando falei de coisas que eu não deveria saber." },
            { "SUSP_rodrigo","Rodrigo desconfiou de mim. Ele parece perigoso quando se sente observado." },
        };
    }
}
