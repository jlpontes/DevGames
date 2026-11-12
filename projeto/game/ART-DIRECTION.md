# Direção de arte — "Relicário de Bronze e Cristal"

Guia curto para quem mexe em telas, animação e efeitos. A identidade nasce do que o jogo já é: um logo de
ferro e bronze forjado, arenas pintadas com círculos de runas e uma história sobre um Cristal Elemental partido
em fragmentos. A interface é o **relicário** que guarda esses fragmentos: metal escuro e envelhecido segurando
luz elemental. Nada de vidro fosco, neon ou cartões planos de aplicativo.

Tudo isto está em código: cores e tempos em `UI/Theme.cs`, formas em `UI/Art.cs` (sprites 9-slice gerados),
componentes em `UI/Ui.cs`, movimento em `UI/Motion.cs`.

## 1. Paleta

| Papel | Token | Uso |
|---|---|---|
| Obsidiana | `Theme.Obsidian` | fundo de tela, letterbox, véus de transição |
| Pedra | `Theme.Panel`, `Theme.PanelLight` | preenchimento de painéis (sempre com leve transparência para a arena respirar) |
| Bronze | `Theme.BorderGold`, `Theme.BorderSubtle` | molduras; o bronze é o "metal" de toda a UI |
| Ouro | `Theme.Gold` | **só** para foco: ação principal, seleção, especial pronto, títulos. Se tudo é dourado, nada é |
| Pergaminho | `Theme.Text`, `Theme.Muted` | texto principal e secundário |
| Elementos | `Theme.ElementColor` | a cor de um elemento é **luz**: brilhos, auras, bordas de quem é daquele elemento. Nunca fundo chapado |
| Estado | `Theme.Good`, `Theme.Bad` | cura/vantagem e dano/desvantagem |

Regra de contraste: texto sobre pedra usa pergaminho; texto sobre bronze/ouro usa obsidiana ou contorno escuro.

## 2. Formas e motivos

- **Moldura chanfrada de bronze** (`Art.FrameSprite`): aresta escura, bisel iluminado por cima, sulco interno.
  Toda caixa importante usa `Ui.Frame`; ninguém desenha retângulo com borda à mão.
- **Losango (fragmento do cristal)** (`Art.Diamond`): é o marcador universal — rebites dos cantos dos painéis,
  pips de efeito, contagem do sorteio de arena, segmentos da stamina, emblema de elemento.
- **Anel de runas** (`Art.VfxRuneCircle`): o círculo das arenas. Aparece sob os pés de quem é favorecido pela
  arena, em invocações e trocas, e no medalhão da rodada.
- Cantos levemente arredondados (raio ~4 px). Sem cantos vivos, sem pílulas.

## 3. Tipografia

Uma só fonte (a embutida do Unity, para funcionar no WebGL). A hierarquia vem de tamanho, peso, caixa e cor:

| Nível | Tamanho | Estilo |
|---|---|---|
| Display | 34 | MAIÚSCULAS, negrito, contorno — nomes de golpe especial, anúncios |
| Título | 24–26 | negrito, ouro — título de tela |
| Cabeçalho | 17–19 | negrito — nome de lutador, cabeçalho de painel |
| Corpo | 14–15 | normal — descrições, falas |
| Legenda | 11–12 | negrito, cor suave — rótulos, valores pequenos |

Símbolos: a fonte embutida não tem emoji nem vários símbolos (★ ✦ ⚔ ✓ ▸). `Ui.Label` troca-os sozinho por
equivalentes que existem (♦ ► ◄ •). Escreva texto limpo; não conte com ícones de fonte.

## 4. Movimento

Tempos em `Theme` (multiplicados pela velocidade 1×/2× na batalha):

- **Interface:** 0,12–0,20 s, sempre com desaceleração (ease-out). Hover levanta o botão 3 %, clique afunda 3 %.
- **Golpe:** antecipação curta (0,10–0,18 s) → avanço rápido → **impacto** → recuperação (0,2 s). O impacto é
  onde tudo acontece junto: flash branco no alvo, número de dano, recuo, partículas.
- **Hit-stop:** 45 ms em golpes normais, até 90 ms em golpes pesados, especiais e contra-ataques. Nunca mais que isso.
- **Tremor de câmera:** proporcional à fração do HP tirada; teto baixo. Golpe fraco quase não treme.
- **Idle por classe:** Guerreiro em base de luta (balanço curto), Mago flutuando, Tanque respirando pesado e lento.
- **Queda:** flash, joelhos cedem, o corpo perde cor e se desfaz em partículas da cor do elemento.

Nada pisca em loop rápido, nada fica tremendo parado. Movimento contínuo é lento e pequeno.

## 5. HUD de batalha

- Topo: um painel por equipe (retrato, HP, efeitos) e, no centro, o **medalhão da arena** com rodada e a
  contagem do sorteio em losangos.
- Sob cada lutador: placa com nome, elemento/classe, HP com "rastro" de dano, efeitos em pips e o selo da arena.
- Base: faixa de narração e as **cartas de ação** (golpe, dano estimado, efeito, atalho de teclado) com dica
  ao passar o mouse. O especial é uma carta de bronze que enche com a stamina e acende quando está pronto.
- Estados visuais em todo botão: normal, hover, pressionado, desabilitado e selecionado.
