using System;
using ComboTactics.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>Uma tela do jogo. Guarda o próprio estado e redesenha tudo em Build().</summary>
    public abstract class UiScreen
    {
        protected App App { get; private set; }
        protected RectTransform Root { get; private set; }

        internal void Open(App app, RectTransform root)
        {
            App = app;
            Root = root;
            Build();
        }

        protected abstract void Build();

        /// <summary>Chamado quando a tela sai de cena. Pare coroutines aqui.</summary>
        public virtual void Close() { }

        /// <summary>Redesenha a tela com o estado atual (depois de uma compra ou de uma seleção, por exemplo).</summary>
        protected void Rebuild() { App.Redraw(this); }
    }

    /// <summary>
    /// Navegação entre telas e acesso aos serviços do jogo. Cada tela recebe este objeto;
    /// é o único lugar que sabe qual tela vem depois de qual.
    /// </summary>
    public sealed class App
    {
        public readonly GameServices Services;
        public readonly MonoBehaviour Host;
        public readonly ErrorReporter Errors;

        private readonly RectTransform _screenLayer;
        private readonly RectTransform _overlayLayer;
        private UiScreen _current;
        private RectTransform _currentRoot;
        private GameObject _toast;

        public App(GameServices services, MonoBehaviour host, RectTransform stage)
        {
            Services = services;
            Host = host;
            _screenLayer = Ui.Node(stage, "Screens", 0, 0, Theme.Width, Theme.Height);
            _overlayLayer = Ui.Node(stage, "Overlay", 0, 0, Theme.Width, Theme.Height);
            Errors = new ErrorReporter(ShowErrorDialog);
            Ui.OnPlayerMessage = Notify;
            Ui.OverlayRoot = _overlayLayer;
            CampaignStory.Bind(services);
        }

        public void Show(UiScreen screen)
        {
            if (_current != null && _current != screen) _current.Close();
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            Errors.Context = null;
            Redraw(screen);
            // Tela nova entra com um esmaecer curto; redesenhar a mesma tela (Rebuild) é instantâneo.
            _currentRoot.gameObject.AddComponent<ScreenFader>().Begin();
        }

        internal void Redraw(UiScreen screen)
        {
            // Redesenhar a mesma tela também encerra as coroutines dela; senão cada Rebuild deixa uma a mais rodando.
            if (_current == screen) screen.Close();
            if (_currentRoot != null) UnityEngine.Object.Destroy(_currentRoot.gameObject);
            _current = screen;
            _currentRoot = Ui.Node(_screenLayer, screen.GetType().Name, 0, 0, Theme.Width, Theme.Height);
            screen.Open(this, _currentRoot);
        }

        // ── Fluxo de telas (telas.excalidraw) ────────────────────────────────────

        public void ShowMenu() { Show(new MainMenuScreen()); }
        public void ShowStore() { Show(new StoreScreen()); }

        /// <summary>Mapa da jornada. Sem afinidade escolhida, começa pela abertura e pela escolha da afinidade.</summary>
        public void ShowCampaign()
        {
            if (!Services.Journey.HasAffinity) { StartJourney(); return; }
            Show(new CampaignMapScreen());
        }

        /// <summary>Contra IA e PvP usam o espectro (R11): sem afinidade, o jogador passa antes pela abertura da história.</summary>
        public void ShowVsAi()
        {
            if (!RequireSpectrum()) return;
            Show(new VsAiScreen());
        }

        public void ShowPvp()
        {
            if (!RequireSpectrum()) return;
            Show(new PvpScreen());
        }

        private bool RequireSpectrum()
        {
            if (Services.Journey.HasAffinity) return true;
            Notify("Escolha a afinidade do seu mago para formar o espectro de lutadores.");
            StartJourney();
            return false;
        }

        /// <summary>Abertura da história → escolha da afinidade → cartão do primeiro chamado → mapa.</summary>
        public void StartJourney()
        {
            Show(new CutsceneScreen(CampaignStory.GetPrologueCutscene(), -1, () => Show(new AffinitySelectScreen())));
        }

        /// <summary>Grava a afinidade e mostra os 3 lutadores entrando no espectro.</summary>
        public void ChooseAffinity(Core.Element affinity)
        {
            Services.Journey.ChooseAffinity(affinity);
            Show(new CutsceneScreen(CampaignStory.GetAffinityCutscene(affinity), -1, ShowCampaign));
        }

        /// <summary>Quadro das missões secundárias ("Ecos do Cristal").</summary>
        public void ShowMissions()
        {
            if (!Services.Journey.HasAffinity) { StartJourney(); return; }
            Show(new MissionBoardScreen());
        }

        /// <summary>Apresentação da missão → seleção de equipe com as regras dela.</summary>
        public void StartMission(MissionDef mission)
        {
            var request = new MatchRequest { Mode = GameMode.Mission, MissionId = mission.Id };
            Show(new CutsceneScreen(CampaignStory.GetMissionBriefing(mission), -1, () => ShowTeamSelect(request, 0)));
        }

        /// <summary>Botão único de História Contínua: retoma do ponto salvo (Cutscene do Ato -> Santuário/Equipe -> Batalha).</summary>
        public void ContinueCampaignStory()
        {
            if (!Services.Journey.HasAffinity) { StartJourney(); return; }
            int currentStage = Mathf.Clamp(Services.Profile.CampaignStage, 0, Services.Campaign.Stages.Count - 1);
            PlayCampaignStage(currentStage, false);
        }

        public void PlayCampaignStage(int stageIndex, bool forceCutscene)
        {
            int idx = Mathf.Clamp(stageIndex, 0, Services.Campaign.Stages.Count - 1);
            var chapter = CampaignStory.Chapter(idx);
            if ((forceCutscene || !CampaignStory.HasSeenCutscene(idx)) && chapter.ActCutscene != null && chapter.ActCutscene.Length > 0)
            {
                Show(new CutsceneScreen(chapter.ActCutscene, idx,
                    () => ShowTeamSelect(new MatchRequest { Mode = GameMode.Campaign, StageIndex = idx }, 0)));
                return;
            }
            ShowTeamSelect(new MatchRequest { Mode = GameMode.Campaign, StageIndex = idx }, 0);
        }

        public void ShowTeamSelect(MatchRequest request, int player) { Show(new TeamSelectScreen(request, player)); }

        public void StartBattle(MatchRequest request)
        {
            var match = Services.Launcher.Create(request);
            Show(new BattleScreen(match));
        }

        public void ShowResult(Match match, MatchResult result) { Show(new ResultScreen(match, result)); }

        /// <summary>Para onde voltar depois de uma batalha ou ao cancelar a seleção de equipe.</summary>
        public void ShowModeHome(GameMode mode)
        {
            if (mode == GameMode.Campaign) ShowCampaign();
            else if (mode == GameMode.Mission) ShowMissions();
            else ShowMenu();
        }

        // ── Avisos e erros ───────────────────────────────────────────────────────

        /// <summary>Aviso curto no topo da tela (moedas insuficientes, equipe incompleta...).</summary>
        public void Notify(string message)
        {
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            var frame = Ui.Frame(_overlayLayer, "Toast", 360, 14, 560, 54, Theme.PanelSolid, Theme.BorderGold, 2f);
            var gem = Ui.Picture(frame, "Gem", Art.Diamond, 14, 16, 18, 18);
            gem.color = Theme.Gold;
            Ui.Label(frame, message, 42, 0, 500, 50, Theme.FontBody + 1, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            _toast = frame.parent.gameObject;
            _toast.AddComponent<ToastAnimator>().Begin();
        }

        private void ShowErrorDialog(string message, string debugInfo)
        {
            var dim = Ui.Box(_overlayLayer, "ErrorDialog", 0, 0, Theme.Width, Theme.Height, new Color(0f, 0f, 0f, 0.80f));
            dim.raycastTarget = true;
            var panel = Ui.Box(dim.transform, "Panel", 240, 170, 800, 380, Theme.PanelSolid);
            Ui.Label(panel.transform, "Ops! Encontramos um erro", 20, 16, 760, 44, 30, Theme.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(panel.transform, message, 30, 70, 740, 170, 19, Theme.Text, TextAnchor.UpperCenter);
            Ui.Label(panel.transform, "Seu progresso salvo não foi afetado.", 30, 246, 740, 30, 17, Theme.Muted);

            Text copied = null;
            Ui.Button(panel.transform, "Copiar informações de debug", 60, 300, 330, 54, () =>
            {
                GUIUtility.systemCopyBuffer = debugInfo;
                if (copied != null) copied.text = "Copiado!";
            }, Theme.Button);
            copied = Ui.Label(panel.transform, "", 60, 354, 330, 22, 15, Theme.Good);
            Ui.Button(panel.transform, "Voltar ao menu", 410, 300, 330, 54, () =>
            {
                UnityEngine.Object.Destroy(dim.gameObject);
                Errors.DialogClosed();
                ShowMenu();
            }, Theme.ButtonPrimary);
        }
    }
}
