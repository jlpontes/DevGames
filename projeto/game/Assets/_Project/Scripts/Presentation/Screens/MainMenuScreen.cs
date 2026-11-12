using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Menu principal: História, Contra IA, PvP e Loja (Caderno p.6, telas.excalidraw).</summary>
    public sealed class MainMenuScreen : UiScreen
    {
        private bool _confirmReset;

        protected override void Build()
        {
            // A arte do menu é 1024×559; cobre o palco inteiro com leve movimento de câmera e fagulhas do torneio.
            var bg = Ui.Picture(Root, "Background", Art.Menu, -24, -14, 1328, 748);
            Ui.AddAmbientSparks(Root, bg.rectTransform, new Color(1f, 0.74f, 0.30f, 0.52f), 18);

            // Moldura inferior integrada ao escudo do torneio, sem cobrir o emblema central.
            var dock = Ui.Frame(Root, "MenuDock", 100, 566, 1080, 104, new Color(0.06f, 0.05f, 0.08f, 0.86f), Theme.BorderGold, 2f);
            int stg = App.Services.Profile.CampaignStage;
            string storySub = stg >= 4 ? "Campanha Concluída ★" : "Salvo · Ato " + (stg + 1) + " de 4";
            string[] labels = { "História", "Contra IA", "PvP Local", "Loja" };
            string[] subs = { storySub, "Treino tático", "2 jogadores", "Itens e evolução" };
            System.Action[] actions = { App.ShowCampaign, App.ShowVsAi, App.ShowPvp, App.ShowStore };
            for (int i = 0; i < labels.Length; i++)
            {
                float bx = 18 + i * 264;
                var btn = Ui.Button(dock.transform, "", bx, 12, 248, 76, actions[i], i == 0 ? Theme.ButtonPrimary : Theme.Button, 24);
                Ui.Label(btn.transform, labels[i], 8, 8, 232, 34, 24, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                Ui.Label(btn.transform, subs[i], 8, 42, 232, 24, 13, i == 0 ? new Color(1f, 0.92f, 0.78f, 0.95f) : Theme.Muted);
            }

            var coins = Ui.Frame(Root, "Coins", 1036, 16, 224, 46, new Color(0.07f, 0.06f, 0.09f, 0.88f), Theme.BorderGold, 2f);
            Ui.Label(coins.transform, "Moedas: " + App.Services.Profile.Coins, 0, 0, 220, 42, 21, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            string warning = App.Services.Profile.StorageWarning;
            if (!string.IsNullOrEmpty(warning))
            {
                var box = Ui.Frame(Root, "Warning", 16, 16, 760, 46, Theme.Panel, Theme.BorderGold);
                Ui.Label(box.transform, warning, 10, 0, 740, 42, 16, Theme.Gold);
            }

            Ui.Label(Root, "Combo Tactics · Torneio das Arenas", 104, 680, 520, 28, 14, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Normal, true);
            Ui.Button(Root, _confirmReset ? "Confirmar: apagar tudo?" : "Zerar progresso", 980, 678, 200, 32, ResetProgress,
                _confirmReset ? Theme.ButtonDanger : new Color(0.12f, 0.11f, 0.16f, 0.85f), 14);
        }

        private void ResetProgress()
        {
            if (!_confirmReset)
            {
                _confirmReset = true;
            }
            else
            {
                App.Services.Profile.Reset();
                CampaignStory.ClearFatigue();
                _confirmReset = false;
                App.Notify("Progresso apagado.");
            }
            Rebuild();
        }
    }
}
