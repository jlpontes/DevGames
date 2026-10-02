# Skill `/architecture` no Claude Code (terminal)

Guia para instalar e usar a skill **architecture** do plugin **engineering** (Anthropic) no Claude Code, a CLI do Claude no terminal.

---

## 1. O que a skill faz

A `/architecture` ajuda a **tomar e documentar decisões de arquitetura**. Ela trabalha em três modos:

| Modo | Exemplo de pedido | Resultado |
|---|---|---|
| **Criar um ADR** | "Kafka ou SQS para nosso event bus?" | Architecture Decision Record com opções, trade-offs e consequências |
| **Avaliar um design** | "Revise esta proposta de microserviços" | Análise crítica de uma proposta existente |
| **Desenhar um sistema** | "Desenhe o sistema de notificações do app" | Proposta de arquitetura a partir de requisitos e restrições |

Para análises mais profundas, ela se apoia na skill irmã **system-design** (levantamento de requisitos, escalabilidade, trade-offs), que o Claude carrega automaticamente quando é relevante.

---

## 2. Instalação

### Opção A — Instalar o plugin completo (recomendado)

Instala a `/architecture` junto com as outras skills de engenharia (`/standup`, `/debug`, `/code-review`, `/incident-response`, `/deploy-checklist` etc.).

```bash
# Adiciona o marketplace de plugins da Anthropic
claude plugin marketplace add anthropics/knowledge-work-plugins

# Instala o plugin engineering
claude plugin install engineering@knowledge-work-plugins
```

Também dá para fazer de dentro de uma sessão interativa:

```
/plugin marketplace add anthropics/knowledge-work-plugins
/plugin install engineering@knowledge-work-plugins
```

Reinicie o `claude` (ou abra uma nova sessão) para as skills aparecerem.

### Opção B — Só esta skill, como skill pessoal

Se quiser apenas a `/architecture`, crie o arquivo manualmente:

```bash
mkdir -p ~/.claude/skills/architecture
$EDITOR ~/.claude/skills/architecture/SKILL.md
```

Cole o conteúdo da [seção 6](#6-conteúdo-do-skillmd). Para limitar a um projeto, use `.claude/skills/architecture/SKILL.md` na raiz do repositório (e faça commit para o time ter acesso).

---

## 3. Como rodar

Abra o Claude Code na pasta do projeto:

```bash
cd ~/meu-projeto
claude
```

E chame a skill:

| Instalação | Comando |
|---|---|
| Plugin (opção A) | `/engineering:architecture <decisão ou sistema>` |
| Skill pessoal (opção B) | `/architecture <decisão ou sistema>` |

Digite `/` no prompt para ver a lista de skills disponíveis e conferir o nome exato.

### Exemplos

```text
/engineering:architecture Devemos usar PostgreSQL ou DynamoDB para o serviço de pedidos? Precisamos de 5K writes/s, time conhece SQL, entrega em 6 semanas.

/engineering:architecture Revise o design em docs/proposta-microservicos.md

/engineering:architecture Desenhe o sistema de notificações (push, e-mail, SMS) para 2M usuários, latência < 5s
```

### Modo não interativo (scripts / CI)

```bash
claude -p "/engineering:architecture Redis vs Memcached para cache de sessão" > docs/adr/ADR-007-cache.md
```

### Invocação automática

Você não precisa digitar o comando: se escrever algo como *"me ajuda a decidir entre gRPC e REST para a API interna"*, o Claude reconhece pela descrição da skill e a usa sozinho.

---

## 4. Formato de saída (ADR)

```markdown
# ADR-[número]: [Título]

**Status:** Proposed | Accepted | Deprecated | Superseded
**Date:** [Data]
**Deciders:** [Quem precisa aprovar]

## Context
[Qual é a situação? Que forças estão em jogo?]

## Decision
[Qual mudança estamos propondo?]

## Options Considered

### Option A: [Nome]
| Dimension | Assessment |
|-----------|------------|
| Complexity | [Low/Med/High] |
| Cost | [Avaliação] |
| Scalability | [Avaliação] |
| Team familiarity | [Avaliação] |

**Pros:** [Lista]
**Cons:** [Lista]

### Option B: [Nome]
[Mesmo formato]

## Trade-off Analysis
[Principais trade-offs com raciocínio claro]

## Consequences
- [O que fica mais fácil]
- [O que fica mais difícil]
- [O que precisaremos revisitar]

## Action Items
1. [ ] [Passo de implementação]
2. [ ] [Follow-up]
```

Dica: peça explicitamente para salvar — *"salve em docs/adr/ADR-012-event-bus.md"* — e o Claude Code grava o arquivo no repositório.

---

## 5. Turbinando com conectores (MCP)

A skill funciona sozinha, mas fica melhor com servidores MCP conectados:

| Categoria | Exemplos | O que a skill passa a fazer |
|---|---|---|
| Knowledge base | Notion, Confluence | Buscar ADRs e design docs anteriores como contexto |
| Project tracker | Linear, Jira, Asana | Linkar épicos/tickets e criar as tarefas de implementação |

Adicionar um MCP no Claude Code:

```bash
claude mcp add --transport http notion https://mcp.notion.com/mcp
claude mcp add --transport http linear https://mcp.linear.app/mcp
```

Use `/mcp` dentro da sessão para autenticar e ver o status.

### Dicas para respostas melhores

1. **Declare restrições logo de início** — prazo, carga ("10K rps"), orçamento.
2. **Nomeie as opções** — mesmo que já tenha preferência, a análise fica mais equilibrada.
3. **Inclua requisitos não funcionais** — latência, custo, experiência do time e manutenção pesam tanto quanto features.
4. **Aponte arquivos do repo** — o Claude Code lê o código e os docs locais para embasar a decisão.

---

## 6. Conteúdo do `SKILL.md`

Use este conteúdo na opção B (`~/.claude/skills/architecture/SKILL.md`):

````markdown
---
name: architecture
description: Create or evaluate an architecture decision record (ADR). Use when choosing between technologies (e.g., Kafka vs SQS), documenting a design decision with trade-offs and consequences, reviewing a system design proposal, or designing a new component from requirements and constraints.
argument-hint: "<decision or system to design>"
---

# /architecture

Create an Architecture Decision Record (ADR) or evaluate a system design.

## Usage

```
/architecture $ARGUMENTS
```

## Modes

**Create an ADR**: "Should we use Kafka or SQS for our event bus?"
**Evaluate a design**: "Review this microservices proposal"
**System design**: "Design the notification system for our app"

## Output — ADR Format

```markdown
# ADR-[number]: [Title]

**Status:** Proposed | Accepted | Deprecated | Superseded
**Date:** [Date]
**Deciders:** [Who needs to sign off]

## Context
[What is the situation? What forces are at play?]

## Decision
[What is the change we're proposing?]

## Options Considered

### Option A: [Name]
| Dimension | Assessment |
|-----------|------------|
| Complexity | [Low/Med/High] |
| Cost | [Assessment] |
| Scalability | [Assessment] |
| Team familiarity | [Assessment] |

**Pros:** [List]
**Cons:** [List]

### Option B: [Name]
[Same format]

## Trade-off Analysis
[Key trade-offs between options with clear reasoning]

## Consequences
- [What becomes easier]
- [What becomes harder]
- [What we'll need to revisit]

## Action Items
1. [ ] [Implementation step]
2. [ ] [Follow-up]
```

## If Connectors Available

If a knowledge base (Notion, Confluence) is connected:
- Search for prior ADRs and design docs
- Find relevant technical context

If a project tracker (Linear, Jira, Asana) is connected:
- Link to related epics and tickets
- Create implementation tasks

## Tips

1. **State constraints upfront** — "We need to ship in 2 weeks" or "Must handle 10K rps" shapes the answer.
2. **Name your options** — Even if you're leaning one way, give a balanced analysis with explicit alternatives.
3. **Include non-functional requirements** — Latency, cost, team expertise, and maintenance burden matter as much as features.
````

> `$ARGUMENTS` é substituído pelo texto que você digita depois do comando.

---

## 7. Solução de problemas

| Problema | Solução |
|---|---|
| Comando não aparece ao digitar `/` | Reinicie o `claude`; confira `claude plugin list` ou se o arquivo está em `~/.claude/skills/architecture/SKILL.md` |
| `Unknown command /architecture` | Instalado via plugin? Use o prefixo: `/engineering:architecture` |
| Conflito de nome com outra skill | Use o nome com prefixo do plugin |
| Conectores não respondem | Rode `/mcp` na sessão e autentique o servidor |
| Atualizar o plugin | `claude plugin marketplace update knowledge-work-plugins` e reinstale |
