using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Mapa da História: Fogo, Água, Planta e Boss Final. Cada fase libera a seguinte (GDD R10).</summary>
    public sealed class CampaignMapScreen : UiScreen
    {
        protected override void Build()
        {
            var services = App.Services;
            Ui.Box(Root, "Background", 0, 0, Theme.Width, Theme.Height, Theme.PanelSolid);
            Ui.Label(Root, "Mapa da História", 0, 20, Theme.Width, 56, 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(Root, "Vença os três treinadores e o Boss Final. Fases vencidas podem ser repetidas para juntar moedas.",
                0, 78, Theme.Width, 30, 18, Theme.Muted);

            var stages = services.Campaign.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var opponent = stages[i];
                bool unlocked = services.Campaign.IsUnlocked(i);
                bool cleared = services.Campaign.IsCleared(i);
                var arena = services.Content.Catalog.Arenas[opponent.HomeArena];

                var card = Ui.Box(Root, "Stage" + i, 44 + i * 302, 130, 286, 470, Theme.PanelLight);
                var picture = Ui.Picture(card.transform, "Arena", Art.Arena(opponent.HomeArena), 10, 10, 266, 150);
                if (!unlocked) picture.color = new Color(0.25f, 0.25f, 0.28f, 1f);

                Ui.Label(card.transform, "Fase " + (i + 1), 10, 168, 266, 24, 16, Theme.Muted);
                Ui.Label(card.transform, opponent.Name, 10, 192, 266, 36, 26, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(card.transform, opponent.Description, 14, 232, 258, 66, 16, Theme.Muted, TextAnchor.UpperCenter);
                Ui.Label(card.transform, "Arena " + arena.Name + "\n" + Theme.ArenaEffect(arena, services.Rules), 10, 300, 266, 48, 14, Theme.Text);
                Ui.Label(card.transform, "Recompensa: " + opponent.Reward + " moedas", 10, 352, 266, 26, 17, Theme.Gold);

                string status = cleared ? "Vencida" : unlocked ? "Disponível" : "Bloqueada";
                Ui.Label(card.transform, status, 10, 380, 266, 26, 17, cleared ? Theme.Good : unlocked ? Theme.Text : Theme.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);

                int stageIndex = i;
                var button = Ui.Button(card.transform, cleared ? "Lutar de novo" : "Lutar", 28, 410, 230, 48,
                    () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.Campaign, StageIndex = stageIndex }, 0),
                    unlocked ? Theme.ButtonPrimary : Theme.Button, 22);
                button.interactable = unlocked;
            }

            Ui.Button(Root, "Voltar", 44, 636, 180, 52, App.ShowMenu, Theme.Button);
            Ui.Label(Root, "Moedas: " + services.Profile.Coins, 980, 636, 256, 52, 22, Theme.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
        }
    }
}
