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
            var attacker = _fighters[e.Side];
            var defender = _fighters[1 - e.Side];
            var fighterSpec = _view.Sides[e.Side].Team[e.FighterIndex].Spec;
            Color elemColor = Theme.ElementColor(fighterSpec.Element);
            float direction = e.Side == 0 ? 1f : -1f;

            if (e.Skill.IsSpecial)
            {
                Say("ESPECIAL! " + Who(e.Side, e.FighterIndex) + " usou " + e.Skill.Name + "!");
                var flash = Ui.Box(_overlay, "SpecialFlash", 0, 0, Theme.Width, Theme.Height, new Color(elemColor.r, elemColor.g, elemColor.b, 0f));
                var aura = SpawnVfx(Art.VfxRing, attacker.CenterX, attacker.CenterY, 80f, 80f, elemColor);
                yield return Tween(0.52f, t =>
                {
                    float pulse = Mathf.Sin(t * Mathf.PI);
                    attacker.SetScale(1f + 0.30f * pulse);
                    flash.color = new Color(elemColor.r * 0.6f, elemColor.g * 0.6f, elemColor.b * 0.6f, 0.38f * pulse);
                    PlaceVfx(aura, attacker.CenterX, attacker.CenterY, Mathf.Lerp(110f, 340f, t), Mathf.Lerp(110f, 340f, t), 1f - t);
                });
                UnityEngine.Object.Destroy(aura.gameObject);
                UnityEngine.Object.Destroy(flash.gameObject);
            }
            else
            {
                Say(Who(e.Side, e.FighterIndex) + " usou " + e.Skill.Name + "!");
            }

            string id = e.Skill.Id ?? "";
            if (id == "fire_mage_skill3") yield return AnimateMeteorShower(attacker, defender, elemColor);
            else if (id == "water_mage_skill3") yield return AnimateBlizzard(attacker, defender, elemColor);
            else if (id == "plant_mage_skill3") yield return AnimateLeafStorm(attacker, defender, elemColor);
            else if (id == "boss_titan_skill3") yield return AnimateTotalEclipse(attacker, defender);
            else if (id == "water_tank_skill3") yield return AnimateTidalWave(attacker, defender, direction, elemColor);
            else if (id == "plant_tank_skill3" || id == "plant_warrior_skill3") yield return AnimateRootsAndDrain(attacker, defender, elemColor, true);
            else if (id == "fire_warrior_skill3" || id == "water_warrior_skill3" || id == "fire_tank_skill3") yield return AnimateEruptionSpecial(attacker, defender, direction, elemColor);
            else if (id == "fire_mage_skill1") yield return AnimateProjectile(attacker, defender, direction, elemColor, new Color(1f, 0.85f, 0.3f, 1f));
            else if (id == "water_mage_skill1") yield return AnimateBeam(attacker, defender, direction, elemColor);
            else if (id == "plant_mage_skill1") yield return AnimateVineWhip(attacker, defender, direction, elemColor);
            else if (id == "fire_mage_skill2") yield return AnimateSolarBurst(attacker, defender, elemColor);
            else if (id == "water_mage_skill2") yield return AnimateFrostCage(attacker, defender, elemColor);
            else if (id == "plant_mage_skill2") yield return AnimateSporeCloud(attacker, defender, new Color(0.68f, 0.32f, 0.85f, 1f));
            else if (e.Skill.EffectTarget == EffectTarget.Self && !e.Skill.IsSpecial) yield return AnimateBuffAndStrike(attacker, defender, direction, elemColor);
            else if (fighterSpec.Class == CharClass.Tank) yield return AnimateHeavySlam(attacker, defender, direction, elemColor);
            else yield return AnimateMeleeSlash(attacker, defender, direction, elemColor, id == "plant_warrior_skill1");
        }

        private IEnumerator AnimateMeleeSlash(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor, bool doubleSlash)
        {
            float dist = Mathf.Abs(defender.CenterX - attacker.CenterX) - 110f;
            yield return Tween(0.20f, t =>
            {
                float ease = t * t;
                attacker.SetOffset(new Vector2(direction * dist * ease, 24f * Mathf.Sin(t * Mathf.PI)));
                attacker.SetRotation(-direction * 14f * ease);
            });

            var slash = SpawnVfx(Art.VfxSlash, defender.CenterX, defender.CenterY, 220f, 220f, elemColor, direction < 0 ? 90f : 0f);
            var ring = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY, 80f, 80f, Color.Lerp(elemColor, Color.white, 0.5f));
            RawImage slash2 = doubleSlash ? SpawnVfx(Art.VfxSlash, defender.CenterX, defender.CenterY, 220f, 220f, Color.Lerp(elemColor, Color.white, 0.35f), direction < 0 ? 0f : 90f) : null;

            yield return Tween(0.24f, t =>
            {
                float s = Mathf.Lerp(120f, 250f, t);
                PlaceVfx(slash, defender.CenterX, defender.CenterY, s, s, 1f - t * t);
                PlaceVfx(ring, defender.CenterX, defender.CenterY, Mathf.Lerp(60f, 220f, t), Mathf.Lerp(60f, 220f, t), 1f - t);
                if (slash2 != null) PlaceVfx(slash2, defender.CenterX, defender.CenterY, s * 0.9f, s * 0.9f, Mathf.Sin(t * Mathf.PI));
            });

            UnityEngine.Object.Destroy(slash.gameObject);
            UnityEngine.Object.Destroy(ring.gameObject);
            if (slash2 != null) UnityEngine.Object.Destroy(slash2.gameObject);
        }

        private IEnumerator AnimateBuffAndStrike(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor)
        {
            var shield = SpawnVfx(Art.VfxShield, attacker.CenterX, attacker.CenterY, 150f, 150f, elemColor);
            var ring = SpawnVfx(Art.VfxRing, attacker.CenterX, attacker.CenterY, 90f, 90f, Color.Lerp(elemColor, Color.white, 0.4f));
            yield return Tween(0.32f, t =>
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                attacker.SetScale(1f + 0.14f * pulse);
                PlaceVfx(shield, attacker.CenterX, attacker.CenterY, Mathf.Lerp(150f, 230f, t), Mathf.Lerp(150f, 230f, t), pulse);
                PlaceVfx(ring, attacker.CenterX, attacker.CenterY, Mathf.Lerp(90f, 240f, t), Mathf.Lerp(90f, 240f, t), 1f - t);
            });
            UnityEngine.Object.Destroy(shield.gameObject);
            UnityEngine.Object.Destroy(ring.gameObject);
            attacker.SetScale(1f);

            yield return AnimateMeleeSlash(attacker, defender, direction, elemColor, false);
        }

        private IEnumerator AnimateHeavySlam(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor)
        {
            float dist = Mathf.Abs(defender.CenterX - attacker.CenterX) - 130f;
            yield return Tween(0.28f, t =>
            {
                attacker.SetOffset(new Vector2(direction * dist * t, 75f * Mathf.Sin(t * Mathf.PI)));
                attacker.SetScale(1f + 0.15f * Mathf.Sin(t * Mathf.PI));
            });

            var pillar = SpawnVfx(Art.VfxPillar, defender.CenterX, defender.CenterY + 10f, 180f, 220f, elemColor);
            var wave = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY + 55f, 100f, 50f, Color.Lerp(elemColor, Color.white, 0.4f));
            yield return Tween(0.28f, t =>
            {
                PlaceVfx(pillar, defender.CenterX, defender.CenterY + 20f - 40f * t, 190f, Mathf.Lerp(80f, 260f, t), 1f - t * t);
                PlaceVfx(wave, defender.CenterX, defender.CenterY + 60f, Mathf.Lerp(90f, 300f, t), Mathf.Lerp(40f, 110f, t), 1f - t);
            });
            UnityEngine.Object.Destroy(pillar.gameObject);
            UnityEngine.Object.Destroy(wave.gameObject);
        }

        private IEnumerator AnimateProjectile(FighterWidget attacker, FighterWidget defender, float direction, Color mainColor, Color coreColor)
        {
            yield return Tween(0.14f, t => attacker.SetOffset(new Vector2(-direction * 20f * t, 0f)));
            var orb = SpawnVfx(Art.VfxOrb, attacker.CenterX + direction * 50f, attacker.CenterY - 10f, 96f, 96f, mainColor);
            var core = SpawnVfx(Art.VfxOrb, attacker.CenterX + direction * 50f, attacker.CenterY - 10f, 52f, 52f, coreColor);
            var trail = SpawnVfx(Art.VfxShard, attacker.CenterX, attacker.CenterY - 10f, 120f, 44f, mainColor);

            float startX = attacker.CenterX + direction * 50f;
            float endX = defender.CenterX;
            yield return Tween(0.28f, t =>
            {
                attacker.SetOffset(new Vector2(direction * 35f * Mathf.Sin(t * Mathf.PI), 0f));
                float x = Mathf.Lerp(startX, endX, t);
                float y = attacker.CenterY - 10f - 30f * Mathf.Sin(t * Mathf.PI);
                PlaceVfx(orb, x, y, 105f, 105f, 1f);
                PlaceVfx(core, x, y, 56f, 56f, 1f);
                PlaceVfx(trail, x - direction * 45f, y, 130f, 46f, 0.85f);
            });
            UnityEngine.Object.Destroy(orb.gameObject);
            UnityEngine.Object.Destroy(core.gameObject);
            UnityEngine.Object.Destroy(trail.gameObject);

            var burst = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY, 80f, 80f, mainColor);
            yield return Tween(0.20f, t => PlaceVfx(burst, defender.CenterX, defender.CenterY, Mathf.Lerp(80f, 240f, t), Mathf.Lerp(80f, 240f, t), 1f - t));
            UnityEngine.Object.Destroy(burst.gameObject);
        }

        private IEnumerator AnimateBeam(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor)
        {
            yield return Tween(0.14f, t => attacker.SetOffset(new Vector2(direction * 30f * t, 0f)));
            float midX = (attacker.CenterX + defender.CenterX) * 0.5f;
            float span = Mathf.Abs(defender.CenterX - attacker.CenterX);
            var beam = SpawnVfx(Art.VfxShard, midX, attacker.CenterY, span, 68f, elemColor);
            var core = SpawnVfx(Art.VfxShard, midX, attacker.CenterY, span, 28f, Color.Lerp(elemColor, Color.white, 0.65f));
            var splash = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY, 90f, 90f, Color.Lerp(elemColor, Color.white, 0.4f));

            yield return Tween(0.32f, t =>
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                PlaceVfx(beam, midX, attacker.CenterY, span, 76f * pulse, pulse);
                PlaceVfx(core, midX, attacker.CenterY, span, 32f * pulse, pulse);
                PlaceVfx(splash, defender.CenterX, defender.CenterY, Mathf.Lerp(70f, 220f, t), Mathf.Lerp(70f, 220f, t), 1f - t);
            });

            UnityEngine.Object.Destroy(beam.gameObject);
            UnityEngine.Object.Destroy(core.gameObject);
            UnityEngine.Object.Destroy(splash.gameObject);
        }

        private IEnumerator AnimateVineWhip(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor)
        {
            yield return Tween(0.14f, t => attacker.SetOffset(new Vector2(direction * 36f * t, 0f)));
            float startX = attacker.CenterX + direction * 40f;
            float span = Mathf.Abs(defender.CenterX - startX);
            var vine = SpawnVfx(Art.VfxShard, startX, attacker.CenterY, 40f, 38f, elemColor, -direction * 8f);
            var slash = SpawnVfx(Art.VfxSlash, defender.CenterX, defender.CenterY, 180f, 180f, Color.Lerp(elemColor, Color.white, 0.3f));

            yield return Tween(0.28f, t =>
            {
                float len = span * Mathf.Clamp01(t * 1.5f);
                PlaceVfx(vine, startX + direction * len * 0.5f, attacker.CenterY, len, 42f, 1f - t * 0.5f);
                float slashAlpha = t > 0.35f ? Mathf.Sin((t - 0.35f) / 0.65f * Mathf.PI) : 0f;
                PlaceVfx(slash, defender.CenterX, defender.CenterY, 210f, 210f, slashAlpha);
            });

            UnityEngine.Object.Destroy(vine.gameObject);
            UnityEngine.Object.Destroy(slash.gameObject);
        }

        private IEnumerator AnimateSolarBurst(FighterWidget attacker, FighterWidget defender, Color elemColor)
        {
            yield return Tween(0.15f, t => attacker.SetScale(1f + 0.12f * t));
            var sun = SpawnVfx(Art.VfxOrb, defender.CenterX, defender.CenterY - 90f, 60f, 60f, new Color(1f, 0.85f, 0.25f, 1f));
            var ring = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY, 60f, 60f, elemColor);
            yield return Tween(0.36f, t =>
            {
                float y = Mathf.Lerp(defender.CenterY - 90f, defender.CenterY, Mathf.Clamp01(t * 1.4f));
                PlaceVfx(sun, defender.CenterX, y, Mathf.Lerp(70f, 240f, t), Mathf.Lerp(70f, 240f, t), 1f - t * t);
                PlaceVfx(ring, defender.CenterX, defender.CenterY, Mathf.Lerp(50f, 270f, t), Mathf.Lerp(50f, 270f, t), Mathf.Sin(t * Mathf.PI));
            });
            UnityEngine.Object.Destroy(sun.gameObject);
            UnityEngine.Object.Destroy(ring.gameObject);
            attacker.SetScale(1f);
        }

        private IEnumerator AnimateFrostCage(FighterWidget attacker, FighterWidget defender, Color elemColor)
        {
            yield return Tween(0.14f, t => attacker.SetOffset(new Vector2((attacker.CenterX < defender.CenterX ? 1f : -1f) * 28f * t, 0f)));
            Color ice = new Color(0.55f, 0.88f, 1f, 1f);
            var p1 = SpawnVfx(Art.VfxPillar, defender.CenterX - 55f, defender.CenterY, 70f, 200f, ice);
            var p2 = SpawnVfx(Art.VfxPillar, defender.CenterX, defender.CenterY, 80f, 230f, Color.Lerp(ice, Color.white, 0.4f));
            var p3 = SpawnVfx(Art.VfxPillar, defender.CenterX + 55f, defender.CenterY, 70f, 200f, ice);

            yield return Tween(0.36f, t =>
            {
                float h = Mathf.Sin(t * Mathf.PI);
                PlaceVfx(p1, defender.CenterX - 55f * (1f - 0.3f * t), defender.CenterY, 70f, 210f * h, h);
                PlaceVfx(p2, defender.CenterX, defender.CenterY, 85f, 240f * h, h);
                PlaceVfx(p3, defender.CenterX + 55f * (1f - 0.3f * t), defender.CenterY, 70f, 210f * h, h);
            });

            UnityEngine.Object.Destroy(p1.gameObject);
            UnityEngine.Object.Destroy(p2.gameObject);
            UnityEngine.Object.Destroy(p3.gameObject);
        }

        private IEnumerator AnimateSporeCloud(FighterWidget attacker, FighterWidget defender, Color sporeColor)
        {
            var o1 = SpawnVfx(Art.VfxOrb, attacker.CenterX, attacker.CenterY, 70f, 70f, sporeColor);
            var o2 = SpawnVfx(Art.VfxOrb, attacker.CenterX, attacker.CenterY, 55f, 55f, Theme.ElementColor(Element.Plant));
            var o3 = SpawnVfx(Art.VfxOrb, attacker.CenterX, attacker.CenterY, 65f, 65f, sporeColor);

            yield return Tween(0.38f, t =>
            {
                float x = Mathf.Lerp(attacker.CenterX, defender.CenterX, t);
                float spread = Mathf.Sin(t * Mathf.PI) * 55f;
                PlaceVfx(o1, x - spread * 0.6f, defender.CenterY - spread * 0.5f, 95f + 50f * t, 95f + 50f * t, 1f - t * 0.6f);
                PlaceVfx(o2, x + spread * 0.6f, defender.CenterY + spread * 0.4f, 80f + 50f * t, 80f + 50f * t, 1f - t * 0.6f);
                PlaceVfx(o3, x, defender.CenterY - spread * 0.2f, 110f + 60f * t, 110f + 60f * t, 1f - t * 0.5f);
            });

            UnityEngine.Object.Destroy(o1.gameObject);
            UnityEngine.Object.Destroy(o2.gameObject);
            UnityEngine.Object.Destroy(o3.gameObject);
        }

        private IEnumerator AnimateEruptionSpecial(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor)
        {
            float dist = Mathf.Abs(defender.CenterX - attacker.CenterX) - 115f;
            yield return Tween(0.24f, t =>
            {
                attacker.SetOffset(new Vector2(direction * dist * t, 50f * Mathf.Sin(t * Mathf.PI)));
                attacker.SetRotation(-direction * 12f * t);
            });

            var pillar = SpawnVfx(Art.VfxPillar, defender.CenterX, defender.CenterY, 220f, 360f, elemColor);
            var core = SpawnVfx(Art.VfxPillar, defender.CenterX, defender.CenterY, 120f, 340f, Color.Lerp(elemColor, Color.white, 0.55f));
            var ring = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY + 50f, 120f, 60f, elemColor);

            yield return Tween(0.42f, t =>
            {
                float h = Mathf.Clamp01(t * 2.2f);
                float fade = 1f - t * t;
                PlaceVfx(pillar, defender.CenterX, defender.CenterY + 50f - 80f * h, 230f, 380f * h, fade);
                PlaceVfx(core, defender.CenterX, defender.CenterY + 50f - 80f * h, 120f, 360f * h, fade);
                PlaceVfx(ring, defender.CenterX, defender.CenterY + 60f, Mathf.Lerp(100f, 340f, t), Mathf.Lerp(45f, 130f, t), 1f - t);
            });

            UnityEngine.Object.Destroy(pillar.gameObject);
            UnityEngine.Object.Destroy(core.gameObject);
            UnityEngine.Object.Destroy(ring.gameObject);
        }

        private IEnumerator AnimateMeteorShower(FighterWidget attacker, FighterWidget defender, Color elemColor)
        {
            float dir = defender.CenterX > attacker.CenterX ? 1f : -1f;
            var m1 = SpawnVfx(Art.VfxOrb, defender.CenterX - dir * 220f, 80f, 95f, 95f, elemColor);
            var m2 = SpawnVfx(Art.VfxOrb, defender.CenterX - dir * 140f, 40f, 110f, 110f, new Color(1f, 0.78f, 0.22f, 1f));
            var m3 = SpawnVfx(Art.VfxOrb, defender.CenterX - dir * 260f, 20f, 125f, 125f, elemColor);
            var ring = SpawnVfx(Art.VfxRing, defender.CenterX, defender.CenterY, 80f, 80f, new Color(1f, 0.75f, 0.2f, 1f));

            yield return Tween(0.48f, t =>
            {
                float t1 = Mathf.Clamp01(t * 1.6f);
                float t2 = Mathf.Clamp01((t - 0.18f) * 1.6f);
                float t3 = Mathf.Clamp01((t - 0.35f) * 1.6f);
                PlaceVfx(m1, Mathf.Lerp(defender.CenterX - dir * 220f, defender.CenterX - 30f, t1), Mathf.Lerp(80f, defender.CenterY, t1), 100f, 100f, 1f - t1 * 0.7f);
                PlaceVfx(m2, Mathf.Lerp(defender.CenterX - dir * 140f, defender.CenterX + 25f, t2), Mathf.Lerp(40f, defender.CenterY + 15f, t2), 115f, 115f, t > 0.18f ? 1f - t2 * 0.7f : 0f);
                PlaceVfx(m3, Mathf.Lerp(defender.CenterX - dir * 260f, defender.CenterX, t3), Mathf.Lerp(20f, defender.CenterY - 10f, t3), 135f, 135f, t > 0.35f ? 1f - t3 * 0.6f : 0f);
                PlaceVfx(ring, defender.CenterX, defender.CenterY, Mathf.Lerp(80f, 320f, t), Mathf.Lerp(80f, 320f, t), Mathf.Sin(t * Mathf.PI));
            });

            UnityEngine.Object.Destroy(m1.gameObject);
            UnityEngine.Object.Destroy(m2.gameObject);
            UnityEngine.Object.Destroy(m3.gameObject);
            UnityEngine.Object.Destroy(ring.gameObject);
        }

        private IEnumerator AnimateBlizzard(FighterWidget attacker, FighterWidget defender, Color elemColor)
        {
            Color ice = new Color(0.65f, 0.92f, 1f, 1f);
            float dir = defender.CenterX > attacker.CenterX ? 1f : -1f;
            var shards = new RawImage[5];
            for (int i = 0; i < shards.Length; i++)
                shards[i] = SpawnVfx(Art.VfxShard, defender.CenterX, defender.CenterY, 110f, 32f, i % 2 == 0 ? ice : elemColor, -dir * 32f);
            var dome = SpawnVfx(Art.VfxShield, defender.CenterX, defender.CenterY, 220f, 220f, ice);

            yield return Tween(0.46f, t =>
            {
                for (int i = 0; i < shards.Length; i++)
                {
                    float phase = Mathf.Repeat(t * 1.8f + i * 0.19f, 1f);
                    float sx = defender.CenterX - dir * Mathf.Lerp(180f, -60f, phase) + (i - 2) * 28f;
                    float sy = defender.CenterY + Mathf.Lerp(-140f, 70f, phase) + (i % 2 == 0 ? -18f : 18f);
                    PlaceVfx(shards[i], sx, sy, 120f, 34f, Mathf.Sin(phase * Mathf.PI));
                }
                PlaceVfx(dome, defender.CenterX, defender.CenterY, Mathf.Lerp(180f, 280f, t), Mathf.Lerp(180f, 280f, t), Mathf.Sin(t * Mathf.PI) * 0.85f);
            });

            for (int i = 0; i < shards.Length; i++) UnityEngine.Object.Destroy(shards[i].gameObject);
            UnityEngine.Object.Destroy(dome.gameObject);
        }

        private IEnumerator AnimateLeafStorm(FighterWidget attacker, FighterWidget defender, Color elemColor)
        {
            float dir = defender.CenterX > attacker.CenterX ? 1f : -1f;
            var leaves = new RawImage[5];
            for (int i = 0; i < leaves.Length; i++)
                leaves[i] = SpawnVfx(Art.VfxShard, attacker.CenterX, attacker.CenterY, 76f, 30f, i % 2 == 0 ? elemColor : new Color(0.55f, 0.92f, 0.42f, 1f), i * 36f);
            var slash = SpawnVfx(Art.VfxSlash, defender.CenterX, defender.CenterY, 230f, 230f, Color.Lerp(elemColor, Color.white, 0.35f));

            yield return Tween(0.44f, t =>
            {
                for (int i = 0; i < leaves.Length; i++)
                {
                    float p = Mathf.Clamp01((t - i * 0.08f) * 1.45f);
                    float x = Mathf.Lerp(attacker.CenterX + dir * 40f, defender.CenterX + (i - 2) * 20f, p);
                    float y = defender.CenterY + Mathf.Sin((p * 2f + i * 0.4f) * Mathf.PI) * 55f;
                    leaves[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, p * 360f + i * 45f);
                    PlaceVfx(leaves[i], x, y, 82f, 32f, Mathf.Sin(p * Mathf.PI));
                }
                PlaceVfx(slash, defender.CenterX, defender.CenterY, 240f, 240f, t > 0.3f ? Mathf.Sin((t - 0.3f) / 0.7f * Mathf.PI) : 0f);
            });

            for (int i = 0; i < leaves.Length; i++) UnityEngine.Object.Destroy(leaves[i].gameObject);
            UnityEngine.Object.Destroy(slash.gameObject);
        }

        private IEnumerator AnimateTidalWave(FighterWidget attacker, FighterWidget defender, float direction, Color elemColor)
        {
            var wave = SpawnVfx(Art.VfxPillar, attacker.CenterX, attacker.CenterY - 10f, 240f, 320f, elemColor);
            var crest = SpawnVfx(Art.VfxShield, attacker.CenterX, attacker.CenterY - 20f, 220f, 260f, Color.Lerp(elemColor, Color.white, 0.45f));

            yield return Tween(0.46f, t =>
            {
                float x = Mathf.Lerp(attacker.CenterX + direction * 40f, defender.CenterX + direction * 30f, t);
                float h = Mathf.Sin(t * Mathf.PI * 0.85f + 0.15f);
                PlaceVfx(wave, x, defender.CenterY, 260f, 340f * h, 1f - t * 0.4f);
                PlaceVfx(crest, x + direction * 25f, defender.CenterY - 15f, 240f, 280f * h, 1f - t * 0.5f);
            });

            UnityEngine.Object.Destroy(wave.gameObject);
            UnityEngine.Object.Destroy(crest.gameObject);
        }

        private IEnumerator AnimateRootsAndDrain(FighterWidget attacker, FighterWidget defender, Color elemColor, bool drainOrbs)
        {
            var r1 = SpawnVfx(Art.VfxPillar, defender.CenterX - 45f, defender.CenterY + 20f, 90f, 260f, elemColor);
            var r2 = SpawnVfx(Art.VfxPillar, defender.CenterX + 45f, defender.CenterY + 20f, 90f, 260f, new Color(0.48f, 0.34f, 0.18f, 1f));
            var orb1 = SpawnVfx(Art.VfxOrb, defender.CenterX, defender.CenterY, 54f, 54f, Theme.Good);
            var orb2 = SpawnVfx(Art.VfxOrb, defender.CenterX, defender.CenterY, 42f, 42f, Color.Lerp(Theme.Good, Color.white, 0.5f));

            yield return Tween(0.46f, t =>
            {
                float crush = Mathf.Sin(t * Mathf.PI);
                PlaceVfx(r1, defender.CenterX - Mathf.Lerp(65f, 15f, t), defender.CenterY + 15f, 95f, 260f * crush, crush);
                PlaceVfx(r2, defender.CenterX + Mathf.Lerp(65f, 15f, t), defender.CenterY + 15f, 95f, 260f * crush, crush);
                if (drainOrbs)
                {
                    float ox = Mathf.Lerp(defender.CenterX, attacker.CenterX, t);
                    float oy = defender.CenterY - 60f * Mathf.Sin(t * Mathf.PI);
                    PlaceVfx(orb1, ox, oy, 58f, 58f, crush);
                    PlaceVfx(orb2, ox, oy + 24f * Mathf.Cos(t * 6f), 42f, 42f, crush);
                }
            });

            UnityEngine.Object.Destroy(r1.gameObject);
            UnityEngine.Object.Destroy(r2.gameObject);
            UnityEngine.Object.Destroy(orb1.gameObject);
            UnityEngine.Object.Destroy(orb2.gameObject);
        }

        private IEnumerator AnimateTotalEclipse(FighterWidget attacker, FighterWidget defender)
        {
            var dim = Ui.Box(_overlay, "EclipseDim", 0, 0, Theme.Width, Theme.Height, new Color(0.03f, 0.02f, 0.08f, 0f));
            var corona = SpawnVfx(Art.VfxRing, defender.CenterX, 170f, 180f, 180f, Theme.Gold);
            var disc = SpawnVfx(Art.VfxOrb, defender.CenterX, 170f, 130f, 130f, new Color(0.06f, 0.04f, 0.12f, 1f));
            var pillar = SpawnVfx(Art.VfxPillar, defender.CenterX, defender.CenterY - 40f, 200f, 420f, new Color(0.68f, 0.50f, 0.95f, 1f));

            yield return Tween(0.55f, t =>
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                dim.color = new Color(0.03f, 0.02f, 0.08f, 0.62f * pulse);
                PlaceVfx(corona, defender.CenterX, 170f, Mathf.Lerp(160f, 260f, t), Mathf.Lerp(160f, 260f, t), pulse);
                PlaceVfx(disc, defender.CenterX, 170f, 135f, 135f, pulse);
                float beamAlpha = t > 0.2f ? Mathf.Sin((t - 0.2f) / 0.8f * Mathf.PI) : 0f;
                PlaceVfx(pillar, defender.CenterX, defender.CenterY - 30f, Mathf.Lerp(140f, 240f, t), 440f, beamAlpha);
            });

            UnityEngine.Object.Destroy(pillar.gameObject);
            UnityEngine.Object.Destroy(disc.gameObject);
            UnityEngine.Object.Destroy(corona.gameObject);
            UnityEngine.Object.Destroy(dim.gameObject);
        }

        private RawImage SpawnVfx(Texture2D texture, float centerX, float centerY, float w, float h, Color color, float angle = 0f)
        {
            var img = Ui.Picture(_overlay, "Vfx", texture, 0, 0, w, h);
            Ui.PlaceCentered(img.rectTransform, centerX, centerY, w, h);
            img.color = color;
            if (Mathf.Abs(angle) > 0.01f) img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return img;
        }

        private static void PlaceVfx(RawImage img, float centerX, float centerY, float w, float h, float alpha)
        {
            Ui.PlaceCentered(img.rectTransform, centerX, centerY, w, h);
            var c = img.color;
            c.a = Mathf.Clamp01(alpha);
            img.color = c;
        }

        private IEnumerator PlayReaction(ReactionResolved e)
        {
            Apply(e);
            if (e.Result == ReactionResult.Miss) yield break;

            var side = _view.Sides[e.DefenderSide];
            var widget = _fighters[e.DefenderSide];
            float away = e.DefenderSide == 0 ? -1f : 1f;
            Say(Who(e.DefenderSide, side.ActiveIndex) + (e.Result == ReactionResult.Counter ? " desviou e vai contra-atacar!" : " desviou do golpe!"));

            Color vfxColor = e.Result == ReactionResult.Counter ? Theme.Gold : new Color(0.35f, 0.78f, 1f, 1f);
            var ring = SpawnVfx(e.Result == ReactionResult.Counter ? Art.VfxShield : Art.VfxRing, widget.CenterX, widget.CenterY, 140f, 140f, vfxColor);
            yield return Tween(0.26f, t =>
            {
                float arc = Mathf.Sin(t * Mathf.PI);
                widget.SetOffset(new Vector2(away * 65f * arc, 42f * arc));
                widget.SetRotation(away * 12f * arc);
                PlaceVfx(ring, widget.CenterX, widget.CenterY, Mathf.Lerp(120f, 230f, t), Mathf.Lerp(120f, 230f, t), 1f - t);
            });
            UnityEngine.Object.Destroy(ring.gameObject);
            ResetPoses();
            yield return Wait(0.20f);
        }

        /// <summary>Dano ou cura: número flutuante, barra de HP animada e, no dano, flash vermelho com tremor e impacto.</summary>
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
            RawImage impact = shake
                ? SpawnVfx(Art.VfxRing, widget.CenterX, widget.CenterY, 90f, 90f, Theme.Bad)
                : SpawnVfx(Art.VfxOrb, widget.CenterX, widget.CenterY, 130f, 130f, color);
            var popup = Ui.Label(_overlay, popupText, widget.CenterX - 150f, widget.CenterY - 120f, 300f, 60f, 44, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);
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
                    widget.SetOffset(new Vector2(Mathf.Sin(t * 42f) * 12f * (1f - t), 0f));
                    PlaceVfx(impact, widget.CenterX, widget.CenterY, Mathf.Lerp(90f, 230f, t), Mathf.Lerp(90f, 230f, t), 1f - t);
                }
                else
                {
                    PlaceVfx(impact, widget.CenterX, widget.CenterY - 40f * t, Mathf.Lerp(110f, 190f, t), Mathf.Lerp(110f, 190f, t), Mathf.Sin(t * Mathf.PI) * 0.65f);
                }
            });
            UnityEngine.Object.Destroy(impact.gameObject);
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
