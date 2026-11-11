using System;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Loja (GDD §5.1): Loja de Itens à esquerda e Progressão de Atributos à direita.</summary>
    public sealed class StoreScreen : UiScreen
    {
        private static readonly Stat[] Stats = { Stat.Hp, Stat.Attack, Stat.Defense, Stat.Speed };
        private string _selectedId;

        protected override void Build()
        {
            var services = App.Services;
            var content = services.Content;
            if (_selectedId == null) _selectedId = content.PlayableCharacters[0].Id;

            Ui.Backdrop(Root);
            Ui.Label(Root, "Mercador da Arena", 30, 12, 500, 48, 36, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            var coinFrame = Ui.Frame(Root, "CoinsBox", 960, 16, 290, 44, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Label(coinFrame, "Moedas: " + services.Profile.Coins, 16, 0, 258, 44, 22, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // ── Loja de Itens ──
            var itemsPanel = Ui.Frame(Root, "Items", 30, 76, 480, 540, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Label(itemsPanel, "Suprimentos de Batalha", 16, 10, 448, 34, 24, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(itemsPanel, "Consumíveis: usar um item gasta o turno. Até 3 por batalha.", 16, 42, 448, 24, 14, Theme.Muted, TextAnchor.MiddleLeft);
            float y = 78;
            foreach (var item in content.Items)
            {
                string itemId = item.Id;
                var row = Ui.Frame(itemsPanel, "Item", 12, y, 456, 104, Theme.PanelLight, Theme.BorderSubtle, 1.5f);
                Ui.Label(row, item.Name, 14, 8, 300, 28, 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
                Ui.Label(row, item.Description, 14, 36, 300, 40, 14, Theme.Muted, TextAnchor.UpperLeft);
                Ui.Label(row, "No inventário: " + services.Profile.ItemCount(itemId), 14, 76, 300, 22, 14, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(row, item.Price + " moedas", 320, 8, 124, 28, 17, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                var buy = Ui.Button(row, "Comprar", 320, 44, 124, 46, () => Report(services.Store.BuyItem(itemId), item.Name), Theme.ButtonPrimary, 18);
                buy.interactable = services.Profile.Coins >= item.Price;
                y += 112;
            }

            // ── Progressão de Atributos ──
            var upgrades = Ui.Frame(Root, "Upgrades", 530, 76, 720, 540, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Label(upgrades, "Forja e Treinamento", 16, 10, 688, 34, 24, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(upgrades, "Cada evolução soma 3% do valor base. Máximo de " + services.Economy.MaxUpgradeLevel + " por atributo.",
                16, 42, 688, 24, 14, Theme.Muted, TextAnchor.MiddleLeft);

            var characters = content.PlayableCharacters;
            for (int i = 0; i < characters.Count; i++)
            {
                var def = characters[i];
                string id = def.Id;
                bool selected = id == _selectedId;
                Ui.Button(upgrades, def.Name, 16 + (i % 3) * 232, 76 + (i / 3) * 44, 224, 38,
                    () => { _selectedId = id; Rebuild(); }, selected ? Theme.ElementColor(def.Element) : Theme.Button, 15);
            }

            var chosen = content.Character(_selectedId);
            var heroCard = Ui.Frame(upgrades, "HeroCard", 16, 214, 688, 86, Theme.PanelLight, Theme.ElementColor(chosen.Element), 2f);
            Ui.Picture(heroCard, "Icon", Art.Fighter(chosen.Id, chosen.Element, chosen.Class), 10, 7, 72, 72);
            if (!Art.HasSprite(chosen.Id))
                Ui.Label(heroCard, Theme.ClassLetter(chosen.Class), 10, 7, 72, 72, 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(heroCard, chosen.Name, 96, 10, 576, 34, 26, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(heroCard, Theme.ElementName(chosen.Element) + " · " + Theme.ClassName(chosen.Class) + " · " + chosen.Appearance,
                96, 46, 576, 26, 16, Theme.ElementColor(chosen.Element), TextAnchor.MiddleLeft, FontStyle.Bold);

            float rowY = 308;
            foreach (var stat in Stats)
            {
                Stat current = stat;
                int level = services.Profile.UpgradeLevel(_selectedId, stat);
                int cost = services.Store.UpgradeCost(_selectedId, stat);
                int baseValue = chosen.BaseStat(stat);
                int now = StatsResolver.Scale(baseValue, 1 + services.Economy.UpgradePercentPerLevel * level);

                var row = Ui.Frame(upgrades, "Stat", 16, rowY, 688, 50, Theme.PanelLight, Theme.BorderSubtle, 1f);
                Ui.Label(row, Theme.StatName(stat), 14, 0, 130, 50, 19, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(row, "Nível " + level + "/" + services.Economy.MaxUpgradeLevel, 144, 0, 110, 50, 16, Theme.Muted, TextAnchor.MiddleLeft);

                if (cost < 0)
                {
                    Ui.Label(row, now + "  (máximo)", 256, 0, 200, 50, 17, Theme.Good, TextAnchor.MiddleLeft, FontStyle.Bold);
                }
                else
                {
                    int next = StatsResolver.Scale(baseValue, 1 + services.Economy.UpgradePercentPerLevel * (level + 1));
                    Ui.Label(row, now + "  →  " + next, 256, 0, 150, 50, 17, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                    Ui.Label(row, cost + " moedas", 410, 0, 120, 50, 16, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                    var buy = Ui.Button(row, "Evoluir", 544, 6, 132, 38,
                        () => Report(services.Store.BuyUpgrade(_selectedId, current), Theme.StatName(current) + " de " + chosen.Name), Theme.ButtonPrimary, 17);
                    buy.interactable = services.Profile.Coins >= cost;
                }
                rowY += 56;
            }

            Ui.Button(Root, "Voltar", 30, 628, 190, 56, App.ShowMenu, Theme.Button, 20);
            Ui.Label(Root, "Recompensas de Arena — Vitória: 100 moedas · Derrota: 50 · Boss Final: 500", 240, 628, 800, 56, 16, Theme.Muted, TextAnchor.MiddleLeft);
        }

        private void Report(PurchaseResult result, string what)
        {
            switch (result)
            {
                case PurchaseResult.Ok: App.Notify("Comprado: " + what + "."); break;
                case PurchaseResult.NotEnoughCoins: App.Notify("Moedas insuficientes."); break;
                case PurchaseResult.MaxLevelReached: App.Notify("Este atributo já está no nível máximo."); break;
                default: App.Notify("Item indisponível."); break;
            }
            Rebuild();
        }
    }
}
