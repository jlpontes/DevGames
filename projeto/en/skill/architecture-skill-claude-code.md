# The `/architecture` Skill in Claude Code (terminal)

A guide to installing and using the **architecture** skill from Anthropic's **engineering** plugin in Claude Code, the Claude CLI for the terminal.

---

## 1. What the skill does

`/architecture` helps you **make and document architecture decisions**. It works in three modes:

| Mode | Example request | Result |
|---|---|---|
| **Create an ADR** | "Kafka or SQS for our event bus?" | Architecture Decision Record with options, trade-offs and consequences |
| **Evaluate a design** | "Review this microservices proposal" | Critical analysis of an existing proposal |
| **Design a system** | "Design the app's notification system" | Architecture proposal built from requirements and constraints |

For deeper analysis it relies on its sibling skill **system-design** (requirements gathering, scalability, trade-offs), which Claude loads automatically when relevant.

---

## 2. Installation

### Option A — Install the full plugin (recommended)

Installs `/architecture` together with the other engineering skills (`/standup`, `/debug`, `/code-review`, `/incident-response`, `/deploy-checklist`, etc.).

```bash
# Add Anthropic's plugin marketplace
claude plugin marketplace add anthropics/knowledge-work-plugins

# Install the engineering plugin
claude plugin install engineering@knowledge-work-plugins
```

You can also do it from inside an interactive session:

```
/plugin marketplace add anthropics/knowledge-work-plugins
/plugin install engineering@knowledge-work-plugins
```

Restart `claude` (or open a new session) for the skills to show up.

### Option B — This skill only, as a personal skill

If you only want `/architecture`, create the file manually:

```bash
mkdir -p ~/.claude/skills/architecture
$EDITOR ~/.claude/skills/architecture/SKILL.md
```

Paste the content from [section 6](#6-skillmd-content). To limit it to one project, use `.claude/skills/architecture/SKILL.md` at the repository root (and commit it so the team has access).

---

## 3. How to run it

Open Claude Code in the project folder:

```bash
cd ~/my-project
claude
```

And call the skill:

| Installation | Command |
|---|---|
| Plugin (option A) | `/engineering:architecture <decision or system>` |
| Personal skill (option B) | `/architecture <decision or system>` |

Type `/` at the prompt to see the list of available skills and check the exact name.

### Examples

```text
/engineering:architecture Should we use PostgreSQL or DynamoDB for the orders service? We need 5K writes/s, the team knows SQL, delivery in 6 weeks.

/engineering:architecture Review the design in docs/microservices-proposal.md

/engineering:architecture Design the notification system (push, email, SMS) for 2M users, latency < 5s
```

### Non-interactive mode (scripts / CI)

```bash
claude -p "/engineering:architecture Redis vs Memcached for session cache" > docs/adr/ADR-007-cache.md
```

### Automatic invocation

You don't have to type the command: if you write something like *"help me decide between gRPC and REST for the internal API"*, Claude recognises it from the skill's description and uses it on its own.

---

## 4. Output format (ADR)

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
[Key trade-offs with clear reasoning]

## Consequences
- [What becomes easier]
- [What becomes harder]
- [What we'll need to revisit]

## Action Items
1. [ ] [Implementation step]
2. [ ] [Follow-up]
```

Tip: explicitly ask Claude to save it — *"save it to docs/adr/ADR-012-event-bus.md"* — and Claude Code writes the file into the repository.

---

## 5. Boosting it with connectors (MCP)

The skill works on its own, but it gets better with MCP servers connected:

| Category | Examples | What the skill can then do |
|---|---|---|
| Knowledge base | Notion, Confluence | Search previous ADRs and design docs for context |
| Project tracker | Linear, Jira, Asana | Link epics/tickets and create the implementation tasks |

Adding an MCP server in Claude Code:

```bash
claude mcp add --transport http notion https://mcp.notion.com/mcp
claude mcp add --transport http linear https://mcp.linear.app/mcp
```

Use `/mcp` inside the session to authenticate and see the status.

### Tips for better answers

1. **State constraints up front** — deadline, load ("10K rps"), budget.
2. **Name the options** — even if you already have a preference, the analysis comes out more balanced.
3. **Include non-functional requirements** — latency, cost, team experience and maintenance matter as much as features.
4. **Point to files in the repo** — Claude Code reads the local code and docs to ground the decision.

---

## 6. `SKILL.md` content

Use this content for option B (`~/.claude/skills/architecture/SKILL.md`):

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

> `$ARGUMENTS` is replaced by the text you type after the command.

---

## 7. Troubleshooting

| Problem | Fix |
|---|---|
| Command doesn't appear when typing `/` | Restart `claude`; check `claude plugin list` or whether the file is at `~/.claude/skills/architecture/SKILL.md` |
| `Unknown command /architecture` | Installed via plugin? Use the prefix: `/engineering:architecture` |
| Name clash with another skill | Use the plugin-prefixed name |
| Connectors don't respond | Run `/mcp` in the session and authenticate the server |
| Updating the plugin | `claude plugin marketplace update knowledge-work-plugins` and reinstall |
