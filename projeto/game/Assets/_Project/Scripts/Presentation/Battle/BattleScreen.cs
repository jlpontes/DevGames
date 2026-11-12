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
        private const float FighterY = 378f;

        private readonly Match _match;
        private BattleEngine _engine;
        private BattleSnapshot _view;
        private Coroutine _routine;
        private Coroutine _inputRoutine;
        private bool _confirmQuit;

        private ArenaStage _stage;
        private ArenaMedallion _medallion;
        private Image _dockBorder;
        private GameObject _turnMarker;
        private readonly List<Coroutine> _fxRoutines = new List<Coroutine>();
        private SkillSpec _incomingSkill;
        private ReactionResult? _syncedReaction;
        private float _animTimeScale = 1f;
        private bool _syncingReaction;
        private bool _lastStandTriggered;
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
                _fighters[side] = new FighterWidget(Root, FighterX[side], FighterY);
            for (int side = 0; side < 2; side++)
                _teams[side] = new TeamPanel(Root, side == 0 ? 16 : 850, 10, _match.SideNames[side], _view.Sides[side].Team.Length, side == 1);

            int interval = _engine.Rules.ArenaDrawInterval;
            _medallion = new ArenaMedallion(Root, 440, 10, 400, 86, interval > 0 && interval <= 5 ? interval : 0);

            var speed = Ui.Button(Root, SpeedText(), 540, 102, 96, 26, ToggleSpeed, Theme.Button, 12);
            _speedLabel = speed.GetComponentInChildren<Text>();
            Ui.Tooltip(speed, "Velocidade das animações", "Acelera as animações da batalha. A janela de reação (QTE) não muda.");
            var quit = Ui.Button(Root, "Desistir", 644, 102, 96, 26, Quit, Theme.Button, 12);
            _quitLabel = quit.GetComponentInChildren<Text>();
            Ui.Tooltip(quit, "Desistir", "Sai da batalha sem ganhar moedas nem gastar itens. Clique duas vezes para confirmar.");

            Ui.Shadow(Root, 16, 566, 1248, 146, 0.5f);
            var dock = Ui.Frame(Root, "CombatDock", 16, 566, 1248, 146, Theme.Panel, Theme.BorderGold, 2f);
            _dockBorder = dock.parent.GetComponent<Image>();
            var msgStrip = Ui.Box(dock, "MessageStrip", 6, 4, 1232, 30, new Color(0.02f, 0.02f, 0.03f, 0.55f));
            var gem = Ui.Picture(msgStrip.transform, "Gem", Art.Diamond, 10, 9, 12, 12);
            gem.color = Theme.Gold;
            _message = Ui.Label(msgStrip.transform, "", 30, 0, 1180, 30, Theme.FontBody + 2, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            _message.gameObject.AddComponent<TextReveal>();

            _menu = Ui.Node(Root, "Menu", 16, 602, 1248, 108);
            _overlay = Ui.Node(Root, "Overlay", 0, 0, Theme.Width, Theme.Height);
            Root.gameObject.AddComponent<HudTicker>().Tick = t =>
            {
                _teams[0].Pulse(t);
                _teams[1].Pulse(t);
                _medallion.Spin(t);
            };

            ShowActive(0);
            ShowActive(1);
            RefreshHud();
            Say("A batalha começa na arena " + CurrentArena.Name + "!");

            IBattleController side0 = new HumanController(this, _match.Mode == GameMode.LocalPvp);
            IBattleController side1 = _match.OpponentAi != null
                ? (IBattleController)new RuleBasedAIController(_match.OpponentAi, _match.Setup.Seed + 17)
                : new HumanController(this, true);
            var director = new BattleDirector(_engine, side0, side1, this);
            _routine = App.Host.StartCoroutine(Coroutines.Guard(RunBattle(director), OnError));
        }

        public override void Close()
        {
            foreach (var fx in _fxRoutines) if (fx != null) App.Host.StopCoroutine(fx);
            _fxRoutines.Clear();
            if (_routine != null) App.Host.StopCoroutine(_routine);
            if (_inputRoutine != null) App.Host.StopCoroutine(_inputRoutine);
            _routine = null;
            _inputRoutine = null;
        }

        private IEnumerator RunBattle(BattleDirector director)
        {
            yield return PlayBattleIntro();
            if (_match.Mode == GameMode.Campaign)
                yield return PlayStorySequence(CampaignStory.Chapter(_match.StageIndex).IntroDialogue);

            yield return director.Run();

            // Fim: paga as moedas, consome os itens usados, atualiza campanha, HP da jornada, espectro e missões, e salva.
            var result = App.Services.Launcher.Finish(_match, _engine);

            if (_match.Mode == GameMode.Campaign)
            {
                var chapter = CampaignStory.Chapter(_match.StageIndex);
                yield return PlayStorySequence(_engine.WinnerSide == 0 ? chapter.VictoryDialogue : chapter.DefeatDialogue);

                if (_engine.WinnerSide == 0 && result.CampaignCompleted)
                {
                    App.Show(new CutsceneScreen(CampaignStory.GetEpilogueCutscene(), CampaignStory.EpilogueCutsceneId, () => App.ShowResult(_match, result)));
                    yield break;
                }
            }
            else if (_match.Mode == GameMode.Mission)
            {
                yield return PlayStorySequence(result.MissionCompleted
                    ? CampaignStory.GetMissionVictory(_match.Mission)
                    : CampaignStory.GetMissionDefeat(_match.Mission));
            }

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
            _speedLabel.text = SpeedText();
        }

        private static string SpeedText() { return "Vel. " + Speed + "\u00D7"; }

        private void Quit()
        {
            if (!_confirmQuit)
            {
                _confirmQuit = true;
                _quitLabel.text = "Confirmar?";
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
            var cover = Ui.Box(_overlay, "PassDevice", 0, 0, Theme.Width, Theme.Height, Theme.WithAlpha(Theme.Obsidian, 0.97f));
            cover.raycastTarget = true;
            var rune = Ui.Picture(cover.transform, "Rune", Art.VfxRuneCircle, 440, 160, 400, 400);
            rune.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.10f);
            var card = Ui.Frame(cover.transform, "PassCard", 300, 190, 680, 340, Theme.Panel, Theme.BorderGold, 2.5f);
            Ui.Label(card, "PASSE O APARELHO PARA", 20, 34, 636, 30, Theme.FontHeading, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(card, _match.SideNames[side], 20, 70, 636, 70, 52, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(card, "O outro jogador não pode ver a sua escolha de turno.", 20, 150, 636, 26, Theme.FontBody + 1, Theme.Text);
            var ready = Ui.Button(card, "Estou pronto", 188, 214, 300, 68, () =>
            {
                UnityEngine.Object.Destroy(cover.gameObject);
                onReady();
            }, Theme.ButtonPrimary, 24);
            ready.gameObject.AddComponent<KeyShortcut>().Key = KeyCode.Space;
        }

        private void ShowActionMenu(int side, Action<ICommand> reply)
        {
            ClearMenu();
            var state = _engine.State.Sides[side];
            var active = state.Active;
            MenuHeader("SUA VEZ  ·  " + _match.SideNames[side].ToUpperInvariant() + "  ·  ação de " + active.Spec.Name,
                "1  2  3 golpes  ·  Q mochila  ·  E trocar");
            ShowTurnMarker(side);

            Color element = Theme.ElementColor(active.Spec.Element);
            for (int i = 0; i < active.Spec.Skills.Count && i < 3; i++)
            {
                var skill = active.Spec.Skills[i];
                var command = new AttackCommand(side, i);
                var preview = _engine.PreviewDamage(side, i);
                bool legal = _engine.IsLegal(command);
                SkillCard(skill, i, preview, legal, element, state.Stamina, () => Choose(command, reply));
            }

            int itemCount = 0;
            foreach (var pair in state.Items) if (pair.Value > 0) itemCount += pair.Value;
            var bag = ActionCard(764, 228, "Mochila", itemCount > 0 ? itemCount + (itemCount == 1 ? " item" : " itens") + " · gasta o turno" : "vazia",
                "Q", KeyCode.Q, () => ShowItemMenu(side, reply), Theme.Button);
            bag.interactable = itemCount > 0;
            Ui.Tooltip(bag, "Mochila", "Usar um item consome a ação deste turno.");

            int bench = 0;
            for (int i = 0; i < state.Team.Length; i++) if (i != state.ActiveIndex && !state.Team[i].IsFainted) bench++;
            var swap = ActionCard(1004, 228, "Trocar lutador", bench > 0 ? bench + " no banco · troca antes dos golpes" : "ninguém no banco",
                "E", KeyCode.E, () => ShowSwitchMenu(side, reply), Theme.Button);
            swap.interactable = state.HasBenchAlive;
            Ui.Tooltip(swap, "Trocar lutador", "A troca acontece no início do turno, antes dos golpes. O novo lutador recebe o golpe do adversário.");
        }

        /// <summary>Carta de golpe: nome, dano estimado, efeito, efetividade contra o alvo e atalho de teclado.</summary>
        private void SkillCard(SkillSpec skill, int index, DamageResult preview, bool legal, Color element, int stamina, Action onPick)
        {
            float x = 14 + index * 250, w = 240, h = 80;
            bool special = skill.IsSpecial;
            var button = Ui.Button(_menu, "", x, 24, w, h, onPick, special && legal ? Theme.ButtonPrimary : Theme.Button);
            button.interactable = legal;
            button.gameObject.name = "Skill" + index;
            var t = button.transform;
            float dim = legal ? 1f : 0.45f;

            Ui.Box(t, "Accent", 5, 8, 3, h - 16, Theme.WithAlpha(special ? Theme.Gold : element, 0.9f * dim));
            var key = Ui.Frame(t, "Key", 13, 8, 20, 20, Theme.PanelSolid, Theme.BorderGold, 1f);
            Ui.Label(key, (index + 1).ToString(), 0, 0, 18, 18, 12, Theme.WithAlpha(Theme.Gold, dim), TextAnchor.MiddleCenter, FontStyle.Bold);
            var nameLabel = Ui.Label(t, skill.Name, 40, 7, w - 46, 22, special && skill.Name.Length > 18 ? 14 : 16,
                Theme.WithAlpha(special ? Theme.Gold : Theme.Text, dim), TextAnchor.MiddleLeft, FontStyle.Bold, special);
            nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            if (special)
                Ui.Label(t, "ESPECIAL", w - 82, 55, 72, 18, Theme.FontCaption, legal ? Theme.Gold : Theme.WithAlpha(Theme.Muted, 0.8f), TextAnchor.MiddleRight, FontStyle.Bold, true);

            string hint = Theme.SkillHint(skill, _engine.Catalog);
            if (special && !legal)
            {
                Ui.Label(t, "Stamina " + stamina + " / " + _engine.Rules.StaminaMax, 14, 34, w - 24, 18, Theme.FontSmall, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                var gauge = new UiBar(t, 14, 59, w - 104, 10, Theme.Stamina, true);
                gauge.SetInstant(stamina / (float)_engine.Rules.StaminaMax);
            }
            else
            {
                Ui.Label(t, "~" + preview.Amount + " de dano", 14, 33, 110, 20, Theme.FontSmall + 1, Theme.WithAlpha(Theme.Gold, dim), TextAnchor.MiddleLeft, FontStyle.Bold);
                if (hint.Length > 0)
                    Ui.Label(t, hint, 120, 33, w - 128, 20, Theme.FontCaption + 1, Theme.WithAlpha(Theme.Muted, dim), TextAnchor.MiddleRight);
                string tag = EffectivenessTag(preview);
                if (tag.Length > 0)
                    Ui.Label(t, tag, 14, 55, w - (special ? 96 : 24), 18, Theme.FontCaption, Theme.WithAlpha(TagColor(preview), dim), TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            if (index < 3) button.gameObject.AddComponent<KeyShortcut>().Key = KeyCode.Alpha1 + index;
            string body = "Poder " + skill.Power + (special ? " · gasta toda a stamina" : "") +
                          "\nDano estimado: " + preview.Amount + (EffectivenessTag(preview).Length > 0 ? " (" + EffectivenessTag(preview).ToLowerInvariant() + ")" : "") +
                          (hint.Length > 0 ? "\nEfeito: " + hint : "") +
                          (special && !legal ? "\nPrecisa da stamina cheia (" + _engine.Rules.StaminaMax + ")." : "");
            Ui.Tooltip(button, skill.Name, body);
        }

        private static string EffectivenessTag(DamageResult preview)
        {
            string tag = preview.Effectiveness == Effectiveness.SuperEffective ? "▲ SUPER EFETIVO"
                : preview.Effectiveness == Effectiveness.NotEffective ? "▼ POUCO EFETIVO" : "";
            if (preview.ArenaMultiplier > 1.001) tag += (tag.Length > 0 ? "  ·  " : "") + "ARENA +10%";
            else if (preview.ArenaMultiplier < 0.999) tag += (tag.Length > 0 ? "  ·  " : "") + "ARENA −10%";
            return tag;
        }

        private static Color TagColor(DamageResult preview)
        {
            if (preview.Effectiveness == Effectiveness.SuperEffective) return Theme.Good;
            if (preview.Effectiveness == Effectiveness.NotEffective) return Theme.Bad;
            return preview.ArenaMultiplier >= 1.0 ? Theme.Good : Theme.Bad;
        }

        /// <summary>Carta secundária (mochila, troca, item, voltar): título, linha de apoio e atalho.</summary>
        private Button ActionCard(float x, float w, string title, string subtitle, string keyLabel, KeyCode key, Action onPick, Color color)
        {
            var button = Ui.Button(_menu, "", x, 24, w, 80, onPick, color);
            var t = button.transform;
            if (!string.IsNullOrEmpty(keyLabel))
            {
                var badge = Ui.Frame(t, "Key", 13, 8, 20, 20, Theme.PanelSolid, Theme.BorderGold, 1f);
                Ui.Label(badge, keyLabel, 0, 0, 18, 18, 12, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                button.gameObject.AddComponent<KeyShortcut>().Key = key;
            }
            var titleLabel = Ui.Label(t, title, 40, 7, w - 48, 24, 17, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            var sub = Ui.Label(t, subtitle, 14, 36, w - 24, 38, Theme.FontSmall, Theme.Muted, TextAnchor.UpperLeft);
            button.gameObject.AddComponent<CardDimmer>().Setup(button, titleLabel, sub);
            return button;
        }

        private void MenuHeader(string title, string hint)
        {
            var gem = Ui.Picture(_menu, "HeaderGem", Art.Diamond, 16, 5, 12, 12);
            gem.color = Theme.Gold;
            Ui.Label(_menu, title, 34, 0, 860, 22, Theme.FontBody, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (!string.IsNullOrEmpty(hint)) Ui.Label(_menu, hint, 860, 0, 372, 22, Theme.FontCaption, Theme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
        }

        private void ShowItemMenu(int side, Action<ICommand> reply)
        {
            ClearMenu();
            var state = _engine.State.Sides[side];
            MenuHeader("MOCHILA  ·  usar um item consome o turno", "Esc volta");
            ShowTurnMarker(side);
            int column = 0;
            foreach (var pair in state.Items)
            {
                if (pair.Value <= 0 || column >= 3) continue;
                var item = _engine.Catalog.Items[pair.Key];
                var command = new UseItemCommand(side, item.Id);
                var card = ActionCard(14 + column * 250, 240, item.Name + "  ×" + pair.Value, item.Description,
                    (column + 1).ToString(), KeyCode.Alpha1 + column, () => Choose(command, reply), Theme.Button);
                card.interactable = _engine.IsLegal(command);
                Ui.Tooltip(card, item.Name, item.Description);
                column++;
            }
            ActionCard(1004, 228, "Voltar", "ao menu de ações", "Esc", KeyCode.Escape, () => ShowActionMenu(side, reply), Theme.Button);
        }

        private void ShowSwitchMenu(int side, Action<ICommand> reply)
        {
            ClearMenu();
            var state = _engine.State.Sides[side];
            MenuHeader("TROCAR LUTADOR  ·  a troca acontece no início do turno", "Esc volta");
            ShowTurnMarker(side);
            int column = 0;
            for (int i = 0; i < state.Team.Length; i++)
            {
                if (i == state.ActiveIndex) continue;
                var command = new SwitchCommand(side, i);
                var card = BenchCard(_menu, 14 + column * 372, 24, 360, 80, state.Team[i], () => Choose(command, reply));
                card.interactable = _engine.IsLegal(command);
                card.gameObject.AddComponent<KeyShortcut>().Key = KeyCode.Alpha1 + column;
                column++;
            }
            ActionCard(1004, 228, "Voltar", "ao menu de ações", "Esc", KeyCode.Escape, () => ShowActionMenu(side, reply), Theme.Button);
        }

        /// <summary>Carta de lutador do banco: retrato, nome, elemento e classe, HP e efeitos.</summary>
        private Button BenchCard(Transform parent, float x, float y, float w, float h, Fighter fighter, Action onPick)
        {
            var spec = fighter.Spec;
            Color element = Theme.ElementColor(spec.Element);
            var button = Ui.Button(parent, "", x, y, w, h, onPick, Theme.Button);
            var t = button.transform;
            var portrait = Ui.Frame(t, "Portrait", 8, 8, h - 16, h - 16, Theme.ElementDark(spec.Element), element, 1.5f);
            var icon = Ui.Picture(portrait, "Icon", Art.Fighter(spec.Id, spec.Element, spec.Class), 2, 2, h - 23, h - 23);
            if (fighter.IsFainted) icon.color = new Color(0.4f, 0.4f, 0.42f, 0.6f);
            float tx = h + 2;
            Ui.Label(t, spec.Name, tx, 6, w - tx - 10, 22, 16, fighter.IsFainted ? Theme.Muted : Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(t, Theme.ElementName(spec.Element).ToUpperInvariant() + " · " + Theme.ClassName(spec.Class).ToUpperInvariant(),
                tx, 28, w - tx - 10, 16, Theme.FontCaption, Theme.ElementLight(spec.Element), TextAnchor.MiddleLeft, FontStyle.Bold);
            if (fighter.IsFainted)
            {
                Ui.Label(t, "CAÍDO", tx, 48, w - tx - 10, 20, Theme.FontSmall, Theme.Bad, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
            else
            {
                float fraction = fighter.Hp / (float)spec.MaxHp;
                var bar = new UiBar(t, tx, 50, w - tx - 84, 12, Theme.HpColor(fraction));
                bar.SetInstant(fraction);
                Ui.Label(t, fighter.Hp + " / " + spec.MaxHp, w - 80, 46, 70, 20, Theme.FontCaption + 1, Theme.Text, TextAnchor.MiddleRight, FontStyle.Bold);
            }
            Ui.Tooltip(button, spec.Name, "Ataque " + spec.Attack + " · Defesa " + spec.Defense + " · Velocidade " + spec.Speed +
                (fighter.Effects.Count > 0 ? "\nEfeitos: " + EffectList(fighter) : ""));
            return button;
        }

        private static string EffectList(Fighter fighter)
        {
            var parts = new List<string>();
            foreach (var effect in fighter.Effects) parts.Add(effect.Spec.Name + " (" + effect.TurnsLeft + ")");
            return string.Join(" · ", parts.ToArray());
        }

        private static string BenchLabel(Fighter fighter)
        {
            if (fighter.IsFainted) return fighter.Spec.Name + "\nderrotado";
            return fighter.Spec.Name + "\nHP " + fighter.Hp + " / " + fighter.Spec.MaxHp + " · " +
                   Theme.ElementName(fighter.Spec.Element) + " · " + Theme.ClassName(fighter.Spec.Class);
        }

        private void ShowTurnMarker(int side)
        {
            if (_turnMarker != null) UnityEngine.Object.Destroy(_turnMarker);
            var widget = _fighters[side];
            var marker = Ui.Picture(Root, "TurnMarker", Art.Diamond, 0, 0, 22, 26);
            Ui.PlaceCentered(marker.rectTransform, widget.CenterX, widget.CenterY - 132f, 22f, 26f);
            marker.color = Theme.Gold;
            marker.gameObject.AddComponent<TurnMarker>().Setup(marker);
            _turnMarker = marker.gameObject;
        }

        private void ClearMenu()
        {
            Ui.Clear(_menu);
            if (_turnMarker != null) UnityEngine.Object.Destroy(_turnMarker);
            _turnMarker = null;
        }

        private void Choose(ICommand command, Action<ICommand> reply)
        {
            ClearMenu();
            _confirmQuit = false;
            _quitLabel.text = "Desistir";
            reply(command);
        }

        public void AskForcedSwitch(int side, Action<int> reply)
        {
            var state = _engine.State.Sides[side];
            var dim = Ui.Box(_overlay, "ForcedSwitch", 0, 0, Theme.Width, Theme.Height, new Color(0f, 0f, 0f, 0.62f));
            dim.raycastTarget = true;
            var panel = Ui.Frame(dim.transform, "Panel", 270, 200, 740, 250, Theme.PanelSolid, Theme.BorderGold, 2.5f);
            Ui.Label(panel, _match.SideNames[side].ToUpperInvariant() + "  ·  QUEM ENTRA NA ARENA?", 10, 16, 716, 36, 24, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(panel, "O lutador caiu. Escolha quem assume o combate.", 10, 52, 716, 22, Theme.FontBody, Theme.Muted);

            var options = new List<int>();
            for (int i = 0; i < state.Team.Length; i++)
                if (i != state.ActiveIndex && !state.Team[i].IsFainted) options.Add(i);
            for (int n = 0; n < options.Count; n++)
            {
                int index = options[n];
                float x = options.Count == 1 ? 188f : 16f + n * 358f;
                var card = BenchCard(panel, x, 100, 346, 96, state.Team[index], () =>
                {
                    UnityEngine.Object.Destroy(dim.gameObject);
                    reply(index);
                });
                card.gameObject.AddComponent<KeyShortcut>().Key = KeyCode.Alpha1 + n;
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

            var barFrame = Ui.Box(root, "BarFrame", barX - 4f, barY - 4f, barWidth + 8f, barHeight + 8f, frameColor);
            barFrame.sprite = Art.FrameSprite;
            barFrame.type = Image.Type.Sliced;
            barFrame.pixelsPerUnitMultiplier = 1.25f;
            var bar = Ui.Box(barFrame.transform, "Bar", 4f, 4f, barWidth, barHeight, new Color(0.04f, 0.05f, 0.08f, 0.97f));
            bar.sprite = Art.BarSprite;
            bar.type = Image.Type.Sliced;

            Color goodBaseColor = new Color(0.20f, 0.64f, 0.98f, 0.92f);
            Color perfBaseColor = Theme.Gold;
            var goodBox = Ui.Box(bar.transform, "Good", targetX - goodWidth * 0.5f, 3f, goodWidth, barHeight - 6f, goodBaseColor);
            var perfBox = Ui.Box(bar.transform, "Perfect", targetX - perfectWidth * 0.5f, 3f, perfectWidth, barHeight - 6f, perfBaseColor);

            var trail = Ui.Picture(bar.transform, "Trail", Art.VfxShard, 0, 0, 44f, barHeight - 4f);
            trail.color = new Color(1f, 0.92f, 0.65f, 0f);
            var markerGlow = Ui.Picture(bar.transform, "MarkerGlow", Art.VfxOrb, 0, 0, 34f, barHeight + 22f);
            markerGlow.color = new Color(1f, 0.95f, 0.78f, 0.75f);
            goodBox.sprite = Art.BarSprite;
            goodBox.type = Image.Type.Sliced;
            perfBox.sprite = Art.BarSprite;
            perfBox.type = Image.Type.Sliced;
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
    }
}
