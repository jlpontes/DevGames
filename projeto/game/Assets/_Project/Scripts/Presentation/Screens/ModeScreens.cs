using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Modo contra IA: escolha da dificuldade (perfis do ADR-004).</summary>
    public sealed class VsAiScreen : UiScreen
    {
        protected override void Build()
        {
            Ui.Box(Root, "Background", 0, 0, Theme.Width, Theme.Height, Theme.PanelSolid);
            Ui.Label(Root, "Contra IA", 0, 40, Theme.Width, 56, 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(Root, "A IA sorteia 3 personagens com atributos base. Você usa suas evoluções e até 3 itens do inventário.",
                0, 100, Theme.Width, 30, 18, Theme.Muted);

            var difficulties = App.Services.Content.Difficulties;
            for (int i = 0; i < difficulties.Count; i++)
            {
                var profile = difficulties[i];
                var card = Ui.Box(Root, "Difficulty" + i, 130 + i * 350, 190, 320, 300, Theme.PanelLight);
                Ui.Label(card.transform, profile.Name, 10, 20, 300, 44, 32, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(card.transform,
                    "Acerta a defesa em " + Mathf.RoundToInt((float)profile.QteChance * 100) + "% dos golpes\n" +
                    "Troca por vantagem: " + Mathf.RoundToInt((float)profile.SwitchChance * 100) + "%\n" +
                    "Chance de errar a jogada: " + Mathf.RoundToInt((float)profile.MistakeChance * 100) + "%",
                    16, 80, 288, 110, 17, Theme.Muted);

                string id = profile.Id;
                Ui.Button(card.transform, "Escolher", 45, 220, 230, 52,
                    () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = id }, 0), Theme.ButtonPrimary, 22);
            }

            Ui.Button(Root, "Voltar", 44, 636, 180, 52, App.ShowMenu, Theme.Button);
        }
    }

    /// <summary>PvP local no mesmo aparelho, com o Modo Justo ligado por padrão (GDD R6).</summary>
    public sealed class PvpScreen : UiScreen
    {
        protected override void Build()
        {
            var profile = App.Services.Profile;
            Ui.Box(Root, "Background", 0, 0, Theme.Width, Theme.Height, Theme.PanelSolid);
            Ui.Label(Root, "PvP Local", 0, 40, Theme.Width, 56, 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(Root, "Dois jogadores no mesmo aparelho. Cada um escolhe a ação em segredo; uma tela de \"passe o aparelho\" separa as escolhas.",
                140, 104, 1000, 54, 18, Theme.Muted);

            var panel = Ui.Box(Root, "Fair", 290, 200, 700, 250, Theme.PanelLight);
            Ui.Label(panel.transform, "Modo Justo: " + (profile.FairPvp ? "LIGADO" : "DESLIGADO"), 20, 20, 660, 44, 30,
                profile.FairPvp ? Theme.Good : Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(panel.transform,
                profile.FairPvp
                    ? "Atributos base para os dois jogadores, sem as evoluções da loja."
                    : "Os dois jogadores usam os atributos com as evoluções compradas neste perfil.",
                30, 70, 640, 50, 18, Theme.Text);
            Ui.Label(panel.transform, "Cada jogador recebe o kit fixo: 1 Kit Médico, 1 Remédio e 1 Poção de Ataque. PvP não rende moedas.",
                30, 118, 640, 50, 16, Theme.Muted);
            Ui.Button(panel.transform, profile.FairPvp ? "Desligar Modo Justo" : "Ligar Modo Justo", 215, 182, 270, 46, ToggleFair, Theme.Button, 18);

            Ui.Button(Root, "Escolher equipes", 490, 490, 300, 66,
                () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.LocalPvp }, 0), Theme.ButtonPrimary, 26);
            Ui.Button(Root, "Voltar", 44, 636, 180, 52, App.ShowMenu, Theme.Button);
        }

        private void ToggleFair()
        {
            var profile = App.Services.Profile;
            profile.SetFairPvp(!profile.FairPvp);
            profile.Save();
            Rebuild();
        }
    }
}
