# ADR-003: ScriptableObjects para o conteúdo do jogo

**Status:** Proposto
**Data:** 02/10/2026
**Decisores:** Time de desenvolvimento

## Contexto
O jogo tem 9 personagens mais um boss, cerca de 30 habilidades, 4 itens, cerca de 6 efeitos, 4 arenas, 4 oponentes e os números da economia. Tudo isso vai ser rebalanceado com frequência nas semanas 2–3. Designers e pessoas que não programam precisam conseguir mudar valores sem mexer no código.

## Decisão
Cadastrar todo o conteúdo como **assets ScriptableObject** (`CharacterDef`, `SkillDef`, `EffectDef`, `ItemDef`, `ArenaDef`, `OpponentDef`, `AIProfile`, `EffectivenessTable`, `EconomyConfig`). Um asset `ContentDatabase` lista todos eles. O `BattleLauncher` converte esses assets em registros imutáveis do núcleo antes de cada batalha, assim o núcleo nunca toca em tipos do Unity.

## Opções Consideradas

### Opção A: Constantes fixas no código C#
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa |
| Custo | Todo ajuste exige um programador e uma recompilação |
| Escalabilidade | Ruim |
| Familiaridade do time | Alta |

**Prós:** o mais simples. **Contras:** trava os designers; conflitos de merge em arquivos compartilhados.

### Opção B: ScriptableObjects (escolhida)
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Baixa–Média |
| Custo | Pequeno |
| Escalabilidade | Boa para este tamanho |
| Familiaridade do time | Alta (nativo do Unity) |

**Prós:** edição no Inspector; referência direta a sprites e áudios; um arquivo por asset significa menos conflitos de merge; valores validados no `OnValidate`.
**Contras:** os valores só aparecem como texto puro no code review se a serialização Force Text estiver ligada (é o padrão).

### Opção C: Arquivos JSON/CSV
| Dimensão | Avaliação |
|-----------|------------|
| Complexidade | Média (parser, referências a assets por id) |
| Custo | Médio |
| Escalabilidade | Boa; uma planilha pode alimentar |
| Familiaridade do time | Média |

**Prós:** fácil de editar numa planilha. **Contras:** sprites e áudios precisam ser ligados por id em texto; mais código para escrever em 4 semanas.

## Análise de Trade-offs
ScriptableObjects dão edição amigável para designers quase sem código extra. JSON só valeria a pena com um backend de operação ao vivo, que está fora do escopo ([ADR-005](ADR-005-persistence.md)).

## Consequências
- Fica mais fácil: balanceamento, adicionar personagens e trabalhar no conteúdo em paralelo.
- Fica mais difícil: o save precisa se referir ao conteúdo por um `id` estável em texto, nunca por referência de asset ou nome.
- Revisitar: se surgir conteúdo pago ou balanceamento remoto, exportar os SOs para JSON servido por um backend.

## Itens de Ação
1. [ ] Definir as classes SO com um campo `id` e checagens no `OnValidate` (atributos dentro de 0–100 onde o GDD limita).
2. [ ] Criar os 9 personagens com a tabela de atributos do GDD §4.
3. [ ] Adicionar um `ContentDatabase` com busca por id.
