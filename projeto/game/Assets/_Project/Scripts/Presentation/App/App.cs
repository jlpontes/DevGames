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
        }

        public void Show(UiScreen screen)
        {
            if (_current != null && _current != screen) _current.Close();
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            Errors.Context = null;
            Redraw(screen);
        }

        internal void Redraw(UiScreen screen)
        {
            if (_currentRoot != null) UnityEngine.Object.Destroy(_currentRoot.gameObject);
            _current = screen;
            _currentRoot = Ui.Node(_screenLayer, screen.GetType().Name, 0, 0, Theme.Width, Theme.Height);
            screen.Open(this, _currentRoot);
        }

        // ── Fluxo de telas (telas.excalidraw) ────────────────────────────────────

        public void ShowMenu() { Show(new MainMenuScreen()); }
        public void ShowCampaign() { Show(new CampaignMapScreen()); }
        public void ShowStore() { Show(new StoreScreen()); }
        public void ShowVsAi() { Show(new VsAiScreen()); }
        public void ShowPvp() { Show(new PvpScreen()); }

        public void ShowTeamSelect(MatchRequest request, int player) { Show(new TeamSelectScreen(request, player)); }

        public void StartBattle(MatchRequest request)
        {
            var match = Services.Launcher.Create(request);
            CampaignStory.EnhanceCampaignMatch(match, Services.Content, Services.Rules);
            Show(new BattleScreen(match));
        }

        public void ShowResult(Match match, MatchResult result) { Show(new ResultScreen(match, result)); }

        /// <summary>Para onde voltar depois de uma batalha ou ao cancelar a seleção de equipe.</summary>
        public void ShowModeHome(GameMode mode)
        {
            if (mode == GameMode.Campaign) ShowCampaign();
            else ShowMenu();
        }

        // ── Avisos e erros ───────────────────────────────────────────────────────

        /// <summary>Aviso curto no topo da tela (moedas insuficientes, equipe incompleta...).</summary>
        public void Notify(string message)
        {
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            var box = Ui.Box(_overlayLayer, "Toast", 420, 12, 600, 48, new Color(0.10f, 0.10f, 0.14f, 0.97f));
            Ui.Label(box.transform, message, 10, 0, 580, 48, 18, Theme.Gold);
            _toast = box.gameObject;
            UnityEngine.Object.Destroy(_toast, 3f);
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
