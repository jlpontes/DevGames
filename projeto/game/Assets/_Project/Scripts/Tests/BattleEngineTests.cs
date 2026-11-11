using System;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    [TestFixture]
    public class BattleEngineTests
    {
        private static readonly BattleRules Rules = new BattleRules();

        // ── Ordem do turno (R2) ──────────────────────────────────────────────────

        [Test]
        public void FasterFighterActsFirst()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("slow", speed: 10)), TestKit.Team(TestKit.Plain("fast", speed: 90)));
            var used = TestKit.All<SkillUsed>(TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1)));
            Assert.AreEqual(1, used[0].Side);
            Assert.AreEqual(0, used[1].Side);
        }

        [Test]
        public void SwitchResolvesBeforeAttack_AndTheNewFighterTakesTheHit()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", speed: 10), TestKit.Plain("b", speed: 10)),
                TestKit.Team(TestKit.Plain("fast", speed: 90)));
            var events = TestKit.Round(engine, TestKit.Switch(0, 1), TestKit.Attack(1));

            Assert.IsInstanceOf<Switched>(events[0]);
            Assert.AreEqual(1, TestKit.All<DamageDealt>(events)[0].FighterIndex);
            Assert.AreEqual(1000, engine.State.Sides[0].Team[0].Hp);
            Assert.AreEqual(900, engine.State.Sides[0].Team[1].Hp);
        }

        [Test]
        public void SpeedTieIsDecidedByTheSeed_AndBothOrdersHappen()
        {
            int side0First = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a")), TestKit.Team(TestKit.Plain("b")), seed);
                if (TestKit.All<SkillUsed>(TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1)))[0].Side == 0) side0First++;
            }
            Assert.That(side0First, Is.InRange(70, 130));
        }

        // ── Comandos ilegais ─────────────────────────────────────────────────────

        [Test]
        public void IllegalCommandsAreRejected_AndStateDoesNotChange()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a"), TestKit.Plain("b")), TestKit.Team(TestKit.Plain("c")));

            Assert.IsFalse(engine.SubmitActions(TestKit.Attack(0, 2), TestKit.Attack(1)).Ok, "especial sem stamina");
            Assert.IsFalse(engine.SubmitActions(TestKit.Item(0, BuiltInContent.MedKit), TestKit.Attack(1)).Ok, "item que não tem");
            Assert.IsFalse(engine.SubmitActions(TestKit.Switch(0, 0), TestKit.Attack(1)).Ok, "troca para o próprio ativo");
            Assert.IsFalse(engine.SubmitActions(TestKit.Switch(0, 5), TestKit.Attack(1)).Ok, "troca para índice inexistente");
            Assert.IsFalse(engine.SubmitActions(TestKit.Attack(1), TestKit.Attack(0)).Ok, "comandos com os lados trocados");
            Assert.IsFalse(engine.SubmitReaction(ReactionResult.Dodge).Ok, "reação fora de hora");
            Assert.IsFalse(engine.SubmitForcedSwitch(1).Ok, "troca forçada fora de hora");

            Assert.AreEqual(1, engine.State.Round);
            Assert.AreEqual(PendingKind.Actions, engine.Pending);
            Assert.AreEqual(1000, engine.State.Sides[1].Active.Hp);
            Assert.AreEqual(0, engine.CommandLog.Count);
        }

        [Test]
        public void LegalCommands_ListAttacksItemsAndSwitches()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.AttackPotion, 1 }, { BuiltInContent.MedKit, 1 } };
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a"), TestKit.Plain("b")), TestKit.Team(TestKit.Plain("c")), 7, items);
            var legal = engine.LegalCommands(0);

            // 2 habilidades normais (especial bloqueado), Poção de Ataque (Kit Médico bloqueado com HP cheio) e 1 troca.
            Assert.AreEqual(4, legal.Count);
            foreach (var command in legal) Assert.IsTrue(engine.IsLegal(command));
        }

        [Test]
        public void MoreThanThreeItemsPerBattleIsRefused()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.MedKit, 4 } };
            Assert.Throws<ArgumentException>(() => TestKit.Engine(TestKit.Team(TestKit.Plain("a")), TestKit.Team(TestKit.Plain("b")), 7, items));
        }

        // ── Stamina e especial (R7) ──────────────────────────────────────────────

        [Test]
        public void StaminaGrowsByDealingTakingAndReacting()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", speed: 90)), TestKit.Team(TestKit.Plain("b", speed: 10)));

            // Lado 0 acerta (+20) e depois desvia do golpe do lado 1 (+25). Lado 1 só sofre dano (+15).
            TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Miss, ReactionResult.Dodge);
            Assert.AreEqual(45, engine.State.Sides[0].Stamina);
            Assert.AreEqual(15, engine.State.Sides[1].Stamina);
        }

        [Test]
        public void SpecialNeedsFullStamina_AndResetsIt()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", speed: 90)), TestKit.Team(TestKit.Plain("b", hp: 5000, speed: 10)));
            for (int i = 0; i < 3; i++) TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1)); // 35 por rodada

            Assert.AreEqual(100, engine.State.Sides[0].Stamina);
            Assert.IsTrue(engine.IsLegal(TestKit.Attack(0, 2)));

            int hpBefore = engine.State.Sides[1].Active.Hp;
            TestKit.Round(engine, TestKit.Attack(0, 2), TestKit.Attack(1));
            Assert.AreEqual(hpBefore - 200, engine.State.Sides[1].Active.Hp);
            // Zerou ao usar, +20 pelo dano causado e +15 pelo golpe recebido em seguida.
            Assert.AreEqual(35, engine.State.Sides[0].Stamina);
            Assert.IsFalse(engine.IsLegal(TestKit.Attack(0, 2)));
        }

        [Test]
        public void StaminaIsSharedByTheTeam_AndSurvivesASwitch()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", speed: 90), TestKit.Plain("b")), TestKit.Team(TestKit.Plain("c", speed: 10)));
            TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));
            int before = engine.State.Sides[0].Stamina;
            TestKit.Round(engine, TestKit.Switch(0, 1), TestKit.Attack(1));
            Assert.AreEqual(before + 15, engine.State.Sides[0].Stamina);
        }

        // ── Janela de resposta (R3) ──────────────────────────────────────────────

        [Test]
        public void DodgeCancelsAllDamage()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", speed: 90)), TestKit.Team(TestKit.Plain("b", speed: 10)));
            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Dodge);

            Assert.AreEqual(1000, engine.State.Sides[1].Active.Hp);
            Assert.AreEqual(900, engine.State.Sides[0].Active.Hp);
            Assert.AreEqual(1, TestKit.All<DamageDealt>(events).Count);
        }

        [Test]
        public void CounterCancelsDamage_AndHitsBackWithTheFirstNormalSkill()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", attack: 100, speed: 90)), TestKit.Team(TestKit.Plain("b", attack: 70, speed: 10)));
            // O lado 0 ataca; o lado 1 contra-ataca com a sua primeira habilidade normal (poder 100, Ataque 70).
            var step = engine.SubmitActions(TestKit.Attack(0), TestKit.Attack(1));
            Assert.AreEqual(PendingKind.Reaction, engine.Pending);
            Assert.AreEqual(1, engine.PendingSide);

            step = engine.SubmitReaction(ReactionResult.Counter);
            var counter = TestKit.All<DamageDealt>(step.Events)[0];
            Assert.IsTrue(counter.IsCounter);
            Assert.AreEqual(0, counter.TargetSide);
            Assert.AreEqual(70, counter.Amount);
            Assert.AreEqual(1000, engine.State.Sides[1].Active.Hp);

            // O contra-ataque não abre janela de resposta: a próxima pendência é o ataque normal do lado 1.
            Assert.AreEqual(PendingKind.Reaction, engine.Pending);
            Assert.AreEqual(0, engine.PendingSide);
        }

        [Test]
        public void FighterKnockedOutByACounterLosesTheBattle_WhenAlone()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", hp: 50, speed: 90)), TestKit.Team(TestKit.Plain("b", speed: 10)));
            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Counter);

            Assert.IsTrue(engine.IsOver);
            Assert.AreEqual(1, engine.WinnerSide);
            Assert.IsInstanceOf<BattleEnded>(events[events.Count - 1]);
            Assert.AreEqual(PendingKind.None, engine.Pending);
        }

        // ── Itens (R8) ───────────────────────────────────────────────────────────

        [Test]
        public void MedKitHeals30PercentOfMaxHp_AndIsConsumed()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.MedKit, 1 } };
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", speed: 10)), TestKit.Team(TestKit.Plain("b", attack: 400, speed: 90)), 7, items);
            TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));            // 1000 → 600
            TestKit.Round(engine, TestKit.Item(0, BuiltInContent.MedKit), TestKit.Attack(1, 1)); // golpe de 200, depois +300

            Assert.AreEqual(700, engine.State.Sides[0].Active.Hp);
            Assert.AreEqual(0, engine.State.Sides[0].ItemCount(BuiltInContent.MedKit));
            Assert.AreEqual(1, engine.State.Sides[0].UsedItems.Count);
            Assert.IsFalse(engine.IsLegal(TestKit.Item(0, BuiltInContent.MedKit)));
        }

        [Test]
        public void AttackPotionGivesTenPercentForThreeTurns()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.AttackPotion, 1 } };
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a")), TestKit.Team(TestKit.Plain("b", hp: 5000)), 7, items);

            TestKit.Round(engine, TestKit.Item(0, BuiltInContent.AttackPotion), TestKit.Attack(1));
            var damages = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                foreach (var d in TestKit.All<DamageDealt>(TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1))))
                    if (d.TargetSide == 1) damages.Add(d.Amount);
            }
            // A poção gasta o turno 1; vale nos turnos 2 e 3 e expira no fim do turno 3.
            Assert.AreEqual(new[] { 110, 110, 100 }, damages.ToArray());
        }

        [Test]
        public void MedicineRemovesNegativeEffectsOnly()
        {
            var items = new Dictionary<string, int> { { BuiltInContent.Medicine, 1 }, { BuiltInContent.DefensePotion, 1 } };
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", speed: 10)),
                TestKit.Team(TestKit.Plain("b", speed: 90, secondEffect: BuiltInContent.Poison)), 7, items);

            Assert.IsFalse(engine.IsLegal(TestKit.Item(0, BuiltInContent.Medicine)), "sem efeito negativo, o Remédio não pode ser usado");
            TestKit.Round(engine, TestKit.Item(0, BuiltInContent.DefensePotion), TestKit.Attack(1, 1));
            Assert.IsNotNull(engine.State.Sides[0].Active.FindEffect(BuiltInContent.Poison));

            TestKit.Round(engine, TestKit.Item(0, BuiltInContent.Medicine), TestKit.Attack(1));
            Assert.IsNull(engine.State.Sides[0].Active.FindEffect(BuiltInContent.Poison));
            Assert.IsNotNull(engine.State.Sides[0].Active.FindEffect(BuiltInContent.DefenseUp));
        }

        // ── Efeitos de 3 turnos (GDD §5.2) ───────────────────────────────────────

        [Test]
        public void DamageOverTimeTicksExactlyThreeTimes()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", attack: 100, speed: 90, secondEffect: BuiltInContent.Burn)),
                TestKit.Team(TestKit.Plain("b", hp: 5000, attack: 1, speed: 10)));

            var ticks = new List<int>();
            ticks.AddRange(DotTicks(TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1)), 1));
            for (int i = 0; i < 4; i++) ticks.AddRange(DotTicks(TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Dodge), 1));

            // Chamas: 20% do Ataque de quem aplicou (100) por turno, 3 turnos.
            Assert.AreEqual(new[] { -20, -20, -20 }, ticks.ToArray());
            Assert.IsNull(engine.State.Sides[1].Active.FindEffect(BuiltInContent.Burn));
        }

        [Test]
        public void EffectsFollowTheFighterToTheBench()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", attack: 100, speed: 90, secondEffect: BuiltInContent.Poison)),
                TestKit.Team(TestKit.Plain("b", speed: 10), TestKit.Plain("c", speed: 10)));

            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));        // 50 de dano + veneno (20/turno)
            Assert.AreEqual(1000 - 50 - 20, engine.State.Sides[1].Team[0].Hp);

            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Switch(1, 1)); // "b" vai para o banco envenenado
            var tick = TestKit.All<EffectTicked>(events)[0];
            Assert.AreEqual(0, tick.FighterIndex);
            Assert.AreEqual(1000 - 50 - 40, engine.State.Sides[1].Team[0].Hp);
            Assert.AreEqual(1, engine.State.Sides[1].ActiveIndex);
        }

        [Test]
        public void RegenerationHealsEachTurn_WithoutPassingMaxHp()
        {
            var healer = TestKit.Plain("a", hp: 1000, speed: 90, secondEffect: BuiltInContent.Regen, secondTarget: EffectTarget.Self);
            var engine = TestKit.Engine(TestKit.Team(healer), TestKit.Team(TestKit.Plain("b", attack: 30, speed: 10)));

            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));
            // Perdeu 30 e recuperou 4% de 1000 = 40, limitado ao HP máximo.
            Assert.AreEqual(1000, engine.State.Sides[0].Active.Hp);
        }

        [Test]
        public void ReapplyingAnEffectRefreshesItInsteadOfStacking()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", attack: 100, speed: 90, secondEffect: BuiltInContent.Burn)),
                TestKit.Team(TestKit.Plain("b", hp: 5000, attack: 1, speed: 10)));

            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));
            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));
            var effects = engine.State.Sides[1].Active.Effects;
            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual(2, effects[0].TurnsLeft);
        }

        [Test]
        public void DamageOverTimeCanKnockOutAFighterOnTheBench()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", attack: 100, speed: 90, secondEffect: BuiltInContent.Burn)),
                TestKit.Team(TestKit.Plain("b", hp: 80, speed: 10), TestKit.Plain("c", speed: 10)));

            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));          // 80 − 50 − 20 = 10
            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Switch(1, 1));
            Assert.AreEqual(1, TestKit.All<Fainted>(events).Count);
            Assert.IsTrue(engine.State.Sides[1].Team[0].IsFainted);
            Assert.AreEqual(PendingKind.Actions, engine.Pending, "o lutador caiu no banco; ninguém precisa trocar");
        }

        // ── Arena (R4) ───────────────────────────────────────────────────────────

        [Test]
        public void ArenaIsDrawnEveryThreeRounds()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", hp: 100000)), TestKit.Team(TestKit.Plain("b", hp: 100000)), 11, null, null, ArenaId.Volcano);
            Assert.AreEqual(ArenaId.Volcano, engine.State.Arena);
            Assert.AreEqual(3, engine.State.RoundsUntilArenaDraw);

            var drawsAtRound = new List<int>();
            for (int round = 1; round <= 9; round++)
            {
                var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));
                if (TestKit.All<ArenaChanged>(events).Count > 0) drawsAtRound.Add(round);
            }
            Assert.AreEqual(new[] { 3, 6, 9 }, drawsAtRound.ToArray());
            Assert.AreEqual(10, engine.State.Round);
        }

        [Test]
        public void ArenaModifierAppliesToDamage()
        {
            var water = TestKit.Fighter("w", Element.Water, CharClass.Neutral, 1000, 100, 0, 90);
            var fire = TestKit.Fighter("f", Element.Fire, CharClass.Neutral, 1000, 100, 0, 10);
            var engine = TestKit.Engine(TestKit.Team(water), TestKit.Team(fire), 7, null, null, ArenaId.Sea);
            var damages = TestKit.All<DamageDealt>(TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1)));

            Assert.AreEqual(143, damages[0].Amount); // 100 × 1,3 (Água → Fogo) × 1,10 (Mar)
            Assert.AreEqual(63, damages[1].Amount);  // 100 × 0,7 × 0,90
        }

        // ── Derrota, troca forçada e fim ─────────────────────────────────────────

        [Test]
        public void KnockedOutFighterLosesItsAction_AndTheSideMustSwitch()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", attack: 500, speed: 90)),
                TestKit.Team(TestKit.Plain("b", hp: 100, speed: 10), TestKit.Plain("c", speed: 10)));
            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));

            Assert.AreEqual(1, TestKit.All<SkillUsed>(events).Count, "o lado 1 caiu antes de agir");
            Assert.AreEqual(PendingKind.ForcedSwitch, engine.Pending);
            Assert.AreEqual(1, engine.PendingSide);
            Assert.IsFalse(engine.SubmitForcedSwitch(0).Ok, "não pode voltar o lutador derrotado");

            var step = engine.SubmitForcedSwitch(1);
            Assert.IsTrue(step.Ok);
            Assert.AreEqual(1, engine.State.Sides[1].ActiveIndex);
            Assert.AreEqual(PendingKind.Actions, engine.Pending);
            Assert.AreEqual(2, engine.State.Round);
        }

        [Test]
        public void BattleEndsWhenATeamHasNoFightersLeft()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", attack: 500, speed: 90)), TestKit.Team(TestKit.Plain("b", hp: 100, speed: 10)));
            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));

            Assert.IsTrue(engine.IsOver);
            Assert.AreEqual(0, engine.WinnerSide);
            Assert.AreEqual(0, TestKit.All<BattleEnded>(events)[0].WinnerSide);
            Assert.IsFalse(engine.SubmitActions(TestKit.Attack(0), TestKit.Attack(1)).Ok);
        }

        [Test]
        public void BothLastFightersFallingOnTheSameTickIsADraw()
        {
            var engine = TestKit.Engine(
                TestKit.Team(TestKit.Plain("a", hp: 60, attack: 100, speed: 90, secondEffect: BuiltInContent.Burn)),
                TestKit.Team(TestKit.Plain("b", hp: 60, attack: 100, speed: 10, secondEffect: BuiltInContent.Burn)));
            TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1, 1)); // 50 de dano e 20 de Chamas em cada um

            Assert.IsTrue(engine.IsOver);
            Assert.AreEqual(-1, engine.WinnerSide);
        }

        // ── Determinismo e contrato de eventos (N7, ADR-002) ─────────────────────

        [Test]
        public void SameSeedAndCommandsProduceTheSameBattle()
        {
            var first = PlayScripted(1234);
            var second = PlayScripted(1234);
            Assert.AreEqual(first, second);
            Assert.AreNotEqual(first, PlayScripted(99));
        }

        private static string PlayScripted(int seed)
        {
            var content = SimulationHelper.Content;
            var engine = SimulationHelper.Engine(
                SimulationHelper.Team("fire_mage", "water_warrior", "plant_tank"),
                SimulationHelper.Team("water_mage", "plant_warrior", "fire_tank"), seed, ArenaId.Forest, content.FairKit, content.FairKit);
            var outcome = BattleRunner.Run(engine,
                new RuleBasedAIController(content.AiProfiles[BuiltInContent.Normal], seed + 1),
                new RuleBasedAIController(content.AiProfiles[BuiltInContent.Hard], seed + 2));
            return outcome.WinnerSide + "|" + outcome.Rounds + "|" + string.Join(";", engine.CommandLog);
        }

        [Test]
        public void EventsCarryEverythingTheViewNeeds_SnapshotMatchesStateAfterEveryStep()
        {
            var content = SimulationHelper.Content;
            for (int seed = 1; seed <= 60; seed++)
            {
                var rng = new Rng(seed);
                var engine = SimulationHelper.Engine(SimulationHelper.RandomTeam(rng), SimulationHelper.RandomTeam(rng), seed,
                    ArenaSystem.Draw(rng), content.FairKit, content.FairKit);
                var snapshot = new BattleSnapshot(engine.State);
                var normal = content.AiProfiles[BuiltInContent.Normal];
                var controllers = new IBattleController[] { new RuleBasedAIController(normal, seed), new RuleBasedAIController(normal, seed + 500) };

                int steps = 0;
                while (engine.Pending != PendingKind.None && steps++ < 5000)
                {
                    StepResult step;
                    if (engine.Pending == PendingKind.Actions)
                    {
                        ICommand c0 = null, c1 = null;
                        controllers[0].RequestAction(engine, 0, c => c0 = c);
                        controllers[1].RequestAction(engine, 1, c => c1 = c);
                        step = engine.SubmitActions(c0, c1);
                    }
                    else if (engine.Pending == PendingKind.Reaction)
                    {
                        ReactionResult reaction = ReactionResult.Miss;
                        controllers[engine.PendingSide].RequestReaction(engine, engine.PendingSide, 1f, r => reaction = r);
                        step = engine.SubmitReaction(reaction);
                    }
                    else
                    {
                        int index = -1;
                        controllers[engine.PendingSide].RequestForcedSwitch(engine, engine.PendingSide, i => index = i);
                        step = engine.SubmitForcedSwitch(index);
                    }
                    Assert.IsTrue(step.Ok, step.Error);
                    foreach (var ev in step.Events) snapshot.Apply(ev);
                    AssertSnapshotMatches(snapshot, engine, seed);
                }
                Assert.IsTrue(engine.IsOver, "a batalha da seed " + seed + " não terminou");
                Assert.AreEqual(engine.WinnerSide, snapshot.WinnerSide);
            }
        }

        private static void AssertSnapshotMatches(BattleSnapshot snapshot, BattleEngine engine, int seed)
        {
            var state = engine.State;
            string where = "seed " + seed + ", rodada " + state.Round;
            Assert.AreEqual(state.Arena, snapshot.Arena, where);
            Assert.AreEqual(state.Round, snapshot.Round, where);
            Assert.AreEqual(state.RoundsUntilArenaDraw, snapshot.RoundsUntilArenaDraw, where);
            for (int s = 0; s < 2; s++)
            {
                var side = state.Sides[s];
                var view = snapshot.Sides[s];
                Assert.AreEqual(side.ActiveIndex, view.ActiveIndex, where);
                Assert.AreEqual(side.Stamina, view.Stamina, where);
                foreach (var pair in side.Items) Assert.AreEqual(pair.Value, view.Items[pair.Key], where + " item " + pair.Key);
                for (int f = 0; f < side.Team.Length; f++)
                {
                    var fighter = side.Team[f];
                    Assert.AreEqual(fighter.Hp, view.Team[f].Hp, where + " hp " + s + "/" + f);
                    Assert.AreEqual(fighter.IsFainted, view.Team[f].Fainted, where);
                    Assert.AreEqual(fighter.Effects.Count, view.Team[f].Effects.Count, where + " efeitos " + s + "/" + f);
                    foreach (var effect in fighter.Effects)
                        Assert.AreEqual(effect.TurnsLeft, view.Team[f].Effects[effect.Spec.Id], where + " " + effect.Spec.Id);
                }
            }
        }

        private static IEnumerable<int> DotTicks(IEnumerable<BattleEvent> events, int side)
        {
            foreach (var tick in TestKit.All<EffectTicked>(events))
                if (tick.Side == side && tick.Delta < 0) yield return tick.Delta;
        }
    }
}
