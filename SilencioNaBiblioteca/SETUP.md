# 🎮 Silêncio na Biblioteca — Demo Jogável (Unity)

Guia passo a passo para rodar a demo do zero. **Tempo total: ~10 minutos.**

## ✅ Pré-requisitos

- **Unity 6000.6.6f1** (Unity 6.6) — versão instalada no PC do grupo. Versões 6000.6.x anteriores/também devem abrir normalmente (o Unity avisa e atualiza o projeto se o patch for diferente).
- Módulo **Windows Build Support** instalado no Editor (Mac/Linux também funcionam).
- Este repositório já contém tudo: `SilencioNaBiblioteca/` é o projeto Unity pronto.

## 🚀 Passo a passo

1. **Abra o Unity Hub → Add → Add project from disk** e selecione a pasta
   `SilencioNaBiblioteca/` deste repositório.

2. **Abra o projeto** com o Editor **6000.6.6f1**.
   > ✅ **A cena já vem pronta no repositório** (`Assets/Scenes/Main.unity`, gerada e validada em batch). Basta abrir e apertar Play.

3. **Pressione Play (▶)** no editor. A demo começa na tela de título.

4. Para gerar um build: `File → Build Settings → Build`. A cena `Main` já está listada.

> 🛠️ O menu **`Silêncio na Biblioteca → 1. Preparar projeto (artes + cena)`** ainda existe — use apenas se quiser **recriar** a cena do zero (ex.: depois de mudar os scripts de Editor). No dia a dia, não é necessário.

## 🕹️ Controles

| Ação | Controle |
|---|---|
| Interagir / avançar diálogo | Clique do mouse nos objetos que acendem |
| Adiantar o tempo (x8) | `F` |
| Caderno de anotações (pistas) | `J` (ou botão 📓 canto superior direito) |
| Pular para o próximo evento agendado | `F1` |
| Fechar o caderno | `Esc` |

> Dica para testar: `F1` pula direto para o próximo acontecimento agendado — útil para verificar os 3 ciclos rápido.

## 🧩 Estrutura da demo

- **Ciclo 1** — tutoria: rotina, clientes com falas repetíveis, 1º puzzle de catalogação. Termina às 18h com o desmaio.
- **Ciclo 2** — os mesmos clientes dizem as mesmas falas; o **visitante de casaco cinza** aparece, a luz pisca e o computador revela as vistorias de incêndio.
- **Ciclo final** — você precisa das **3 pistas** para abrir a Porta dos Fundos e desligar o disjuntor antes das 18h.
  - Acerto → **18:01** (final de sobrevivência).
  - Erro/falta de pista → final trágico com dica do que faltou.

### Regras dos puzzles (uma por ciclo)

1. `código = (páginas ÷ 10) + (dois últimos dígitos do ano)`
2. `código = (soma dos dígitos do ano) + (páginas ÷ 10)`
3. `código = (páginas ÷ 10) × (último dígito do ano)`

## 📁 Onde mexer (ajustes rápidos)

| O quê | Arquivo |
|---|---|
| Diálogos de clientes e textos dos objetos | `Assets/Scripts/StoryData.cs` |
| Livros e regra de cada puzzle | `Assets/Scripts/PuzzleData.cs` |
| Agenda dos eventos de cada ciclo (minuto → ação) | `Assets/Scripts/Game.cs` → `BuildEvents()` |
| Posições das zonas clicáveis (frações da tela) | `Assets/Scripts/HudUI.cs` → `Hotspots[]` |
| Velocidade do relógio (segundos reais por minuto do jogo) | `Assets/Scripts/Game.cs` → `SecondsPerGameMinute` |
| Visual da interface (cores/tamanhos) | `Assets/Scripts/HudUI.cs` e `UiKit.cs` |

## 🎨 Artes usadas

- `Assets/Resources/Art/BgBiblioteca.png` — fundo fixo da sala (câmera estática, point-and-click).
- `Assets/Resources/Art/MoveisBiblioteca.png`, `SpritesRPG.png`, `SalaVazia.png` — reservadas para pós-demo (recorte de NPCs reais etc.).

## 🐞 Limitações conhecidas da demo

- NPCs são silhuetas geradas por código (marcador simples), conforme decidido para a entrega.
- O fundo estica levemente em janelas com proporção diferente de 16:9 — para apresentar, use a Game view em proporção 16:9 ou fullscreen em monitor 1080p.
- Não há áudio ainda (a ser feito no refinamento).

## 🛠️ Problemas conhecidos e soluções

| Sintoma | Solução |
|---|---|
| Diálogo: **cliques não avançam** / erro no Console mencionando `StandaloneInputModule` / `Input System` | `Edit → Project Settings → Player → Other Settings → Active Input Handling` → mude para **Input Manager (Old)** ou **Both** (a demo usa o Input clássico) |
| Fundo branco no lugar da biblioteca | Rode o menu **Silêncio na Biblioteca → 1. Preparar projeto** de novo (garante que os PNGs viram Sprite) |
| Erro de pacotes ao abrir (`cannot be found: com.unity.modules...`) | Não deve mais ocorrer (manifest já corrigido para o Unity 6.6). Se aparecer, é porque o projeto foi aberto a partir de uma versão antiga do repositório — atualize (`git pull`) e clique em **Retry** |
| Hotspot "deslocado" em relação ao objeto da arte | Ajuste as frações em `HudUI.cs → Hotspots[]` (cx/cy = centro, w/h = tamanho, tudo em fração da tela) |
| Projeto não aparece no Unity Hub | Confirme que a pasta selecionada contém `ProjectSettings/ProjectVersion.txt` |

## 👥 Grupo

Arthur Carvalho Rotkis · Cauê Iwamoto Meira · Hanna de Oliveira Melo · Sérgio de Siqueira Santos — Jogos Digitais, FEI (7º ciclo).
