using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Tela de resultado: vitória ou derrota, moedas ganhas, descanso do Grimório e avanço contínuo da história.</summary>
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

            var panel = Ui.Frame(Root, "Panel", 210, 108, 860, 504, Theme.PanelLight, color, 3f);
            Ui.Box(panel.transform, "HeaderGlow", 0, 0, 854, 104, new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 0.7f));
            Ui.Label(panel.transform, title, 20, 14, 814, 76, 54, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var rewardBox = Ui.Frame(panel.transform, "Reward", 90, 120, 674, 62, Theme.Panel, Theme.BorderGold, 1.5f);
            string detail = _match.Mode == GameMode.LocalPvp
                ? "Partidas de PvP local não rendem moedas."
                : "Recompensa: +" + _result.CoinsEarned + " moedas   ·   Saldo salvo: " + App.Services.Profile.Coins + " moedas";
            Ui.Label(rewardBox.transform, detail, 16, 0, 642, 58, 22, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            if (_match.Mode == GameMode.Campaign)
            {
                string extra;
                if (_result.CampaignCompleted)
                {
                    extra = "★ NOVO GUARDIÃO DO NÚCLEO! ★\nVocê reuniu os 3 Fragmentos ao Núcleo na Lua, restaurou o Cristal Elemental e uniu as 9 Lendas no seu Espectro!";
                }
                else if (_result.UnlockedNextStage)
                {
                    var nextChapter = CampaignStory.Chapter(_match.StageIndex + 1);
                    extra = "Fragmento Conquistado! Próxima Zona: " + nextChapter.ActTitle + " (" + nextChapter.MasterName + ")!\n" +
                            (nextChapter.NewlySummonedIds.Length > 0
                                ? "✦ O Fragmento conquistado integrará +3 novas Lendas ao seu Espectro na próxima Cutscene!"
                                : "✦ Os 3 Fragmentos estão reunidos! Todas as 9 Lendas estão prontas para o teste final na Lua!");
                }
                else if (!_result.PlayerWon)
                {
                    extra = "Suas lendas recuaram para o Espectro.\nCure seus lutadores no Santuário do Cristal ou evolua seus atributos na Loja antes de tentar novamente!";
                }
                else
                {
                    extra = "Provação concluída! Seu progresso e o estado do Espectro foram salvos.";
                }

                Ui.Label(panel.transform, extra, 40, 196, 774, 68, 17, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);

                string restReport = CampaignStory.GetLastRestReport();
                if (_result.PlayerWon && !string.IsNullOrEmpty(restReport))
                {
                    var restBox = Ui.Frame(panel.transform, "RestBox", 60, 274, 734, 52, Theme.Panel, Theme.BorderSubtle, 1.5f);
                    Ui.Label(restBox, "✦ " + restReport, 12, 0, 710, 48, 14, Theme.Good, TextAnchor.MiddleCenter, FontStyle.Bold);
                }

                if (_result.PlayerWon && !_result.CampaignCompleted)
                {
                    Ui.Button(panel.transform, "Ver Espectro", 48, 396, 210, 68, App.ShowCampaign, Theme.Button, 20);
                    Ui.Button(panel.transform, "Loja & Evolução", 274, 396, 220, 68, App.ShowStore, Theme.Button, 20);
                    Ui.Button(panel.transform, "▶ Próxima Fase (História)", 510, 396, 300, 68, App.ContinueCampaignStory, Theme.ButtonPrimary, 21);
                }
                else
                {
                    Ui.Button(panel.transform, "Voltar à Jornada", 96, 396, 310, 68, App.ShowCampaign, Theme.ButtonPrimary, 22);
                    Ui.Button(panel.transform, "Loja & Evolução", 448, 396, 310, 68, App.ShowStore, Theme.Button, 22);
                }
            }
            else
            {
                Ui.Button(panel.transform, "Continuar", 126, 376, 280, 68, () => App.ShowModeHome(_match.Mode), Theme.ButtonPrimary, 24);
                Ui.Button(panel.transform, "Ir para a Loja", 448, 376, 280, 68, App.ShowStore, Theme.Button, 24);
            }
        }
    }
}
