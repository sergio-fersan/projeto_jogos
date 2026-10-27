namespace SilencioBiblioteca
{
    public static class StoryData
    {
        delegate DialogueLine[] Dlg(int cycle);

        // ------------------- Clientes (se repetem em todos os ciclos) --------------------

        public static DialogueLine[] Almeida(int cycle)
        {
            var arr = new DialogueLine[5];
            arr[0] = L.N("Uma senhora de vestido rosa se aproxima do balcão com dois livros em mãos.");
            arr[1] = L.D("Dona Almeida", "Boa tarde, querido. As devoluções do mês, como sempre.");
            arr[2] = L.D("Você", "Os mesmos dois romances, dona Almeida.");
            arr[3] = L.D("Dona Almeida", "Gosto do que é seguro, filho. Até já!");
            if (cycle == 1)
            {
                arr[4] = L.N("(Ela sai sorrindo, como se soubesse de alguma coisa. Uma senhora simpática. Sempre.)");
            }
            else if (cycle == 2)
            {
                arr[4] = L.N("(As mesmas palavras. No mesmo minuto exato.)");
            }
            else
            {
                arr[4] = L.N("(Pela terceira vez, as mesmas palavras. Só quem aqui envelhece no ciclo é você.)");
            }
            return arr;
        }

        public static DialogueLine[] Abreu(int cycle)
        {
            var arr = new DialogueLine[5];
            arr[0] = L.N("Um senhor de suéter verde deixa umas pilhas de revistas no balcão.");
            arr[1] = L.D("Sr. Abreu", "Trouxe as revistas do meu neto. E uma coisinha: esse aquecedor do canto está zunindo desde ontem, sente?");
            arr[2] = L.D("Você", "Vou verificar, Sr. Abreu.");
            arr[3] = L.D("Sr. Abreu", "Verifique com calma. Um dia desses esse zumbido não vai ser só zumbido...");
            arr[4] = cycle == 1
                ? L.N("(Ele sai balançando a cabeça. O zumbido do canto, de longe, existe mesmo.)")
                : L.N("(As mesmas palavras. O mesmo medo no fim da frase.)");
            return arr;
        }

        public static DialogueLine[] Visitante(int cycle)
        {
            var arr = new DialogueLine[5];
            arr[0] = L.N("Um homem de casaco cinza e chapéu entra sem fazer barulho. Você nunca o viu antes.");
            arr[1] = L.D("Visitante", "Boa tarde. Não atrapalho: só queria a estante de História Militar.");
            arr[2] = L.D("Você", "É a do fundo, junto da escada de mão.");
            arr[3] = L.D("Visitante", "Procurava o Manual de Artilharia de Campanha, de 1943. Coisas de... pesquisa.");
            arr[4] = cycle == 2
                ? L.N("(Ele não pede empréstimo. Não registra nada. Só olha o relógio e espera as 18h.)")
                : L.N("(O mesmo pedido. O mesmo relógio. Mas agora você já sabe o que ele espera: as 18h.)");
            return arr;
        }

        // ------------------- Registros do computador --------------------

        public static DialogueLine[] Registros(int cycle, bool fireClue)
        {
            if (cycle == 1)
            {
                return L.Lines(
                    L.N("Sistema da Biblioteca Municipal — terminal do balcão."),
                    L.D("Registro", "Empréstimos hoje: 23. Multas pendentes: 2. Agenda de amanhã: manutenção predial."),
                    L.D("Você", "Nada fora do comum. Como sempre.")
                );
            }
            // ciclos 2 e 3
            if (!fireClue)
            {
                return L.Lines(
                    L.N("Sistema da Biblioteca Municipal — terminal do balcão."),
                    L.D("Vistoria 14/03", "Fiação do corredor dos fundos classificada como \"antiga, risco leve\"."),
                    L.D("Vistoria 28/05", "Extintor do corredor dos fundos: prazo vencido. Reposição pendente."),
                    L.N("(Dois relatórios apontando para o mesmo corredor. A biblioteca pode pegar fogo às 18h? A possibilidade agora existe...)")
                );
            }
            return L.Lines(
                L.N("Você lê os relatórios de vistoria outra vez. Fiação antiga. Extintor vencido. Corredor dos fundos."),
                L.D("Você", "Os registros confirmam o que o diário já contava.")
            );
        }

        // ------------------- Flavors de hotspots --------------------

        public static DialogueLine[] Flavor(string id, int cycle, string extraState)
        {
            switch (id)
            {
                case "entrada":
                    if (cycle == 1)
                        return L.Lines(L.N("Você olha a rua pela porta dupla. Um dia de sol, o tapete vermelho na entrada."), L.D("Você", "Sexta-feira normal. Nada a prever."));
                    if (cycle == 2)
                        return L.Lines(L.N("Você espia a entrada: o tapete está na mesma posição, torto do mesmo jeito."), L.D("Você", "Nem o vento muda neste dia."));
                    return L.Lines(L.N("A rua lá fora está exatamente como nas outras vezes."), L.D("Você", "Ninguém entra pela porta se você não deixa."));
                case "relogio":
                {
                    string hora = extraState;
                    return L.Lines(L.N("O relógio da parede marca " + hora + "."), L.D("Você", cycle == 1 ? "Falta ainda um bocado para as 18h." : "Daqui a pouco ele vai chegar nas 18h. Sempre chega."));
                }
                case "balcao":
                    if (cycle == 1)
                        return L.Lines(L.N("Carimbo, ficha, prateleira. O expediente inteiro neste balcão."), L.D("Você", "Trabalhar é bom para a cabeça. Normalmente."));
                    if (cycle == 2)
                        return L.Lines(L.N("Carimbo, ficha, prateleira. Você já fez isso há uma hora." ), L.D("Você", "A pilha está na mesma ordem de ontem. Livro por livro."));
                    return L.Lines(L.N("Você quase conhece de cor toda a devolução do dia."), L.D("Você", "Rotina não é mais segurança: agora é mapa de hora."));
                case "estante_esq":
                    if (cycle == 1)
                        return L.Lines(L.N("Estante geral: edição, edição e... uma edição em ordem perfeita."), L.D("Você", "Catalogou um desses hoje, acho."));
                    if (cycle == 2)
                        return L.Lines(L.N("Um livro está meio dedo fora do lugar, exatamente como estava antes da 17h."), L.D("Você", "Você arrumou. Ele voltou. Sozinho."));
                    return L.Lines(L.N("O livro meio para fora do lugar continua lá, esperando você."));
                case "estante_hist":
                    if (cycle == 1)
                        return L.Lines(L.N("Estante de História Militar: diários de guerra, manuals técnicos, velhas fotografias." ), L.D("Você", "Seção pouco frequentada. Quase nunca ninguém vem aqui."));
                    if (cycle == 2)
                        return L.Lines(L.N("A estante de História Militar. No lugar do Manual de Artilharia de 1943, um espaço vazio."), L.D("Você", "Alguém já mexeu neste livro hoje. Antes de ele chegar."));
                    return L.Lines(L.N("A estante de História Militar continua no lugar dele — e, com ela, o homem de chapéu.") );
                case "mesa":
                    if (cycle == 1)
                        return L.Lines(L.N("Mesas de leitura com jornais do dia e um quebra-cabeça de 500 peças abandonado."), L.D("Você", "Se calmaria existia num objeto, seria este."));
                    return L.Lines(L.N("Os mesmos jornais, páginas nas mesmas aberturas."), L.D("Você", "Ninguém mais vem ler no fim do expediente."));
                case "aquecedor":
                    if (cycle == 2)
                        return L.Lines(L.N("O aquecedor elétrico do canto. O zumbido está mais alto hoje."), L.N("A fiação atrás dele está quente ao toque — quente demais para um aquecedor de bancada."));
                    return L.Lines(L.N("O aquecedor do canto. Na tomada, marcas de chamado: um ponto derretido, um cheiro leve de fumaça."), L.D("Você", "É isto. É aqui que começa."));
                default:
                    return L.Lines(L.N("Nada além do silêncio de sempre."));
            }
        }
    }
}
