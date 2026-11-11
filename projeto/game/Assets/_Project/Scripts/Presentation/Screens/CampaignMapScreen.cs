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
            Ui.Backdrop(Root);
            Ui.Label(Root, "Mapa da História", 44, 14, 600, 42, 34, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(Root, "Vença os três treinadores elementais e desafie o Titã Lunar. Fases vencidas podem ser repetidas.",
                44, 50, 860, 24, 15, Theme.Muted, TextAnchor.MiddleLeft);

            var coins = Ui.Frame(Root, "Coins", 1020, 18, 216, 44, Theme.Panel, Theme.BorderGold);
            Ui.Label(coins.transform, "Moedas: " + services.Profile.Coins, 0, 0, 212, 40, 20, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            var stages = services.Campaign.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var opponent = stages[i];
                bool unlocked = services.Campaign.IsUnlocked(i);
                bool cleared = services.Campaign.IsCleared(i);
                var arena = services.Content.Catalog.Arenas[opponent.HomeArena];
                Color accent = arena.UsesSpeedBands ? Theme.Gold : Theme.ElementColor(arena.Favoured);
                Color border = !unlocked ? new Color(0.24f, 0.22f, 0.28f, 0.7f) : cleared ? Theme.Good : accent;

                var card = Ui.Frame(Root, "Stage" + i, 44 + i * 302, 108, 286, 500, Theme.PanelLight, border, 2.5f);
                var picture = Ui.Picture(card.transform, "Arena", Art.Arena(opponent.HomeArena), 10, 10, 262, 148);
                if (!unlocked) picture.color = new Color(0.25f, 0.25f, 0.28f, 1f);

                // Exibe os lutadores do oponente sobre a prévia da arena
                int count = opponent.TeamIds.Count;
                for (int t = 0; t < count; t++)
                {
                    var def = services.Content.Character(opponent.TeamIds[t]);
                    float size = count == 1 ? 92f : 68f;
                    float spacing = 74f;
                    float startX = 141f - ((count - 1) * spacing) * 0.5f - size * 0.5f;
                    var icon = Ui.Picture(card.transform, "Fighter" + t, Art.Fighter(def.Id, def.Element, def.Class), startX + t * spacing, 152f - size, size, size);
                    if (!unlocked) icon.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);
                }

                var badge = Ui.Box(card.transform, "StageBadge", 10, 10, 84, 26, new Color(0.05f, 0.04f, 0.07f, 0.85f));
                Ui.Label(badge.transform, "FASE " + (i + 1), 0, 0, 84, 26, 13, accent, TextAnchor.MiddleCenter, FontStyle.Bold);

                Ui.Label(card.transform, opponent.Name, 10, 168, 262, 34, 24, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                Ui.Label(card.transform, opponent.Description, 14, 206, 254, 64, 15, Theme.Muted, TextAnchor.UpperCenter);

                var arenaBox = Ui.Frame(card.transform, "ArenaBox", 12, 276, 258, 60, Theme.Panel, Theme.BorderSubtle, 1f);
                Ui.Label(arenaBox.transform, "Arena: " + arena.Name, 6, 4, 246, 22, 15, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(arenaBox.transform, Theme.ArenaEffect(arena, services.Rules), 6, 26, 246, 28, 12, Theme.Text);

                Ui.Label(card.transform, "Recompensa: " + opponent.Reward + " moedas", 10, 346, 262, 26, 16, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

                string status = cleared ? "★ VENCIDA" : unlocked ? "DISPONÍVEL" : "BLOQUEADA";
                Ui.Label(card.transform, status, 10, 376, 262, 26, 15, cleared ? Theme.Good : unlocked ? Theme.Text : Theme.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);

                int stageIndex = i;
                var button = Ui.Button(card.transform, cleared ? "Lutar de novo" : "Desafiar", 26, 418, 230, 52,
                    () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.Campaign, StageIndex = stageIndex }, 0),
                    unlocked ? Theme.ButtonPrimary : Theme.Button, 21);
                button.interactable = unlocked;
            }

            Ui.Button(Root, "Voltar ao Menu", 44, 636, 200, 52, App.ShowMenu, Theme.Button, 19);
        }
    }
}
