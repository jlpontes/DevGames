# A skill `system-design` no Claude Code

Guia para instalar e usar a skill **system-design** do plugin **engineering** (Anthropic) no Claude Code, a CLI do Claude.

---

## 1. O que ela faz

A `system-design` ajuda a **desenhar sistemas, serviços e arquiteturas** e a avaliar decisões de arquitetura. Ela também cobre design de API, modelagem de dados e fronteiras entre serviços.

Ela ativa sozinha quando você diz coisas como:

- "desenhe um sistema para ..."
- "como devemos arquitetar ..."
- "system design para ..."
- "qual a arquitetura certa para ..."

A skill irmã, `/architecture`, é usada quando o objetivo é *registrar uma decisão* como ADR (Architecture Decision Record). A `system-design` é o framework para *desenhar o sistema em si*, e a `/architecture` recorre a ela para análises mais profundas.

---

## 2. O framework de cinco passos

### 1. Levantamento de requisitos
- **Funcionais:** o que o sistema faz.
- **Não funcionais:** escala, latência, disponibilidade, custo.
- **Restrições:** tamanho do time, prazo, stack existente.

### 2. Visão geral
- Diagrama de componentes
- Fluxo de dados
- Contratos de API
- Escolhas de armazenamento

### 3. Detalhamento
- Modelo de dados
- Design dos endpoints (REST, GraphQL, gRPC)
- Estratégia de cache
- Design de filas/eventos
- Tratamento de erros e lógica de nova tentativa

### 4. Escala e confiabilidade
- Estimativa de carga
- Escala horizontal x vertical
- Failover e redundância
- Monitoramento e alertas

### 5. Análise de trade-offs
Toda decisão tem trade-offs, e a skill os deixa explícitos. Ela pesa complexidade, custo, familiaridade do time, tempo até o lançamento e manutenção.

---

## 3. Resultado

A skill produz um documento de design estruturado contendo:

- Diagramas (em ASCII ou descritos)
- Suposições explícitas
- Análise de trade-offs
- Uma lista do que **revisitar quando o sistema crescer**

---

## 4. Instalação

### Opção A: o plugin completo (recomendado)

```bash
claude plugin marketplace add anthropics/knowledge-work-plugins
claude plugin install engineering@knowledge-work-plugins
```

Ou de dentro de uma sessão interativa:

```
/plugin marketplace add anthropics/knowledge-work-plugins
/plugin install engineering@knowledge-work-plugins
```

Reinicie o `claude` para as skills aparecerem.

### Opção B: só esta skill, como skill pessoal

```bash
mkdir -p ~/.claude/skills/system-design
$EDITOR ~/.claude/skills/system-design/SKILL.md
```

Cole o conteúdo da [seção 7](#7-conteúdo-do-skillmd). Para valer só num projeto, use `.claude/skills/system-design/SKILL.md` na raiz do repositório.

---

## 5. Como rodar

```bash
cd ~/meu-projeto
claude
```

| Instalação | Como chamar |
|---|---|
| Plugin (opção A) | `/engineering:system-design <o que desenhar>` |
| Skill pessoal (opção B) | `/system-design <o que desenhar>` |
| Automático | Basta descrever o problema, ex.: "como devemos arquitetar um serviço de matchmaking multiplayer?" |

### Exemplos

```text
/engineering:system-design Desenhe o serviço de matchmaking de um jogo 2D online, 50 mil jogadores simultâneos, partidas em menos de 10 s

/engineering:system-design Sistema de notificações (push, e-mail, SMS) para 2 milhões de usuários, latência < 5 s, time de 4, 8 semanas

/engineering:system-design Revise o modelo de dados e a API em docs/proposta.md
```

Modo não interativo (scripts):

```bash
claude -p "/engineering:system-design Serviço de ranking para um jogo mobile" > docs/design-ranking.md
```

Dica: peça para o Claude salvar o resultado, ex.: *"salve em docs/design/ranking.md"*.

---

## 6. Dicas para resultados melhores

1. **Declare as restrições logo no início:** prazo, carga esperada, orçamento, habilidades do time.
2. **Dê números:** "10 mil requisições/s" é muito mais útil que "tráfego alto".
3. **Diga qual stack você já tem:** o design vai partir dela em vez de ignorá-la.
4. **Aponte arquivos do repositório:** o Claude Code lê o código e os documentos locais para embasar o design.
5. **Continue com `/engineering:architecture`** para transformar qualquer escolha importante (ex.: Redis x Memcached) num ADR formal.

---

## 7. Conteúdo do `SKILL.md`

Para a opção B (`~/.claude/skills/system-design/SKILL.md`):

```markdown
---
name: system-design
description: Design systems, services, and architectures. Trigger with "design a system for", "how should we architect", "system design for", "what's the right architecture for", or when the user needs help with API design, data modeling, or service boundaries.
---

# System Design

Help design systems and evaluate architectural decisions.

## Framework

### 1. Requirements Gathering
- Functional requirements (what it does)
- Non-functional requirements (scale, latency, availability, cost)
- Constraints (team size, timeline, existing tech stack)

### 2. High-Level Design
- Component diagram
- Data flow
- API contracts
- Storage choices

### 3. Deep Dive
- Data model design
- API endpoint design (REST, GraphQL, gRPC)
- Caching strategy
- Queue/event design
- Error handling and retry logic

### 4. Scale and Reliability
- Load estimation
- Horizontal vs. vertical scaling
- Failover and redundancy
- Monitoring and alerting

### 5. Trade-off Analysis
- Every decision has trade-offs. Make them explicit.
- Consider: complexity, cost, team familiarity, time to market, maintainability

## Output

Produce clear, structured design documents with diagrams (ASCII or described), explicit assumptions, and trade-off analysis. Always identify what you'd revisit as the system grows.
```

> O conteúdo do `SKILL.md` fica em inglês, que é como o plugin original o distribui.

---

## 8. Solução de problemas

| Problema | Solução |
|---|---|
| O comando não aparece ao digitar `/` | Reinicie o `claude`; confira `claude plugin list` ou o caminho `~/.claude/skills/system-design/SKILL.md` |
| `Unknown command /system-design` | Instalado via plugin? Use o prefixo: `/engineering:system-design` |
| Conflito de nome com outra skill | Use o nome com o prefixo do plugin |
| Atualizar o plugin | `claude plugin marketplace update knowledge-work-plugins` e reinstale |
