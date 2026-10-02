# The `system-design` Skill in Claude Code

A guide to installing and using the **system-design** skill from Anthropic's **engineering** plugin in Claude Code, the Claude CLI.

---

## 1. What it does

`system-design` helps you **design systems, services and architectures**, and evaluate architectural decisions. It also covers API design, data modeling and service boundaries.

It activates automatically when you say things like:

- "design a system for ..."
- "how should we architect ..."
- "system design for ..."
- "what's the right architecture for ..."

Its sibling skill, `/architecture`, is used when the goal is to *record a decision* as an ADR (Architecture Decision Record). `system-design` is the framework for *designing the system itself*, and `/architecture` calls on it for deeper analysis.

---

## 2. The five-step framework

### 1. Requirements gathering
- **Functional:** what the system does.
- **Non-functional:** scale, latency, availability, cost.
- **Constraints:** team size, timeline, existing tech stack.

### 2. High-level design
- Component diagram
- Data flow
- API contracts
- Storage choices

### 3. Deep dive
- Data model design
- API endpoint design (REST, GraphQL, gRPC)
- Caching strategy
- Queue/event design
- Error handling and retry logic

### 4. Scale and reliability
- Load estimation
- Horizontal vs. vertical scaling
- Failover and redundancy
- Monitoring and alerting

### 5. Trade-off analysis
Every decision has trade-offs, and the skill makes them explicit. It weighs complexity, cost, team familiarity, time to market and maintainability.

---

## 3. Output

The skill produces a structured design document containing:

- Diagrams (ASCII or described)
- Explicit assumptions
- Trade-off analysis
- A list of what to **revisit as the system grows**

---

## 4. Installation

### Option A: the full plugin (recommended)

```bash
claude plugin marketplace add anthropics/knowledge-work-plugins
claude plugin install engineering@knowledge-work-plugins
```

Or from inside an interactive session:

```
/plugin marketplace add anthropics/knowledge-work-plugins
/plugin install engineering@knowledge-work-plugins
```

Restart `claude` so the skills show up.

### Option B: this skill only, as a personal skill

```bash
mkdir -p ~/.claude/skills/system-design
$EDITOR ~/.claude/skills/system-design/SKILL.md
```

Paste the content from [section 7](#7-skillmd-content). For project-only scope, use `.claude/skills/system-design/SKILL.md` in the repository root.

---

## 5. How to run it

```bash
cd ~/my-project
claude
```

| Installation | How to invoke |
|---|---|
| Plugin (option A) | `/engineering:system-design <what to design>` |
| Personal skill (option B) | `/system-design <what to design>` |
| Automatic | Just describe the problem, e.g. "how should we architect a multiplayer matchmaking service?" |

### Examples

```text
/engineering:system-design Design the matchmaking service for a 2D online game, 50K concurrent players, matches under 10s

/engineering:system-design Notification system (push, email, SMS) for 2M users, latency < 5s, team of 4, 8 weeks

/engineering:system-design Review the data model and API in docs/proposal.md
```

Non-interactive mode (scripts):

```bash
claude -p "/engineering:system-design Leaderboard service for a mobile game" > docs/design-leaderboard.md
```

Tip: ask Claude to save the result, e.g. *"save it to docs/design/leaderboard.md"*.

---

## 6. Tips for better results

1. **State constraints up front:** deadline, expected load, budget, team skills.
2. **Give numbers:** "10K requests/s" is far more useful than "high traffic".
3. **Name the stack you already have:** the design will build on it instead of ignoring it.
4. **Point to files in the repo:** Claude Code reads local code and docs to ground the design.
5. **Follow up with `/engineering:architecture`** to turn any key choice (e.g. Redis vs. Memcached) into a formal ADR.

---

## 7. `SKILL.md` content

For option B (`~/.claude/skills/system-design/SKILL.md`):

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

---

## 8. Troubleshooting

| Problem | Fix |
|---|---|
| Command doesn't appear after typing `/` | Restart `claude`; check `claude plugin list` or the path `~/.claude/skills/system-design/SKILL.md` |
| `Unknown command /system-design` | Installed via plugin? Use the prefix: `/engineering:system-design` |
| Name clashes with another skill | Use the plugin-prefixed name |
| Updating the plugin | `claude plugin marketplace update knowledge-work-plugins`, then reinstall |
