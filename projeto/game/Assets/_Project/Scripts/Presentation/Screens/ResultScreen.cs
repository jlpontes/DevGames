using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Tela de resultado: vitória ou derrota, moedas ganhas e fase liberada.</summary>
    public sealed class ResultScreen : UiScreen
    {
        private readonly Match _match;
        private readonly MatchResult _result;

        public ResultScreen(Match match, MatchResult result)
        {
            _match = match;
            _result = result;
        }

        protected override void Build()
        {
            Ui.Backdrop(Root);

            string title;
            Color color;
            if (_result.WinnerSide < 0) { title = "EMPATE"; color = Theme.Gold; }
            else if (_match.Mode == GameMode.LocalPvp) { title = _match.SideNames[_result.WinnerSide] + " VENCEU!"; color = Theme.Good; }
            else if (_result.PlayerWon) { title = "VITÓRIA!"; color = Theme.Good; }
            else { title = "DERROTA"; color = Theme.Bad; }

            var panel = Ui.Frame(Root, "Panel", 270, 136, 740, 448, Theme.PanelLight, color, 3f);
            Ui.Box(panel.transform, "HeaderGlow", 0, 0, 734, 110, new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 0.7f));
            Ui.Label(panel.transform, title, 20, 18, 694, 78, 56, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var rewardBox = Ui.Frame(panel.transform, "Reward", 90, 132, 554, 68, Theme.Panel, Theme.BorderGold, 1.5f);
            string detail = _match.Mode == GameMode.LocalPvp
                ? "Partidas de PvP local não rendem moedas."
                : "Recompensa: +" + _result.CoinsEarned + " moedas   (Saldo: " + App.Services.Profile.Coins + ")";
            Ui.Label(rewardBox.transform, detail, 16, 0, 522, 64, 24, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            string extra = "";
            if (_result.CampaignCompleted) extra = "Você derrotou o Titã Lunar e conquistou a glória eterna do torneio!";
            else if (_result.UnlockedNextStage) extra = "Nova fase liberada: " + App.Services.Campaign.Stages[_match.StageIndex + 1].Name + "!";
            else if (_match.Mode == GameMode.Campaign && !_result.PlayerWon) extra = "Dica: repita fases anteriores ou visite a Loja para evoluir seus atributos.";
            Ui.Label(panel.transform, extra, 40, 220, 654, 64, 19, Theme.Text);

            Ui.Button(panel.transform, "Continuar", 96, 332, 260, 64, () => App.ShowModeHome(_match.Mode), Theme.ButtonPrimary, 24);
            Ui.Button(panel.transform, "Ir para a Loja", 378, 332, 260, 64, App.ShowStore, Theme.Button, 24);
        }
    }
}
