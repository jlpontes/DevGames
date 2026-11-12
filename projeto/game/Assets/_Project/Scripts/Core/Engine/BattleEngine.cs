using System;
using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>
    /// Implementação das regras de batalha (GDD §5.7, Level Design §5). C# puro e determinístico:
    /// a única fonte de acaso é o Rng com seed, e o timing do QTE chega de fora como ReactionResult.
    /// </summary>
    public sealed class BattleEngine : IBattleEngine
    {
        private readonly Rng _rng;
        private readonly List<string> _log = new List<string>();
        private readonly List<ICommand> _queue = new List<ICommand>();
        private readonly Queue<int> _forcedSwitches = new Queue<int>();
        private int _queueIndex;
        private int _attackerSide = -1;
        private SkillSpec _pendingSkill;

        public BattleState State { get; private set; }
        public BattleRules Rules { get; private set; }
        public BattleCatalog Catalog { get; private set; }
        public PendingKind Pending { get; private set; }
        public int PendingSide { get; private set; }
        public SkillSpec PendingSkill { get { return Pending == PendingKind.Reaction ? _pendingSkill : null; } }
        public bool IsOver { get; private set; }
        public int WinnerSide { get; private set; }
        public int Seed { get; private set; }
        public IReadOnlyList<string> CommandLog { get { return _log; } }

        public BattleEngine(BattleSetup setup)
        {
            if (setup == null) throw new ArgumentNullException("setup");
            Rules = setup.Rules;
            Catalog = setup.Catalog;
            Seed = setup.Seed;
            _rng = new Rng(setup.Seed);
            if (!Catalog.Arenas.ContainsKey(setup.StartArena))
                throw new ArgumentException("Arena inicial não cadastrada: " + setup.StartArena);

            State = new BattleState(BuildSide(setup, 0), BuildSide(setup, 1), setup.StartArena, Rules.ArenaDrawInterval);
            Pending = PendingKind.Actions;
            PendingSide = -1;
            WinnerSide = -1;
        }

        private Side BuildSide(BattleSetup setup, int index)
        {
            var specs = setup.Teams[index];
            if (specs == null || specs.Count < 1 || specs.Count > Rules.MaxTeamSize)
                throw new ArgumentException("A equipe " + index + " precisa ter de 1 a " + Rules.MaxTeamSize + " lutadores.");
            var team = new Fighter[specs.Count];
            var startingHp = setup.StartingHp[index];
            if (startingHp != null && startingHp.Count != specs.Count)
                throw new ArgumentException("O HP inicial do lado " + index + " precisa de um valor por lutador.");
            for (int i = 0; i < specs.Count; i++)
            {
                team[i] = new Fighter(specs[i]);
                if (startingHp == null) continue;
                if (startingHp[i] <= 0) throw new ArgumentException("Lutador sem HP não pode entrar na batalha: " + specs[i].Id);
                team[i].Hp = Math.Min(startingHp[i], specs[i].MaxHp);
            }

            var items = new Dictionary<string, int>();
            int total = 0;
            if (setup.Items[index] != null)
            {
                foreach (var pair in setup.Items[index])
                {
                    if (pair.Value <= 0) continue;
                    if (!Catalog.Items.ContainsKey(pair.Key)) throw new ArgumentException("Item não cadastrado: " + pair.Key);
                    items[pair.Key] = pair.Value;
                    total += pair.Value;
                }
            }
            if (total > Rules.MaxItemsPerBattle)
                throw new ArgumentException("A equipe " + index + " leva " + total + " itens; o máximo é " + Rules.MaxItemsPerBattle + ".");
            return new Side(team, items);
        }

        // ── Consultas ────────────────────────────────────────────────────────────

        public bool IsLegal(ICommand command)
        {
            if (command == null || Pending != PendingKind.Actions) return false;
            if (command.Side != 0 && command.Side != 1) return false;
            var side = State.Sides[command.Side];

            var attack = command as AttackCommand;
            if (attack != null)
            {
                var skills = side.Active.Spec.Skills;
                if (attack.SkillIndex < 0 || attack.SkillIndex >= skills.Count) return false;
                return !skills[attack.SkillIndex].IsSpecial || side.Stamina >= Rules.StaminaMax;
            }

            var useItem = command as UseItemCommand;
            if (useItem != null)
            {
                ItemSpec item;
                if (useItem.ItemId == null || !Catalog.Items.TryGetValue(useItem.ItemId, out item)) return false;
                if (side.ItemCount(item.Id) <= 0) return false;
                if (item.Kind == ItemKind.Heal) return side.Active.Hp < side.Active.Spec.MaxHp;
                if (item.Kind == ItemKind.Cleanse) return side.Active.HasNegativeEffect;
                return true;
            }

            var sw = command as SwitchCommand;
            if (sw != null) return CanSwitchTo(side, sw.TeamIndex);

            return false;
        }

        public IReadOnlyList<ICommand> LegalCommands(int side)
        {
            var result = new List<ICommand>();
            if (Pending != PendingKind.Actions || (side != 0 && side != 1)) return result;
            var s = State.Sides[side];
            for (int i = 0; i < s.Active.Spec.Skills.Count; i++) AddIfLegal(result, new AttackCommand(side, i));
            foreach (var pair in s.Items) AddIfLegal(result, new UseItemCommand(side, pair.Key));
            for (int i = 0; i < s.Team.Length; i++) AddIfLegal(result, new SwitchCommand(side, i));
            return result;
        }

        private void AddIfLegal(List<ICommand> list, ICommand command)
        {
            if (IsLegal(command)) list.Add(command);
        }

        public DamageResult PreviewDamage(int attackerSide, int skillIndex)
        {
            var attacker = State.Sides[attackerSide].Active;
            var defender = State.Sides[1 - attackerSide].Active;
            return DamageCalculator.Compute(attacker, defender, attacker.Spec.Skills[skillIndex], CurrentArena, Rules);
        }

        private ArenaSpec CurrentArena { get { return Catalog.Arenas[State.Arena]; } }

        private static bool CanSwitchTo(Side side, int teamIndex)
        {
            return teamIndex >= 0 && teamIndex < side.Team.Length
                && teamIndex != side.ActiveIndex && !side.Team[teamIndex].IsFainted;
        }

        // ── Entradas ─────────────────────────────────────────────────────────────

        public StepResult SubmitActions(ICommand side0, ICommand side1)
        {
            if (Pending != PendingKind.Actions) return StepResult.Fail("O motor não está esperando ações (Pending = " + Pending + ").");
            if (side0 == null || side1 == null || side0.Side != 0 || side1.Side != 1)
                return StepResult.Fail("É preciso um comando do lado 0 e um do lado 1.");
            if (!IsLegal(side0)) return StepResult.Fail("Comando ilegal do lado 0: " + side0);
            if (!IsLegal(side1)) return StepResult.Fail("Comando ilegal do lado 1: " + side1);

            _log.Add("R" + State.Round + " " + side0 + " " + side1);
            var events = new List<BattleEvent>();
            _queue.Clear();
            _queue.AddRange(TurnOrder.Resolve(side0, side1, State, _rng));
            _queueIndex = 0;
            ProcessQueue(events);
            return StepResult.Success(events);
        }

        public StepResult SubmitReaction(ReactionResult result)
        {
            if (Pending != PendingKind.Reaction) return StepResult.Fail("O motor não está esperando uma reação (Pending = " + Pending + ").");

            _log.Add("R" + State.Round + " Reaction(" + PendingSide + "," + result + ")");
            var events = new List<BattleEvent>();
            int attacker = _attackerSide;
            int defender = 1 - attacker;
            var skill = _pendingSkill;
            _attackerSide = -1;
            _pendingSkill = null;
            PendingSide = -1;

            events.Add(new ReactionResolved(defender, result));
            if (result == ReactionResult.Miss)
            {
                ApplyHit(attacker, skill, false, events);
            }
            else
            {
                AddStamina(defender, Rules.StaminaOnReaction, events);
                if (result == ReactionResult.Counter)
                {
                    // R3: o contra-ataque usa a primeira habilidade normal do defensor e não pode ser respondido.
                    var counterSkill = State.Sides[defender].Active.Spec.Skills[0];
                    events.Add(new SkillUsed(defender, State.Sides[defender].ActiveIndex, counterSkill));
                    ApplyHit(defender, counterSkill, true, events);
                }
            }

            _queueIndex++;
            ProcessQueue(events);
            return StepResult.Success(events);
        }

        public StepResult SubmitForcedSwitch(int teamIndex)
        {
            if (Pending != PendingKind.ForcedSwitch) return StepResult.Fail("O motor não está esperando uma troca forçada (Pending = " + Pending + ").");
            var side = State.Sides[PendingSide];
            if (!CanSwitchTo(side, teamIndex)) return StepResult.Fail("Troca forçada inválida para o índice " + teamIndex + ".");

            _log.Add("R" + State.Round + " ForcedSwitch(" + PendingSide + "," + teamIndex + ")");
            var events = new List<BattleEvent>();
            int from = side.ActiveIndex;
            side.ActiveIndex = teamIndex;
            events.Add(new Switched(PendingSide, from, teamIndex, true));
            _forcedSwitches.Dequeue();
            NextForcedSwitchOrRound(events);
            return StepResult.Success(events);
        }

        // ── Resolução do turno ───────────────────────────────────────────────────

        private void ProcessQueue(List<BattleEvent> events)
        {
            Pending = PendingKind.None;
            while (_queueIndex < _queue.Count)
            {
                if (CheckBattleEnd(events)) return;

                var command = _queue[_queueIndex];
                var side = State.Sides[command.Side];
                var foe = State.Sides[1 - command.Side];

                // Quem caiu antes de agir perde a ação.
                if (side.Active.IsFainted) { _queueIndex++; continue; }

                var sw = command as SwitchCommand;
                if (sw != null)
                {
                    int from = side.ActiveIndex;
                    side.ActiveIndex = sw.TeamIndex;
                    events.Add(new Switched(command.Side, from, sw.TeamIndex, false));
                    _queueIndex++;
                    continue;
                }

                var useItem = command as UseItemCommand;
                if (useItem != null)
                {
                    ResolveItem(command.Side, useItem.ItemId, events);
                    _queueIndex++;
                    continue;
                }

                var attack = (AttackCommand)command;
                // Sem alvo de pé (caiu num contra-ataque, por exemplo): o golpe não acontece.
                if (foe.Active.IsFainted) { _queueIndex++; continue; }

                var skill = side.Active.Spec.Skills[attack.SkillIndex];
                if (skill.IsSpecial)
                {
                    side.Stamina = 0;
                    events.Add(new StaminaChanged(command.Side, 0));
                }
                events.Add(new SkillUsed(command.Side, side.ActiveIndex, skill));

                // Pausa: o defensor responde à janela de reação (humano pelo QTE, IA por sorteio).
                _attackerSide = command.Side;
                _pendingSkill = skill;
                Pending = PendingKind.Reaction;
                PendingSide = 1 - command.Side;
                events.Add(new ReactionRequested(PendingSide, Rules.ReactionWindowSeconds));
                return;
            }

            if (CheckBattleEnd(events)) return;
            EndOfTurn(events);
        }

        private void ResolveItem(int sideIndex, string itemId, List<BattleEvent> events)
        {
            var side = State.Sides[sideIndex];
            var item = Catalog.Items[itemId];
            var fighter = side.Active;

            side.Items[itemId] = side.ItemCount(itemId) - 1;
            side.UsedItems.Add(itemId);
            events.Add(new ItemUsed(sideIndex, side.ActiveIndex, item));

            if (item.Kind == ItemKind.Heal)
            {
                int amount = (int)Math.Round(fighter.Spec.MaxHp * item.Value, MidpointRounding.AwayFromZero);
                amount = Math.Min(amount, fighter.Spec.MaxHp - fighter.Hp);
                fighter.Hp += amount;
                events.Add(new Healed(sideIndex, side.ActiveIndex, amount, fighter.Hp));
            }
            else if (item.Kind == ItemKind.Cleanse)
            {
                var removed = new List<string>();
                for (int i = fighter.Effects.Count - 1; i >= 0; i--)
                {
                    if (!fighter.Effects[i].Spec.IsNegative) continue;
                    removed.Add(fighter.Effects[i].Spec.Id);
                    fighter.Effects.RemoveAt(i);
                }
                events.Add(new EffectsCleansed(sideIndex, side.ActiveIndex, removed));
            }
            else
            {
                ApplyEffect(fighter, sideIndex, side.ActiveIndex, Catalog.Effects[item.EffectId], events);
            }
        }

        private void ApplyHit(int dealerSide, SkillSpec skill, bool isCounter, List<BattleEvent> events)
        {
            int targetSide = 1 - dealerSide;
            var dealer = State.Sides[dealerSide];
            var target = State.Sides[targetSide];
            var attacker = dealer.Active;
            var defender = target.Active;

            var damage = DamageCalculator.Compute(attacker, defender, skill, CurrentArena, Rules);
            defender.Hp = Math.Max(0, defender.Hp - damage.Amount);
            events.Add(new DamageDealt(targetSide, target.ActiveIndex, damage.Amount, defender.Hp, damage.Effectiveness, isCounter));

            // R7. No contra-ataque o defensor já recebeu a stamina da reação bem-sucedida.
            if (!isCounter) AddStamina(dealerSide, Rules.StaminaOnDealDamage, events);
            AddStamina(targetSide, Rules.StaminaOnTakeDamage, events);

            if (defender.IsFainted) Faint(targetSide, target.ActiveIndex, events);
            else CheckPhaseShift(targetSide, target.ActiveIndex, events);

            if (skill.EffectId != null && _rng.Chance(skill.EffectChance))
            {
                bool onSelf = skill.EffectTarget == EffectTarget.Self;
                var recipient = onSelf ? attacker : defender;
                if (!recipient.IsFainted)
                {
                    ApplyEffect(attacker, onSelf ? dealerSide : targetSide,
                        onSelf ? dealer.ActiveIndex : target.ActiveIndex, Catalog.Effects[skill.EffectId], events);
                }
            }
        }

        /// <summary>Ativa a segunda fase do lutador (uma vez por batalha) quando o HP cai até o limite.</summary>
        private void CheckPhaseShift(int sideIndex, int fighterIndex, List<BattleEvent> events)
        {
            var fighter = State.Sides[sideIndex].Team[fighterIndex];
            var phase = fighter.Spec.PhaseShift;
            if (phase == null || fighter.PhaseShifted || fighter.IsFainted) return;
            if (fighter.Hp > fighter.Spec.MaxHp * phase.HpFraction) return;

            fighter.PhaseShifted = true;
            events.Add(new PhaseShifted(sideIndex, fighterIndex, phase.Title));
            for (int i = 0; i < phase.EffectIds.Count; i++)
                ApplyEffect(fighter, sideIndex, fighterIndex, Catalog.Effects[phase.EffectIds[i]], events);
            if (phase.FillsStamina) AddStamina(sideIndex, Rules.StaminaMax, events);
        }

        private void ApplyEffect(Fighter source, int sideIndex, int fighterIndex, EffectSpec spec, List<BattleEvent> events)
        {
            var target = State.Sides[sideIndex].Team[fighterIndex];
            var effect = EffectSystem.Apply(source, target, spec);
            events.Add(new EffectApplied(sideIndex, fighterIndex, spec, effect.TurnsLeft));
        }

        private void AddStamina(int sideIndex, int amount, List<BattleEvent> events)
        {
            var side = State.Sides[sideIndex];
            int value = Math.Min(Rules.StaminaMax, side.Stamina + amount);
            if (value == side.Stamina) return;
            side.Stamina = value;
            events.Add(new StaminaChanged(sideIndex, value));
        }

        private void Faint(int sideIndex, int fighterIndex, List<BattleEvent> events)
        {
            var fighter = State.Sides[sideIndex].Team[fighterIndex];
            fighter.Hp = 0;
            fighter.Effects.Clear();
            events.Add(new Fainted(sideIndex, fighterIndex));
        }

        /// <summary>Fim de turno: DoT e HoT em todos os lutadores, inclusive os do banco, e contagem dos 3 turnos.</summary>
        private void EndOfTurn(List<BattleEvent> events)
        {
            for (int s = 0; s < 2; s++)
            {
                var team = State.Sides[s].Team;
                for (int f = 0; f < team.Length; f++)
                {
                    var fighter = team[f];
                    if (fighter.IsFainted) continue;

                    var effects = fighter.Effects.ToArray();
                    for (int e = 0; e < effects.Length && !fighter.IsFainted; e++)
                    {
                        var effect = effects[e];
                        // A segunda fase pode ter renovado um efeito durante este laço; a cópia antiga não conta mais.
                        if (!fighter.Effects.Contains(effect)) continue;
                        int delta = 0;
                        if (effect.Spec.Kind == EffectKind.DamageOverTime)
                        {
                            delta = -Math.Min(effect.Value, fighter.Hp);
                        }
                        else if (effect.Spec.Kind == EffectKind.HealOverTime)
                        {
                            delta = Math.Min(effect.Value, fighter.Spec.MaxHp - fighter.Hp);
                        }
                        fighter.Hp += delta;
                        effect.TurnsLeft--;
                        effect.TurnsActive++;
                        events.Add(new EffectTicked(s, f, effect.Spec, delta, fighter.Hp, effect.TurnsLeft));

                        if (fighter.IsFainted)
                        {
                            Faint(s, f, events);
                            continue;
                        }
                        if (effect.TurnsLeft <= 0)
                        {
                            fighter.Effects.Remove(effect);
                            events.Add(new EffectExpired(s, f, effect.Spec));
                        }
                        if (delta < 0) CheckPhaseShift(s, f, events);
                    }
                }
            }

            if (CheckBattleEnd(events)) return;

            _forcedSwitches.Clear();
            for (int s = 0; s < 2; s++)
                if (State.Sides[s].Active.IsFainted) _forcedSwitches.Enqueue(s);
            NextForcedSwitchOrRound(events);
        }

        private void NextForcedSwitchOrRound(List<BattleEvent> events)
        {
            if (_forcedSwitches.Count > 0)
            {
                Pending = PendingKind.ForcedSwitch;
                PendingSide = _forcedSwitches.Peek();
                return;
            }

            State.Round++;
            State.RoundsUntilArenaDraw--;
            if (State.RoundsUntilArenaDraw <= 0)
            {
                State.Arena = ArenaSystem.Draw(_rng);
                State.RoundsUntilArenaDraw = Rules.ArenaDrawInterval;
                events.Add(new ArenaChanged(State.Arena));
            }
            events.Add(new RoundStarted(State.Round, State.RoundsUntilArenaDraw));
            Pending = PendingKind.Actions;
            PendingSide = -1;
        }

        private bool CheckBattleEnd(List<BattleEvent> events)
        {
            bool lost0 = State.Sides[0].AllFainted;
            bool lost1 = State.Sides[1].AllFainted;
            if (!lost0 && !lost1) return false;

            IsOver = true;
            WinnerSide = lost0 && lost1 ? -1 : lost0 ? 1 : 0;
            Pending = PendingKind.None;
            PendingSide = -1;
            events.Add(new BattleEnded(WinnerSide));
            return true;
        }
    }
}
