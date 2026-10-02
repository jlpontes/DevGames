# Combo Tactics — Arquitetura do Sistema

**Status:** Proposta
**Data:** 02/10/2026
**Veja também:** [SYSTEM-DESIGN.md](SYSTEM-DESIGN.md) (requisitos, fluxos de dados, contratos internos, modelo de dados, confiabilidade)
**Fontes:** [GDD](../GDD.md), [Caderno](../caderno.md), [Level Design](../leveldesign.md), [Mecânicas](../poderesMecanicas.md), `fluxograma.excalidraw`, `telas.excalidraw`

## 1. Restrições

| Restrição | Valor | Impacto na arquitetura |
|---|---|---|
| Plataforma | Navegador de PC, sem instalação | Build Unity **WebGL**: uma única thread, tamanho do download importa, áudio só libera depois de um clique |
| Engine | Unity (experiência do time) | C#, ScriptableObjects, uGUI/UI Toolkit |
| Online | **Sem backend no MVP** | Toda a lógica no cliente, save no armazenamento do navegador, sem loja com dinheiro real por enquanto |
| Multiplayer | Hot-seat local (mesmo aparelho) | Sem rede; dois controladores de entrada dividem uma tela |
| Time | 3–5 devs | O trabalho precisa ser dividido por módulos com fronteiras claras |
| Prazo | ~4 semanas (novembro de 2026) | O design mais simples que funcione; sem Addressables, sem framework de DI, sem ECS |

## 2. Visão geral da arquitetura

Três camadas. As dependências apontam **só para baixo**.

```
┌───────────────────────────────────────────────────────────────┐
│ APRESENTAÇÃO  (MonoBehaviours, cenas, UI, animação, áudio)    │
│  MenuUI · CampaignMapUI · TeamSelectUI · StoreUI · BattleView │
│  HUD · ReactionWindow (QTE) · ArenaRoulette · CameraDirector  │
└──────────────▲──────────────────────────────▲─────────────────┘
               │ chama serviços                │ consome BattleEvents,
               │                               │ envia comandos
┌──────────────┴───────────────┐ ┌─────────────┴─────────────────┐
│ JOGO / META  (C# comum)      │ │ NÚCLEO DE BATALHA  (C# puro)  │
│  GameRoot · ProfileService   │ │  BattleEngine (máq. estados)  │
│  StoreService · Campaign     │ │  DamageCalculator · Effects   │
│  SaveStore (PlayerPrefs)     │ │  Stamina · ArenaDraw · RNG    │
│  BattleLauncher ─────────────┼─▶  Controladores: Humano / IA   │
└──────────────▲───────────────┘ └─────────────▲─────────────────┘
               │ lê                             │ lê
┌──────────────┴────────────────────────────────┴───────────────┐
│ CONTEÚDO  (ScriptableObjects — só dados)                      │
│  CharacterDef · SkillDef · ItemDef · EffectDef · ArenaDef     │
│  OpponentDef · AIProfile · EffectivenessTable · EconomyConfig │
└───────────────────────────────────────────────────────────────┘
```

A decisão central ([ADR-002](adr/ADR-002-battle-core-separation.md)): **as regras da batalha são C# puro, sem dependência de `UnityEngine`.** Essa escolha barateia três coisas que o GDD já exige:
- **IA**: a IA simula ações candidatas numa cópia do estado.
- **Simulações de balanceamento**: o GDD diz que os 9 personagens foram balanceados por simulação. Isso vira um teste EditMode que roda milhares de duelos em segundos.
- **PvP hot-seat e PvIA** usam o mesmo motor com controladores diferentes.

## 3. Módulos

### 3.1 Conteúdo (ScriptableObjects) — [ADR-003](adr/ADR-003-data-driven-content.md)

| Asset | Campos (resumo) | Quantidade no MVP |
|---|---|---|
| `CharacterDef` | id, nome, `Element`, `CharClass`, HP/Atq/Def/Vel base, 2 habilidades normais + 1 especial, sprite/animator, clipes de voz | 9 + 1 boss |
| `SkillDef` | poder, `isSpecial`, alvo, `EffectDef` opcional + chance, VFX/SFX | ~30 |
| `EffectDef` | tipo (DoT / HoT / StatMod), valor, duração (padrão 3), `removableByMedicine` | ~6 (Chamas, Afogamento, Veneno, Regeneração, Atq↑, Def↑) |
| `ItemDef` | tipo (Cura / Limpeza / Poção de Ataque / Poção de Defesa), valor, preço | 4 |
| `ArenaDef` | Mar / Vulcão / Floresta / Lua, regra de favorecido/prejudicado, arte de fundo | 4 |
| `OpponentDef` | nome, equipe (1–3 `CharacterDef`), `AIProfile`, arena de origem (arena inicial), bônus de atributos (+0 / +7 / +12%), itens iniciais, recompensa | 4 (Fogo, Água, Planta, Boss) |
| `AIProfile` | limites e chances das regras de prioridade ([ADR-004](adr/ADR-004-ai-approach.md)) | 7 (Fácil, Normal, Difícil + 4 oponentes) |
| `EffectivenessTable` | os dois ciclos (Fogo→Planta→Água, Guerreiro→Mago→Tanque), multiplicadores 1,3/1,5 e 0,7/0,5 | 1 |
| `EconomyConfig` | moedas (vitória 100 / derrota 50 / boss 500), +3% por evolução, máx. 5 por atributo | 1 |

Os designers ajustam números no Inspector sem mudar código. O núcleo de batalha nunca vê ScriptableObjects. O `BattleLauncher` os converte em registros imutáveis do núcleo (`FighterSpec`, `SkillSpec`, ...), assim o núcleo continua testável e livre do Unity.

### 3.2 Núcleo de Batalha (C# puro, `ComboTactics.Core.asmdef`, `noEngineReferences: true`)

**Estado**

```csharp
class BattleState {
    Side[] sides;            // 2 lados
    int round;
    ArenaId currentArena;    // toda batalha começa numa arena (GDD R4)
    Rng rng;                 // com seed → batalhas reproduzíveis
}
class Side   { Fighter[] team; int activeIndex; int stamina; Inventory items; }
class Fighter{ FighterSpec spec; int hp; List<ActiveEffect> effects; bool Fainted => hp <= 0; }
```

**Comandos** (as três ações de turno do GDD):
`AttackCommand(skillIndex)`, `UseItemCommand(itemId)`, `SwitchCommand(benchIndex)`.

**Loop do motor.** O motor é uma máquina de estados por etapas. Ele nunca espera por tempo nem por entrada. Ele devolve o que precisa a seguir:

```
 ┌──────────────┐  os dois lados  ┌──────────────┐  cada ação, em    ┌───────────────────┐
 │ AwaitActions ├────enviaram────▶│ ResolveOrder ├──ordem de Vel.───▶│ ResolveAction     │
 └──────▲───────┘                 └──────────────┘ (troca primeiro)  │  ataque? ─▶ Await │
        │                                                            │  Reaction (QTE)   │
        │                                                            └─────────┬─────────┘
 ┌──────┴────────┐  a cada 3 rodadas ┌──────────────┐   ┌──────────────────┐   │
 │ ArenaDraw     │◀──────────────────┤ RoundEnd     │◀──┤ EndOfTurnEffects │◀──┘
 └──────┬────────┘                   │ vitória?     │   │ DoT/HoT, -1 turno│
        └──────────▶ AwaitActions    └──────┬───────┘   └──────────────────┘
                                            └─ um lado todo derrotado ─▶ BattleOver
                         (lutador ativo derrotado ─▶ AwaitForcedSwitch)
```

```csharp
interface IBattleEngine {
    BattleState State { get; }
    PendingInput Pending { get; }          // Actions | Reaction | ForcedSwitch | None
    IReadOnlyList<BattleEvent> Submit(int side, ICommand cmd);
    IReadOnlyList<BattleEvent> SubmitReaction(int side, ReactionResult r); // Miss | Dodge | Counter
}
```

Cada chamada devolve uma lista ordenada de **`BattleEvent`s** (`SkillUsed`, `DamageDealt{amount, effectiveness}`, `ReactionRequested`, `Dodged`, `Countered`, `EffectApplied`, `EffectTicked`, `StaminaChanged`, `SpecialUnlocked`, `Switched`, `Fainted`, `ArenaChanged`, `BattleEnded`). A apresentação toca esses eventos um a um como animações. O núcleo nunca sabe de animação. O QTE é uma entrada pendente comum, então a IA responde com um sorteio de probabilidade e a pessoa responde pelo widget da interface.

**Serviços de regra** (sem estado, com testes unitários): `DamageCalculator`, `EffectivenessResolver`, `EffectSystem`, `StaminaSystem`, `ArenaDraw`, `TurnOrder`.

**Fórmula de dano.** O GDD dá os ingredientes. Esta é a composição, segundo o GDD §5.7 (R1, R4):

```
raw      = skill.power/100 × Atq × buffs(poção de ataque, efeitos)
advMult  = A(vantagens do atacante) × D(vantagens do defensor)
           A: 0→1,0, 1→1,3, 2→1,5     D: 0→1,0, 1→0,7, 2→0,5
envMult  = 1,10 favorecido / 0,90 prejudicado / 1,0   (arena atual, ativa até o próximo sorteio)
defCut   = 1 − min(Def, 100) × 0,005                 (máx. 50%)
damage   = round(raw × advMult × envMult × defCut)
```

### 3.3 Controladores (quem decide as ações)

```csharp
interface IBattleController {
    void RequestAction(BattleState s, int side, Action<ICommand> reply);
    void RequestReaction(BattleState s, int side, Action<ReactionResult> reply);
    void RequestForcedSwitch(BattleState s, int side, Action<int> reply);
}
```

| Modo | Lado 0 | Lado 1 |
|---|---|---|
| Campanha / contra IA | `HumanController` (UI) | `RuleBasedAIController(AIProfile)` |
| PvP local (hot-seat) | `HumanController(P1)` | `HumanController(P2)` |

**IA** ([ADR-004](adr/ADR-004-ai-approach.md)) segue as regras de prioridade do Level Design: sobreviver (Kit Médico), trocar por vantagem, limpar DoTs, usar o especial quando a stamina enche e, fora isso, atacar com a melhor habilidade normal. Todos os limites e chances (inclusive o acerto no QTE: Fácil 20% / Normal 50% / Difícil 80%, Treinador de Água 40%) vêm de um asset `AIProfile`.

**PvP hot-seat:** os dois jogadores escolhem na mesma rodada (GDD R2), então o P2 não pode ver a escolha do P1: uma tela curta de "Passe para o Jogador 2" fica entre as duas escolhas. As teclas do QTE são separadas (ex.: P1 `Espaço`, P2 `Enter`), para ficar sempre claro quem reage.

### 3.4 Camada de Jogo / Meta (C# comum, pertence ao `GameRoot`)

- **`GameRoot`** é um objeto `DontDestroyOnLoad` criado na cena `Boot`. Ele é dono dos serviços abaixo e os expõe por um único acesso estático (`Game.Profile`, `Game.Store`, ...). O projeto é pequeno demais para precisar de framework de DI.
- **`ProfileService`** guarda moedas, inventário, níveis de evolução por personagem (0–5 por atributo), progresso da campanha (índice do último oponente vencido) e configurações.
- **`StoreService`** trata `BuyItem` e `BuyUpgrade`. Valida o preço, o limite de 5 evoluções e o saldo, e depois salva.
- **`CampaignService`** cuida das 4 fases (Fogo, Água, Planta, Boss), do desbloqueio e das recompensas.
- **`StatsResolver`** calcula os atributos efetivos como base × (1 + 0,03 × nível). No Modo Justo do PvP (padrão) devolve os atributos base (GDD R6). Os oponentes da campanha recebem o bônus da fase (GDD R10).
- **`BattleLauncher`** monta um `BattleSetup` (equipes, controladores, seed, arena inicial, recompensas) e carrega a cena `Battle`. No `BattleEnded`, paga as moedas, atualiza o progresso e volta para o menu certo.
- **`SaveStore`** ([ADR-005](adr/ADR-005-persistence.md)) grava um `SaveData` versionado, serializado com `JsonUtility`, no `PlayerPrefs`. No WebGL ele fica no IndexedDB. A interface `ISaveStore` é o ponto de troca para um futuro save na nuvem e pacotes de moedas pagos.

### 3.5 Apresentação

**Cenas** (de `telas.excalidraw`):

```
Boot ─▶ MainMenu ─┬─▶ CampaignMap (Fogo · Água · Planta · Boss Final) ─▶ TeamSelect ─▶ Battle
                  ├─▶ PvP ─▶ TeamSelect (Equipe 1 / Equipe 2) ─────────────────────▶ Battle
                  ├─▶ Contra IA (dificuldade) ─▶ TeamSelect ────────────────────────▶ Battle
                  └─▶ Store (Loja de Itens · Progressão de Atributos)
Battle ─▶ Tela de resultado (moedas ganhas) ─▶ CampaignMap / MainMenu
```

**Componentes da cena de batalha**
- `BattleDirector` é dono do motor e dos controladores e roda a fila de eventos: pega o próximo `BattleEvent`, espera a view terminar (coroutine) e pega o seguinte. É o único lugar onde núcleo e visual se encontram.
- `FighterView` ×2 para sprite, animator, animações de golpe/derrota e voz/rugido.
- `BattleHUD` para barras de HP, barra de stamina, ícones de efeitos e o banco.
- `ActionMenu` para Habilidades / Itens / Troca (o especial fica desabilitado até a stamina encher).
- `ReactionWindow`: o widget de timing do QTE. Devolve `Miss` / `Dodge` / `Counter` (GDD R3).
- `ArenaRoulette` para a animação do sorteio de cassino e a troca do fundo.
- `CameraDirector`: câmera lateral fixa que dá zoom nos ataques especiais (Cinemachine é opcional).

## 4. Estrutura do projeto

```
Assets/_Project/
  Scripts/
    Core/            ComboTactics.Core.asmdef   (noEngineReferences)
    Core.Tests/      testes EditMode + BalanceSimulation
    Game/            ComboTactics.Game.asmdef   (→ Core)
    Presentation/    ComboTactics.UI.asmdef     (→ Game, Core)
  Content/
    Characters/  Skills/  Effects/  Items/  Arenas/  Opponents/  Config/
  Art/  Audio/  Prefabs/  Scenes/
```

As assembly definitions garantem as camadas: um `using UnityEngine;` dentro de `Core/` não compila.

## 5. Checklist de WebGL

- **Compressão:** Brotli, com *Decompression Fallback* ligado se o host não enviar `Content-Encoding` (o GitHub Pages não envia; o itch.io funciona).
- **Orçamento de tamanho:** menos de ~30 MB comprimido. Usar sprite atlases, áudio comprimido (Vorbis) e *Strip Engine Code*.
- **Áudio** só começa depois do primeiro clique, então o "Clique para começar" no `Boot` também serve para liberar o áudio.
- **Sem threads, sem `System.Threading.Tasks.Delay`**: usar coroutines. A IA é leve o bastante para rodar na thread principal.
- **Save:** chamar `PlayerPrefs.Save()` depois de cada gravação. Sem isso, dados podem se perder quando a aba fecha.
- **Hospedagem:** itch.io (mais simples) ou GitHub Pages. Fazer um build web na semana 1 e testar no Chrome, Firefox e Safari.

## 6. Divisão do time e plano de 4 semanas (3–5 devs)

| Dono | Área |
|---|---|
| Dev A | Núcleo de batalha + testes + simulação de balanceamento |
| Dev B | Apresentação da batalha (BattleDirector, HUD, QTE, roleta, animações) |
| Dev C | Meta: menus, TeamSelect, Loja, Campanha, Save |
| Dev D (opc.) | IA + cadastro de conteúdo (9 personagens, habilidades, oponentes) |
| Dev E (opc.) | Integração de arte/áudio, build WebGL e publicação |

| Semana | Objetivo |
|---|---|
| 1 | **Esqueleto jogável:** Boot → Menu → Batalha com 2 lutadores placeholder, só ataque, publicado em WebGL. Congelar os contratos `BattleEvent` e `ICommand` para B e C trabalharem em paralelo. |
| 2 | Regras completas do núcleo (itens, troca, efeitos, stamina/especial, sorteio de arena, QTE). Conteúdo dos 9 personagens. IA básica. Loja e save. |
| 3 | Campanha (4 fases + boss), PvP hot-seat, dificuldades da IA, polimento (VFX/SFX, câmera). Rodada de simulação de balanceamento. |
| 4 | Congelamento de funcionalidades, correção de bugs, testes nos navegadores, desempenho e tamanho, lançamento. |

## 7. Perguntas de design (resolvidas)

As nove lacunas encontradas no GDD (Q1–Q9) foram fechadas em 02/10/2026 e escritas no GDD como as regras R1–R10, em [§5.7 Esclarecimentos de Regras](../GDD.md#57-esclarecimentos-de-regras). O código segue o GDD; esta tabela só mapeia a numeração antiga.

| # | Pergunta | Resolvida por |
|---|---|---|
| Q1 | Vantagens mistas | R1: multiplicar (1,3 × 0,7 = 0,91) |
| Q2 | Modelo de turno | R2: os dois escolhem, ordem de Velocidade, trocas primeiro |
| Q3 / Q3b | Desvio x contra-ataque; dano do contra-ataque | R3: perfeito = contra-ataque, bom = desvio; o contra-ataque usa a habilidade 1 do defensor |
| Q4 | Duração da arena | R4: até o próximo sorteio; toda batalha começa numa arena |
| Q5 | Limites da Lua | R5: Vel < 50 favorecido, Vel ≥ 70 prejudicado |
| Q6 | Justiça no PvP | R6: Modo Justo por padrão |
| Q7 | Stamina | R7: por jogador, +20 / +15 / +25, zera depois do especial |
| Q8 | Itens | R8: máx. 3 por batalha, Kit Médico 30%, poções +10% por 3 turnos |
| Q9 | Boss | R9: lutador único, ~3× HP, sem ações extras, arena Lua |
| — | Ordem da campanha e força dos oponentes | R10: Fogo → Água → Planta → Boss; equipes de um só elemento; +0 / +7 / +12% |

Ainda em aberto (não bloqueia a semana 1): preços da loja, atributos e habilidades do Boss, e o level design das fases de Fogo, Planta e Boss. Veja [NEXT-STEPS.md](../../NEXT-STEPS.md).

## 8. Adiado (fora do MVP de propósito)

Conforme o [GDD §6 Escopo do MVP](../GDD.md#6-escopo-do-mvp): pacotes de moedas com dinheiro real, contas e save na nuvem, PvP online e o sistema de apostas com moedas (o elemento de gambling do MVP é o sorteio de arena). Dois pontos de extensão mantêm isso possível depois: `ISaveStore` e o núcleo de batalha determinístico, com seed, que um servidor pode verificar.

## Índice de ADRs

| ADR | Título | Status |
|---|---|---|
| [001](adr/ADR-001-engine-unity-webgl.md) | Unity WebGL como engine e plataforma | Aceito |
| [002](adr/ADR-002-battle-core-separation.md) | Núcleo de batalha em C# puro com apresentação por eventos | Proposto |
| [003](adr/ADR-003-data-driven-content.md) | ScriptableObjects para o conteúdo do jogo | Proposto |
| [004](adr/ADR-004-ai-approach.md) | IA por regras de prioridade com perfis por oponente | Aceito |
| [005](adr/ADR-005-persistence.md) | Save local em PlayerPrefs JSON, sem backend | Proposto |
