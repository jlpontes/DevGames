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
            // A arte do menu é 1024×559; cobre o palco inteiro com uma sobra pequena dos lados.
            Ui.Picture(Root, "Background", Art.Menu, -20, 0, 1320, 720);
            Ui.Box(Root, "Shade", 0, 560, Theme.Width, 160, new Color(0f, 0f, 0f, 0.62f));

            string[] labels = { "História", "Contra IA", "PvP Local", "Loja" };
            System.Action[] actions = { App.ShowCampaign, App.ShowVsAi, App.ShowPvp, App.ShowStore };
            for (int i = 0; i < labels.Length; i++)
                Ui.Button(Root, labels[i], 104 + i * 274, 590, 250, 66, actions[i], i == 0 ? Theme.ButtonPrimary : Theme.Button, 26);

            var coins = Ui.Box(Root, "Coins", 1040, 16, 224, 44, Theme.Panel);
            Ui.Label(coins.transform, "Moedas: " + App.Services.Profile.Coins, 0, 0, 224, 44, 22, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            string warning = App.Services.Profile.StorageWarning;
            if (!string.IsNullOrEmpty(warning))
            {
                var box = Ui.Box(Root, "Warning", 16, 16, 760, 44, Theme.Panel);
                Ui.Label(box.transform, warning, 10, 0, 740, 44, 17, Theme.Gold);
            }

            Ui.Label(Root, "MVP 0.1 · Progresso salvo neste navegador", 20, 680, 520, 28, 15, Theme.Muted, TextAnchor.MiddleLeft);
            Ui.Button(Root, _confirmReset ? "Confirmar: apagar tudo?" : "Zerar progresso", 1040, 676, 224, 32, ResetProgress,
                _confirmReset ? Theme.ButtonDanger : new Color(0.16f, 0.17f, 0.22f, 0.9f), 15);
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
                _confirmReset = false;
                App.Notify("Progresso apagado.");
            }
            Rebuild();
        }
    }
}
