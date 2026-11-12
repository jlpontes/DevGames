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
            Ui.Label(Root, "Crônicas do Eclipse · Modo História", 44, 10, 680, 38, 32, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(Root, CampaignStory.CurrentActSummary(services.Profile.CampaignStage),
                44, 46, 940, 32, 14, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);

            var coins = Ui.Frame(Root, "Coins", 1020, 16, 216, 44, Theme.Panel, Theme.BorderGold);
            Ui.Label(coins.transform, "Moedas: " + services.Profile.Coins, 0, 0, 212, 40, 20, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            var stages = services.Campaign.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var opponent = stages[i];
                var chapter = CampaignStory.Chapter(i);
                bool unlocked = services.Campaign.IsUnlocked(i);
                bool cleared = services.Campaign.IsCleared(i);
                var arena = services.Content.Catalog.Arenas[opponent.HomeArena];
                Color accent = arena.UsesSpeedBands ? Theme.Gold : Theme.ElementColor(arena.Favoured);
                Color border = !unlocked ? new Color(0.24f, 0.22f, 0.28f, 0.7f) : cleared ? Theme.Good : accent;

                var card = Ui.Frame(Root, "Stage" + i, 44 + i * 302, 92, 286, 524, Theme.PanelLight, border, 2.5f);
                var picture = Ui.Picture(card.transform, "Arena", Art.Arena(opponent.HomeArena), 10, 10, 262, 144);
                if (!unlocked) picture.color = new Color(0.25f, 0.25f, 0.28f, 1f);

                // Exibe os lutadores do oponente sobre a prévia da arena
                int count = opponent.TeamIds.Count;
                for (int t = 0; t < count; t++)
                {
                    var def = services.Content.Character(opponent.TeamIds[t]);
                    float size = count == 1 ? 92f : 68f;
                    float spacing = 74f;
                    float startX = 141f - ((count - 1) * spacing) * 0.5f - size * 0.5f;
                    var icon = Ui.Picture(card.transform, "Fighter" + t, Art.Fighter(def.Id, def.Element, def.Class), startX + t * spacing, 148f - size, size, size);
                    if (!unlocked) icon.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);
                }

                var badge = Ui.Box(card.transform, "StageBadge", 10, 10, 106, 26, new Color(0.05f, 0.04f, 0.07f, 0.88f));
                Ui.Label(badge.transform, "CAPÍTULO " + (i + 1), 0, 0, 106, 26, 12, accent, TextAnchor.MiddleCenter, FontStyle.Bold);

                Ui.Label(card.transform, chapter.MasterName, 10, 160, 262, 30, 20, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                Ui.Label(card.transform, chapter.MasterTitle + " · " + chapter.SealName, 10, 188, 262, 20, 12, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(card.transform, chapter.MapSynopsis, 14, 212, 254, 66, 13, Theme.Muted, TextAnchor.UpperCenter);

                var arenaBox = Ui.Frame(card.transform, "ArenaBox", 12, 282, 258, 62, Theme.Panel, Theme.BorderSubtle, 1f);
                string bonusTag = i == 3 ? "Boss 2 Fases (Despertar <50% HP)" : "Atributos +" + Mathf.RoundToInt((float)chapter.ChallengingStatBonus * 100f) + "%";
                Ui.Label(arenaBox.transform, "Arena " + arena.Name + " · " + bonusTag, 6, 4, 246, 22, 13, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(arenaBox.transform, Theme.ArenaEffect(arena, services.Rules), 6, 28, 246, 28, 12, Theme.Text);

                Ui.Label(card.transform, "Recompensa: " + opponent.Reward + " moedas", 10, 352, 262, 24, 15, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

                string status = cleared ? "★ SELO CONQUISTADO" : unlocked ? "DESAFIAR AGORA" : "SELO ANTERIOR NECESSÁRIO";
                Ui.Label(card.transform, status, 10, 380, 262, 24, 14, cleared ? Theme.Good : unlocked ? Theme.Text : Theme.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);

                int stageIndex = i;
                var button = Ui.Button(card.transform, cleared ? "Rejogar Capítulo" : "Entrar na Arena", 26, 434, 230, 54,
                    () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.Campaign, StageIndex = stageIndex }, 0),
                    unlocked ? Theme.ButtonPrimary : Theme.Button, 20);
                button.interactable = unlocked;
            }

            Ui.Button(Root, "Voltar ao Menu", 44, 636, 200, 52, App.ShowMenu, Theme.Button, 19);

            var fatigued = CampaignStory.GetFatiguedCharacters(services.Profile.CampaignStage, services.Profile.CampaignStage);
            if (fatigued.Count > 0)
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (var id in fatigued)
                {
                    var def = services.Content.Character(id);
                    if (def != null) names.Add(def.Name);
                }
                var restBox = Ui.Frame(Root, "FatigueInfo", 264, 636, 972, 52, Theme.Panel, Theme.BorderGold, 1.5f);
                Ui.Label(restBox, "★ Regra de Rodízio do Torneio — Em descanso da última vitória: " + string.Join(", ", names.ToArray()),
                    14, 0, 944, 48, 14, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
        }
    }
}
