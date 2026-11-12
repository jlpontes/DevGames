using System;
using System.Collections.Generic;

namespace ComboTactics.Core
{
    /// <summary>
    /// IA por regras de prioridade (Level Design §4, ADR-004). A primeira regra que se aplica decide:
    /// 1 sobrevivência, 2 troca por vantagem, 3 status, 4 especial, 5 melhor habilidade normal.
    /// </summary>
    public sealed class RuleBasedAIController : IBattleController
    {
        private readonly AIProfile _profile;
        private readonly Rng _rng;
        private int _lastSwitchRound = int.MinValue;

        /// <summary>Número da regra que decidiu a última ação (0 = erro proposital). Útil para depurar.</summary>
        public int LastRule { get; private set; }

        public RuleBasedAIController(AIProfile profile, int seed)
        {
            _profile = profile;
            _rng = new Rng(seed);
        }

        public void RequestAction(IBattleEngine engine, int side, Action<ICommand> reply)
        {
            reply(Decide(engine, side));
        }

        public void RequestReaction(IBattleEngine engine, int side, float windowSeconds, Action<ReactionResult> reply)
        {
            ReactionResult result = ReactionResult.Miss;
            if (_rng.Chance(_profile.QteChance))
                result = _rng.Chance(_profile.CounterShare) ? ReactionResult.Counter : ReactionResult.Dodge;

            // Especiais são mais difíceis de ler: contra eles a IA nunca contra-ataca, no máximo desvia.
            var skill = engine.PendingSkill;
            if (skill != null && skill.IsSpecial && result == ReactionResult.Counter) result = ReactionResult.Dodge;
            reply(result);
        }

        public void RequestForcedSwitch(IBattleEngine engine, int side, Action<int> reply)
        {
            reply(BestBenchIndex(engine.State, side, false));
        }

        private ICommand Decide(IBattleEngine engine, int side)
        {
            var state = engine.State;
            var me = state.Sides[side];
            var active = me.Active;
            var target = state.Sides[1 - side].Active;
            // Proteção contra vai e volta: nada de troca em dois turnos seguidos.
            bool canSwitch = me.HasBenchAlive && _lastSwitchRound != state.Round - 1;

            if (_rng.Chance(_profile.MistakeChance))
            {
                LastRule = 0;
                return new AttackCommand(side, _rng.Next(NormalSkillCount(active.Spec)));
            }

            // 1. Sobrevivência
            if (active.Hp < active.Spec.MaxHp * _profile.SurvivalHpPct)
            {
                var heal = FindLegalItem(engine, side, ItemKind.Heal);
                if (heal != null) { LastRule = 1; return heal; }
            }

            // 2. Troca por vantagem
            if (canSwitch && EffectivenessResolver.CountAdvantages(active.Spec, target.Spec) == 0)
            {
                int candidate = BestBenchIndex(state, side, true);
                if (candidate >= 0 && _rng.Chance(_profile.SwitchChance))
                {
                    LastRule = 2;
                    _lastSwitchRound = state.Round;
                    return new SwitchCommand(side, candidate);
                }
            }

            // 3. Gerenciamento de status
            if (HasLongDot(active))
            {
                var cleanse = FindLegalItem(engine, side, ItemKind.Cleanse);
                if (cleanse != null) { LastRule = 3; return cleanse; }
                if (canSwitch)
                {
                    LastRule = 3;
                    _lastSwitchRound = state.Round;
                    return new SwitchCommand(side, BestBenchIndex(state, side, false));
                }
            }

            // 4. Especial
            int specialIndex = active.Spec.Skills.Count - 1;
            var special = new AttackCommand(side, specialIndex);
            if (active.Spec.Skills[specialIndex].IsSpecial && engine.IsLegal(special)) { LastRule = 4; return special; }

            // 5. Habilidade normal de maior dano
            LastRule = 5;
            int best = 0;
            int bestDamage = -1;
            for (int i = 0; i < NormalSkillCount(active.Spec); i++)
            {
                int damage = engine.PreviewDamage(side, i).Amount;
                if (damage > bestDamage) { bestDamage = damage; best = i; }
            }
            return new AttackCommand(side, best);
        }

        private static int NormalSkillCount(FighterSpec spec)
        {
            int count = 0;
            for (int i = 0; i < spec.Skills.Count; i++) if (!spec.Skills[i].IsSpecial) count++;
            return Math.Max(1, count);
        }

        private bool HasLongDot(Fighter fighter)
        {
            for (int i = 0; i < fighter.Effects.Count; i++)
            {
                var effect = fighter.Effects[i];
                if (effect.Spec.Kind == EffectKind.DamageOverTime && effect.TurnsActive >= _profile.DotTurnsBeforeCleanse) return true;
            }
            return false;
        }

        private static ICommand FindLegalItem(IBattleEngine engine, int side, ItemKind kind)
        {
            IReadOnlyList<ICommand> legal = engine.LegalCommands(side);
            for (int i = 0; i < legal.Count; i++)
            {
                var useItem = legal[i] as UseItemCommand;
                if (useItem != null && engine.Catalog.Items[useItem.ItemId].Kind == kind) return useItem;
            }
            return null;
        }

        /// <summary>
        /// Melhor lutador do banco contra o ativo do oponente: mais vantagens, menos desvantagens,
        /// desempate pelo HP restante. Com requireAdvantage, só vale quem tem ao menos uma vantagem.
        /// </summary>
        private static int BestBenchIndex(BattleState state, int side, bool requireAdvantage)
        {
            var me = state.Sides[side];
            var target = state.Sides[1 - side].Active;
            int best = -1;
            double bestScore = double.MinValue;
            for (int i = 0; i < me.Team.Length; i++)
            {
                var fighter = me.Team[i];
                if (i == me.ActiveIndex || fighter.IsFainted) continue;
                int advantages = EffectivenessResolver.CountAdvantages(fighter.Spec, target.Spec);
                if (requireAdvantage && advantages == 0) continue;
                int disadvantages = EffectivenessResolver.CountAdvantages(target.Spec, fighter.Spec);
                double score = (advantages - disadvantages) * 10.0 + (double)fighter.Hp / fighter.Spec.MaxHp;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }
    }
}
