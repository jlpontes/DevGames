using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>Tela de resultado: vitória ou derrota, moedas, quem entrou no espectro, descanso e o próximo passo.</summary>
    public sealed class ResultScreen : UiScreen
    {
        private readonly Match _match;
        private readonly MatchResult _result;

        public ResultScreen(Match match, MatchResult result)
        {
            _match = match;
            _result = result;
        }

        /// <summary>Retratos dos lutadores que acabaram de entrar no espectro.</summary>
        private void DrawNewFighters(Transform parent, float y)
        {
            int count = _result.NewFighters.Count;
            if (count == 0) return;
            Ui.Label(parent, count == 1 ? "NOVO NO ESPECTRO" : "NOVOS NO ESPECTRO", 40, y, 774, 20, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            float startX = 427f - count * 60f;
            for (int i = 0; i < count; i++)
            {
                var def = App.Services.Content.Character(_result.NewFighters[i]);
                var frame = Ui.Frame(parent, "New" + i, startX + i * 120f, y + 22, 112, 58, Theme.Panel, Theme.ElementColor(def.Element), 1.5f);
                Ui.Picture(frame, "Icon", Art.Fighter(def.Id, def.Element, def.Class), 4, 3, 50, 50);
                Ui.Label(frame, def.Name.Replace(" de ", "\nde "), 54, 2, 56, 52, 10, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
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
                    extra = "O Cristal Elemental está inteiro de novo!\nKaelen é o novo Guardião do Núcleo.";
                }
                else if (_result.UnlockedNextStage)
                {
                    var nextChapter = CampaignStory.Chapter(_match.StageIndex + 1);
                    extra = "Próximo: " + nextChapter.ActTitle + " (" + nextChapter.MasterName + ")";
                }
                else if (!_result.PlayerWon)
                {
                    extra = "O HP da jornada não mudou: a fase recomeça como estava.\nCure no Altar, evolua na Loja ou tente uma missão para reforçar o espectro.";
                }
                else
                {
                    extra = "Ato vencido de novo. O progresso e o HP da jornada foram salvos.";
                }

                Ui.Label(panel.transform, extra, 40, 190, 774, 52, 17, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                DrawNewFighters(panel.transform, 246);

                string restReport = CampaignStory.GetLastRestReport();
                if (_result.PlayerWon && !string.IsNullOrEmpty(restReport))
                {
                    var restBox = Ui.Frame(panel.transform, "RestBox", 60, 330, 734, 50, Theme.Panel, Theme.BorderSubtle, 1.5f);
                    Ui.Label(restBox, restReport, 12, 0, 710, 46, 13, Theme.Good, TextAnchor.MiddleCenter, FontStyle.Bold);
                }

                if (_result.PlayerWon && !_result.CampaignCompleted)
                {
                    Ui.Button(panel.transform, "Mapa da jornada", 48, 396, 210, 68, App.ShowCampaign, Theme.Button, 19);
                    Ui.Button(panel.transform, "Loja", 274, 396, 220, 68, App.ShowStore, Theme.Button, 20);
                    Ui.Button(panel.transform, "▶ Próximo ato", 510, 396, 300, 68, App.ContinueCampaignStory, Theme.ButtonPrimary, 21);
                }
                else
                {
                    Ui.Button(panel.transform, "Mapa da jornada", 96, 396, 310, 68, App.ShowCampaign, Theme.ButtonPrimary, 22);
                    Ui.Button(panel.transform, "Loja", 448, 396, 310, 68, App.ShowStore, Theme.Button, 22);
                }
            }
            else if (_match.Mode == GameMode.Mission)
            {
                string extra;
                if (_result.MissionCompleted && _result.MissionFirstClear)
                {
                    var recruit = App.Services.Content.Character(_match.Mission.RecruitId);
                    extra = "Missão cumprida!" + (_result.NewFighters.Count > 0 ? " " + recruit.Name + " entrou no seu espectro." : "");
                    if (_result.ItemRewardId != null) extra += "\nBônus: 1 " + App.Services.Content.Item(_result.ItemRewardId).Name + ".";
                }
                else if (_result.MissionCompleted)
                {
                    extra = "Missão cumprida de novo. A recompensa completa só vale na primeira vez.";
                }
                else
                {
                    extra = "Objetivo não cumprido: " + string.Join(", ", _result.FailedObjectives.ToArray()) + ".\nVocê pode tentar de novo quando quiser.";
                }
                Ui.Label(panel.transform, extra, 40, 190, 774, 52, 17, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                DrawNewFighters(panel.transform, 246);

                Ui.Button(panel.transform, "Quadro de missões", 96, 396, 310, 68, App.ShowMissions, Theme.ButtonPrimary, 22);
                Ui.Button(panel.transform, "Mapa da jornada", 448, 396, 310, 68, App.ShowCampaign, Theme.Button, 22);
            }
            else
            {
                Ui.Button(panel.transform, "Continuar", 126, 376, 280, 68, () => App.ShowModeHome(_match.Mode), Theme.ButtonPrimary, 24);
                Ui.Button(panel.transform, "Ir para a Loja", 448, 376, 280, 68, App.ShowStore, Theme.Button, 24);
            }
        }
    }
}
