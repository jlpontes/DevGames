# Combo Tactics — Próximos Passos

**Atualizado em:** 02/10/2026
**Lançamento previsto:** novembro de 2026 (cerca de 4 semanas)
**Time:** 3–5 devs · **Stack:** Unity WebGL, sem backend

---

## 1. Onde estamos

| Área | Situação |
|---|---|
| Design de jogo (GDD, caderno, mecânicas) | Completo. As lacunas de regra foram fechadas em [GDD §5.7](pt-br/GDD.md#57-esclarecimentos-de-regras) (R1–R10) e o escopo do MVP está em [GDD §6](pt-br/GDD.md#6-escopo-do-mvp) |
| Level design | 1 de 4 fases documentada (Treinador de Água), igual em en e pt-br |
| Arquitetura | [ARCHITECTURE.md](pt-br/architecture/ARCHITECTURE.md), [SYSTEM-DESIGN.md](pt-br/architecture/SYSTEM-DESIGN.md), 5 ADRs, em pt-br e en. Ainda na branch `docs/architecture` |
| Implementação | Nada ainda: sem projeto Unity |
| Arte e áudio | Logo, menu e 4 arenas prontos. **Faltam os 9 personagens, o Boss e todos os sons** |

### Incoerências resolvidas em 02/10/2026

| Incoerência | Resolução |
|---|---|
| `en/leveldesign.md` tinha perdido conteúdo na tradução (23 linhas contra 62 em pt-br) | Reescrito com o conteúdo completo; en e pt-br agora são equivalentes |
| A IA do ADR-004 (pontuação) não seguia as regras de prioridade do level design | ADR-004 reescrito: IA por regras de prioridade, com um `AIProfile` por oponente e por dificuldade |
| Level design pedia "pelo menos 2 de Água" no time do treinador; o caderno diz equipes exclusivas por elemento | Time do Treinador de Água = os 3 personagens de Água (R10) |
| Level design começava na arena Mar, a arquitetura começava neutra (e não existe arte de arena neutra) | Toda batalha começa numa arena: a do oponente na campanha, sorteada nos outros modos (R4) |
| GDD (en) dizia que o efeito da arena vale só na rodada do sorteio | Vale até o próximo sorteio (R4), em en e pt-br |
| Sistema de apostas listado como diferencial, mas sem design | No MVP, o elemento de gambling é o sorteio de arena. Apostas com moedas ficam para depois do MVP (GDD §1 e §6) |
| Caderno previa pacotes de moedas pagos; a arquitetura não tem backend | Caderno p.10 atualizado: sem compras no MVP; pacotes de moedas só depois, com contas e servidor |
| GDD (en) não tinha as recompensas de derrota e do Boss | Adicionado: vitória 100, derrota 50, Boss 500 |
| Pasta `architecture` só existia em inglês | Traduzida para `pt-br/architecture` (ARCHITECTURE, SYSTEM-DESIGN e os 5 ADRs) |
| Perguntas Q1–Q9 sem resposta | Respondidas como R1–R9 no GDD; o `ARCHITECTURE.md` §7 aponta para elas |
| `projeto/README.md` era uma cópia do caderno com imagens quebradas | Virou a página inicial do projeto, com links para todos os documentos |

---

## 2. Próximos passos

### Fase 0 — Preparação (até 06/10)

- [ ] **Revisar com o time as regras R1–R10** do GDD §5.7. São decisões tomadas para destravar o código; se alguém discordar, mudar agora custa pouco.
- [ ] Abrir o Pull Request da branch `docs/architecture` e fazer o merge na `main`.
- [ ] Limpeza do repositório:
  - [ ] apagar a pasta `DevGames/` na raiz (clone antigo e desatualizado, com `.git` próprio);
  - [ ] adicionar `.gitignore` (macOS + Unity) e tirar o `.DS_Store` do versionamento;
- [ ] Definir dono de cada área (tabela da seção 3).

### Semana 1 (05–09/10) — Esqueleto jogável

- [ ] Criar o projeto Unity com as 3 assemblies (Core / Game / UI) — [ADR-002](pt-br/architecture/adr/ADR-002-battle-core-separation.md).
- [ ] Escrever e **congelar** os contratos: `ICommand`, `BattleEvent`, `IBattleEngine`, `IBattleController` ([SYSTEM-DESIGN §2.4](pt-br/architecture/SYSTEM-DESIGN.md)).
- [ ] `DamageCalculator` com testes (incluindo o exemplo do GDD: Mago de Fogo contra Tanque de Planta = 135).
- [ ] Batalha mínima: Menu → Batalha com 2 placeholders, só ataque, `RandomController` como IA.
- [ ] **Build WebGL publicado** (itch.io e GitHub Pages) e testado no Chrome, Firefox e Safari.
- [ ] Arte: sprites placeholder dos 9 personagens (formas coloridas por elemento e classe).

### Semana 2 (12–16/10) — Regras completas

- [ ] Itens, troca, efeitos (incluindo no banco), stamina e especial, QTE, sorteio de arena.
- [ ] ScriptableObjects dos 9 personagens com os atributos do GDD §4; habilidades e efeitos.
- [ ] `RuleBasedAIController` + os 7 `AIProfile` ([ADR-004](pt-br/architecture/adr/ADR-004-ai-approach.md)).
- [ ] Loja e save local ([ADR-005](pt-br/architecture/adr/ADR-005-persistence.md)).
- [ ] **Simulação de balanceamento:** matriz de vitórias 9×9 e média de rodadas (alerta se passar de 40 rodadas: risco de Tanque contra Tanque).

### Semana 3 (19–23/10) — Conteúdo e modos

- [ ] Campanha com 4 fases (Fogo, Água, Planta, Boss), desbloqueio e recompensas.
- [ ] PvP local com tela de "passe o aparelho" e Modo Justo.
- [ ] Modo vs IA com escolha de dificuldade.
- [ ] Arte e sons finais entrando no lugar dos placeholders; VFX e câmera no especial.
- [ ] **Congelamento de funcionalidades no fim da semana.**

### Semana 4 (26–30/10) — Polimento e entrega

- [ ] Correção de bugs, teste nos navegadores, tamanho do build (≤ 30 MB) e tempo de carregamento.
- [ ] Playtest com pessoas de fora do time; ajuste de números.
- [ ] Publicar a versão final e preparar a apresentação.

---

## 3. Divisão sugerida

| Dono | Área |
|---|---|
| Dev A | Core de batalha, testes e simulação de balanceamento |
| Dev B | Apresentação da batalha (BattleDirector, HUD, QTE, roleta, animações) |
| Dev C | Meta: menus, seleção de time, loja, campanha, save |
| Dev D (opcional) | IA e cadastro de conteúdo (personagens, habilidades, oponentes) |
| Dev E (opcional) | Integração de arte e áudio, build WebGL e publicação |

---

## 4. Ainda em aberto (não bloqueia a semana 1)

| Item | Precisa ser decidido até | Sugestão |
|---|---|---|
| Preços da loja (itens e evoluções) | Semana 2 | Itens 40–60 moedas; evolução custa 50 × (nível + 1). Com 100 moedas por vitória, uma evolução custa de 1 a 5 batalhas |
| Atributos, habilidades e arte do Boss | Semana 2 | Personagem próprio, cerca de 3× o HP (≈ 2.600), arena Lua (R9) |
| Level design das fases de Fogo, Planta e Boss | Semana 2 | Copiar o modelo da fase de Água, mudando time, arena, bônus de atributos e `AIProfile` |
| Critérios de avaliação da disciplina | Agora | Ajustar os entregáveis abaixo a eles |

---

## 5. Entregáveis

| Nível | Conteúdo | Prazo |
|---|---|---|
| **Mínimo (garantido)** | Build WebGL jogável: batalha completa (ataque, item, troca, stamina e especial, QTE, efeitos, arena), modo vs IA, 9 personagens (arte placeholder aceitável). Documentação: GDD, caderno, level design, arquitetura, system design, ADRs | Fim da semana 3 |
| **Alvo** | Mínimo + campanha com 4 fases, loja com save, PvP local, arte e sons finais, relatório de balanceamento | Fim da semana 4 |
| **Depois do MVP** | Pacotes de moedas pagos, contas e save na nuvem, PvP online, sistema de apostas | Pós-lançamento |

**Se o prazo apertar, cortar nesta ordem:**
1. PvP local
2. Boss
3. VFX e câmera
4. Loja de evoluções

O núcleo de batalha contra a IA com os 9 personagens não se corta.

---

## 6. Riscos

| Risco | Impacto | Mitigação |
|---|---|---|
| Arte e som dos personagens ainda não existem | Alto | Placeholders na semana 1; arte final entra só na semana 3 |
| Nenhuma linha de código com 4 semanas pela frente | Alto | Esqueleto jogável e build web já na semana 1; congelar funcionalidades no fim da semana 3 |
| Partidas longas (Tanque contra Tanque: 18–23 golpes) | Médio | A simulação de balanceamento mede as rodadas; ajustar o poder das habilidades |
| WebGL lento ou com falhas no Safari | Médio | Build publicado e testado desde a semana 1 |
| Regras R1–R10 contestadas depois do código pronto | Médio | Revisão com o time na Fase 0 |
