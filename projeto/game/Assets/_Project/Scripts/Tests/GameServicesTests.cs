using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ComboTactics.Core;
using ComboTactics.Game;
using NUnit.Framework;

namespace ComboTactics.Tests
{
    /// <summary>Armazenamento em memória no lugar do PlayerPrefs.</summary>
    public sealed class FakeStorage : IKeyValueStorage
    {
        public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public bool FailOnSet;

        public string Get(string key)
        {
            string value;
            return Data.TryGetValue(key, out value) ? value : null;
        }

        public void Set(string key, string value)
        {
            if (FailOnSet) throw new InvalidOperationException("cota excedida");
            Data[key] = value;
        }
    }

    /// <summary>Serializador de texto simples no lugar do JsonUtility, que só existe dentro do Unity.</summary>
    public sealed class FakeSerializer : ISaveSerializer
    {
        public string Serialize(SaveData d)
        {
            var sb = new StringBuilder();
            sb.Append(d.version).Append('\n').Append(d.coins).Append('\n').Append(d.campaignStage).Append('\n')
              .Append(d.settings.fairPvp ? 1 : 0).Append('\n');
            foreach (var item in d.items) sb.Append(item.id).Append(':').Append(item.count).Append(',');
            sb.Append('\n');
            foreach (var u in d.upgrades) sb.Append(u.charId).Append(':').Append(u.hp).Append(':').Append(u.atk).Append(':').Append(u.def).Append(':').Append(u.spd).Append(',');
            sb.Append('\n').Append(d.affinity ?? "").Append('\n').Append(string.Join(",", d.recruits)).Append('\n');
            foreach (var e in d.journeyHp) sb.Append(e.charId).Append(':').Append(e.hp).Append(',');
            sb.Append('\n').Append(string.Join(",", d.seenStory)).Append('\n').Append(string.Join(",", d.missionsDone));
            return sb.ToString();
        }

        public SaveData Deserialize(string text)
        {
            var lines = text.Split('\n');
            var d = new SaveData
            {
                version = int.Parse(lines[0], CultureInfo.InvariantCulture),
                coins = int.Parse(lines[1], CultureInfo.InvariantCulture),
                campaignStage = int.Parse(lines[2], CultureInfo.InvariantCulture),
            };
            d.settings.fairPvp = lines[3] == "1";
            foreach (var part in lines[4].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(':');
                d.items.Add(new ItemStack { id = kv[0], count = int.Parse(kv[1], CultureInfo.InvariantCulture) });
            }
            foreach (var part in lines[5].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split(':');
                d.upgrades.Add(new UpgradeEntry
                {
                    charId = kv[0],
                    hp = int.Parse(kv[1], CultureInfo.InvariantCulture), atk = int.Parse(kv[2], CultureInfo.InvariantCulture),
                    def = int.Parse(kv[3], CultureInfo.InvariantCulture), spd = int.Parse(kv[4], CultureInfo.InvariantCulture),
                });
            }
            if (lines.Length > 6)
            {
                d.affinity = lines[6];
                d.recruits.AddRange(lines[7].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                foreach (var part in lines[8].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split(':');
                    d.journeyHp.Add(new HpEntry { charId = kv[0], hp = int.Parse(kv[1], CultureInfo.InvariantCulture) });
                }
                d.seenStory.AddRange(lines[9].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                d.missionsDone.AddRange(lines[10].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            return d;
        }
    }

    [TestFixture]
    public class GameServicesTests
    {
        private FakeStorage _storage;
        private GameServices _game;

        [SetUp]
        public void SetUp()
        {
            _storage = new FakeStorage();
            _game = NewSession();
            // Estes testes cobrem regras de batalha e economia: o espectro inteiro e a afinidade ficam liberados.
            // Afinidade, espectro e missões têm testes próprios em JourneyAndMissionTests.
            UnlockWholeSpectrum(_game);
        }

        public static void UnlockWholeSpectrum(GameServices game)
        {
            game.Profile.SetAffinity(Element.Fire);
            foreach (var c in game.Content.PlayableCharacters) game.Profile.AddRecruit(c.Id);
        }

        /// <summary>Simula abrir o jogo de novo sobre o mesmo armazenamento.</summary>
        private GameServices NewSession()
        {
            return new GameServices(new TwoSlotSaveStore(_storage, new FakeSerializer()));
        }

        private static List<string> MixedTeam() { return new List<string> { "fire_mage", "water_warrior", "plant_tank" }; }

        private MatchResult PlayToTheEnd(Match match, AIProfile player)
        {
            var engine = new BattleEngine(match.Setup);
            var side1 = match.OpponentAi ?? _game.Content.AiProfiles[BuiltInContent.Easy];
            BattleRunner.Run(engine, new RuleBasedAIController(player, 11), new RuleBasedAIController(side1, 12));
            return _game.Launcher.Finish(match, engine);
        }

        private static AIProfile Unbeatable() { return new AIProfile("pro", "Pro", 0.3, 0.9, 2, 1.0, 1.0, 0.0); }
        private static AIProfile Hopeless() { return new AIProfile("afk", "Afk", 0.0, 0.0, 2, 0.0, 0.0, 0.0); }

        // ── Conteúdo ─────────────────────────────────────────────────────────────

        [Test]
        public void BuiltInContentIsValid()
        {
            var errors = ContentValidator.Validate(_game.Content);
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
            Assert.AreEqual(9, _game.Content.PlayableCharacters.Count);
            Assert.AreEqual(4, _game.Content.Opponents.Count);
            Assert.AreEqual(4, _game.Content.Items.Count);
            Assert.AreEqual(8, _game.Content.AiProfiles.Count);
            Assert.AreEqual(9, _game.Content.Missions.Count);
        }

        [TestCase("fire_warrior", 620, 130, 45, 77)]
        [TestCase("water_warrior", 620, 119, 55, 90)]
        [TestCase("plant_warrior", 620, 112, 65, 73)]
        [TestCase("fire_mage", 560, 171, 15, 52)]
        [TestCase("water_mage", 560, 160, 25, 65)]
        [TestCase("plant_mage", 560, 153, 35, 48)]
        [TestCase("fire_tank", 880, 73, 75, 27)]
        [TestCase("water_tank", 880, 68, 85, 40)]
        [TestCase("plant_tank", 880, 63, 95, 23)]
        public void CharacterStatsMatchTheGdd(string id, int hp, int attack, int defense, int speed)
        {
            var c = _game.Content.Character(id);
            Assert.AreEqual(hp, c.BaseHp);
            Assert.AreEqual(attack, c.BaseAttack);
            Assert.AreEqual(defense, c.BaseDefense);
            Assert.AreEqual(speed, c.BaseSpeed);
        }

        // ── Loja e evoluções (GDD §5.1) ──────────────────────────────────────────

        [Test]
        public void BuyingAnItemSpendsCoinsAndPersists()
        {
            Assert.AreEqual(PurchaseResult.NotEnoughCoins, _game.Store.BuyItem(BuiltInContent.MedKit));
            _game.Profile.AddCoins(100);
            Assert.AreEqual(PurchaseResult.Ok, _game.Store.BuyItem(BuiltInContent.MedKit));
            Assert.AreEqual(40, _game.Profile.Coins);
            Assert.AreEqual(1, _game.Profile.ItemCount(BuiltInContent.MedKit));
            Assert.AreEqual(PurchaseResult.UnknownId, _game.Store.BuyItem("nope"));

            var reopened = NewSession();
            Assert.AreEqual(40, reopened.Profile.Coins);
            Assert.AreEqual(1, reopened.Profile.ItemCount(BuiltInContent.MedKit));
        }

        [Test]
        public void UpgradesCostMoreEachLevel_AndStopAtFive()
        {
            _game.Profile.AddCoins(10000);
            var costs = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                costs.Add(_game.Store.UpgradeCost("fire_mage", Stat.Attack));
                Assert.AreEqual(PurchaseResult.Ok, _game.Store.BuyUpgrade("fire_mage", Stat.Attack));
            }
            Assert.AreEqual(new[] { 50, 100, 150, 200, 250 }, costs.ToArray());
            Assert.AreEqual(10000 - 750, _game.Profile.Coins);
            Assert.AreEqual(5, _game.Profile.UpgradeLevel("fire_mage", Stat.Attack));
            Assert.AreEqual(PurchaseResult.MaxLevelReached, _game.Store.BuyUpgrade("fire_mage", Stat.Attack));
            Assert.AreEqual(-1, _game.Store.UpgradeCost("fire_mage", Stat.Attack));
            Assert.AreEqual(PurchaseResult.UnknownId, _game.Store.BuyUpgrade(BuiltInContent.BossId, Stat.Attack), "o Boss não é jogável");
        }

        [Test]
        public void EachUpgradeLevelAddsThreePercentOfTheBaseStat()
        {
            _game.Profile.AddCoins(10000);
            for (int i = 0; i < 5; i++) _game.Store.BuyUpgrade("fire_mage", Stat.Attack);
            _game.Store.BuyUpgrade("fire_mage", Stat.Hp);

            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = MixedTeam(), Seed = 1 });
            var mage = match.Setup.Teams[0][0];
            Assert.AreEqual(197, mage.Attack);  // 171 × 1,15 = 196,65
            Assert.AreEqual(577, mage.MaxHp);   // 560 × 1,03 = 576,8
            Assert.AreEqual(15, mage.Defense);
        }

        // ── Montagem da batalha ──────────────────────────────────────────────────

        [Test]
        public void CampaignBattleUsesTheOpponentsArenaTeamAndBonus()
        {
            _game.Profile.CompleteStage(0);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 1, Team = MixedTeam(), Seed = 1 });

            Assert.AreEqual(ArenaId.Sea, match.Setup.StartArena);
            Assert.AreEqual(3, match.Setup.Teams[1].Count);
            foreach (var fighter in match.Setup.Teams[1]) Assert.AreEqual(Element.Water, fighter.Element);
            Assert.AreEqual(127, match.Setup.Teams[1][0].Attack); // Guerreiro de Água: 119 × 1,07 = 127,33
            Assert.AreSame(_game.Content.AiProfiles[BuiltInContent.GuardianWater], match.OpponentAi);
            Assert.AreEqual(1, match.Setup.Items[1][BuiltInContent.MedKit]);
            Assert.AreEqual(1, match.Setup.Items[1][BuiltInContent.Medicine]);
        }

        [Test]
        public void BossStageIsASingleFighterOnTheMoon()
        {
            for (int i = 0; i < 3; i++) _game.Profile.CompleteStage(i);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 3, Team = MixedTeam(), Seed = 1 });

            Assert.AreEqual(ArenaId.Moon, match.Setup.StartArena);
            Assert.AreEqual(1, match.Setup.Teams[1].Count);
            Assert.That(match.Setup.Teams[1][0].MaxHp, Is.InRange(2400, 2800)); // R9: cerca de 3× o HP de um jogável
        }

        [Test]
        public void InvalidRequestsAreRefusedWithAPlayerMessage()
        {
            var launcher = _game.Launcher;
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 1, Team = MixedTeam() }), "fase bloqueada");
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = new List<string> { "fire_mage", "fire_mage", "plant_tank" } }), "personagem repetido");
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = new List<string> { "fire_mage" } }), "equipe incompleta");
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = new List<string> { "fire_mage", BuiltInContent.BossId, "plant_tank" } }), "Boss na equipe");
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest { Mode = GameMode.VsAi, Team = MixedTeam() }), "sem dificuldade");
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest
            {
                Mode = GameMode.Campaign, StageIndex = 0, Team = MixedTeam(),
                Items = new Dictionary<string, int> { { BuiltInContent.MedKit, 1 } },
            }), "item que não está no inventário");

            _game.Profile.AddItem(BuiltInContent.MedKit, 5);
            Assert.Throws<GameException>(() => launcher.Create(new MatchRequest
            {
                Mode = GameMode.Campaign, StageIndex = 0, Team = MixedTeam(),
                Items = new Dictionary<string, int> { { BuiltInContent.MedKit, 4 } },
            }), "mais de 3 itens");
        }

        [Test]
        public void VsAiDrawsThreeDifferentOpponentsWithBaseStats()
        {
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Hard, Team = MixedTeam(), Seed = 42 });
            var ids = new HashSet<string>();
            foreach (var fighter in match.Setup.Teams[1])
            {
                ids.Add(fighter.Id);
                Assert.AreEqual(_game.Content.Character(fighter.Id).BaseAttack, fighter.Attack);
            }
            Assert.AreEqual(3, ids.Count);
            Assert.AreEqual(0.80, match.OpponentAi.QteChance, 1e-9);
        }

        [Test]
        public void FairPvpUsesBaseStatsAndTheFixedKit()
        {
            _game.Profile.AddCoins(1000);
            _game.Store.BuyUpgrade("fire_mage", Stat.Attack);
            var request = new MatchRequest { Mode = GameMode.LocalPvp, Team = MixedTeam(), Team2 = new List<string> { "fire_mage", "plant_mage", "water_tank" }, Seed = 3 };

            var fair = _game.Launcher.Create(request);
            Assert.AreEqual(171, fair.Setup.Teams[0][0].Attack);
            Assert.AreEqual(171, fair.Setup.Teams[1][0].Attack);
            Assert.IsNull(fair.OpponentAi);
            for (int side = 0; side < 2; side++)
            {
                Assert.AreEqual(1, fair.Setup.Items[side][BuiltInContent.MedKit]);
                Assert.AreEqual(1, fair.Setup.Items[side][BuiltInContent.Medicine]);
                Assert.AreEqual(1, fair.Setup.Items[side][BuiltInContent.AttackPotion]);
            }

            _game.Profile.SetFairPvp(false);
            var open = _game.Launcher.Create(request);
            Assert.AreEqual(176, open.Setup.Teams[0][0].Attack); // 171 × 1,03 = 176,13
            Assert.AreEqual(176, open.Setup.Teams[1][0].Attack);
        }

        // ── Fim da batalha: recompensas, itens e campanha ────────────────────────

        [Test]
        public void WinningAStagePays100AndUnlocksTheNext()
        {
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = MixedTeam(), Seed = 5 });
            var result = PlayToTheEnd(match, Unbeatable());

            Assert.IsTrue(result.PlayerWon);
            Assert.AreEqual(100, result.CoinsEarned);
            Assert.IsTrue(result.UnlockedNextStage);
            Assert.AreEqual(100, _game.Profile.Coins);
            Assert.IsTrue(_game.Campaign.IsUnlocked(1));
            Assert.IsFalse(_game.Campaign.IsUnlocked(2));
            Assert.AreEqual(1, NewSession().Profile.CampaignStage, "o progresso precisa estar salvo");
        }

        [Test]
        public void LosingPays50AndKeepsTheStageLocked()
        {
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = MixedTeam(), Seed = 5 });
            var result = PlayToTheEnd(match, Hopeless());

            Assert.IsFalse(result.PlayerWon);
            Assert.AreEqual(50, result.CoinsEarned);
            Assert.AreEqual(0, _game.Profile.CampaignStage);
        }

        [Test]
        public void ReplayingAClearedStagePaysButDoesNotUnlockAgain()
        {
            var request = new MatchRequest { Mode = GameMode.Campaign, StageIndex = 0, Team = MixedTeam(), Seed = 5 };
            PlayToTheEnd(_game.Launcher.Create(request), Unbeatable());
            var again = PlayToTheEnd(_game.Launcher.Create(request), Unbeatable());

            Assert.AreEqual(100, again.CoinsEarned);
            Assert.IsFalse(again.UnlockedNextStage);
            Assert.AreEqual(1, _game.Profile.CampaignStage);
        }

        [Test]
        public void BeatingTheBossPays500AndCompletesTheCampaign()
        {
            for (int i = 0; i < 3; i++) _game.Profile.CompleteStage(i);
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.Campaign, StageIndex = 3, Team = MixedTeam(), Seed = 5 });
            var result = PlayToTheEnd(match, Unbeatable());

            Assert.IsTrue(result.PlayerWon);
            Assert.AreEqual(500, result.CoinsEarned);
            Assert.IsTrue(result.CampaignCompleted);
            Assert.IsTrue(_game.Campaign.IsCompleted);
        }

        [Test]
        public void OnlyUsedItemsLeaveTheInventory()
        {
            _game.Profile.AddItem(BuiltInContent.MedKit, 2);
            _game.Profile.AddItem(BuiltInContent.AttackPotion, 1);
            var match = _game.Launcher.Create(new MatchRequest
            {
                Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = MixedTeam(), Seed = 9,
                Items = new Dictionary<string, int> { { BuiltInContent.MedKit, 2 }, { BuiltInContent.AttackPotion, 1 } },
            });

            var engine = new BattleEngine(match.Setup);
            // O jogador toma um golpe e usa um Kit Médico; depois a batalha segue até o fim sem mais itens.
            TestKit.Round(engine, TestKit.Attack(0), TestKit.Attack(1));
            TestKit.Round(engine, TestKit.Item(0, BuiltInContent.MedKit), TestKit.Attack(1));
            BattleRunner.Run(engine, new RuleBasedAIController(Hopeless(), 1), new RuleBasedAIController(match.OpponentAi, 2));
            var result = _game.Launcher.Finish(match, engine);

            Assert.AreEqual(1, _game.Profile.ItemCount(BuiltInContent.MedKit));
            Assert.AreEqual(1, _game.Profile.ItemCount(BuiltInContent.AttackPotion));
            Assert.AreEqual(result.PlayerWon ? 100 : 50, result.CoinsEarned);
        }

        [Test]
        public void LocalPvpPaysNoCoins()
        {
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.LocalPvp, Team = MixedTeam(), Team2 = MixedTeam(), Seed = 3 });
            var result = PlayToTheEnd(match, Unbeatable());
            Assert.AreEqual(0, result.CoinsEarned);
            Assert.AreEqual(0, _game.Profile.Coins);
        }

        [Test]
        public void FinishingAnUnfinishedBattleIsABug()
        {
            var match = _game.Launcher.Create(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = BuiltInContent.Easy, Team = MixedTeam(), Seed = 9 });
            Assert.Throws<InvalidOperationException>(() => _game.Launcher.Finish(match, new BattleEngine(match.Setup)));
        }

        // ── Save (ADR-005) ───────────────────────────────────────────────────────

        [Test]
        public void SavesAlternateBetweenTwoSlots()
        {
            _game.Profile.AddCoins(10); _game.Profile.Save();
            Assert.IsTrue(_storage.Data.ContainsKey(TwoSlotSaveStore.SlotA));
            Assert.IsFalse(_storage.Data.ContainsKey(TwoSlotSaveStore.SlotB));
            _game.Profile.AddCoins(10); _game.Profile.Save();
            Assert.IsTrue(_storage.Data.ContainsKey(TwoSlotSaveStore.SlotB));
            Assert.AreEqual(20, NewSession().Profile.Coins);
        }

        [Test]
        public void ACorruptedSlotFallsBackToThePreviousSave()
        {
            _game.Profile.AddCoins(10); _game.Profile.Save(); // slot A, 10 moedas
            _game.Profile.AddCoins(10); _game.Profile.Save(); // slot B, 20 moedas
            _storage.Data[TwoSlotSaveStore.SlotB] = _storage.Data[TwoSlotSaveStore.SlotB].Replace("20", "99999");

            var reopened = NewSession();
            Assert.AreEqual(10, reopened.Profile.Coins);
            Assert.IsNull(reopened.Profile.StorageWarning);
        }

        [Test]
        public void WhenEverySlotIsCorrupted_AFreshProfileStartsAndTheOldTextIsKept()
        {
            _game.Profile.AddCoins(10); _game.Profile.Save();
            _storage.Data[TwoSlotSaveStore.SlotA] = "isto não é um save";

            var reopened = NewSession();
            Assert.AreEqual(0, reopened.Profile.Coins);
            Assert.IsNotNull(reopened.Profile.StorageWarning);
            Assert.AreEqual("isto não é um save", _storage.Data[TwoSlotSaveStore.BackupKey]);
        }

        [Test]
        public void ASaveFromANewerVersionIsNotLoaded()
        {
            var serializer = new FakeSerializer();
            string json = serializer.Serialize(new SaveData { version = SaveData.CurrentVersion + 1, coins = 777 });
            _storage.Data[TwoSlotSaveStore.SlotA] = "1|" + TwoSlotSaveStore.Checksum(json) + "|" + json;

            var reopened = NewSession();
            Assert.AreEqual(0, reopened.Profile.Coins);
            Assert.IsNotNull(reopened.Profile.StorageWarning);
        }

        [Test]
        public void AFailedWriteWarnsThePlayerButNeverThrows()
        {
            _storage.FailOnSet = true;
            _game.Profile.AddCoins(10);
            Assert.DoesNotThrow(() => _game.Profile.Save());
            Assert.IsNotNull(_game.Profile.StorageWarning);
            Assert.AreEqual(10, _game.Profile.Coins, "o jogo continua com o perfil em memória");
        }

        [Test]
        public void UnknownIdsAndOutOfRangeValuesAreCleanedOnLoad()
        {
            var serializer = new FakeSerializer();
            var dirty = new SaveData { coins = -5, campaignStage = 99 };
            dirty.items.Add(new ItemStack { id = "item_removido", count = 3 });
            dirty.items.Add(new ItemStack { id = BuiltInContent.MedKit, count = 2 });
            dirty.upgrades.Add(new UpgradeEntry { charId = "personagem_removido", atk = 2 });
            dirty.upgrades.Add(new UpgradeEntry { charId = "fire_mage", atk = 42, hp = -3 });
            string json = serializer.Serialize(dirty);
            _storage.Data[TwoSlotSaveStore.SlotA] = "1|" + TwoSlotSaveStore.Checksum(json) + "|" + json;

            var profile = NewSession().Profile;
            Assert.AreEqual(0, profile.Coins);
            Assert.AreEqual(4, profile.CampaignStage);
            Assert.AreEqual(0, profile.ItemCount("item_removido"));
            Assert.AreEqual(2, profile.ItemCount(BuiltInContent.MedKit));
            Assert.AreEqual(5, profile.UpgradeLevel("fire_mage", Stat.Attack));
            Assert.AreEqual(0, profile.UpgradeLevel("fire_mage", Stat.Hp));
        }

        [Test]
        public void ResetErasesTheProgress()
        {
            _game.Profile.AddCoins(300);
            _game.Profile.CompleteStage(0);
            _game.Profile.Save();
            _game.Profile.Reset();

            var reopened = NewSession().Profile;
            Assert.AreEqual(0, reopened.Coins);
            Assert.AreEqual(0, reopened.CampaignStage);
        }
    }
}
