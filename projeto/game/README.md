# Combo Tactics — projeto Unity (MVP)

Versão jogável do Combo Tactics seguindo o [GDD](../pt-br/GDD.md), a [arquitetura](../pt-br/architecture/ARCHITECTURE.md) e os ADRs. Unity 6, C# 9, interface em uGUI montada por código, sem backend.

## Como abrir e jogar

1. No Unity Hub: **Add > Add project from disk** e escolha esta pasta (`projeto/game`).
2. O projeto está fixado em `6000.0.23f1`. Se a sua versão do Unity 6 for outra, o Hub pergunta qual usar: escolha a que estiver instalada e confirme.
3. Abra a cena `Assets/_Project/Scenes/Boot.unity` e aperte **Play**. O jogo se monta sozinho em qualquer cena: o `GameRoot` usa `RuntimeInitializeOnLoadMethod`, então não há prefabs nem objetos para arrastar.

O menu **Combo Tactics > Configurar projeto** refaz a configuração (cena Boot no Build Settings, opções de WebGL e importação dos sprites) se algo se perder.

Para o build web: **File > Build Profiles > Web > Build**. O `ProjectSetup` já deixa Brotli com *Decompression Fallback* ligado (funciona no itch.io e no GitHub Pages).

## O que está implementado

| Área | Situação |
|---|---|
| Batalha | Atacar, usar item, trocar; ordem por Velocidade com troca primeiro; dano com Defesa, vantagens de elemento e classe (R1) e arena |
| Stamina e especial | Barra por jogador, +20 / +15 / +25, zera ao usar o especial (R7) |
| Janela de resposta (QTE) | Marcador cruza a barra; perfeito (±60 ms) = contra-ataque, bom (±150 ms) = desvio (R3). Clique ou Espaço |
| Efeitos | Chamas, Afogamento, Veneno, Regeneração, Ataque +10%, Defesa +10%; 3 turnos; continuam no banco |
| Arena | Começa na arena do oponente (campanha) ou sorteada; roleta a cada 3 rodadas (R4, R5) |
| IA | Regras de prioridade do Level Design, um perfil por dificuldade, guardião e missão; contra especiais a IA nunca contra-ataca |
| Modo História | A história do Caderno: abertura, escolha da afinidade (R11), 3 guardiões e o Guardião do Núcleo com 2ª fase, cartões e falas em batalha (R12), epílogo |
| Espectro (R11) | Começa com o trio da afinidade; cada guardião vencido traz o trio dele. Vale em todos os modos |
| Missões (R13) | 9 "Ecos do Cristal" em três levas, com regras próprias; a primeira vitória recruta o lutador |
| Jornada (R14) | HP que continua entre as fases, descanso de quem fica de fora, Altar de cura por moedas |
| Modos | Campanha (4 fases), missões, contra IA (Fácil, Normal, Difícil), PvP local com "passe o aparelho" e Modo Justo (R6) |
| Meta | Moedas (100 / 50 / 500), loja de itens, evoluções de +3% até 5 níveis, save local em dois slots |
| Arte | Fundos de arena e menu do projeto; sprites dos 9 lutadores e do Boss em `Resources/Art/Characters`. Sem áudio |

## Estrutura

```
Assets/_Project/
  Resources/Art/          fundos de arena e do menu
  Resources/Art/Characters/   um sprite por personagem, com o id dele no nome (fire_mage.png)
  Scenes/Boot.unity       cena vazia de entrada
  Scripts/
    Core/                 regras de batalha em C# puro (sem UnityEngine)      ComboTactics.Core
    Game/                 conteúdo, roteiro, perfil, jornada, missões, loja, save (C# puro)    ComboTactics.Game
    Presentation/         telas, batalha, entrada do jogador (Unity)          ComboTactics.UI
    Editor/               configuração automática do projeto
    Tests/                testes EditMode e simulação de balanceamento
```

As dependências apontam só para baixo: `Presentation → Game → Core`. `Core` e `Game` têm `noEngineReferences: true`, então um `using UnityEngine;` ali não compila.

O fluxo de uma batalha: o `BattleDirector` pergunta aos controladores o que o motor espera (`Pending`), envia a resposta ao `BattleEngine` e toca os `BattleEvent`s devolvidos, um por vez, na `BattleScreen`. A tela desenha a partir de um `BattleSnapshot` atualizado evento a evento e nunca lê as regras.

## Testes

**Window > General > Test Runner > EditMode > Run All.** São 132 testes: fórmula de dano com os exemplos do GDD (135 e 29), regras R1–R14, cada regra da IA, afinidade e espectro, missões, HP da jornada, 2ª fase do guardião, roteiro, loja, save (inclusive a migração da versão 1) e a simulação de balanceamento.

O teste `PrintBalanceReport` roda milhares de batalhas IA contra IA e mostra a matriz 9×9 de duelos, a duração média das batalhas e a jornada completa por afinidade (`CampaignSimulation`). Rode depois de mexer em atributos, habilidades, guardiões, missões ou perfis de IA.

Situação da validação: o MVP abre e roda no Unity `6000.0.23f1` (conferido pelo time em 11/11/2026). `Core`, `Game` e os testes compilam e passam fora do Unity (132 de 132), e a camada `Presentation` é exercitada por um roteiro automático sobre uma simulação da API do Unity: abertura, escolha de afinidade, missão, Ato I, 2ª fase do Guardião do Núcleo, epílogo, contra IA com espectro, save e reset. As telas novas (afinidade e quadro de missões) ainda precisam ser vistas no editor. Ainda falta um build WebGL testado nos navegadores.

## Pontos de extensão

| Para mudar | Onde |
|---|---|
| Números de regra (multiplicadores, stamina, intervalo da arena) | `Core/Model/BattleRules.cs` |
| Personagens, habilidades, itens, oponentes, perfis de IA | `Game/Content/BuiltInContent.cs`. Outra fonte (ScriptableObjects, JSON) só precisa devolver um `ContentDatabase` |
| Preços e recompensas | `EconomyConfig` em `Game/Content/Defs.cs` |
| Onde o save é gravado | `ISaveStore` (hoje `TwoSlotSaveStore` sobre `PlayerPrefs`); a troca é feita em `GameRoot.Awake` |
| Quem joga um lado (outra IA, rede) | `IBattleController` |
| Como a batalha é desenhada (sprites, VFX, câmera) | `IBattleView`, implementada hoje por `BattleScreen` |
| Áudio, câmera e analytics reagindo à batalha | evento `BattleDirector.EventPlayed` |
| Arte dos lutadores e fundos | `Presentation/UI/Art.cs`. Para trocar um sprite, substitua o PNG em `Resources/Art/Characters`; sem o arquivo, o jogo desenha uma forma colorida no lugar |
| Timing do QTE | `QteConfig` em `Presentation/Battle/BattleWidgets.cs` |
| Cores e textos de interface | `Presentation/UI/Theme.cs` |

Erros: regras violadas pelo jogador lançam `GameException` e viram um aviso na tela. Qualquer outra exceção cai no `ErrorReporter`, que abre um diálogo com a seed e o log de comandos da batalha (reproduzem a partida).

## Valores que os documentos deixavam em aberto

Estes números são pontos de partida escolhidos para o MVP e validados pela simulação. Revisem com o time.

| Tema | Valor adotado |
|---|---|
| Habilidades | Cada personagem: golpe de poder 100; golpe de poder 80–85 com efeito; especial de poder 200 com efeito garantido |
| Efeitos nas habilidades | Guerreiros e Tanques: bônus próprio de Ataque ou Defesa no segundo golpe. Magos: 50% de chance de DoT no segundo golpe. Especiais aplicam o DoT do elemento; o do Guerreiro de Planta aplica Regeneração |
| DoT | 20% do Ataque de quem aplicou, por turno; ignora Defesa e vantagens |
| Regeneração | 4% do HP máximo por turno |
| Boss "Titã Lunar" | HP 2600, Ataque 105, Defesa 50, Velocidade 45, sem elemento nem classe |
| Preços | Kit Médico 60, Remédio 40, Poções 50; evolução custa 50 × (nível + 1) |
| Itens dos oponentes | Fogo: 1 Kit; Água: 1 Kit e 1 Remédio; Planta: 2 Kits e 1 Remédio; Boss: 1 Remédio |
| Contra IA | A IA sorteia 3 personagens com atributos base e leva 1 Kit e 1 Remédio |
| PvP local | Não rende moedas. Com o Modo Justo desligado, os dois lados usam as evoluções do perfil |
| Stamina no contra-ataque | Quem contra-ataca ganha os +25 da reação, sem somar +20 pelo dano |
| Efeito em golpe desviado | Não é aplicado, nem o bônus próprio |
| Empate | Se os dois últimos lutadores caem no mesmo fim de turno, a batalha empata e paga como derrota |

## O que a simulação mostra

- **Equilíbrio dos 9 personagens:** com QTE de 50% para os dois lados, cada personagem vence entre 48% e 53% dos seus duelos. Nos 9 confrontos em que as vantagens se anulam (ex.: Guerreiro de Fogo contra Mago de Água), ninguém passa de 60%.
- **Duração das batalhas 3v3** (média de rodadas): 28 sem QTE, 33 no Fácil, 45 no Normal, 62 no Difícil. O alerta do NEXT-STEPS é 40. O que alonga a partida é a chance de desvio, não o duelo de Tanques.
- **O QTE decide quase tudo:** a IA Difícil (80% de acerto) vence a Fácil (20%) em quase todas as batalhas simuladas.
- **Jornada completa** (`CampaignSimulation`: um jogador médio com 55% de acerto no QTE, sem comprar evoluções, até 6 tentativas por fase): com as missões, mais de 90% das jornadas terminam com qualquer afinidade; a 1ª tentativa vence em cerca de 85%, 55%, 45% e 50% das vezes nas quatro fases. Sem missões, a afinidade Fogo termina só um terço das jornadas, porque o 2º guardião (Água) tem vantagem sobre todo o espectro inicial. As missões são o que abre caminho.

## Limitações conhecidas

- **Origem da arte:** a arte foi refeita em 11/11/2026 usando as referências só como inspiração. O Mago de Planta ainda lembra bastante uma das referências (chapéu de cogumelo e túnica verde); vale uma revisão antes de publicar.
- Textos com símbolos (★, ▶, ⚡) usam a fonte padrão do Unity; no WebGL, que não tem fontes do sistema para cair de volta, eles podem aparecer como quadrados. Conferir no primeiro build web.
- Sem áudio.
- Sem build WebGL testado ainda.
- Evolução de 3% sobre um atributo baixo pode não mudar o valor: a Defesa 15 do Mago de Fogo continua 15 no nível 1.
- A regra 3 da IA troca de lutador quando não tem Remédio, como está no Level Design, mas os efeitos acompanham o lutador no banco, então a troca não livra do DoT.
- Fechar a aba no meio da batalha perde a batalha (o perfil só é gravado no fim, como previsto no System Design).
- O conteúdo está em C#, não em ScriptableObjects (ADR-003): assim a simulação roda fora do Unity. Migrar é preencher o mesmo `ContentDatabase` a partir de assets.

## Fase 2 (sugestões)

Técnicas:

- Publicar um build WebGL de teste no itch.io e conferir no Chrome, Firefox e Safari.
- Pipeline de CI (GameCI) rodando os testes EditMode e gerando o build a cada push.
- Conteúdo em ScriptableObjects com validação no editor (ADR-003).
- Áudio e VFX ligados ao `BattleDirector.EventPlayed`; arte própria e animações dos lutadores.
- Painel de debug nos builds de desenvolvimento: adicionar moedas, forçar arena, forçar resultado do QTE.
- Atributos fracionários (ou evolução mínima de +1) para toda evolução comprada ter efeito.
- Localização: os textos estão em português dentro do código.

De jogo e negócio:

- Rever o peso do QTE: reduzir as chances da IA, limitar o desvio a parte do dano, ou encurtar as janelas. É o maior fator de duração e de dificuldade.
- Level design das fases de Fogo, Planta e Boss, e atributos finais do Boss.
- Mais missões (uma segunda rodada de ecos depois do epílogo) e conquistas por missão cumprida com objetivos extras.
- Playtest com pessoas de fora do time para calibrar os tempos do QTE e os preços da loja.
- Depois do MVP: sistema de apostas, PvP online e save na nuvem (pedem backend; o motor determinístico com seed já permite validar batalhas no servidor).
