using System;
using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    public interface IProfileService
    {
        int Coins { get; }
        int CampaignStage { get; }
        bool FairPvp { get; }
        int ItemCount(string itemId);
        int UpgradeLevel(string charId, Stat stat);
    }

    /// <summary>Moedas, inventário, evoluções, progresso da campanha e configurações do jogador.</summary>
    public sealed class ProfileService : IProfileService
    {
        private readonly ISaveStore _store;
        private readonly ContentDatabase _content;
        private readonly EconomyConfig _economy;
        private SaveData _data;

        /// <summary>Disparado depois de cada gravação, para as telas atualizarem moedas e inventário.</summary>
        public event Action Changed;

        public ProfileService(ISaveStore store, ContentDatabase content, EconomyConfig economy)
        {
            _store = store;
            _content = content;
            _economy = economy;
            _data = Sanitize(store.Load());
        }

        public string StorageWarning { get { return _store.Warning; } }
        public int Coins { get { return _data.coins; } }
        public int CampaignStage { get { return _data.campaignStage; } }
        public bool FairPvp { get { return _data.settings.fairPvp; } }

        public int ItemCount(string itemId)
        {
            var stack = FindStack(itemId);
            return stack == null ? 0 : stack.count;
        }

        public int UpgradeLevel(string charId, Stat stat)
        {
            var entry = FindUpgrade(charId);
            return entry == null ? 0 : GetLevel(entry, stat);
        }

        public void AddCoins(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException("amount");
            _data.coins += amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || _data.coins < amount) return false;
            _data.coins -= amount;
            return true;
        }

        public void AddItem(string itemId, int amount)
        {
            if (_content.Item(itemId) == null || amount <= 0) return;
            var stack = FindStack(itemId);
            if (stack == null) { stack = new ItemStack { id = itemId }; _data.items.Add(stack); }
            stack.count += amount;
        }

        public void RemoveItem(string itemId, int amount)
        {
            var stack = FindStack(itemId);
            if (stack == null) return;
            stack.count = Math.Max(0, stack.count - amount);
        }

        public bool TryIncrementUpgrade(string charId, Stat stat)
        {
            if (_content.Character(charId) == null) return false;
            var entry = FindUpgrade(charId);
            if (entry == null) { entry = new UpgradeEntry { charId = charId }; _data.upgrades.Add(entry); }
            int level = GetLevel(entry, stat);
            if (level >= _economy.MaxUpgradeLevel) return false;
            SetLevel(entry, stat, level + 1);
            return true;
        }

        /// <summary>Marca a fase como vencida. Devolve true se isso liberou uma fase nova.</summary>
        public bool CompleteStage(int stageIndex)
        {
            if (stageIndex != _data.campaignStage || stageIndex >= _content.Opponents.Count) return false;
            _data.campaignStage++;
            return true;
        }

        public void SetFairPvp(bool value) { _data.settings.fairPvp = value; }

        public void Save()
        {
            _store.Save(_data);
            var handler = Changed;
            if (handler != null) handler();
        }

        /// <summary>Apaga todo o progresso. Usado pelo menu de debug.</summary>
        public void Reset()
        {
            _data = Sanitize(new SaveData());
            Save();
        }

        private ItemStack FindStack(string itemId)
        {
            foreach (var stack in _data.items) if (stack.id == itemId) return stack;
            return null;
        }

        private UpgradeEntry FindUpgrade(string charId)
        {
            foreach (var entry in _data.upgrades) if (entry.charId == charId) return entry;
            return null;
        }

        private static int GetLevel(UpgradeEntry entry, Stat stat)
        {
            switch (stat)
            {
                case Stat.Hp: return entry.hp;
                case Stat.Attack: return entry.atk;
                case Stat.Defense: return entry.def;
                default: return entry.spd;
            }
        }

        private static void SetLevel(UpgradeEntry entry, Stat stat, int level)
        {
            switch (stat)
            {
                case Stat.Hp: entry.hp = level; break;
                case Stat.Attack: entry.atk = level; break;
                case Stat.Defense: entry.def = level; break;
                default: entry.spd = level; break;
            }
        }

        /// <summary>Deixa o save coerente com o conteúdo atual: remove ids desconhecidos e corrige valores fora da faixa.</summary>
        private SaveData Sanitize(SaveData data)
        {
            if (data == null) data = new SaveData();
            if (data.items == null) data.items = new List<ItemStack>();
            if (data.upgrades == null) data.upgrades = new List<UpgradeEntry>();
            if (data.settings == null) data.settings = new SettingsData();

            data.coins = Math.Max(0, data.coins);
            data.campaignStage = Math.Max(0, Math.Min(_content.Opponents.Count, data.campaignStage));
            data.items.RemoveAll(s => s == null || _content.Item(s.id) == null || s.count <= 0);
            data.upgrades.RemoveAll(u => u == null || _content.Character(u.charId) == null);
            foreach (var entry in data.upgrades)
            {
                entry.hp = ClampLevel(entry.hp);
                entry.atk = ClampLevel(entry.atk);
                entry.def = ClampLevel(entry.def);
                entry.spd = ClampLevel(entry.spd);
            }
            return data;
        }

        private int ClampLevel(int level) { return Math.Max(0, Math.Min(_economy.MaxUpgradeLevel, level)); }
    }
}
