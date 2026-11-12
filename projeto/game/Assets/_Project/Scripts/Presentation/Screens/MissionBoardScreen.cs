using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Quadro das missões secundárias ("Ecos do Cristal"): lutadores desgarrados das suas zonas que desafiam o mago
    /// sob uma regra especial. Vencer pela primeira vez traz o lutador para o espectro antes do guardião da zona dele.
    /// </summary>
    public sealed class MissionBoardScreen : UiScreen
    {
        private const float CardW = 396f;
        private const float CardH = 168f;

        protected override void Build()
        {
            var services = App.Services;
            Ui.Backdrop(Root, Art.Arena(ArenaId.Forest), 0.22f);

            Ui.Label(Root, "ECOS DO CRISTAL · MISSÕES SECUNDÁRIAS", 36, 12, 900, 34, 26, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(Root, "Quando o cristal se partiu, alguns lutadores se perderam das suas zonas. Vença o desafio de cada um para trazê-lo ao espectro.",
                36, 46, 1000, 24, 14, Theme.Text, TextAnchor.MiddleLeft);
            var coins = Ui.Frame(Root, "Coins", 1048, 14, 196, 48, Theme.Panel, Theme.BorderGold, 1.5f);
            Ui.Label(coins, "Moedas: " + services.Profile.Coins, 0, 0, 192, 44, 18, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var missions = services.Missions.All;
            for (int i = 0; i < missions.Count; i++)
                DrawMission(missions[i], 36 + (i % 3) * (CardW + 8), 84 + (i / 3) * (CardH + 8));

            Ui.Button(Root, "Mapa da jornada", 36, 632, 240, 60, App.ShowCampaign, Theme.Button, 19);
            Ui.Button(Root, "Menu", 288, 632, 140, 60, App.ShowMenu, Theme.Button, 19);
            Ui.Label(Root, "Primeira vitória: o lutador entra no espectro, mais moedas e um bônus. Repetir rende " +
                services.Economy.MissionReplayReward + " moedas.", 444, 632, 800, 60, 13, Theme.Muted, TextAnchor.MiddleLeft);
        }

        private void DrawMission(MissionDef mission, float x, float y)
        {
            var services = App.Services;
            var recruit = services.Content.Character(mission.RecruitId);
            var state = services.Missions.State(mission);
            bool pending = services.Missions.RecruitPending(mission);
            Color color = Theme.ElementColor(recruit.Element);
            Color border = state == MissionState.Completed ? Theme.Good : state == MissionState.Locked ? Theme.BorderSubtle : color;

            var card = Ui.Frame(Root, "Mission_" + mission.Id, x, y, CardW, CardH,
                state == MissionState.Locked ? new Color(0.06f, 0.05f, 0.08f, 0.92f) : Theme.PanelLight, border, 2f);

            var portrait = Ui.Frame(card, "Portrait", 8, 8, 92, 92, Theme.Panel, border, 1.5f);
            var sprite = Ui.Picture(portrait, "Sprite", Art.Fighter(recruit.Id, recruit.Element, recruit.Class), 4, 4, 82, 82);
            if (state == MissionState.Locked) sprite.color = new Color(0.20f, 0.20f, 0.24f, 0.9f);

            Ui.Label(card, mission.Title, 108, 6, 280, 24, 17, state == MissionState.Locked ? Theme.Muted : Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(card, recruit.Name + " · " + recruit.Appearance, 108, 28, 280, 18, 12, color, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(card, mission.Summary, 108, 46, 280, 54, 12, Theme.Muted, TextAnchor.UpperLeft);
            Ui.Label(card, string.Join(" · ", mission.RuleTexts().ToArray()), 8, 104, 380, 18, 11, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

            string status;
            Color statusColor;
            if (state == MissionState.Locked) { status = services.Missions.LockedReason(mission); statusColor = Theme.Muted; }
            else if (state == MissionState.Completed) { status = "Concluída · " + recruit.Name + " está no espectro"; statusColor = Theme.Good; }
            else if (!pending) { status = "Já está no espectro · vale moedas"; statusColor = Theme.Muted; }
            else { status = "Recompensa: " + recruit.Name + " + " + mission.RewardCoins + " moedas" + RewardItem(mission); statusColor = Theme.Gold; }
            Ui.Label(card, status, 8, 126, 250, 34, 11, statusColor, TextAnchor.MiddleLeft, FontStyle.Bold);

            string label = state == MissionState.Completed ? "Jogar de novo" : "Aceitar";
            var accept = Ui.Button(card, label, 264, 124, 124, 36, () => App.StartMission(mission),
                state == MissionState.Available && pending ? Theme.ButtonPrimary : Theme.Button, 15);
            accept.interactable = state != MissionState.Locked;
        }

        private string RewardItem(MissionDef mission)
        {
            if (mission.RewardItemId == null) return "";
            return " + " + App.Services.Content.Item(mission.RewardItemId).Name;
        }
    }
}
