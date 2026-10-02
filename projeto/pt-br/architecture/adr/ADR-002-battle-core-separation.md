# ADR-002: Núcleo de batalha em C# puro com apresentação por eventos

**Status:** Proposto
**Data:** 02/10/2026
**Decisores:** Time de desenvolvimento

## Contexto
As batalhas são o jogo. Elas envolvem fórmulas de dano, dois ciclos de vantagem, efeitos de 3 turnos que acompanham os lutadores no banco, stamina, especiais, uma janela de reação (QTE) e um sorteio de arena a cada 3 rodadas. As mesmas regras precisam servir:
- ao jogador contra a **IA** (a IA precisa avaliar jogadas);
- ao **PvP local** (duas pessoas);
- ao **ajuste de balanceamento**: o GDD afirma que os atributos "foram balanceados por simulação".

Vários desenvolvedores vão trabalhar nas regras e no visual ao mesmo tempo.

## Decisão
Colocar todas as regras de batalha num **assembly C# puro (`ComboTactics.Core`, `noEngineReferences: true`)**. Ele é uma máquina de estados por etapas que recebe `ICommand`s e devolve uma lista ordenada de `BattleEvent`s. Um único MonoBehaviour, o `BattleDirector`, toca esses eventos como animações. O QTE é modelado como uma entrada pendente (`SubmitReaction`), não como um timer dentro das regras.

## Opções Consideradas

### Opção A: Lógica dentro de MonoBehaviours
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa no começo, cresce rápido |
| Custo | O mais rápido para o primeiro protótipo |
| Escalabilidade | Ruim: as regras se misturam com coroutines e tempo de animação |
| Familiaridade do time | Alta |

**Prós:** a abordagem típica dos tutoriais de Unity; primeira luta sai rápido.
**Contras:** impossível rodar 10.000 batalhas simuladas; a IA não consegue olhar adiante; animações e regras travam o progresso uma da outra; bugs difíceis de reproduzir.

### Opção B: Núcleo em C# puro + reprodução de eventos (escolhida)
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Média: exige um contrato claro de comandos e eventos |
| Custo | Cerca de 1–2 dias a mais na semana 1 |
| Escalabilidade | Boa: IA, simulação e testes reaproveitam o núcleo |
| Familiaridade do time | Média (C# comum) |

**Prós:** testes unitários EditMode em milissegundos; simulação de balanceamento; RNG com seed dá relatórios de bug reproduzíveis; quem faz núcleo e quem faz interface trabalham em paralelo sobre um contrato fixo; um servidor poderia, no futuro, rodar a batalha de novo para validá-la.
**Contras:** alguém precisa ser dono do contrato e protegê-lo; algumas conversões de dados são necessárias (ScriptableObject → spec do núcleo).

## Análise de Trade-offs
A opção B custa um pouco no começo e economiza muito nas semanas 2–4, que é justamente quando IA, balanceamento e correção de bugs acontecem juntos. O custo é basicamente definir cerca de 15 tipos de evento e 3 comandos, e isso também funciona como ponto de coordenação do time.

## Consequências
- Fica mais fácil: testes, IA, balanceamento, divisão do trabalho e reprodução de bugs a partir de uma seed.
- Fica mais difícil: tudo o que aparece na tela precisa ser expresso como evento; o visual não pode "espiar" as regras.
- Revisitar: se os eventos passarem de cerca de 25 tipos, agrupá-los ou criar eventos genéricos como `StatChanged`.

## Itens de Ação
1. [ ] Criar os 3 asmdefs (Core / Game / UI) com referências num só sentido.
2. [ ] Escrever e congelar `ICommand`, `BattleEvent` e `PendingInput` na semana 1.
3. [ ] Implementar o `DamageCalculator` com testes cobrindo o exemplo do GDD (Mago de Fogo contra Tanque de Planta).
4. [ ] Adicionar a `BalanceSimulation` (todos os 9×9 duelos, N seeds) como teste EditMode que imprime uma matriz de taxa de vitória.
