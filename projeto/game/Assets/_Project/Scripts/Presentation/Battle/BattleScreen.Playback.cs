using System;
using System.Collections;
using ComboTactics.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>
    /// Reprodução dos eventos do motor (IBattleView). Cada evento atualiza a cópia visível do estado
    /// (BattleSnapshot) e toca a animação correspondente; nada aqui consulta as regras.
    /// </summary>
    public sealed partial class BattleScreen
    {
        public IEnumerator Play(BattleEvent ev)
        {
            var skill = ev as SkillUsed;
            if (skill != null) { yield return PlaySkill(skill); yield break; }

            var reaction = ev as ReactionResolved;
            if (reaction != null) { yield return PlayReaction(reaction); yield break; }

            var damage = ev as DamageDealt;
            if (damage != null)
            {
                string prefix = damage.Effectiveness == Effectiveness.SuperEffective ? "Super efetivo! "
                    : damage.Effectiveness == Effectiveness.NotEffective ? "Pouco efetivo... " : "";
                Say(prefix + Who(damage.TargetSide, damage.FighterIndex) + " sofreu " + damage.Amount + " de dano" +
                    (damage.IsCounter ? " no contra-ataque." : "."));
                yield return PlayHpChange(ev, damage.TargetSide, damage.FighterIndex, damage.HpAfter, "-" + damage.Amount, Theme.Bad, true);
                ResetPoses();
                yield break;
            }

            var healed = ev as Healed;
            if (healed != null)
            {
                Say(Who(healed.Side, healed.FighterIndex) + " recuperou " + healed.Amount + " de HP.");
                yield return PlayHpChange(ev, healed.Side, healed.FighterIndex, healed.HpAfter, "+" + healed.Amount, Theme.Good, false);
                yield break;
            }

            var ticked = ev as EffectTicked;
            if (ticked != null)
            {
                if (ticked.Delta == 0) { Apply(ev); yield break; }
                bool harm = ticked.Delta < 0;
                Say(Who(ticked.Side, ticked.FighterIndex) + (harm ? " sofreu " : " recuperou ") + Mathf.Abs(ticked.Delta) + " com " + ticked.Effect.Name + ".");
                yield return PlayHpChange(ev, ticked.Side, ticked.FighterIndex, ticked.HpAfter,
                    (harm ? "-" : "+") + Mathf.Abs(ticked.Delta), harm ? Theme.Bad : Theme.Good, false);
                yield break;
            }

            var applied = ev as EffectApplied;
            if (applied != null)
            {
                Apply(ev);
                Say(Who(applied.Side, applied.FighterIndex) + ": " + applied.Effect.Name + " por " + applied.Turns + " turnos.");
                yield return Wait(0.45f);
                yield break;
            }

            var cleansed = ev as EffectsCleansed;
            if (cleansed != null)
            {
                Apply(ev);
                Say(Who(cleansed.Side, cleansed.FighterIndex) + " ficou livre dos efeitos negativos.");
                yield return Wait(0.45f);
                yield break;
            }

            var item = ev as ItemUsed;
            if (item != null)
            {
                Apply(ev);
                Say(_match.SideNames[item.Side] + " usou " + item.Item.Name + ".");
                yield return Wait(0.6f);
                yield break;
            }

            var stamina = ev as StaminaChanged;
            if (stamina != null)
            {
                bool wasReady = _view.Sides[stamina.Side].Stamina >= _engine.Rules.StaminaMax;
                Apply(ev);
                if (!wasReady && stamina.Value >= _engine.Rules.StaminaMax)
                {
                    Say(_match.SideNames[stamina.Side] + ": ataque especial liberado!");
                    yield return Wait(0.45f);
                }
                yield break;
            }

            var switched = ev as Switched;
            if (switched != null) { yield return PlaySwitch(switched); yield break; }

            var fainted = ev as Fainted;
            if (fainted != null)
            {
                bool wasActive = fainted.FighterIndex == _view.Sides[fainted.Side].ActiveIndex;
                Say(Who(fainted.Side, fainted.FighterIndex) + " foi derrotado!");
                Apply(ev);
                if (wasActive)
                {
                    var widget = _fighters[fainted.Side];
                    yield return Tween(0.4f, t =>
                    {
                        widget.SetAlpha(Mathf.Lerp(1f, 0.25f, t));
                        widget.SetOffset(new Vector2(0f, -30f * t));
                    });
                }
                yield return Wait(0.35f);
                yield break;
            }

            var arena = ev as ArenaChanged;
            if (arena != null) { yield return PlayArenaDraw(arena); yield break; }

            var ended = ev as BattleEnded;
            if (ended != null)
            {
                Apply(ev);
                Say(ended.WinnerSide < 0 ? "Empate! Os dois últimos lutadores caíram juntos." : _match.SideNames[ended.WinnerSide] + " venceu a batalha!");
                yield return Wait(1.4f);
                yield break;
            }

            // RoundStarted, ReactionRequested, EffectExpired: só atualizam o HUD.
            Apply(ev);
        }

        // ── Animações ────────────────────────────────────────────────────────────

        private IEnumerator PlaySkill(SkillUsed e)
        {
            Apply(e);
            var widget = _fighters[e.Side];
            float direction = e.Side == 0 ? 1f : -1f;
            if (e.Skill.IsSpecial)
            {
                // Sem câmera de verdade no MVP: o "foco no personagem" do especial é um destaque com zoom.
                Say("ESPECIAL! " + Who(e.Side, e.FighterIndex) + " usou " + e.Skill.Name + "!");
                var flash = Ui.Box(_overlay, "SpecialFlash", 0, 0, Theme.Width, Theme.Height, new Color(1f, 0.9f, 0.5f, 0f));
                yield return Tween(0.6f, t =>
                {
                    widget.SetScale(1f + 0.35f * Mathf.Sin(t * Mathf.PI));
                    flash.color = new Color(1f, 0.9f, 0.5f, 0.35f * Mathf.Sin(t * Mathf.PI));
                });
                UnityEngine.Object.Destroy(flash.gameObject);
            }
            else
            {
                Say(Who(e.Side, e.FighterIndex) + " usou " + e.Skill.Name + "!");
            }
            yield return Tween(0.18f, t => widget.SetOffset(new Vector2(direction * 80f * t, 0f)));
            yield return Wait(0.15f);
        }

        private IEnumerator PlayReaction(ReactionResolved e)
        {
            Apply(e);
            if (e.Result == ReactionResult.Miss) yield break;

            var side = _view.Sides[e.DefenderSide];
            var widget = _fighters[e.DefenderSide];
            float away = e.DefenderSide == 0 ? -1f : 1f;
            Say(Who(e.DefenderSide, side.ActiveIndex) + (e.Result == ReactionResult.Counter ? " desviou e vai contra-atacar!" : " desviou do golpe!"));
            yield return Tween(0.25f, t => widget.SetOffset(new Vector2(away * 60f * Mathf.Sin(t * Mathf.PI), 40f * Mathf.Sin(t * Mathf.PI))));
            ResetPoses();
            yield return Wait(0.25f);
        }

        /// <summary>Dano ou cura: número flutuante, barra de HP animada e, no dano, flash vermelho com tremor.</summary>
        private IEnumerator PlayHpChange(BattleEvent ev, int side, int fighterIndex, int hpAfter, string popupText, Color color, bool shake)
        {
            bool isActive = fighterIndex == _view.Sides[side].ActiveIndex;
            var widget = _fighters[side];
            float from = isActive ? widget.ShownHp : hpAfter;
            Apply(ev);
            if (!isActive)
            {
                // Lutador no banco (efeitos continuam valendo lá): o cartão da equipe já mostra o HP novo.
                yield return Wait(0.45f);
                yield break;
            }

            widget.SetHp(from);
            var popup = Ui.Label(_overlay, popupText, widget.CenterX - 150f, 300f, 300f, 60f, 46, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            var popupRect = popup.rectTransform;
            Vector2 popupHome = popupRect.anchoredPosition;
            yield return Tween(0.55f, t =>
            {
                widget.SetHp(Mathf.Lerp(from, hpAfter, Mathf.Clamp01(t * 1.6f)));
                popupRect.anchoredPosition = popupHome + new Vector2(0f, 70f * t);
                popup.color = new Color(color.r, color.g, color.b, 1f - t * t);
                if (shake)
                {
                    widget.SetTint(Color.Lerp(new Color(1f, 0.3f, 0.3f, 1f), Color.white, t));
                    widget.SetOffset(new Vector2(Mathf.Sin(t * 40f) * 10f * (1f - t), 0f));
                }
            });
            UnityEngine.Object.Destroy(popup.gameObject);
            widget.SetHp(hpAfter);
            yield return Wait(0.15f);
        }

        private IEnumerator PlaySwitch(Switched e)
        {
            var widget = _fighters[e.Side];
            string incoming = Who(e.Side, e.ToIndex);
            Say(e.Forced ? incoming + " entrou na arena!" : _match.SideNames[e.Side] + " trocou: " + incoming + " entrou na arena!");
            if (!e.Forced) yield return Tween(0.2f, t => widget.SetAlpha(1f - t));
            Apply(e);
            ShowActive(e.Side);
            widget.ResetPose();
            yield return Tween(0.25f, t => widget.SetAlpha(t));
            yield return Wait(0.25f);
        }

        /// <summary>Sorteio de arena no estilo roleta de cassino (GDD §5.6): as arenas giram e desaceleram até a sorteada.</summary>
        private IEnumerator PlayArenaDraw(ArenaChanged e)
        {
            var arenas = _engine.Catalog.Arenas;
            var dim = Ui.Box(_overlay, "ArenaRoulette", 0, 0, Theme.Width, Theme.Height, new Color(0f, 0f, 0f, 0.6f));
            var panel = Ui.Box(dim.transform, "Panel", 380, 140, 520, 420, Theme.PanelSolid);
            Ui.Label(panel.transform, "SORTEIO DE ARENA", 0, 10, 520, 44, 30, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            var picture = Ui.Picture(panel.transform, "Arena", Art.Arena(_view.Arena), 20, 62, 480, 270);
            var name = Ui.Label(panel.transform, arenas[_view.Arena].Name, 0, 338, 520, 40, 30, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            var effect = Ui.Label(panel.transform, "", 0, 380, 520, 28, 16, Theme.Muted);
            Say("Três rodadas se passaram: a roleta decide a nova arena...");

            int current = (int)_view.Arena;
            int target = (int)e.Arena;
            int flips = 12 + (((target - current) % 4) + 4) % 4;
            for (int i = 1; i <= flips; i++)
            {
                var id = (ArenaId)((current + i) % 4);
                picture.texture = Art.Arena(id);
                name.text = arenas[id].Name;
                float progress = i / (float)flips;
                yield return Wait(0.05f + 0.28f * progress * progress);
            }

            Apply(e);
            _background.texture = Art.Arena(e.Arena);
            name.color = Theme.Gold;
            effect.text = Theme.ArenaEffect(arenas[e.Arena], _engine.Rules);
            Say("A arena agora é " + arenas[e.Arena].Name + "! " + effect.text);
            yield return Wait(1.5f);
            UnityEngine.Object.Destroy(dim.gameObject);
        }

        // ── Estado visível e utilitários ─────────────────────────────────────────

        private void Apply(BattleEvent ev)
        {
            _view.Apply(ev);
            RefreshHud();
        }

        private void RefreshHud()
        {
            var arena = CurrentArena;
            _arenaName.text = "Arena: " + arena.Name;
            _arenaEffect.text = Theme.ArenaEffect(arena, _engine.Rules);
            _roundInfo.text = "Rodada " + _view.Round + "  ·  sorteio em " + _view.RoundsUntilArenaDraw +
                              (_view.RoundsUntilArenaDraw == 1 ? " rodada" : " rodadas");
            for (int side = 0; side < 2; side++)
            {
                _teams[side].Refresh(_view.Sides[side], _engine.Catalog, _engine.Rules.StaminaMax);
                _fighters[side].SetEffects(BattleText.Effects(_view.Sides[side].Active.Effects, _engine.Catalog));
            }
        }

        private void ShowActive(int side)
        {
            _fighters[side].Show(_view.Sides[side].Active, _engine.Catalog);
        }

        private void ResetPoses()
        {
            _fighters[0].ResetPose();
            _fighters[1].ResetPose();
        }

        private void Say(string text)
        {
            _message.text = text;
        }

        private string Who(int side, int fighterIndex)
        {
            return _view.Sides[side].Team[fighterIndex].Spec.Name + " (" + _match.SideNames[side] + ")";
        }

        private static IEnumerator Tween(float seconds, Action<float> step)
        {
            float t = 0f;
            while (t < 1f)
            {
                step(t);
                yield return null;
                t += Time.deltaTime * Speed / seconds;
            }
            step(1f);
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds / Speed);
        }
    }
}
