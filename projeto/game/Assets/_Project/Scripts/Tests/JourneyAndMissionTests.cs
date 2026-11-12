using System;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    /// <summary>Afinidade e espectro (R11), HP da jornada, missões secundárias, segunda fase do guardião e história.</summary>
    [TestFixture]
    public class JourneyAndMissionTests
    {
        private FakeStorage _storage;
        private GameServices _game;

        [SetUp]
        public void SetUp()
        {
            _storage = new FakeStorage();
            _game = NewSession();
        }

        private GameServices NewSession() { return new GameServices(new TwoSlotSaveStore(_storage, new FakeSerializer())); }

        private static AIProfile Unbeatable() { return new AIProfile("pro", "Pro", 0.3, 0.9, 2, 1.0, 1.0, 0.0); }
        private static AIProfile Hopeless() { return new AIProfile("afk", "Afk", 0.0, 0.0, 2, 0.0, 0.0, 0.0); }

        private BattleEngine Play(Match match, AIProfile player, int seed = 11)
        {
            var engine = new BattleEngine(match.Setup);
            BattleRunner.Run(engine, new RuleBasedAIController(player, seed), new RuleBasedAIController(match.OpponentAi, seed + 1));
            return engine;
        }

        private static List<string> Ids(params string[] ids) { return new List<string>(ids); }

        // ── Afinidade e espectro ─────────────────────────────────────────────────

        [Test]
        public void WithoutAffinityTheSpectrumIsEmptyAndNoModeStarts()
        {
            Assert.IsFalse(_game.Journey.HasAffinity);
            Assert.AreEqual(0, _game.Journey.Spectrum().Count);
            Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("fire_mage", "fire_warrior", "fire_tank") }));
            Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = Ids("fire_mage", "fire_warrior", "fire_tank") }));
        }

        [TestCase(Element.Fire, "fire_warrior", "fire_mage", "fire_tank")]
        [TestCase(Element.Water, "water_warrior", "water_mage", "water_tank")]
        [TestCase(Element.Plant, "plant_warrior", "plant_mage", "plant_tank")]
        public void AffinityStartsTheSpectrumWithItsThreeFighters(Element affinity, string a, string b, string c)
        {
            _game.Journey.ChooseAffinity(affinity);
            CollectionAssert.AreEquivalent(new[] { a, b, c }, _game.Journey.Spectrum());
            Assert.Throws<GameException>(() => _game.Journey.ChooseAffinity(Element.Fire), "a afinidade é escolhida uma vez");
            Assert.AreEqual(affinity, NewSession().Journey.Affinity, "a afinidade fica salva");
        }

        [Test]
        public void FightersOutsideTheSpectrumAreRefusedInEveryMode()
        {
            _game.Journey.ChooseAffinity(Element.Water);
            var mixed = Ids("water_mage", "water_tank", "fire_mage");
            var ex = Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = mixed }));
            StringAssert.Contains("Mago de Fogo", ex.Message);
            Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.LocalPvp, Team = Ids("water_mage", "water_tank", "water_warrior"), Team2 = mixed }));
            Assert.DoesNotThrow(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = Ids("water_mage", "water_tank", "water_warrior") }));
        }

        [Test]
        public void BeatingAGuardianBringsItsThreeFightersIntoTheSpectrum()
        {
            _game.Journey.ChooseAffinity(Element.Plant);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("plant_warrior", "plant_mage", "plant_tank"), Seed = 5 });
            var result = _game.Launcher.Finish(match, Play(match, Unbeatable()));

            Assert.IsTrue(result.PlayerWon);
            CollectionAssert.AreEquivalent(new[] { "fire_warrior", "fire_mage", "fire_tank" }, result.NewFighters);
            Assert.AreEqual(6, _game.Journey.Spectrum().Count);
            Assert.AreEqual(6, NewSession().Journey.Spectrum().Count, "o espectro sobrevive a reabrir o jogo");
        }

        [Test]
        public void TheGuardianOfYourOwnElementBringsNobodyNew()
        {
            _game.Journey.ChooseAffinity(Element.Fire);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("fire_warrior", "fire_mage", "fire_tank"), Seed = 5 });
            var result = _game.Launcher.Finish(match, Play(match, Unbeatable()));

            Assert.IsTrue(result.UnlockedNextStage);
            Assert.AreEqual(0, result.NewFighters.Count);
            Assert.AreEqual(3, _game.Journey.Spectrum().Count);
        }

        [Test]
        public void UnlockHintNamesTheGuardianAndTheMission()
        {
            _game.Journey.ChooseAffinity(Element.Fire);
            string hint = _game.Journey.UnlockHint("plant_mage");
            StringAssert.Contains("Sylvan", hint);
            StringAssert.Contains("Esporos na Névoa", hint);
        }

        // ── HP da jornada ────────────────────────────────────────────────────────

        [Test]
        public void JourneyHpIsTheStartingHpOfACampaignBattle()
        {
            _game.Journey.ChooseAffinity(Element.Water);
            _game.Profile.SetJourneyHp("water_tank", 100);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("water_warrior", "water_mage", "water_tank"), Seed = 5 });
            CollectionAssert.AreEqual(new[] { 620, 560, 100 }, match.Setup.StartingHp[0]);
            Assert.AreEqual(100, new BattleEngine(match.Setup).State.Sides[0].Team[2].Hp);
        }

        [Test]
        public void VictoryRecordsHpRestsTheBenchAndKeepsThreeFightersStanding()
        {
            _game.Journey.ChooseAffinity(Element.Water);
            _game.Profile.AddRecruit("plant_mage");
            _game.Profile.SetJourneyHp("plant_mage", 60);  // fica no banco e descansa
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("water_warrior", "water_mage", "water_tank"), Seed = 5 });
            var engine = Play(match, Unbeatable());
            var result = _game.Launcher.Finish(match, engine);
            Assert.IsTrue(result.PlayerWon);

            var team = engine.State.Sides[0].Team;
            foreach (var fighter in team)
                Assert.AreEqual(fighter.Hp, _game.Journey.CurrentHp(fighter.Spec.Id), "HP de " + fighter.Spec.Id);

            int max = _game.Journey.MaxHp("plant_mage");
            Assert.AreEqual(Math.Min(max, 60 + max / 2), _game.Journey.CurrentHp("plant_mage"));
            Assert.That(result.RestReport, Has.Some.Contains("Mago de Planta"));

            int standing = 0;
            foreach (var id in _game.Journey.Spectrum()) if (!_game.Journey.IsExhausted(id)) standing++;
            Assert.That(standing, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void DefeatLeavesTheJourneyHpUntouched()
        {
            _game.Journey.ChooseAffinity(Element.Fire);
            _game.Profile.SetJourneyHp("fire_mage", 300);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("fire_warrior", "fire_mage", "fire_tank"), Seed = 5 });
            var result = _game.Launcher.Finish(match, Play(match, Hopeless()));

            Assert.IsFalse(result.PlayerWon);
            Assert.AreEqual(300, _game.Journey.CurrentHp("fire_mage"));
            Assert.AreEqual(_game.Journey.MaxHp("fire_tank"), _game.Journey.CurrentHp("fire_tank"));
        }

        [Test]
        public void ExhaustedFightersCannotEnterACampaignBattle()
        {
            _game.Journey.ChooseAffinity(Element.Fire);
            _game.Profile.SetJourneyHp("fire_tank", 0);
            var ex = Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = Ids("fire_warrior", "fire_mage", "fire_tank") }));
            StringAssert.Contains("exausto", ex.Message);
            // Fora da campanha o HP da jornada não vale.
            Assert.DoesNotThrow(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = Ids("fire_warrior", "fire_mage", "fire_tank") }));
        }

        [Test]
        public void TheAltarHealsForCoins()
        {
            _game.Journey.ChooseAffinity(Element.Fire);
            _game.Profile.SetJourneyHp("fire_tank", 0);
            _game.Profile.SetJourneyHp("fire_mage", 100);

            Assert.AreEqual(PurchaseResult.NotEnoughCoins, _game.Journey.HealOne("fire_tank"));
            _game.Profile.AddCoins(100);
            Assert.AreEqual(PurchaseResult.Ok, _game.Journey.HealOne("fire_tank"));
            Assert.AreEqual(75, _game.Profile.Coins);
            Assert.IsFalse(_game.Journey.IsDamaged("fire_tank"));
            Assert.AreEqual(PurchaseResult.Ok, _game.Journey.HealAll());
            Assert.AreEqual(15, _game.Profile.Coins);
            Assert.IsFalse(_game.Journey.AnyDamaged());
            Assert.AreEqual(PurchaseResult.MaxLevelReached, _game.Journey.HealAll(), "nada para curar");
        }

        // ── Missões secundárias ──────────────────────────────────────────────────

        [Test]
        public void MissionsAppearInThreeWavesAfterChoosingAnAffinity()
        {
            foreach (var m in _game.Missions.All) Assert.AreEqual(MissionState.Locked, _game.Missions.State(m), "sem afinidade");

            _game.Journey.ChooseAffinity(Element.Plant);
            Assert.AreEqual(3, Count(MissionState.Available));
            _game.Profile.CompleteStage(0);
            Assert.AreEqual(6, Count(MissionState.Available));
            _game.Profile.CompleteStage(1);
            Assert.AreEqual(9, Count(MissionState.Available));
        }

        private int Count(MissionState state)
        {
            int n = 0;
            foreach (var m in _game.Missions.All) if (_game.Missions.State(m) == state) n++;
            return n;
        }

        [TestCase(Element.Fire)]
        [TestCase(Element.Water)]
        [TestCase(Element.Plant)]
        public void EveryAffinityHasAnEarlyRecruitWithAdvantageOverTheNextGuardian(Element affinity)
        {
            // Para cada fase de fragmento, a leva disponível antes dela tem alguém com vantagem de elemento sobre o guardião.
            for (int stage = 0; stage < 3; stage++)
            {
                var guardianElement = _game.Content.Opponents[stage].UnlocksElement;
                bool found = false;
                foreach (var m in _game.Content.Missions)
                {
                    if (m.RequiredStagesCleared > stage) continue;
                    var recruit = _game.Content.Character(m.RecruitId);
                    if (EffectivenessResolver.Beats(recruit.Element, guardianElement)) found = true;
                }
                Assert.IsTrue(found, "fase " + stage);
            }
        }

        [Test]
        public void MissionRulesShapeTheBattle()
        {
            _game.Journey.ChooseAffinity(Element.Fire);
            _game.Profile.AddItem(BuiltInContent.MedKit, 2);
            var duel = new MatchRequest { Mode = GameMode.Mission, MissionId = "eco_water_warrior", Team = Ids("fire_mage"), Seed = 3,
                Items = new Dictionary<string, int> { { BuiltInContent.MedKit, 1 } } };

            Assert.AreEqual(1, _game.Launcher.RequiredTeamSize(duel));
            Assert.IsFalse(_game.Launcher.ItemsAllowed(duel));
            var match = _game.Launcher.Create(duel);
            Assert.AreEqual(1, match.Setup.Teams[0].Count);
            Assert.AreEqual(0, match.Setup.Items[0].Count, "itens pedidos são ignorados quando a missão proíbe");
            Assert.AreEqual(ArenaId.Volcano, match.Setup.StartArena);
            Assert.AreEqual(int.MaxValue, match.Setup.Rules.ArenaDrawInterval, "arena fixa");
            Assert.AreEqual(3, _game.Rules.ArenaDrawInterval, "as regras gerais não mudam");

            Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.Mission, MissionId = "eco_water_warrior", Team = Ids("fire_mage", "fire_tank") }));
            Assert.Throws<GameException>(() => _game.Launcher.Create(new MatchRequest { Mode = GameMode.Mission, MissionId = "eco_fire_warrior", Team = Ids("fire_mage", "fire_tank") }), "leva 2 ainda fechada");
        }

        [Test]
        public void WinningAMissionRecruitsTheFighterAndPaysTheRewardOnce()
        {
            _game.Journey.ChooseAffinity(Element.Plant);
            var request = new MatchRequest { Mode = GameMode.Mission, MissionId = "eco_water_warrior", Team = Ids("plant_mage"), Seed = 3 };
            var match = _game.Launcher.Create(request);
            var result = _game.Launcher.Finish(match, Play(match, Unbeatable()));

            Assert.IsTrue(result.MissionCompleted);
            Assert.IsTrue(result.MissionFirstClear);
            CollectionAssert.AreEqual(new[] { "water_warrior" }, result.NewFighters);
            Assert.AreEqual(60, result.CoinsEarned);
            Assert.AreEqual(BuiltInContent.MedKit, result.ItemRewardId);
            Assert.AreEqual(1, _game.Profile.ItemCount(BuiltInContent.MedKit));
            Assert.IsTrue(_game.Journey.InSpectrum("water_warrior"));
            Assert.AreEqual(MissionState.Completed, _game.Missions.State(_game.Content.Mission("eco_water_warrior")));

            var again = _game.Launcher.Finish(_game.Launcher.Create(request), Play(_game.Launcher.Create(request), Unbeatable()));
            Assert.IsFalse(again.MissionFirstClear);
            Assert.AreEqual(_game.Economy.MissionReplayReward, again.CoinsEarned);
            Assert.AreEqual(0, again.NewFighters.Count);
            Assert.IsTrue(NewSession().Journey.InSpectrum("water_warrior"), "o recruta fica salvo");
        }

        [Test]
        public void LosingAMissionPaysTheConsolationAndRecruitsNobody()
        {
            _game.Journey.ChooseAffinity(Element.Plant);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Mission, MissionId = "eco_water_warrior", Team = Ids("plant_tank"), Seed = 3 });
            var result = _game.Launcher.Finish(match, Play(match, Hopeless()));

            Assert.IsFalse(result.MissionCompleted);
            Assert.AreEqual(_game.Economy.MissionFailReward, result.CoinsEarned);
            Assert.IsFalse(_game.Journey.InSpectrum("water_warrior"));
        }

        [Test]
        public void ObjectivesAreCheckedAfterTheBattle()
        {
            var noLosses = _game.Content.Mission("eco_plant_tank");
            var fast = _game.Content.Mission("eco_fire_mage");

            var lost = TestKit.Engine(TestKit.Team(TestKit.Plain("a", hp: 10, speed: 10), TestKit.Plain("b", attack: 5000, speed: 10)),
                TestKit.Team(TestKit.Plain("e", hp: 100, attack: 500, speed: 90)));
            TestKit.Round(lost, TestKit.Attack(0), TestKit.Attack(1));   // "a" cai
            lost.SubmitForcedSwitch(1);
            TestKit.Round(lost, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Dodge);
            Assert.IsTrue(lost.IsOver);
            Assert.AreEqual(0, lost.WinnerSide);
            CollectionAssert.AreEqual(new[] { "Nenhum lutador seu pode cair" }, MissionService.FailedObjectives(noLosses, lost));
            Assert.AreEqual(0, MissionService.FailedObjectives(fast, lost).Count, "venceu na 2ª rodada");

            var slow = TestKit.Engine(TestKit.Team(TestKit.Plain("a", attack: 1)), TestKit.Team(TestKit.Plain("e", hp: 100, attack: 1)));
            for (int i = 0; i < 9; i++) TestKit.Round(slow, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Dodge, ReactionResult.Dodge);
            while (!slow.IsOver) TestKit.Round(slow, TestKit.Attack(0), TestKit.Attack(1), ReactionResult.Miss, ReactionResult.Dodge);
            Assert.That(MissionService.FailedObjectives(fast, slow), Has.Some.Contains("rodada 9"));
        }

        // ── Segunda fase do Guardião do Núcleo ───────────────────────────────────

        [Test]
        public void TheCoreGuardianShiftsPhaseOnceBelowHalfHp()
        {
            var boss = SimulationHelper.Spec(BuiltInContent.BossId);
            Assert.IsNotNull(boss.PhaseShift);
            Assert.AreEqual(Element.Neutral, boss.Element);
            Assert.AreNotEqual(CharClass.Neutral, boss.Class, "R9: o guardião do núcleo tem classe");

            var hitter = TestKit.Plain("a", hp: 20000, attack: 2000, speed: 90); // 2000 × 0,75 = 1500 de 2600
            var engine = TestKit.Engine(TestKit.Team(hitter), TestKit.Team(boss), 7, null, null, ArenaId.Sea);
            var events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));
            var shifts = TestKit.All<PhaseShifted>(events);
            Assert.AreEqual(1, shifts.Count);
            var fighter = engine.State.Sides[1].Active;
            Assert.IsTrue(fighter.PhaseShifted);
            Assert.IsNotNull(fighter.FindEffect(BuiltInContent.AttackUp));
            Assert.IsNotNull(fighter.FindEffect(BuiltInContent.DefenseUp));

            events = TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));
            Assert.AreEqual(0, TestKit.All<PhaseShifted>(events).Count, "só uma vez por batalha");
        }

        [Test]
        public void APhaseShiftCanStartFromADamageOverTimeTick()
        {
            var phase = new PhaseShiftSpec(0.5, new[] { BuiltInContent.DefenseUp }, false, "teste");
            var target = new FighterSpec("t", "Alvo", Element.Neutral, CharClass.Neutral, 1000, 1, 0, 10, TestKit.Plain("x").Skills, phase);
            var burner = TestKit.Plain("b", attack: 100, speed: 90, secondEffect: BuiltInContent.Burn);
            var setup = new BattleSetup(TestKit.Team(burner), TestKit.Team(target), null, null, ArenaId.Sea, 7,
                SimulationHelper.Rules, SimulationHelper.Content.Catalog, null, new[] { 560 });
            var engine = new BattleEngine(setup);

            // Golpe de 50 (560 → 510, acima da metade) e depois Chamas de 20 no fim do turno (490).
            var events = TestKit.Round(engine, TestKit.Attack(0, 1), TestKit.Attack(1));
            int tick = events.FindIndex(e => e is EffectTicked);
            int shift = events.FindIndex(e => e is PhaseShifted);
            Assert.That(tick, Is.GreaterThanOrEqualTo(0));
            Assert.That(shift, Is.GreaterThan(tick), "a segunda fase veio do dano contínuo");
            Assert.IsNotNull(engine.State.Sides[1].Active.FindEffect(BuiltInContent.DefenseUp));
        }

        // ── IA contra especiais ──────────────────────────────────────────────────

        [Test]
        public void TheAiNeverCountersASpecial()
        {
            var engine = TestKit.Engine(TestKit.Team(TestKit.Plain("a", hp: 9000, speed: 90)), TestKit.Team(TestKit.Plain("b", hp: 9000, speed: 10)));
            for (int i = 0; i < 3; i++) TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));
            Assert.IsTrue(engine.IsLegal(TestKit.Attack(0, 2)));
            engine.SubmitActions(TestKit.Attack(0, 2), TestKit.Attack(1));
            Assert.IsTrue(engine.PendingSkill.IsSpecial);

            var ai = new RuleBasedAIController(new AIProfile("c", "Contra", 0, 0, 2, 1.0, 1.0, 0), 1);
            ReactionResult reaction = ReactionResult.Miss;
            ai.RequestReaction(engine, 1, 0.6f, r => reaction = r);
            Assert.AreEqual(ReactionResult.Dodge, reaction);
        }

        // ── História ─────────────────────────────────────────────────────────────

        [Test]
        public void TheStoryCoversEveryStageMissionAndAffinity()
        {
            var content = _game.Content;
            Assert.That(StoryScript.Prologue().Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(StoryScript.Finale().Count, Is.GreaterThanOrEqualTo(1));
            foreach (var affinity in new[] { Element.Fire, Element.Water, Element.Plant })
            {
                Assert.That(StoryScript.AffinityChosen(affinity, content)[0].ShowFighters.Count, Is.EqualTo(3));
                for (int stage = 0; stage < content.Opponents.Count; stage++)
                {
                    var chapter = StoryScript.Chapter(stage, affinity, content);
                    Assert.That(chapter.Opening.Count, Is.GreaterThan(0));
                    Assert.That(chapter.BattleIntro.Count, Is.GreaterThan(0));
                    Assert.That(chapter.Victory.Count, Is.GreaterThan(0));
                    Assert.That(chapter.Defeat.Count, Is.GreaterThan(0));
                    StringAssert.StartsWith(content.Opponents[stage].Name.Split(',')[0], chapter.GuardianName);
                    AssertPortraits(chapter.Opening);
                }
            }
            foreach (var mission in content.Missions)
            {
                var script = StoryScript.Mission(mission, content);
                Assert.That(script.Briefing.Count, Is.GreaterThan(0), mission.Id);
                Assert.AreEqual(mission.RecruitId, script.Victory[0].PortraitId);
            }
        }

        [Test]
        public void TheOwnElementGuardianSaysItBringsNobodyNew()
        {
            var own = StoryScript.Chapter(0, Element.Fire, _game.Content);
            var other = StoryScript.Chapter(0, Element.Water, _game.Content);
            StringAssert.Contains("já estão no seu espectro", own.MapSynopsis);
            StringAssert.Contains("trazer os 3 lutadores", other.MapSynopsis);
        }

        private void AssertPortraits(IEnumerable<StoryBeat> beats)
        {
            foreach (var beat in beats)
            {
                if (beat.Line == null) continue;
                bool known = beat.Line.PortraitId == StoryScript.MagePortraitId || _game.Content.Character(beat.Line.PortraitId) != null;
                Assert.IsTrue(known, "retrato desconhecido: " + beat.Line.PortraitId);
            }
        }

        // ── Save v1 → v2 ─────────────────────────────────────────────────────────

        [Test]
        public void AVersion1SaveKeepsItsProgressAndAsksForTheAffinity()
        {
            var serializer = new FakeSerializer();
            var old = new SaveData { version = 1, coins = 120, campaignStage = 1 };
            string text = serializer.Serialize(old);
            text = text.Substring(0, IndexOfNth(text, '\n', 6)); // um save v1 não tem as linhas novas
            _storage.Data[TwoSlotSaveStore.SlotA] = "1|" + TwoSlotSaveStore.Checksum(text) + "|" + text;

            var game = NewSession();
            Assert.AreEqual(120, game.Profile.Coins);
            Assert.AreEqual(1, game.Profile.CampaignStage);
            Assert.IsFalse(game.Journey.HasAffinity);
            CollectionAssert.AreEquivalent(new[] { "fire_warrior", "fire_mage", "fire_tank" }, game.Journey.Spectrum(), "o guardião já vencido continua liberando os seus lutadores");
        }

        private static int IndexOfNth(string text, char c, int n)
        {
            int index = -1;
            for (int i = 0; i < n; i++) index = text.IndexOf(c, index + 1);
            return index;
        }

        [Test]
        public void ResetErasesTheWholeJourney()
        {
            _game.Journey.ChooseAffinity(Element.Water);
            _game.Profile.AddRecruit("fire_mage");
            _game.Profile.SetJourneyHp("water_mage", 10);
            _game.Profile.MarkStorySeen("prologue");
            _game.Profile.MarkMissionDone("eco_fire_mage");
            _game.Profile.Save();
            _game.Profile.Reset();

            var reopened = NewSession();
            Assert.IsFalse(reopened.Journey.HasAffinity);
            Assert.AreEqual(0, reopened.Journey.Spectrum().Count);
            Assert.IsFalse(reopened.Profile.HasSeenStory("prologue"));
            Assert.IsFalse(reopened.Profile.IsMissionDone("eco_fire_mage"));
        }
    }
}
