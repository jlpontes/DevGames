using ComboTactics.Core;

namespace ComboTactics.Game
{
    public enum PurchaseResult { Ok, NotEnoughCoins, MaxLevelReached, UnknownId }

    public interface IStoreService
    {
        PurchaseResult BuyItem(string itemId);
        PurchaseResult BuyUpgrade(string charId, Stat stat);
        /// <summary>Preço da próxima evolução, ou -1 se já está no nível máximo.</summary>
        int UpgradeCost(string charId, Stat stat);
    }

    /// <summary>Loja (GDD §5.1): itens consumíveis e evoluções permanentes de +3% por nível, até 5 por atributo.</summary>
    public sealed class StoreService : IStoreService
    {
        private readonly ProfileService _profile;
        private readonly ContentDatabase _content;
        private readonly EconomyConfig _economy;

        public StoreService(ProfileService profile, ContentDatabase content, EconomyConfig economy)
        {
            _profile = profile; _content = content; _economy = economy;
        }

        public PurchaseResult BuyItem(string itemId)
        {
            var item = _content.Item(itemId);
            if (item == null) return PurchaseResult.UnknownId;
            if (!_profile.TrySpend(item.Price)) return PurchaseResult.NotEnoughCoins;
            _profile.AddItem(itemId, 1);
            _profile.Save();
            return PurchaseResult.Ok;
        }

        public PurchaseResult BuyUpgrade(string charId, Stat stat)
        {
            var character = _content.Character(charId);
            if (character == null || !character.Playable) return PurchaseResult.UnknownId;
            int level = _profile.UpgradeLevel(charId, stat);
            if (level >= _economy.MaxUpgradeLevel) return PurchaseResult.MaxLevelReached;
            if (!_profile.TrySpend(_economy.UpgradeCost(level))) return PurchaseResult.NotEnoughCoins;
            _profile.TryIncrementUpgrade(charId, stat);
            _profile.Save();
            return PurchaseResult.Ok;
        }

        public int UpgradeCost(string charId, Stat stat)
        {
            int level = _profile.UpgradeLevel(charId, stat);
            return level >= _economy.MaxUpgradeLevel ? -1 : _economy.UpgradeCost(level);
        }
    }
}
