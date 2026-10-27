# 🎮 Silêncio na Biblioteca — Demo Jogável (Unity)

Guia passo a passo para rodar a demo do zero. **Tempo total: ~10 minutos.**

## ✅ Pré-requisitos

- **Unity 2022.3 LTS** (qualquer patch 2022.3.x) com o módulo **Windows Build Support** (Mac/Linux também funciona).
- Este repositório já contém tudo: `SilencioNaBiblioteca/` é o projeto Unity pronto.

## 🚀 Passo a passo

1. **Abra o Unity Hub → Add → Add project from disk** e selecione a pasta
   `SilencioNaBiblioteca/` deste repositório.

2. **Abra o projeto** com Unity 2022.3 LTS. A primeira importação demora alguns minutos (artes grandes).

3. No menu do Editor, clique em:
   **`Silêncio na Biblioteca → 1. Preparar projeto (artes + cena)`**

   Esse comando:
   - ajusta os PNGs de `Assets/Resources/Art/` como *Sprites*;
   - cria e salva a cena `Assets/Scenes/Main.unity`;
   - coloca a cena no Build Settings.

4. **Pressione Play (▶)** no editor. Pronto — a demo começa na tela de título.

5. Para gerar um build: `File → Build Settings → Build`. A cena `Main` já está listada.

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

## 👥 Grupo

Arthur Carvalho Rotkis · Cauê Iwamoto Meira · Hanna de Oliveira Melo · Sérgio de Siqueira Santos — Jogos Digitais, FEI (7º ciclo).
