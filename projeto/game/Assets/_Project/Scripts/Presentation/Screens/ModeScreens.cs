using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Modo contra IA: escolha da dificuldade (perfis do ADR-004).</summary>
    public sealed class VsAiScreen : UiScreen
    {
        protected override void Build()
        {
            Ui.Backdrop(Root);
            Ui.Label(Root, "Batalha Contra IA", 44, 14, 600, 42, 34, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(Root, "A IA sorteia 3 personagens com atributos base. Você usa suas evoluções e até 3 itens do inventário.",
                44, 50, 900, 24, 15, Theme.Muted, TextAnchor.MiddleLeft);

            Color[] accents = { Theme.Good, Theme.Gold, Theme.Bad };
            var difficulties = App.Services.Content.Difficulties;
            for (int i = 0; i < difficulties.Count; i++)
            {
                var profile = difficulties[i];
                Color accent = accents[Mathf.Min(i, accents.Length - 1)];
                var card = Ui.Frame(Root, "Difficulty" + i, 120 + i * 356, 160, 328, 360, Theme.PanelLight, accent, 2.5f);
                Ui.Box(card.transform, "Banner", 0, 0, 323, 58, new Color(accent.r * 0.25f, accent.g * 0.25f, accent.b * 0.25f, 0.65f));
                Ui.Label(card.transform, profile.Name.ToUpperInvariant(), 10, 10, 304, 40, 28, accent, TextAnchor.MiddleCenter, FontStyle.Bold, true);

                var statsBox = Ui.Frame(card.transform, "Stats", 18, 78, 288, 160, Theme.Panel, Theme.BorderSubtle, 1f);
                Ui.Label(statsBox.transform,
                    "Reflexo na defesa: " + Mathf.RoundToInt((float)profile.QteChance * 100) + "%\n\n" +
                    "Troca tática por vantagem: " + Mathf.RoundToInt((float)profile.SwitchChance * 100) + "%\n\n" +
                    "Margem de erro tático: " + Mathf.RoundToInt((float)profile.MistakeChance * 100) + "%",
                    14, 14, 260, 132, 16, Theme.Text);

                Ui.Label(card.transform, "Vitória: 100 moedas · Derrota: 50", 14, 250, 296, 24, 14, Theme.Gold);

                string id = profile.Id;
                Ui.Button(card.transform, "Escolher Dificuldade", 36, 286, 252, 52,
                    () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.VsAi, DifficultyId = id }, 0), Theme.ButtonPrimary, 20);
            }

            Ui.Button(Root, "Voltar ao Menu", 44, 636, 200, 52, App.ShowMenu, Theme.Button, 19);
        }
    }

    /// <summary>PvP local no mesmo aparelho, com o Modo Justo ligado por padrão (GDD R6).</summary>
    public sealed class PvpScreen : UiScreen
    {
        protected override void Build()
        {
            var profile = App.Services.Profile;
            Ui.Backdrop(Root);
            Ui.Label(Root, "Duelo PvP Local", 44, 14, 600, 42, 34, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(Root, "Dois jogadores no mesmo aparelho. Cada turno usa uma cortina de \"passe o aparelho\" para manter o segredo.",
                44, 50, 960, 24, 15, Theme.Muted, TextAnchor.MiddleLeft);

            var panel = Ui.Frame(Root, "Fair", 260, 154, 760, 300, Theme.PanelLight, profile.FairPvp ? Theme.Good : Theme.BorderGold, 2.5f);
            Ui.Label(panel.transform, "MODO JUSTO: " + (profile.FairPvp ? "ATIVADO" : "DESATIVADO"), 20, 22, 716, 44, 28,
                profile.FairPvp ? Theme.Good : Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var descBox = Ui.Frame(panel.transform, "Rules", 36, 78, 684, 122, Theme.Panel, Theme.BorderSubtle, 1f);
            Ui.Label(descBox.transform,
                profile.FairPvp
                    ? "Equilíbrio competitivo: ambos os jogadores lutam com atributos base de fábrica, ignorando as evoluções da loja."
                    : "Força total do perfil: ambos os jogadores utilizam as evoluções compradas na loja neste perfil.",
                20, 12, 644, 54, 17, Theme.Text);
            Ui.Label(descBox.transform, "Mochila de torneio para cada lado: 1 Kit Médico, 1 Remédio e 1 Poção de Ataque.",
                20, 72, 644, 38, 15, Theme.Gold);

            Ui.Button(panel.transform, profile.FairPvp ? "Alternar: Usar Evoluções" : "Alternar: Ligar Modo Justo",
                220, 222, 316, 48, ToggleFair, Theme.Button, 18);

            Ui.Button(Root, "Selecionar Equipes", 470, 492, 340, 68,
                () => App.ShowTeamSelect(new MatchRequest { Mode = GameMode.LocalPvp }, 0), Theme.ButtonPrimary, 26);
            Ui.Button(Root, "Voltar ao Menu", 44, 636, 200, 52, App.ShowMenu, Theme.Button, 19);
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
