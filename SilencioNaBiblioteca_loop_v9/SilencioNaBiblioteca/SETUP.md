# Silêncio na Biblioteca — loop temporal top-down (Unity 6000.6.6f1)

## Como rodar
1. Unity Hub → Add project from disk → pasta `SilencioNaBiblioteca/` → Editor **6000.6.6f1**.
2. Abra `Assets/Scenes/Main.unity` e aperte **Play** (a 1ª abertura importa os sprites de `Resources/Art/Chars`).

## Controles
WASD/setas mover · E/Espaço interagir e avançar falas (ou clique) · 1-9 escolhem a fala · Tab/I caderno · F alterna a velocidade do tempo (1x → 3x → 6x → 1x) · Esc voltar/desistir.
Brilho dourado = tarefa de assistente. Brilho azul (embaixo da pessoa/objeto) = há algo novo a descobrir ali; no Rodrigo, quer dizer que você já tem 3 provas para acusá-lo.

## Como funciona
- Protagonista: assistente da biblioteca (feminina). Um único loop: 13/10/1999, 12:00 → 15:00 em **2min30 reais**.
  Conversar custa tempo. Ao fim (alarme) você acorda às 12:00; só você lembra e o caderno guarda o que descobriu.
- **Tarefas de assistente (minigames):** (1) carrinho de devolução = bullet hell de livros; (2) estante de empréstimos =
  ordenar 8 livros por seção e ordem alfabética (sem pistas nos títulos); (3) terminal do balcão = Genius (Simon) com sons.
  Devolver os livros do carrinho faz a Dona Marta confiar mais em você.
- **Ordem de conversa:** uma fala constrói a próxima (abertura → assunto → detalhe). Não dá para ir direto à resposta.
  O tom da sua fala (gentil/grosseira) mexe na confiança sem mostrar etiqueta; as pistas não são marcadas.
- **Na volta 1 você não sabe nada do incêndio.** Falas sobre incêndio/loop só aparecem depois que você os viveu.
- **Confiança e desconfiança** (por pessoa, só dentro da volta): ser grosseira fecha informações (peça desculpas para recuperar).
  Dizer o que não devia saber gera suspeita; use "provar" (diga algo que a pessoa ainda não te contou). Quem desconfia espalha
  para a Marta; Marta desconfiada tranca o arquivo; no limite a segurança te escolta para fora. Rodrigo desconfiado acende o fogo antes.
- **Provas só valem depois que aconteceram**; mostrar o futuro levanta suspeitas. Sem álibi pronto nem menu de dedução:
  o caderno só guarda fatos soltos (com pistas falsas). Confronte o culpado com 3 provas válidas (ou 2 + blefe).

## Onde mexer
| O quê | Arquivo |
|---|---|
| Falas, ordem das conversas, rotinas, textos das pistas | `Assets/Scripts/Story.cs` |
| Loop, desconfiança, tarefas, confronto, finais | `Assets/Scripts/Game.cs` |
| Interface (falas, escolhas, caderno) — estilo madeira/âmbar | `Assets/Scripts/Hud.cs` |
| Minigames (estante, Genius, bullet hell) | `Assets/Scripts/MiniGames.cs` |
| Duração da volta | `Game.cs` → `MinPerSec` (1.2 = 2min30) |

## Áudio (Assets/Resources/Audio)
| Arquivo | Quando toca |
|---|---|
| `blues.ogg` | título, abertura e despertar de cada volta |
| `jazz.ogg` | durante o jogo (em loop) |
| `alarme.ogg` | no alarme das 15h (em loop até a volta reiniciar) |
| `vitoria.ogg` | tela final, quando você impede o incêndio |
Volumes e trocas: `Game.cs` (chamadas `_audio.*`) e `AudioManager.cs`.
O Genius fica no **terminal do balcão** (brilho dourado até você destravar).

## Como acusar
Fale com a PRÓPRIA pessoa que você acusa e escolha ACUSAR. Mostre provas que já aconteceram; com 3 provas certas o culpado confessa.

## Menus
- **Menu principal:** JOGAR · OPÇÕES · COMO JOGAR · SAIR. **Esc** durante o jogo abre a pausa (Continuar, Opções, Voltar ao título).
- **Opções** (salvas automaticamente): volume geral, música, efeitos/alarme, velocidade do texto, tremor de tela, luzes de ajuda, tela cheia, restaurar padrão.
- Para acusar: cumprimente a pessoa primeiro (ser educada) e só depois escolha ACUSAR.

Todos os áudios foram normalizados para ~-16 LUFS (mesmo volume percebido); ajuste fino nas chamadas `_audio.*` do `Game.cs` e nas Opções.
