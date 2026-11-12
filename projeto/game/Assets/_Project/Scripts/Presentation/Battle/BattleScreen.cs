using System;
using System.Collections;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>
    /// Tela de batalha: arena, HUD, menu de ações, QTE, roleta de arena e "passe o aparelho".
    /// Esta parte monta a tela e coleta as entradas; BattleScreen.Playback toca os eventos do motor.
    /// </summary>
    public sealed partial class BattleScreen : UiScreen, IBattleView, IHumanInput
    {
        /// <summary>Multiplicador das animações (1× ou 2×). Não afeta o QTE.</summary>
        public static float Speed = 1f;

        private static readonly float[] FighterX = { 370f, 910f };
        private const float FighterY = 396f;

        private readonly Match _match;
        private BattleEngine _engine;
        private BattleSnapshot _view;
        private Coroutine _routine;
        private Coroutine _inputRoutine;
        private bool _confirmQuit;

        private ArenaStage _stage;
        private Image _topBorder;
        private Image _dockBorder;
        private SkillSpec _incomingSkill;
        private ReactionResult? _syncedReaction;
        private float _animTimeScale = 1f;
        private bool _syncingReaction;
        private Text _arenaName;
        private Text _arenaEffect;
        private Text _roundInfo;
        private Text _message;
        private Text _speedLabel;
        private Text _quitLabel;
        private RectTransform _menu;
        private RectTransform _overlay;
        private readonly FighterWidget[] _fighters = new FighterWidget[2];
        private readonly TeamPanel[] _teams = new TeamPanel[2];

        public BattleScreen(Match match)
        {
            _match = match;
        }

        protected override void Build()
        {
            _engine = new BattleEngine(_match.Setup);
            _view = new BattleSnapshot(_engine.State);
            // Seed e comandos reproduzem a batalha inteira: vão junto no relatório de erro.
            App.Errors.Context = () => "Batalha " + _match.Mode + " · seed " + _engine.Seed + "\n" + string.Join("\n", _engine.CommandLog);

            _stage = ArenaStage.Create(Root, _view.Arena);

            for (int side = 0; side < 2; side++)
            {
                _fighters[side] = new FighterWidget(Root, FighterX[side], FighterY);
                _teams[side] = new TeamPanel(Root, side == 0 ? 16 : 836, 10, _match.SideNames[side], _view.Sides[side].Team.Length);
            }

            var top = Ui.Frame(Root, "ArenaInfo", 456, 10, 368, 104, Theme.Panel, Theme.BorderGold, 2f);
            _topBorder = top.parent.GetComponent<Image>();
            _arenaName = Ui.Label(top, "", 8, 2, 352, 26, 20, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            _arenaEffect = Ui.Label(top, "", 8, 26, 352, 20, 12, Theme.Text);
            _roundInfo = Ui.Label(top, "", 8, 46, 352, 20, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            var speed = Ui.Button(top, "Velocidade: " + Speed + "×", 10, 68, 168, 28, ToggleSpeed, Theme.Button, 13);
            _speedLabel = speed.GetComponentInChildren<Text>();
            var quit = Ui.Button(top, "Desistir", 190, 68, 168, 28, Quit, Theme.Button, 13);
            _quitLabel = quit.GetComponentInChildren<Text>();

            var dock = Ui.Frame(Root, "CombatDock", 16, 566, 1248, 144, Theme.Panel, Theme.BorderGold, 2f);
            _dockBorder = dock.parent.GetComponent<Image>();
            var msgStrip = Ui.Box(dock, "MessageStrip", 4, 4, 1236, 32, new Color(0.05f, 0.04f, 0.04f, 0.72f));
            Ui.Box(dock, "Divider", 4, 36, 1236, 1.5f, Theme.BorderSubtle);
            _message = Ui.Label(msgStrip.transform, "", 14, 0, 1208, 32, 18, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            _menu = Ui.Node(Root, "Menu", 16, 604, 1248, 106);
            _overlay = Ui.Node(Root, "Overlay", 0, 0, Theme.Width, Theme.Height);

            ShowActive(0);
            ShowActive(1);
            RefreshHud();
            Say("A batalha começa na arena " + CurrentArena.Name + "!");

            IBattleController side0 = new HumanController(this, _match.Mode == GameMode.LocalPvp);
            IBattleController side1 = _match.OpponentAi != null
                ? (IBattleController)new SpecialAwareAiController(new RuleBasedAIController(_match.OpponentAi, _match.Setup.Seed + 17), () => _incomingSkill)
                : new HumanController(this, true);
            var director = new BattleDirector(_engine, side0, side1, this);
            _routine = App.Host.StartCoroutine(Coroutines.Guard(RunBattle(director), OnError));
        }

        public override void Close()
        {
            if (_routine != null) App.Host.StopCoroutine(_routine);
            if (_inputRoutine != null) App.Host.StopCoroutine(_inputRoutine);
            _routine = null;
            _inputRoutine = null;
        }

        private IEnumerator RunBattle(BattleDirector director)
        {
            yield return PlayBattleIntro();
            yield return director.Run();
            // Fim: paga as moedas, consome os itens usados, atualiza a campanha e salva (SYSTEM-DESIGN, Fluxo A).
            var result = App.Services.Launcher.Finish(_match, _engine);
            App.ShowResult(_match, result);
        }

        private void OnError(Exception e)
        {
            App.Errors.Report(e, "batalha");
        }

        private ArenaSpec CurrentArena { get { return _engine.Catalog.Arenas[_view.Arena]; } }

        // ── Botões fixos ─────────────────────────────────────────────────────────

        private void ToggleSpeed()
        {
            Speed = Speed > 1f ? 1f : 2f;
            _speedLabel.text = "Velocidade: " + Speed + "×";
        }

        private void Quit()
        {
            if (!_confirmQuit)
            {
                _confirmQuit = true;
                _quitLabel.text = "Confirmar desistência?";
                return;
            }
            // Desistir não paga moedas nem consome itens: o perfil só muda no fim de uma batalha (SYSTEM-DESIGN §3.5).
            App.ShowModeHome(_match.Mode);
        }

        // ── Entrada do jogador (IHumanInput) ─────────────────────────────────────

        public void AskAction(int side, bool passDevice, Action<ICommand> reply)
        {
            if (passDevice) ShowPassDevice(side, () => ShowActionMenu(side, reply));
            else ShowActionMenu(side, reply);
        }

        private void ShowPassDevice(int side, Action onReady)
        {
            var cover = Ui.Box(_overlay, "PassDevice", 0, 0, Theme.Width, Theme.Height, Theme.PanelSolid);
            cover.raycastTarget = true;
            var card = Ui.Frame(cover.transform, "PassCard", 280, 170, 720, 380, Theme.Panel, Theme.BorderGold, 2.5f);
            Ui.Label(card, "Passe o aparelho para", 20, 36, 680, 36, 26, Theme.Muted);
            Ui.Label(card, _match.SideNames[side], 20, 82, 680, 80, 56, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(card, "O outro jogador não pode ver a sua escolha de turno.", 20, 174, 680, 30, 18, Theme.Text);
            Ui.Button(card, "Estou pronto", 210, 250, 300, 68, () =>
            {
                UnityEngine.Object.Destroy(cover.gameObject);
                onReady();
            }, Theme.ButtonPrimary, 26);
        }

        private void ShowActionMenu(int side, Action<ICommand> reply)
        {
            Ui.Clear(_menu);
            var state = _engine.State.Sides[side];
            var active = state.Active;
            Ui.Label(_menu, _match.SideNames[side] + " — Escolha a ação de " + active.Spec.Name, 16, 2, 900, 22, 15, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

            for (int i = 0; i < active.Spec.Skills.Count; i++)
            {
                var skill = active.Spec.Skills[i];
                var command = new AttackCommand(side, i);
                int damage = _engine.PreviewDamage(side, i).Amount;
                string hint = Theme.SkillHint(skill, _engine.Catalog);
                string label = skill.Name + "\n~" + damage + " de dano" + (hint.Length > 0 ? " · " + hint : "");
                if (skill.IsSpecial) label = "★ ESPECIAL: " + label;

                bool legal = _engine.IsLegal(command);
                if (skill.IsSpecial && !legal) label = "★ ESPECIAL: " + skill.Name + "\nStamina " + state.Stamina + " / " + _engine.Rules.StaminaMax;
                var button = Ui.Button(_menu, label, 14 + i * 254, 26, 244, 72, () => Choose(command, reply),
                    skill.IsSpecial ? Theme.ButtonPrimary : Theme.Button, 14);
                button.interactable = legal;
            }

            bool hasItems = false;
            foreach (var pair in state.Items) if (pair.Value > 0) hasItems = true;
            var bag = Ui.Button(_menu, "Mochila", 784, 26, 216, 72, () => ShowItemMenu(side, reply), Theme.Button, 19);
            bag.interactable = hasItems;
            var swap = Ui.Button(_menu, "Trocar\nLutador", 1012, 26, 220, 72, () => ShowSwitchMenu(side, reply), Theme.Button, 17);
            swap.interactable = state.HasBenchAlive;
        }

        private void ShowItemMenu(int side, Action<ICommand> reply)
        {
            Ui.Clear(_menu);
            var state = _engine.State.Sides[side];
            Ui.Label(_menu, "Mochila — Usar um item consome o turno", 16, 2, 900, 22, 15, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            int column = 0;
            foreach (var pair in state.Items)
            {
                if (pair.Value <= 0) continue;
                var item = _engine.Catalog.Items[pair.Key];
                var command = new UseItemCommand(side, item.Id);
                var button = Ui.Button(_menu, item.Name + "  ×" + pair.Value + "\n" + item.Description, 14 + column * 254, 26, 244, 72,
                    () => Choose(command, reply), Theme.Button, 14);
                button.interactable = _engine.IsLegal(command);
                column++;
            }
            Ui.Button(_menu, "Voltar", 1012, 26, 220, 72, () => ShowActionMenu(side, reply), Theme.Button, 19);
        }

        private void ShowSwitchMenu(int side, Action<ICommand> reply)
        {
            Ui.Clear(_menu);
            var state = _engine.State.Sides[side];
            Ui.Label(_menu, "Trocar Lutador — A troca ocorre no início do turno", 16, 2, 900, 22, 15, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            int column = 0;
            for (int i = 0; i < state.Team.Length; i++)
            {
                if (i == state.ActiveIndex) continue;
                var command = new SwitchCommand(side, i);
                var button = Ui.Button(_menu, BenchLabel(state.Team[i]), 14 + column * 336, 26, 324, 72, () => Choose(command, reply),
                    Theme.ElementColor(state.Team[i].Spec.Element) * 0.72f, 15);
                button.interactable = _engine.IsLegal(command);
                column++;
            }
            Ui.Button(_menu, "Voltar", 1012, 26, 220, 72, () => ShowActionMenu(side, reply), Theme.Button, 19);
        }

        private static string BenchLabel(Fighter fighter)
        {
            if (fighter.IsFainted) return fighter.Spec.Name + "\nderrotado";
            return fighter.Spec.Name + "\nHP " + fighter.Hp + " / " + fighter.Spec.MaxHp + " · " +
                   Theme.ElementName(fighter.Spec.Element) + " · " + Theme.ClassName(fighter.Spec.Class);
        }

        private void Choose(ICommand command, Action<ICommand> reply)
        {
            Ui.Clear(_menu);
            _confirmQuit = false;
            _quitLabel.text = "Desistir";
            reply(command);
        }

        public void AskForcedSwitch(int side, Action<int> reply)
        {
            var state = _engine.State.Sides[side];
            var dim = Ui.Box(_overlay, "ForcedSwitch", 0, 0, Theme.Width, Theme.Height, new Color(0f, 0f, 0f, 0.60f));
            dim.raycastTarget = true;
            var panel = Ui.Frame(dim.transform, "Panel", 290, 220, 700, 260, Theme.Panel, Theme.BorderGold, 2.5f);
            Ui.Label(panel, _match.SideNames[side] + " — Escolha quem entra na arena", 10, 16, 680, 40, 26, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var options = new List<int>();
            for (int i = 0; i < state.Team.Length; i++)
                if (i != state.ActiveIndex && !state.Team[i].IsFainted) options.Add(i);
            for (int n = 0; n < options.Count; n++)
            {
                int index = options[n];
                float width = options.Count == 1 ? 400f : 320f;
                float x = options.Count == 1 ? 150f : 20f + n * 340f;
                Ui.Button(panel, BenchLabel(state.Team[index]), x, 90, width, 110, () =>
                {
                    UnityEngine.Object.Destroy(dim.gameObject);
                    reply(index);
                }, Theme.ElementColor(state.Team[index].Spec.Element) * 0.72f, 18);
            }
        }

        public void AskReaction(int side, float windowSeconds, Action<ReactionResult> reply)
        {
            if (_syncedReaction.HasValue)
            {
                var ready = _syncedReaction.Value;
                _syncedReaction = null;
                reply(ready);
                return;
            }
            var qte = ComputeQteForSide(side);
            _inputRoutine = App.Host.StartCoroutine(Coroutines.Guard(ReactionWindow(side, qte, reply), OnError));
        }

        private QteParams ComputeQteForSide(int defenderSide)
        {
            var defender = _engine.State.Sides[defenderSide].Active;
            var attacker = _engine.State.Sides[1 - defenderSide].Active;
            bool isSpecial = _incomingSkill != null && _incomingSkill.IsSpecial;
            return QteConfig.Compute(defender.Spec.Speed, attacker.Spec.Speed, isSpecial);
        }

        /// <summary>
        /// Janela de resposta dinâmica, fluida e sincronizada com o golpe inimigo (GDD §5.5, R3):
        /// o marcador corre junto com o avanço do ataque inimigo e atinge o alvo no exato momento do impacto.
        /// </summary>
        private IEnumerator ReactionWindow(int side, QteParams qte, Action<ReactionResult> reply)
        {
            var defender = _engine.State.Sides[side].Active;
            var defenderWidget = _fighters[side];
            int defSpeed = defender.Spec.Speed;
            bool isSpecial = qte.IsSpecial;

            const float barWidth = 440f, barHeight = 28f;
            const float barX = (Theme.Width - barWidth) * 0.5f, barY = 644f;
            float targetX = qte.TargetNormalized * barWidth;
            float goodWidth = Mathf.Clamp(2f * qte.GoodSeconds / qte.SweepSeconds, 0.04f, 0.55f) * barWidth;
            float perfectWidth = Mathf.Clamp(2f * qte.PerfectSeconds / qte.SweepSeconds, 0.018f, 0.28f) * barWidth;

            var root = Ui.Node(_overlay, "ReactionWindow", 0, 0, Theme.Width, Theme.Height);
            var hitBox = Ui.Box(root, "HitCatcher", 0, 0, Theme.Width, Theme.Height, new Color(0f, 0f, 0f, isSpecial ? 0.22f : 0.08f));
            hitBox.raycastTarget = true;
            var catcher = hitBox.gameObject.AddComponent<ReactionInputCatcher>();

            // Anel sincronizado no corpo do defensor que se fecha junto com o golpe inimigo até o momento exato do impacto.
            var bodyRing = Ui.Picture(root, "ImpactTelegraph", Art.VfxRing, 0, 0, 260f, 260f);
            var targetRing = Ui.Picture(root, "TargetCore", Art.VfxRing, 0, 0, 124f, 124f);
            Ui.PlaceCentered(targetRing.rectTransform, defenderWidget.CenterX, defenderWidget.CenterY, 124f, 124f);
            targetRing.color = new Color(1f, 0.85f, 0.30f, 0.35f);

            Color frameColor = isSpecial ? new Color(0.96f, 0.28f, 0.18f, 0.96f) : Theme.BorderGold;
            string headerText = isSpecial
                ? "⚡ " + _match.SideNames[side].ToUpper() + ": DEFENDA O ESPECIAL NO IMPACTO!"
                : _match.SideNames[side] + ": DEFENDA NO IMPACTO DO GOLPE!";
            var title = Ui.Label(root, headerText, 0, 604, Theme.Width, 34, 24, isSpecial ? Theme.Bad : Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            string speedSign = qte.SpeedBonusPercent >= 0 ? "+" + qte.SpeedBonusPercent + "%" : qte.SpeedBonusPercent + "%";
            string subText = isSpecial
                ? "Golpe Supremo em curso! Janela reduzida e velocidade extrema · Clique ou Espaço no impacto"
                : "VEL " + defSpeed + " (" + speedSign + " janela de reflexo) · Azul = Desvio · Dourado = Contra-ataque";
            Color subColor = !isSpecial && qte.SpeedBonusPercent >= 15 ? Theme.Good : Theme.Text;
            Ui.Label(root, subText, 0, 680, Theme.Width, 24, 14, subColor, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var barFrame = Ui.Box(root, "BarFrame", barX - 3f, barY - 3f, barWidth + 6f, barHeight + 6f, frameColor);
            var bar = Ui.Box(barFrame.transform, "Bar", 3f, 3f, barWidth, barHeight, new Color(0.04f, 0.05f, 0.08f, 0.96f));

            Color goodBaseColor = new Color(0.20f, 0.64f, 0.98f, 0.92f);
            Color perfBaseColor = Theme.Gold;
            var goodBox = Ui.Box(bar.transform, "Good", targetX - goodWidth * 0.5f, 3f, goodWidth, barHeight - 6f, goodBaseColor);
            var perfBox = Ui.Box(bar.transform, "Perfect", targetX - perfectWidth * 0.5f, 3f, perfectWidth, barHeight - 6f, perfBaseColor);

            var trail = Ui.Picture(bar.transform, "Trail", Art.VfxShard, 0, 0, 44f, barHeight - 4f);
            trail.color = new Color(1f, 0.92f, 0.65f, 0f);
            var markerGlow = Ui.Picture(bar.transform, "MarkerGlow", Art.VfxOrb, 0, 0, 34f, barHeight + 22f);
            markerGlow.color = new Color(1f, 0.95f, 0.78f, 0.75f);
            var marker = Ui.Box(bar.transform, "Marker", 0, -6f, 6f, barHeight + 12f, Color.white);
            Ui.PlaceCentered(marker.rectTransform, 0f, barHeight * 0.5f, 6f, barHeight + 12f);
            Ui.PlaceCentered(markerGlow.rectTransform, 0f, barHeight * 0.5f, 34f, barHeight + 22f);

            // Breve entrada suave (0.14s) enquanto o atacante inicia o movimento, sem punir clique residual.
            float leadIn = 0f;
            while (leadIn < QteConfig.LeadInSeconds)
            {
                leadIn += Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(leadIn / QteConfig.LeadInSeconds);
                markerGlow.color = new Color(1f, 0.95f, 0.78f, 0.75f * a);
                Ui.PlaceCentered(bodyRing.rectTransform, defenderWidget.CenterX, defenderWidget.CenterY, 270f, 270f);
                bodyRing.color = new Color(1f, 0.9f, 0.6f, 0.35f * a);
                yield return null;
            }

            bool ignoreInitial = catcher.Pressed;

            ReactionResult result = ReactionResult.Miss;
            float elapsed = 0f;
            float targetTime = qte.TargetNormalized * qte.SweepSeconds;
            float limit = qte.SweepSeconds + qte.GoodSeconds * 0.75f;
            float finalMarkerX = barWidth;

            while (elapsed <= limit)
            {
                float norm = Mathf.Clamp01(elapsed / qte.SweepSeconds);
                float mx = norm * barWidth;
                finalMarkerX = mx;

                Ui.PlaceCentered(marker.rectTransform, mx, barHeight * 0.5f, 6f, barHeight + 12f);
                Ui.PlaceCentered(markerGlow.rectTransform, mx, barHeight * 0.5f, 34f, barHeight + 22f);
                Ui.PlaceCentered(trail.rectTransform, Mathf.Max(12f, mx - 18f), barHeight * 0.5f, Mathf.Min(44f, mx), barHeight - 6f);
                trail.color = new Color(isSpecial ? 1f : 0.65f, 0.88f, 1f, 0.48f);

                // Sincroniza o anel de impacto sobre o lutador defensor com a aproximação do marcador até o alvo.
                float timeError = Mathf.Abs(elapsed - targetTime);
                bool inPerfect = timeError <= qte.PerfectSeconds;
                bool inGood = timeError <= qte.GoodSeconds;
                goodBox.color = inGood ? Color.Lerp(goodBaseColor, Color.white, 0.42f) : goodBaseColor;
                perfBox.color = inPerfect ? Color.white : perfBaseColor;

                float approachRatio = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, targetTime));
                float ringSize = Mathf.Lerp(270f, 124f, approachRatio);
                Color ringCol = inPerfect ? Theme.Gold : inGood ? goodBaseColor : new Color(1f, 0.75f, 0.45f, 0.55f);
                Ui.PlaceCentered(bodyRing.rectTransform, defenderWidget.CenterX, defenderWidget.CenterY, ringSize, ringSize);
                bodyRing.color = new Color(ringCol.r, ringCol.g, ringCol.b, inGood ? 0.92f : 0.55f);
                targetRing.color = new Color(ringCol.r, ringCol.g, ringCol.b, inGood ? 0.85f : 0.35f);

                if (isSpecial)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 28f);
                    barFrame.color = Color.Lerp(frameColor, Theme.Gold, pulse * 0.45f);
                }

                bool mouseOrTouchAdded = !ignoreInitial && catcher.Pressed;
                if (mouseOrTouchAdded || KeyOrMouseDown())
                {
                    result = QteConfig.Classify(elapsed - targetTime, qte.PerfectSeconds, qte.GoodSeconds);
                    break;
                }
                if (!catcher.Pressed) ignoreInitial = false;

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            // Acelera o restante final da animação do golpe para a reação/impacto sair imediatamente no clique!
            _animTimeScale = 1.65f;

            Color resColor = result == ReactionResult.Counter ? Theme.Gold : result == ReactionResult.Dodge ? Theme.Good : Theme.Bad;
            title.text = BattleText.Reaction(result);
            title.color = resColor;
            marker.color = resColor;
            markerGlow.color = resColor;
            bodyRing.color = new Color(resColor.r, resColor.g, resColor.b, 0.9f);
            targetRing.color = new Color(resColor.r, resColor.g, resColor.b, 0f);

            var hitBurst = Ui.Picture(bar.transform, "Burst", Art.VfxRing, 0, 0, 24f, 24f);
            float burstTimer = 0f;
            while (burstTimer < 0.22f)
            {
                burstTimer += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(burstTimer / 0.22f);
                float sz = Mathf.Lerp(22f, 96f, p);
                Ui.PlaceCentered(hitBurst.rectTransform, finalMarkerX, barHeight * 0.5f, sz, sz);
                hitBurst.color = new Color(resColor.r, resColor.g, resColor.b, 1f - p);
                Ui.PlaceCentered(bodyRing.rectTransform, defenderWidget.CenterX, defenderWidget.CenterY, Mathf.Lerp(124f, 220f, p), Mathf.Lerp(124f, 220f, p));
                bodyRing.color = new Color(resColor.r, resColor.g, resColor.b, 1f - p);
                yield return null;
            }

            UnityEngine.Object.Destroy(root.gameObject);
            _inputRoutine = null;
            reply(result);
        }

        private static bool KeyOrMouseDown()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }

        /// <summary>Ajusta a reação da IA quando ela tenta defender um Golpe Especial (Super) ou quando há grande diferença de Velocidade.</summary>
        private sealed class SpecialAwareAiController : IBattleController
        {
            private readonly RuleBasedAIController _inner;
            private readonly Func<SkillSpec> _currentSkill;

            public SpecialAwareAiController(RuleBasedAIController inner, Func<SkillSpec> currentSkill)
            {
                _inner = inner;
                _currentSkill = currentSkill;
            }

            public void RequestAction(IBattleEngine engine, int side, Action<ICommand> reply)
            {
                _inner.RequestAction(engine, side, reply);
            }

            public void RequestReaction(IBattleEngine engine, int side, float windowSeconds, Action<ReactionResult> reply)
            {
                _inner.RequestReaction(engine, side, windowSeconds, raw =>
                {
                    var skill = _currentSkill();
                    if (skill != null && skill.IsSpecial)
                    {
                        // Contra um Super, contra-ataque vira no máximo desvio, e desvios normais falham na maioria das vezes.
                        if (raw == ReactionResult.Counter) raw = ReactionResult.Dodge;
                        else if (raw == ReactionResult.Dodge) raw = ReactionResult.Miss;
                    }
                    reply(raw);
                });
            }

            public void RequestForcedSwitch(IBattleEngine engine, int side, Action<int> reply)
            {
                _inner.RequestForcedSwitch(engine, side, reply);
            }
        }
    }
}
