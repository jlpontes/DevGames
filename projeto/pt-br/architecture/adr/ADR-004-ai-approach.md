# ADR-004: IA por regras de prioridade com perfis por oponente

**Status:** Aceito
**Data:** 02/10/2026 (revisado: substitui a proposta anterior de pontuação por utilidade)
**Decisores:** Time de desenvolvimento

## Contexto
A IA é necessária para os 4 oponentes da campanha (treinadores de Fogo, Água e Planta, mais o Boss) e para o modo "contra IA" com dificuldade escolhida. A cada turno ela escolhe entre 2–3 habilidades, até 3 itens e até 2 trocas. Ela também precisa responder à janela de reação (QTE). Tudo roda na thread principal do WebGL.

O [Level Design](../../leveldesign.md) (§4) já especifica a IA como uma **lista ordenada de regras de prioridade**: sobreviver, trocar por vantagem, cuidar de status, usar o especial, atacar, mais uma chance fixa no QTE. A primeira versão deste ADR propunha pontuação por utilidade, o que não batia com a especificação do time de design.

## Decisão
Implementar a IA como uma **lista de regras de prioridade**, a mesma escrita no Level Design. A primeira regra cuja condição for verdadeira decide a ação. Todos os números das regras vêm de um **ScriptableObject `AIProfile`**, então cada oponente e cada dificuldade é um asset de dados, não código novo.

```
1. Sobrevivência     se HP do ativo < survivalHpPct e tem Kit Médico          → UseItem(MedKit)
2. Troca por vantagem se um lutador do banco tem vantagem sobre o ativo inimigo
                     e o ativo atual não tem, sorteia switchChance           → Switch(esse lutador)
3. Status            se um DoT está ativo há ≥ dotTurnsBeforeCleanse turnos  → UseItem(Medicine) senão Switch
4. Especial          se stamina = 100                                        → Attack(especial)
5. Básico            caso contrário                                          → Attack(melhor habilidade normal pelo dano esperado)
QTE                  sorteia qteChance; nos acertos, counterShare vira contra-ataque e o resto desvio
Erros                com mistakeChance, pula para a regra 5 com uma habilidade normal aleatória
```

Campos do `AIProfile`: `survivalHpPct`, `switchChance`, `dotTurnsBeforeCleanse`, `qteChance`, `counterShare`, `mistakeChance`.

| Perfil | survivalHpPct | switchChance | qteChance | counterShare | mistakeChance |
|---|---|---|---|---|---|
| Fácil (contra IA) | 15% | 30% | 20% | 0,25 | 30% |
| Normal (contra IA) | 20% | 70% | 50% | 0,25 | 10% |
| Difícil (contra IA) | 30% | 90% | 80% | 0,35 | 0% |
| Treinador de Fogo | 20% | 50% | 30% | 0,25 | 15% |
| Treinador de Água (Level Design) | 20% | 70% | 40% | 0,25 | 10% |
| Treinador de Planta | 25% | 80% | 50% | 0,30 | 5% |
| Boss | 0% (não há troca possível) | — | 60% | 0,40 | 0% |

A regra 5 usa o `DamageCalculator` do núcleo para escolher a habilidade normal com maior dano esperado, então a IA nunca copia a fórmula.

## Opções Consideradas

### Opção A: Ação legal aleatória
Complexidade baixa; parece burra; mantida só como placeholder na semana 1.

### Opção B: Lista de regras de prioridade (escolhida)
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa |
| Custo | Cerca de 1–2 dias |
| Escalabilidade | Comportamento novo = regra nova ou asset de perfil novo |
| Familiaridade do time | Alta: é a especificação do próprio time de design |

**Prós:** corresponde um a um ao Level Design, então os designers conseguem ler e mudar a IA; fácil de depurar ("a regra 2 disparou"); dificuldade e personalidade são só dados.
**Contras:** previsível para jogadores experientes; as regras podem interagir de jeitos inesperados (ex.: trocar de um lado para o outro), o que exige uma proteção.

### Opção C: Pontuação por utilidade
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa–Média |
| Custo | Cerca de 2–3 dias, mais o ajuste dos pesos |
| Escalabilidade | Equilíbrio suave entre objetivos |
| Familiaridade do time | Média |

**Prós:** joga de forma mais flexível. **Contras:** se afasta do Level Design escrito; pesos são mais difíceis de entender para os designers do que regras.

### Opção D: Minimax / MCTS
Jogo mais forte, mas mais de uma semana de trabalho, difícil de deixar "divertido" em dificuldade baixa e complicado pelo acaso e pelo QTE. Não é para o MVP.

## Análise de Trade-offs
O time de design já escreveu o comportamento da IA. Implementar exatamente isso é a opção mais barata, e os designers mantêm o controle de como os oponentes se comportam. A pontuação por utilidade jogaria um pouco melhor, mas custa mais e afasta a IA dos documentos. Como o núcleo é C# puro ([ADR-002](ADR-002-battle-core-separation.md)), trocar depois para utilidade exigiria substituir só o controlador.

## Consequências
- Fica mais fácil: cada oponente tem uma personalidade definida num asset; o comportamento da IA pode ser rastreado até um documento.
- Fica mais difícil: é preciso impedir trocas de vai e volta (proteção: nada de troca em dois turnos seguidos).
- Revisitar: se o Difícil parecer fácil nos playtests, adicionar um desempate por utilidade na regra 5 ou uma previsão de 1 jogada.

## Itens de Ação
1. [ ] `RandomController` na semana 1 como placeholder.
2. [ ] `RuleBasedAIController` com as 5 regras + QTE + sorteio de erro, e a proteção contra vai e volta.
3. [ ] Criar os 7 assets `AIProfile` a partir da tabela acima.
4. [ ] Usar a simulação de balanceamento IA contra IA para conferir que o Difícil vence o Fácil em mais de 75% dos confrontos espelhados.
