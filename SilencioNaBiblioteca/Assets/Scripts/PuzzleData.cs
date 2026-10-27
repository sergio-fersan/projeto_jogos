namespace SilencioBiblioteca
{
    public class PuzzleDef
    {
        public string title;    // título da tarefa
        public string book;     // nome do livro / documento
        public string data;     // dados mostrados ao jogador
        public string rule;     // regra matemática (dada pelo GDD: a regra muda por ciclo)
        public int answer;      // código correto
        public string success;  // texto ao acertar

        public PuzzleDef(string title, string book, string data, string rule, int answer, string success)
        {
            this.title = title;
            this.book = book;
            this.data = data;
            this.rule = rule;
            this.answer = answer;
            this.success = success;
        }
    }

    public static class PuzzleData
    {
        public static PuzzleDef Get(int cycle)
        {
            switch (cycle)
            {
                case 1:
                    return new PuzzleDef(
                        "Catalogação do dia",
                        "\"Tratado de Jardins de Inverno\" — 240 páginas — ano 1998",
                        "Nº de páginas: 240\nAno: 1998",
                        "REGRA: código = (páginas ÷ 10) + (dois últimos dígitos do ano)\nEx.: 240÷10 = 24 ; 1998 → 98 ; 24+98 = 122",
                        122,
                        "\"Tratado de Jardins de Inverno\" catalogado na estante geral."
                    );
                case 2:
                    return new PuzzleDef(
                        "Catalogação prioritária",
                        "\"Manual de Artilharia de Campanha (1943)\" — 320 páginas",
                        "Nº de páginas: 320\nAno: 1943",
                        "REGRA: código = (soma dos dígitos do ano) + (páginas ÷ 10)\nEx.: 1+9+4+3 = 17 ; 320÷10 = 32 ; 17+32 = 49",
                        49,
                        "O manual vai para a estante de História Militar. Na folha de registro há uma anotação de caneta: \"guardado pelo mesmo cliente, toda sexta\"."
                    );
                default:
                    return new PuzzleDef(
                        "Catalogação do setor técnico",
                        "\"Relatório de Manutenção Predial — 2015\" — 156 páginas",
                        "Nº de páginas: 156\nAno: 2015",
                        "REGRA: código = (páginas ÷ 10) × (último dígito do ano)\nEx.: 156÷10 = 15 ; 2015 → dígito 5 ; 15×5 = 75",
                        75,
                        "No verso do relatório: \"aquecedor do canto — 3ª manutenção este mês — tomada viciada\". É dito três vezes o mesmo problema."
                    );
            }
        }
    }
}
