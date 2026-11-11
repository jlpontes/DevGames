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

            Ui.Box(Root, "Background", 0, 0, Theme.Width, Theme.Height, Theme.PanelSolid);
            Ui.Label(Root, "Loja", 30, 12, 400, 50, 36, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(Root, "Moedas: " + services.Profile.Coins, 900, 12, 350, 50, 28, Theme.Gold, TextAnchor.MiddleRight, FontStyle.Bold);

            // ── Loja de Itens ──
            var itemsPanel = Ui.Box(Root, "Items", 30, 76, 480, 540, Theme.PanelLight);
            Ui.Label(itemsPanel.transform, "Loja de Itens", 16, 10, 448, 34, 24, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(itemsPanel.transform, "Consumíveis: usar um item gasta o turno. Até 3 por batalha.", 16, 42, 448, 24, 14, Theme.Muted, TextAnchor.MiddleLeft);
            float y = 78;
            foreach (var item in content.Items)
            {
                string itemId = item.Id;
                var row = Ui.Box(itemsPanel.transform, "Item", 12, y, 456, 104, Theme.Panel);
                Ui.Label(row.transform, item.Name, 14, 8, 300, 28, 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(row.transform, item.Description, 14, 36, 300, 40, 14, Theme.Muted, TextAnchor.UpperLeft);
                Ui.Label(row.transform, "No inventário: " + services.Profile.ItemCount(itemId), 14, 76, 300, 22, 14, Theme.Text, TextAnchor.MiddleLeft);
                Ui.Label(row.transform, item.Price + " moedas", 320, 8, 124, 28, 17, Theme.Gold);
                var buy = Ui.Button(row.transform, "Comprar", 320, 44, 124, 46, () => Report(services.Store.BuyItem(itemId), item.Name), Theme.ButtonPrimary, 18);
                buy.interactable = services.Profile.Coins >= item.Price;
                y += 112;
            }

            // ── Progressão de Atributos ──
            var upgrades = Ui.Box(Root, "Upgrades", 530, 76, 720, 540, Theme.PanelLight);
            Ui.Label(upgrades.transform, "Progressão de Atributos", 16, 10, 688, 34, 24, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(upgrades.transform, "Cada evolução soma 3% do valor base. Máximo de " + services.Economy.MaxUpgradeLevel + " por atributo.",
                16, 42, 688, 24, 14, Theme.Muted, TextAnchor.MiddleLeft);

            var characters = content.PlayableCharacters;
            for (int i = 0; i < characters.Count; i++)
            {
                var def = characters[i];
                string id = def.Id;
                bool selected = id == _selectedId;
                Ui.Button(upgrades.transform, def.Name, 16 + (i % 3) * 232, 76 + (i / 3) * 44, 224, 38,
                    () => { _selectedId = id; Rebuild(); }, selected ? Theme.ElementColor(def.Element) : Theme.Button, 15);
            }

            var chosen = content.Character(_selectedId);
            Ui.Picture(upgrades.transform, "Icon", Art.Fighter(chosen.Id, chosen.Element, chosen.Class), 20, 222, 72, 72);
            if (!Art.HasSprite(chosen.Id))
                Ui.Label(upgrades.transform, Theme.ClassLetter(chosen.Class), 20, 222, 72, 72, 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(upgrades.transform, chosen.Name, 104, 224, 580, 34, 26, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(upgrades.transform, Theme.ElementName(chosen.Element) + " · " + Theme.ClassName(chosen.Class) + " · " + chosen.Appearance,
                104, 258, 580, 26, 16, Theme.Muted, TextAnchor.MiddleLeft);

            float rowY = 308;
            foreach (var stat in Stats)
            {
                Stat current = stat;
                int level = services.Profile.UpgradeLevel(_selectedId, stat);
                int cost = services.Store.UpgradeCost(_selectedId, stat);
                int baseValue = chosen.BaseStat(stat);
                int now = StatsResolver.Scale(baseValue, 1 + services.Economy.UpgradePercentPerLevel * level);

                var row = Ui.Box(upgrades.transform, "Stat", 16, rowY, 688, 50, Theme.Panel);
                Ui.Label(row.transform, Theme.StatName(stat), 14, 0, 130, 50, 19, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(row.transform, "Nível " + level + "/" + services.Economy.MaxUpgradeLevel, 144, 0, 110, 50, 16, Theme.Muted, TextAnchor.MiddleLeft);

                if (cost < 0)
                {
                    Ui.Label(row.transform, now + "  (máximo)", 256, 0, 200, 50, 17, Theme.Good, TextAnchor.MiddleLeft);
                }
                else
                {
                    int next = StatsResolver.Scale(baseValue, 1 + services.Economy.UpgradePercentPerLevel * (level + 1));
                    Ui.Label(row.transform, now + "  →  " + next, 256, 0, 150, 50, 17, Theme.Text, TextAnchor.MiddleLeft);
                    Ui.Label(row.transform, cost + " moedas", 410, 0, 120, 50, 16, Theme.Gold);
                    var buy = Ui.Button(row.transform, "Evoluir", 544, 6, 132, 38,
                        () => Report(services.Store.BuyUpgrade(_selectedId, current), Theme.StatName(current) + " de " + chosen.Name), Theme.ButtonPrimary, 17);
                    buy.interactable = services.Profile.Coins >= cost;
                }
                rowY += 56;
            }

            Ui.Button(Root, "Voltar", 30, 636, 180, 52, App.ShowMenu, Theme.Button);
            Ui.Label(Root, "Vitória: 100 moedas · Derrota: 50 · Boss Final: 500", 240, 636, 700, 52, 16, Theme.Muted, TextAnchor.MiddleLeft);
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
