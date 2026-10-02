# Combo Tactics — System Design

**Status:** Proposto
**Data:** 02/10/2026
**Método:** skill `system-design` (framework de 5 passos: requisitos → visão geral → detalhamento → escala e confiabilidade → trade-offs)
**Relacionados:** [ARCHITECTURE.md](ARCHITECTURE.md) (módulos e plano do time), [ADRs](adr/)

Este documento desenha o sistema como um todo: o que ele precisa fazer, como os dados fluem, como são os contratos internos e onde ele pode quebrar. A estrutura de módulos e o plano semana a semana estão no `ARCHITECTURE.md`. As decisões individuais estão registradas como ADRs.

---

## 1. Requisitos

### 1.1 Requisitos funcionais

| ID | Requisito | Fonte |
|---|---|---|
| F1 | Escolher uma equipe de 3 entre 9 personagens (3 classes × 3 elementos) | GDD §2, §4 |
| F2 | Duelo 1v1 por turnos com uma ação por turno: **atacar**, **usar item** ou **trocar** | GDD §2 |
| F3 | Dano = Ataque, corte por Defesa (0,5% por ponto, teto de 50%), vantagem de elemento e de classe (±30% / ±50%) | GDD §3 |
| F4 | A Velocidade decide quem age primeiro no turno | GDD §3.3 |
| F5 | 2 habilidades normais + 1 especial, liberado pela barra de **stamina** cheia (carrega ao causar dano, sofrer dano e reagir com sucesso) | GDD §5.3–5.4 |
| F6 | **Janela de resposta (QTE)** em todo ataque recebido: falha / desvio / contra-ataque | GDD §5.5 |
| F7 | **Efeitos** duram 3 turnos (DoT: Chamas, Afogamento, Veneno; HoT: Regeneração; bônus de atributo) e **acompanham o lutador no banco** | GDD §5.2 |
| F8 | **Sorteio de arena** a cada 3 rodadas (Mar, Vulcão, Floresta, Lua), ±10% de dano | GDD §5.6 |
| F9 | Vitória quando todos os lutadores do outro time são derrotados | GDD §2 |
| F10 | **Campanha:** 4 fases em sequência (treinadores de Fogo, Água e Planta + Boss com 1 lutador exclusivo), cada uma liberada pela anterior | Caderno p.4, p.8 |
| F11 | **Modos:** Campanha, contra IA (dificuldade escolhida), PvP hot-seat local | GDD §1, Caderno p.9 |
| F12 | **Economia:** moedas por batalha (vitória 100 / derrota 50 / boss 500) | Mecânicas §7 |
| F13 | **Loja:** itens consumíveis (Kit Médico, Remédio, Poção de Ataque, Poção de Defesa) e evoluções permanentes (+3% por nível, máx. 5 por atributo) | GDD §5.1 |
| F14 | O progresso é salvo entre sessões | decorre de F10, F13 |

### 1.2 Requisitos não funcionais

| ID | Requisito | Meta |
|---|---|---|
| N1 | Roda num navegador de desktop sem instalação | Chrome, Firefox, Edge, Safari (2 últimas versões) |
| N2 | Primeiro carregamento | < 20 s numa conexão de 20 Mbps (download ≤ 30 MB comprimido) |
| N3 | Taxa de quadros | 60 fps durante as animações de batalha num notebook com GPU integrada |
| N4 | Resposta à entrada | Toda ação reconhecida visualmente em < 100 ms; timing do QTE preciso em um quadro (±17 ms) |
| N5 | Duração da batalha | Cerca de 5 minutos (Level Design) |
| N6 | Durabilidade do save | Nenhum progresso perdido ao fechar a aba normalmente; save corrompido nunca derruba o jogo |
| N7 | Reprodutibilidade | Mesmas entradas + mesma seed → mesma batalha (bugs reproduzíveis, simulação de balanceamento) |
| N8 | Custo | R$ 0 para hospedar e rodar |

### 1.3 Restrições

- **Stack:** Unity (LTS atual), plataforma WebGL, C# ([ADR-001](adr/ADR-001-engine-unity-webgl.md)).
- **Sem backend no MVP** ([ADR-005](adr/ADR-005-persistence.md)).
- **Time:** 3–5 devs. **Tempo:** cerca de 4 semanas.
- **Multiplayer só local:** um aparelho, um teclado e um mouse.

### 1.4 Decisões de regra

As lacunas do GDD foram fechadas como as regras **R1–R10** em [GDD §5.7](../GDD.md#57-esclarecimentos-de-regras). Este design segue essas regras; as mais relevantes aqui:

| Regra | Resumo |
|---|---|
| R1 | Vantagens mistas se multiplicam: bônus do atacante × redução do defensor (ex.: 1,3 × 0,7 = 0,91). |
| R2 | Os dois lados escolhem e depois resolvem em ordem de Velocidade. Trocas primeiro; empate decidido por sorteio com seed. |
| R3 | QTE: perfeito = contra-ataque, bom = desvio, senão falha. O contra-ataque usa a habilidade 1 do defensor e não pode ser respondido. |
| R4 | Toda batalha começa numa arena (campanha: arena do oponente; outros modos: sorteada). A arena dura até o próximo sorteio. |
| R5 | Lua: Vel < 50 favorecido, Vel ≥ 70 prejudicado. |
| R6 | Modo Justo no PvP por padrão: atributos base e kit fixo de itens. |
| R7 | Stamina por jogador: +20 ao causar dano, +15 ao sofrer dano, +25 ao reagir com sucesso; zera depois do especial. |
| R8 | Até 3 itens por batalha, consumidos ao usar. |
| R10 | Campanha Fogo → Água → Planta → Boss; atributos dos treinadores +0 / +7 / +12%. |

---

## 2. Visão geral

### 2.1 Diagrama de contexto

O sistema inteiro é um build web estático. Não existe lado servidor.

```
             ┌──────────────────────────────────────────────┐
 Jogador 1 ─▶│              Aba do navegador                │
 Jogador 2 ─▶│  ┌────────────────────────────────────────┐  │      ┌────────────────────────┐
 (hot-seat)  │  │      Runtime Unity WebGL (WASM)        │  │◀─────│ Host estático (itch.io │
             │  │ Apresentação ─ Jogo/Meta ─ Núcleo      │  │ HTTP │ / GitHub Pages): .wasm,│
             │  └───────────────────┬────────────────────┘  │ 1 vez│ .data, .js, index.html │
             │                      │ PlayerPrefs           │      └────────────────────────┘
             │              ┌───────▼───────┐               │
             │              │   IndexedDB   │  save (~2 KB) │
             │              └───────────────┘               │
             └──────────────────────────────────────────────┘
```

### 2.2 Componentes

```
┌──────────────────────────── APRESENTAÇÃO ────────────────────────────┐
│ Cenas: Boot · MainMenu · CampaignMap · TeamSelect · Store · Battle   │
│ Batalha: BattleDirector ─▶ FighterView×2 · HUD · ActionMenu ·        │
│          ReactionWindow (QTE) · ArenaRoulette · CameraDirector       │
└───────────▲─────────────────────────────────────────▲────────────────┘
            │ chamadas de serviço                     │ fluxo de BattleEvent / ICommand
┌───────────┴──────────── JOGO / META ───────┐ ┌──────┴──── NÚCLEO DE BATALHA (C# puro) ─┐
│ GameRoot  (dono de tudo abaixo)            │ │ BattleEngine (máquina de estados)       │
│ ProfileService · StoreService              │ │ DamageCalculator · EffectivenessResolver│
│ CampaignService · StatsResolver            │ │ EffectSystem · StaminaSystem            │
│ BattleLauncher ────── monta BattleSetup ───┼─▶ ArenaDraw · TurnOrder · Rng (com seed)  │
│ ISaveStore ─▶ PlayerPrefsSaveStore         │ │ Controladores: Humano · RuleBasedAI ·   │
│                                            │ │                Random                   │
└───────────▲────────────────────────────────┘ └──────▲──────────────────────────────────┘
            │ lê (por id)                              │ specs imutáveis
┌───────────┴──────────────────── CONTEÚDO (ScriptableObjects) ──────┴──────────────────┐
│ ContentDatabase: CharacterDef · SkillDef · EffectDef · ItemDef · ArenaDef ·           │
│                  OpponentDef · AIProfile · EffectivenessTable · EconomyConfig         │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Principais fluxos de dados

**Fluxo A — Batalha da campanha, do início ao fim**

```
CampaignMap ──escolhe fase──▶ CampaignService.CanPlay(fase)?
   │ sim
TeamSelect ──equipe + itens──▶ BattleLauncher.Build(setup)
   │   ├─ StatsResolver: atributos base × evoluções   (Profile)
   │   ├─ ContentDatabase → FighterSpec/SkillSpec      (imutáveis)
   │   ├─ OpponentDef → equipe da IA (+ bônus da fase) + RuleBasedAIController(AIProfile)
   │   ├─ arena inicial = OpponentDef.homeArena
   │   └─ seed = inteiro aleatório  (registrado para relatórios de bug)
Cena Battle ──▶ BattleDirector roda o loop do motor (Fluxo B)
   │ BattleEnded(vencedor)
BattleLauncher.Finish ──▶ Profile.coins += recompensa; consome itens usados;
                          Campaign.stage++ se venceu; SaveStore.Save()
   ▼
Tela de resultado ──▶ CampaignMap
```

**Fluxo B — Uma rodada dentro da batalha**

```
BattleDirector                 BattleEngine (núcleo)              Controladores
     │  Pending = Actions             │                                │
     │──RequestAction(lado 0)─────────┼───────────────────────────────▶│ Humano: ActionMenu
     │──RequestAction(lado 1)─────────┼───────────────────────────────▶│ IA: regras de prioridade
     │◀──────────── ICommand × 2 ─────┼────────────────────────────────│
     │──Submit(cmds)─────────────────▶│ ordena por Vel. (troca antes)  │
     │◀─ eventos [SkillUsed, ReactionRequested]                        │
     │  toca anim., Pending = Reaction│                                │
     │──RequestReaction(defensor)─────┼───────────────────────────────▶│ Humano: widget do QTE
     │◀──────────── Miss/Dodge/Counter┼────────────────────────────────│ IA: sorteio %
     │──SubmitReaction──────────────▶ │ aplica dano, efeitos, stamina  │
     │◀─ eventos [DamageDealt, EffectApplied, StaminaChanged, ...]     │
     │  ...segunda ação, DoT/HoT de fim de turno, checagem de derrota..│
     │◀─ eventos [EffectTicked, Fainted?, ArenaChanged? (rodada % 3)]  │
     │  toca a fila em ordem ─▶ próxima rodada ou ForcedSwitch / Fim   │
```

### 2.4 Contratos internos (API)

Não existe API de rede. Os contratos que importam são as **interfaces entre módulos**, porque é nelas que o trabalho do time se divide. Elas são congeladas na semana 1.

```csharp
// ── Núcleo de Batalha: entrada ───────────────────────────────────
public interface ICommand { int Side { get; } }
public record AttackCommand(int Side, int SkillIndex)     : ICommand; // 0,1 normal; 2 especial
public record UseItemCommand(int Side, string ItemId)     : ICommand;
public record SwitchCommand(int Side, int BenchIndex)     : ICommand;
public enum ReactionResult { Miss, Dodge, Counter }

// ── Núcleo de Batalha: motor ─────────────────────────────────────
public enum PendingKind { Actions, Reaction, ForcedSwitch, None }
public interface IBattleEngine {
    BattleState State { get; }                // visão somente leitura para UI/IA
    PendingKind Pending { get; }
    int PendingSide { get; }                  // quem precisa responder (Reaction/ForcedSwitch)
    IReadOnlyList<ICommand> LegalCommands(int side);
    StepResult SubmitActions(ICommand side0, ICommand side1);
    StepResult SubmitReaction(ReactionResult r);
    StepResult SubmitForcedSwitch(int benchIndex);
}
public record StepResult(bool Ok, string Error, IReadOnlyList<BattleEvent> Events);

// ── Núcleo de Batalha: saída (conjunto fechado, ~15 tipos) ───────
public abstract record BattleEvent;
public record SkillUsed(int Side, string SkillId)                                   : BattleEvent;
public record ReactionRequested(int DefenderSide, float WindowSeconds)              : BattleEvent;
public record ReactionResolved(int DefenderSide, ReactionResult Result)             : BattleEvent;
public record DamageDealt(int TargetSide, int Amount, Effectiveness Eff, bool Counter) : BattleEvent;
public record Healed(int Side, int Amount)                                          : BattleEvent;
public record EffectApplied(int Side, string EffectId, int Turns)                   : BattleEvent;
public record EffectTicked(int Side, int FighterIndex, string EffectId, int Delta)  : BattleEvent;
public record EffectExpired(int Side, int FighterIndex, string EffectId)            : BattleEvent;
public record StaminaChanged(int Side, int Value)                                   : BattleEvent;
public record ItemUsed(int Side, string ItemId)                                     : BattleEvent;
public record Switched(int Side, int FromIndex, int ToIndex)                        : BattleEvent;
public record Fainted(int Side, int FighterIndex)                                   : BattleEvent;
public record RoundStarted(int Round)                                               : BattleEvent;
public record ArenaChanged(ArenaId Arena)                                           : BattleEvent;
public record BattleEnded(int WinnerSide)                                           : BattleEvent;

// ── Controladores ────────────────────────────────────────────────
public interface IBattleController {
    void RequestAction(BattleState s, int side, Action<ICommand> reply);
    void RequestReaction(BattleState s, int side, float window, Action<ReactionResult> reply);
    void RequestForcedSwitch(BattleState s, int side, Action<int> reply);
}

// ── Jogo / Meta ──────────────────────────────────────────────────
public interface IProfileService { int Coins { get; } int UpgradeLevel(string charId, Stat s);
                                   int ItemCount(string itemId); int CampaignStage { get; } }
public interface IStoreService   { PurchaseResult BuyItem(string itemId);
                                   PurchaseResult BuyUpgrade(string charId, Stat s); }
public enum PurchaseResult { Ok, NotEnoughCoins, MaxLevelReached, UnknownId }
public interface ISaveStore      { SaveData Load(); void Save(SaveData d); }
```

### 2.5 Armazenamento

| Dado | Onde | Duração | Tamanho |
|---|---|---|---|
| Conteúdo (personagens, habilidades, ...) | ScriptableObjects dentro do build | Somente leitura, vem com o build | ~100 KB de dados mais a arte |
| Estado da batalha | `BattleState` em memória | Uma batalha | < 10 KB |
| Perfil do jogador | `SaveData` → JSON → `PlayerPrefs` → IndexedDB | Persistente | < 2 KB |
| Arte / áudio | Arquivo `.data` do Unity, sprite atlases | Baixado uma vez, guardado no cache do navegador | ≤ 30 MB comprimido |

---

## 3. Detalhamento

### 3.1 Modelo de dados

**Conteúdo (em tempo de autoria, ScriptableObjects)**

```
CharacterDef ──┬── id, displayName, element, charClass
               ├── baseHp, baseAtk, baseDef(0..100), baseSpd
               ├── skills[2] ──▶ SkillDef
               ├── special   ──▶ SkillDef
               └── sprite, animator, voiceClips[]

SkillDef ───── id, power, isSpecial, effect? ──▶ EffectDef, effectChance, vfx, sfx
EffectDef ──── id, kind{DoT,HoT,StatMod}, value, stat?, turns=3, cleansable
ItemDef ────── id, kind{Heal,Cleanse,AtkUp,DefUp}, value, price
ArenaDef ───── id{Sea,Volcano,Forest,Moon}, rule{Element|SpeedBand}, favoured, penalised, background
OpponentDef ── id, name, team[1..3] ──▶ CharacterDef, aiProfile ──▶ AIProfile, homeArena, statBonus, items, reward
AIProfile ──── survivalHpPct, switchChance, dotTurnsBeforeCleanse, qteChance, counterShare, mistakeChance
```

**Estado da batalha em execução (núcleo, mutável só durante a batalha)**

```
BattleState
 ├── round : int
 ├── arena : ArenaId             (definida no início da batalha, R4)
 ├── rng   : Rng(seed)
 └── sides[2] : Side
       ├── team[1..3] : Fighter
       │     ├── spec : FighterSpec   (imutável: atributos efetivos após evoluções, habilidades)
       │     ├── hp   : int
       │     └── effects : List<ActiveEffect{effectId, turnsLeft, value}>   ← continua no banco
       ├── activeIndex : int
       ├── stamina : int (0..100)
       └── items : Dictionary<itemId, count>   (máx. 3 no total, R8)
```

**Perfil persistente (`SaveData`, JSON)**

```json
{
  "version": 1,
  "coins": 350,
  "items": [ { "id": "med_kit", "count": 2 }, { "id": "atk_potion", "count": 1 } ],
  "upgrades": [ { "charId": "fire_mage", "hp": 0, "atk": 3, "def": 1, "spd": 0 } ],
  "campaignStage": 2,
  "settings": { "musicVolume": 0.8, "sfxVolume": 1.0, "fairPvp": true }
}
```

Regras: o save se refere ao conteúdo **só por id em texto**. Ids desconhecidos são ignorados ao carregar, então remover conteúdo nunca quebra saves antigos. O campo `version` controla as migrações.

### 3.2 Algoritmos do núcleo

**Dano** (F3, R1, R4):

```
raw     = skill.power / 100 × Atq × atkBuffs
adv     = A(nVantagensAtacante) × D(nVantagensDefensor)   A = {1; 1,3; 1,5}   D = {1; 0,7; 0,5}
env     = 1,10 | 0,90 | 1,00                     (arena atual x atacante)
defCut  = 1 − min(Def × defBuffs, 100) × 0,005
damage  = max(1, round(raw × adv × env × defCut))
```

Conferência com o exemplo do GDD: um Mago de Fogo (Atq 171) acerta um Tanque de Planta (Def 95) com uma habilidade de poder 100. Ele tem vantagem de classe e de elemento, então adv = 1,5, resultando em 171 × 1,5 × 0,525 = **135**. Um Tanque de Planta atacando um Mago de Fogo não tem vantagem e o Mago tem duas, então adv = 0,5, resultando em 63 × 0,5 × 0,925 = **29**.

**Resolução do turno** (F2, F4, R2):

1. Os dois comandos são coletados.
2. Ordem: trocas primeiro, depois por Velocidade efetiva (a Lua pode alterá-la), com empate decidido pelo `rng`.
3. Para cada ação, se quem age ainda está de pé:
   - **Troca:** muda o `activeIndex`. Os efeitos continuam no lutador.
   - **Item:** aplica e diminui a quantidade.
   - **Ataque:** emite `ReactionRequested` e **pausa** (`Pending = Reaction`). Ao retomar, aplica Falha / Desvio / Contra-ataque, depois o dano, o sorteio do efeito da habilidade e a stamina.
4. Fim do turno: aplica os efeitos de **todos** os lutadores dos dois times, inclusive os do banco (F7). Diminui `turnsLeft` e remove os efeitos expirados.
5. Checagem de derrota. Se um lutador ativo caiu, `Pending = ForcedSwitch` para aquele lado.
6. Se um lado não tem mais lutadores, emite `BattleEnded`.
7. `round++`. Se `round % 3 == 0`, roda o sorteio de arena (uniforme entre as 4 arenas) e emite `ArenaChanged`.

**Timing do QTE** (F6, N4): o núcleo só informa à interface o tamanho da janela (`ReactionRequested.WindowSeconds`, padrão 0,6 s). A interface mede o timing com `Time.unscaledTime` e o converte num resultado: perfeito ±60 ms → Contra-ataque, bom ±150 ms → Desvio, senão Falha. O timing nunca entra no núcleo, então o núcleo continua determinístico (N7).

### 3.3 Estratégia de cache

Não há dados remotos, então "cache" aqui significa evitar trabalho repetido e downloads repetidos.

| O quê | Estratégia |
|---|---|
| Arquivos do build web | Nomes de arquivo com hash (*Name Files As Hashes* do Unity) mais cache longo no navegador, para a volta do jogador carregar do cache |
| Conteúdo | O `ContentDatabase` monta os dicionários `Dictionary<string, Def>` uma vez no `Boot` |
| Efetividade | Tabelas 3×3 de elemento e classe pré-calculadas como arrays no início do jogo |
| Specs dos lutadores | O `StatsResolver` calcula os atributos efetivos uma vez por batalha, não a cada golpe |
| Sprites / áudio | Sprite atlas por personagem; clipes de áudio com *Load in Background*, exceto os sons de interface |

### 3.4 Design de eventos

O **fluxo de `BattleEvent` é a fila** deste sistema:

- **Produtor:** `BattleEngine`. Cada chamada `Submit*` devolve uma lista **ordenada e finita** de eventos. O motor já está no novo estado quando a lista volta.
- **Consumidor:** o `BattleDirector` toca os eventos **um de cada vez**: `foreach event → yield return view.Play(event)`. Cada coroutine de view controla seu próprio tempo (pausa no impacto, números de dano, giro da roleta).
- **Pausa para entrada:** quando o motor precisa de uma resposta (`Pending != None`), o diretor termina de tocar os eventos da fila e só então pergunta ao controlador. Nenhuma entrada é coletada durante as animações, então entrada e animação nunca disputam.
- **Ouvintes paralelos**, como áudio e câmera, assinam um `event Action<BattleEvent> OnEventPlayed` disparado pelo diretor. Eles reagem sem alterar o fluxo.
- **Avanço rápido:** a opção "pular animações" (e a simulação IA contra IA) simplesmente ignora as coroutines de view. Isso só é possível porque os eventos carregam toda a informação.

### 3.5 Tratamento de erros

| Falha | Detecção | Tratamento |
|---|---|---|
| Comando ilegal (especial sem stamina, item que não tem, troca para lutador derrotado) | `LegalCommands()` e validação no `Submit*` | `StepResult.Ok = false`, estado inalterado. A interface desabilita opções ilegais, então isso só acontece por bug. |
| Exceção no motor no meio da batalha | try/catch no `BattleDirector` | Registrar com a **seed e o log de comandos**, mostrar um diálogo de erro, voltar ao menu sem perder moedas já salvas |
| Save ausente ou corrompido | Falha ao ler o JSON ou `version` desconhecida | Guardar o texto original em `save_backup`, começar um perfil novo, mostrar um aviso |
| Id de conteúdo desconhecido no save | Busca sem resultado | Ignorar a entrada e registrar no log |
| Falha ao gravar o save (cota, modo privado) | Exceção em `PlayerPrefs.Save()` | Mostrar um aviso "o progresso não pode ser salvo neste navegador" e seguir jogando |
| Aba fechada no meio da batalha | n/a | A batalha é perdida; o perfil fica inalterado porque o save só é gravado no fim da batalha. Retomar batalha fica para depois. |
| IA trocando de personagem sem parar | Mesmo lutador trocado em turnos seguidos | Proteção no `RuleBasedAIController`: nada de troca em dois turnos seguidos ([ADR-004](adr/ADR-004-ai-approach.md)) |
| Erro de cadastro de conteúdo (atributo > 100, habilidade faltando) | `OnValidate` + teste EditMode sobre o `ContentDatabase` | O teste falha antes de chegar a um build |

Não é preciso lógica de nova tentativa: o sistema não faz chamadas de rede depois do download inicial, que o navegador e o loader do Unity já tratam.

---

## 4. Escala e confiabilidade

### 4.1 Estimativa de carga

"Escala" num jogo só de cliente significa o custo por aparelho e por batalha, não requisições por segundo.

| Item | Estimativa | Comentário |
|---|---|---|
| Tráfego de hospedagem | 30 MB × jogadores | Uma CDN estática (itch.io) absorve de graça qualquer pico de projeto de faculdade ou de lançamento |
| Duração da batalha | Duelos neutros: Mago contra Mago ≈ 4 golpes para derrotar (171 × 0,875 ≈ 150 contra 560 HP); Tanque contra Tanque ≈ 18–23 golpes (63–73 Atq contra 75–95 Def) | 3v3 ≈ 15–40 rodadas. Com ~6 s de animação mais ~4 s de escolha por rodada, dá **3–7 min**. Cabe na meta de 5 min (N5), mas **duelos de Tanques podem se arrastar**; a simulação de balanceamento precisa medir as rodadas. |
| Eventos por rodada | ~8–14 | Desprezível |
| Custo da IA por turno | 5 checagens de regra + no máximo 3 cálculos de dano (melhor habilidade normal) | Microssegundos; nenhum impacto nos quadros |
| Simulação de balanceamento | 81 confrontos × 1.000 seeds × ~30 rodadas ≈ 2,4 milhões de resoluções | De segundos a um minuto num teste EditMode |
| Memória | Heap inicial do Unity WebGL de 256 MB | 10 personagens × 1 atlas cada (≤ 2048²) + 4 fundos cabem |
| Tamanho do save | < 2 KB | Muito abaixo do limite de ~1 MB do `PlayerPrefs` no WebGL |

### 4.2 Horizontal x vertical

Não se aplica no sentido usual. Cada jogador roda a própria cópia. A única parte "horizontal" é a CDN estática, que escala sozinha. A restrição que importa é **vertical, dentro do aparelho**: a thread principal e o tamanho do download (N2, N3).

### 4.3 Failover e redundância

- **Hospedagem:** publicar o mesmo build no itch.io **e** no GitHub Pages. Se um estiver fora do ar ou bloqueado na rede da faculdade, o outro link funciona.
- **Save:** gravar alternando entre duas chaves (`save_a`, `save_b`) com contador e checksum. Ao carregar, usar o mais recente válido. Se uma gravação for interrompida, a anterior sobrevive.
- **Determinismo como rede de segurança:** todo relatório de bug inclui `seed + log de comandos`, que reproduz a batalha exatamente no editor.

### 4.4 Monitoramento e alertas

Sem backend, não há telemetria ao vivo no MVP. No lugar dela:

| Necessidade | Ferramenta no MVP |
|---|---|
| Crashes / erros no navegador | `Application.logMessageReceived` → buffer circular em memória (últimas 200 linhas), mais um botão "Copiar informações de debug" no diálogo de erro |
| Reproduzir uma batalha | Seed + log de comandos nessas informações de debug |
| Saúde do balanceamento | O teste EditMode `BalanceSimulation` imprime uma matriz 9×9 de vitórias e a média de rodadas. Ele **falha** se algum confronto sem vantagens ficar fora de 40–60% ou se a média passar de 40 rodadas. |
| Saúde do build | Unity Cloud Build ou uma GitHub Action com GameCI roda os testes EditMode e gera o WebGL a cada push na `main` (opcional; builds manuais bastam para 4 semanas) |
| Retorno de playtest | Builds de desenvolvimento mostram contador de FPS e um painel de debug (F1): adicionar moedas, zerar save, forçar arena, forçar resultado do QTE |

---

## 5. Análise de trade-offs

| Decisão | Escolha | Alternativa | Por quê | Custo que aceitamos |
|---|---|---|---|---|
| Engine | Unity WebGL | Phaser / Godot | Familiaridade do time com prazo de 4 semanas ([ADR-001](adr/ADR-001-engine-unity-webgl.md)) | Download mais pesado, primeiro carregamento mais lento |
| Onde ficam as regras | Núcleo em C# puro + fluxo de eventos | Lógica em MonoBehaviours | IA, simulação de balanceamento, testes, trabalho em paralelo ([ADR-002](adr/ADR-002-battle-core-separation.md)) | 1–2 dias de preparação; todo visual precisa virar evento |
| Modelo de turno | Escolha simultânea, resolvida por Velocidade | Alternância estrita | Dá sentido à Velocidade (GDD §3.3); familiar de Pokémon | O hot-seat precisa de uma tela de "passe o aparelho" |
| Onde fica o QTE | A interface mede o timing, o núcleo recebe o resultado | O núcleo simula o timing | O núcleo continua determinístico e independente da taxa de quadros | O "timing" da IA é só uma probabilidade |
| Conteúdo | ScriptableObjects | JSON / fixo no código | Ajuste no Inspector, referências a assets ([ADR-003](adr/ADR-003-data-driven-content.md)) | Conteúdo não muda sem um novo build |
| IA | Lista de regras de prioridade (do Level Design) | Pontuação de utilidade / Minimax | Segue a especificação do time de design; ajustável por oponente via dados ([ADR-004](adr/ADR-004-ai-approach.md)) | Previsível para jogadores experientes |
| Persistência | PlayerPrefs JSON em dois slots | Backend na nuvem | Zero infraestrutura para um save de ~2 KB ([ADR-005](adr/ADR-005-persistence.md)) | Sem save entre aparelhos; moedas podem ser editadas |
| Quando salvar | Só no fim da batalha e na compra da loja | Contínuo / no meio da batalha | Simples e consistente | Fechar a aba no meio da batalha perde aquela batalha |
| Ligação dos serviços | Acesso estático `Game` | Framework de DI (Zenject/VContainer) | Projeto pequeno; menos coisa para aprender | Testabilidade menor na camada de meta (o núcleo não é afetado) |

---

## 6. O que revisitar quando o sistema crescer

| Gatilho | O que revisitar |
|---|---|
| **Pacotes de moedas com dinheiro real** (Caderno p.10) | Levar carteira, compras e evoluções para um **backend com autoridade no servidor** (contas, webhooks do provedor de pagamento, validação de recibos). O `ISaveStore` vira um save na nuvem e o cliente deixa de gravar moedas. |
| **PvP online** | Graças ao núcleo determinístico, dá para trocar só os comandos (lockstep com a seed combinada antes) ou rodar o núcleo num servidor com autoridade. Adicionar matchmaking e reconexão. O QTE precisa de um design tolerante a latência, como o resultado de timing medido no cliente e conferido contra um prazo do servidor. |
| **Sistema de apostas com moedas** (pós-MVP, GDD §6) | Precisa primeiro de um design próprio. Se envolver moedas entre jogadores, só pode rodar no servidor. |
| Mais personagens (> 20) ou ajustes de balanceamento frequentes | Mover o conteúdo para JSON ou configuração remota, para mudar o balanceamento sem novo build; considerar Addressables para manter o primeiro download pequeno. |
| Lançamento mobile | Builds nativos de Android/iOS em vez de WebGL no celular; QTE pensado para toque; escala da interface. |
| Batalhas passando de 7 min com frequência | Aumentar o poder das habilidades ou reduzir a Defesa dos Tanques, guiado pelo relatório de rodadas da simulação. |
| Base de jogadores ativa | Adicionar analytics que respeitem a privacidade (duração das batalhas, taxa de vitória por personagem, abandono por fase da campanha) e relatório de crashes. |
