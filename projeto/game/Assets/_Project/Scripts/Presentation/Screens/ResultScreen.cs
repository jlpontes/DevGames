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
            Ui.Picture(Root, "Background", Art.Menu, -20, 0, 1320, 720).color = new Color(0.30f, 0.30f, 0.34f, 1f);
            var panel = Ui.Box(Root, "Panel", 290, 150, 700, 420, Theme.Panel);

            string title;
            Color color;
            if (_result.WinnerSide < 0) { title = "EMPATE"; color = Theme.Gold; }
            else if (_match.Mode == GameMode.LocalPvp) { title = _match.SideNames[_result.WinnerSide] + " venceu!"; color = Theme.Good; }
            else if (_result.PlayerWon) { title = "VITÓRIA!"; color = Theme.Good; }
            else { title = "DERROTA"; color = Theme.Bad; }
            Ui.Label(panel.transform, title, 20, 30, 660, 80, 60, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            string detail;
            if (_match.Mode == GameMode.LocalPvp) detail = "Partidas de PvP local não rendem moedas.";
            else detail = "+" + _result.CoinsEarned + " moedas   (total: " + App.Services.Profile.Coins + ")";
            Ui.Label(panel.transform, detail, 20, 130, 660, 40, 28, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            string extra = "";
            if (_result.CampaignCompleted) extra = "Você venceu o Boss Final e conquistou a glória eterna!";
            else if (_result.UnlockedNextStage) extra = "Nova fase liberada: " + App.Services.Campaign.Stages[_match.StageIndex + 1].Name + ".";
            else if (_match.Mode == GameMode.Campaign && !_result.PlayerWon) extra = "A fase pode ser repetida. Evolua seus personagens na Loja.";
            Ui.Label(panel.transform, extra, 30, 190, 640, 60, 20, Theme.Text);

            Ui.Button(panel.transform, "Continuar", 80, 310, 260, 66, () => App.ShowModeHome(_match.Mode), Theme.ButtonPrimary, 26);
            Ui.Button(panel.transform, "Loja", 360, 310, 260, 66, App.ShowStore, Theme.Button, 26);
        }
    }
}
