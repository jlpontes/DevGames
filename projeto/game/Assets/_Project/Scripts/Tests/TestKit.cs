using System.Collections.Generic;
using ComboTactics.Core;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    /// <summary>Atalhos para montar lutadores e jogar rodadas nos testes do motor.</summary>
    public static class TestKit
    {
        /// <summary>Lutador de teste: golpe de poder 100, golpe de poder 50 com efeito opcional e especial de poder 200.</summary>
        public static FighterSpec Fighter(string id, Element element, CharClass charClass, int hp, int attack, int defense, int speed,
            string secondEffect = null, EffectTarget secondTarget = EffectTarget.Enemy, string specialEffect = null)
        {
            var skills = new List<SkillSpec>
            {
                new SkillSpec(id + "_a", "Golpe", 100, false),
                new SkillSpec(id + "_b", "Golpe com efeito", 50, false, secondEffect, secondTarget, secondEffect == null ? 0 : 1.0),
                new SkillSpec(id + "_s", "Especial", 200, true, specialEffect, EffectTarget.Enemy, specialEffect == null ? 0 : 1.0),
            };
            return new FighterSpec(id, id, element, charClass, hp, attack, defense, speed, skills);
        }

        /// <summary>Lutador sem vantagens (Neutral/Neutral), Defesa 0: o dano de um golpe de poder 100 é igual ao Ataque.</summary>
        public static FighterSpec Plain(string id, int hp = 1000, int attack = 100, int speed = 50,
            string secondEffect = null, EffectTarget secondTarget = EffectTarget.Enemy, string specialEffect = null)
        {
            return Fighter(id, Element.Neutral, CharClass.Neutral, hp, attack, 0, speed, secondEffect, secondTarget, specialEffect);
        }

        public static BattleEngine Engine(IReadOnlyList<FighterSpec> team0, IReadOnlyList<FighterSpec> team1, int seed = 7,
            IReadOnlyDictionary<string, int> items0 = null, IReadOnlyDictionary<string, int> items1 = null, ArenaId arena = ArenaId.Sea)
        {
            return SimulationHelper.Engine(team0, team1, seed, arena, items0, items1);
        }

        public static List<FighterSpec> Team(params FighterSpec[] specs) { return new List<FighterSpec>(specs); }

        public static AttackCommand Attack(int side, int skill = 0) { return new AttackCommand(side, skill); }
        public static SwitchCommand Switch(int side, int index) { return new SwitchCommand(side, index); }
        public static UseItemCommand Item(int side, string itemId) { return new UseItemCommand(side, itemId); }

        /// <summary>Joga uma rodada inteira. As reações são consumidas na ordem dos ataques; sem reação informada, o golpe acerta.</summary>
        public static List<BattleEvent> Round(BattleEngine engine, ICommand side0, ICommand side1, params ReactionResult[] reactions)
        {
            var events = new List<BattleEvent>();
            var step = engine.SubmitActions(side0, side1);
            Assert.IsTrue(step.Ok, step.Error);
            events.AddRange(step.Events);
            int next = 0;
            while (engine.Pending == PendingKind.Reaction)
            {
                step = engine.SubmitReaction(next < reactions.Length ? reactions[next] : ReactionResult.Miss);
                next++;
                Assert.IsTrue(step.Ok, step.Error);
                events.AddRange(step.Events);
            }
            return events;
        }

        public static List<T> All<T>(IEnumerable<BattleEvent> events) where T : BattleEvent
        {
            var result = new List<T>();
            foreach (var ev in events)
            {
                var typed = ev as T;
                if (typed != null) result.Add(typed);
            }
            return result;
        }
    }
}
