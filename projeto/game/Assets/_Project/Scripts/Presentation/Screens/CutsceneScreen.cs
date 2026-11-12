using System;
using System.Collections;
using System.Collections.Generic;
using ComboTactics.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>
    /// Tela de Cutscene Cinematográfica do Modo História: exibe a jornada do Mago Invocador (Kaelen),
    /// o ritual de conquista dos novos Espíritos Elementares no Grimório e o confronto com os Mestres e Hyperion.
    /// </summary>
    public sealed class CutsceneScreen : UiScreen
    {
        private readonly CutsceneSlide[] _slides;
        private readonly int _cutsceneId;
        private readonly Action _onComplete;
        private int _index;
        private bool _finished;
        private Coroutine _animRoutine;

        public CutsceneScreen(CutsceneSlide[] slides, int cutsceneId, Action onComplete)
        {
            _slides = slides ?? new CutsceneSlide[0];
            _cutsceneId = cutsceneId;
            _onComplete = onComplete;
        }

        public override void Close()
        {
            if (_animRoutine != null) App.Host.StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        protected override void Build()
        {
            if (_slides.Length == 0 || _index >= _slides.Length)
            {
                FinishCutscene();
                return;
            }

            var slide = _slides[_index];
            Color accent = Theme.ArenaColor(slide.BackdropArena);

            // 1. Cenário de fundo com vinheta cinematográfica e partículas místicas
            var bg = Ui.Picture(Root, "CutsceneBg", Art.Arena(slide.BackdropArena), -32, -20, Theme.Width + 64, Theme.Height + 40);
            bg.color = new Color(0.82f, 0.80f, 0.88f, 1f);
            Ui.AddAmbientSparks(Root, bg.rectTransform, accent, 24);

            // Barras cinematográficas (letterbox superior e inferior)
            Ui.Box(Root, "TopBar", 0, 0, Theme.Width, 64, new Color(0.03f, 0.02f, 0.05f, 0.92f));
            Ui.Box(Root, "TopDivider", 0, 64, Theme.Width, 2, accent);

            Ui.Label(Root, "CRÔNICAS DO CRISTAL  ·  CENA " + (_index + 1) + " DE " + _slides.Length,
                32, 14, 340, 36, 15, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(Root, slide.HeaderTitle,
                340, 10, 600, 44, 24, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Button(Root, "Pular", 1068, 12, 180, 40, FinishCutscene, Theme.Button, 15);

            // 2. Palco central: Ritual de Invocação dos Elementares OU Confronto de Personagens
            var stage = Ui.Node(Root, "StageArea", 0, 68, Theme.Width, 370);
            var animatedRings = new List<RectTransform>();
            var animatedPortraits = new List<RectTransform>();

            // O Mago Invocador (Você) sempre aparece à esquerda canalizando o Grimório
            var summonerRing = Ui.Picture(stage, "SummonerRing", Art.VfxRing, 76, 56, 260, 260);
            summonerRing.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.55f);
            animatedRings.Add(summonerRing.rectTransform);

            var summonerCard = Ui.Frame(stage, "SummonerFrame", 116, 78, 180, 214, new Color(0.08f, 0.05f, 0.14f, 0.94f), Theme.Gold, 2.5f);
            Ui.Picture(summonerCard, "SummonerArt", Art.Fighter(Art.SummonerId, Element.Neutral, CharClass.Mage), 16, 10, 144, 144);
            Ui.Label(summonerCard, CampaignStory.SummonerName.ToUpperInvariant() + " (VOCÊ)", 6, 156, 164, 22, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(summonerCard, CampaignStory.SummonerTitle, 6, 178, 164, 20, 12, Theme.Muted, TextAnchor.MiddleCenter);
            animatedPortraits.Add((RectTransform)summonerCard.parent);

            if (slide.SummonedElementals != null && slide.SummonedElementals.Length > 0)
            {
                // Cena de Pacto Elemental: mostra os 3 guerreiros sendo despertados pelo Grimório!
                var pactBanner = Ui.Frame(stage, "PactBanner", 376, 12, 840, 42, new Color(0.07f, 0.05f, 0.12f, 0.92f), Theme.Gold, 1.5f);
                Ui.Label(pactBanner, "LUTADORES NO ESPECTRO DE " + CampaignStory.SummonerName.ToUpperInvariant(),
                    10, 0, 816, 38, 17, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

                for (int i = 0; i < slide.SummonedElementals.Length; i++)
                {
                    var def = App.Services.Content.Character(slide.SummonedElementals[i]);
                    if (def == null) continue;
                    Color elemCol = Theme.ElementColor(def.Element);
                    float cx = 376 + i * 286;

                    var pillar = Ui.Picture(stage, "Pillar" + i, Art.VfxPillar, cx + 24, 48, 220, 300);
                    pillar.color = new Color(elemCol.r, elemCol.g, elemCol.b, 0.45f);

                    var ring = Ui.Picture(stage, "Ring" + i, Art.VfxRing, cx + 18, 68, 232, 232);
                    ring.color = new Color(elemCol.r, elemCol.g, elemCol.b, 0.65f);
                    animatedRings.Add(ring.rectTransform);

                    var card = Ui.Frame(stage, "ElemCard" + i, cx + 14, 66, 240, 256, new Color(0.06f, 0.06f, 0.10f, 0.95f), elemCol, 2.5f);
                    var badge = Ui.Box(card, "NewBadge", 10, 8, 216, 24, new Color(elemCol.r * 0.28f, elemCol.g * 0.28f, elemCol.b * 0.28f, 0.95f));
                    Ui.Label(badge.transform, "NO ESPECTRO", 0, 0, 216, 24, 12, elemCol, TextAnchor.MiddleCenter, FontStyle.Bold);

                    Ui.Picture(card, "Sprite", Art.Fighter(def.Id, def.Element, def.Class), 56, 38, 124, 124);
                    Ui.Label(card, def.Name, 8, 166, 220, 26, 18, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                    Ui.Label(card, Theme.ElementName(def.Element) + " · " + Theme.ClassName(def.Class),
                        8, 192, 220, 20, 14, elemCol, TextAnchor.MiddleCenter, FontStyle.Bold);
                    Ui.Label(card, def.Appearance,
                        8, 216, 220, 22, 12, Theme.Good, TextAnchor.MiddleCenter, FontStyle.Italic);
                    animatedPortraits.Add((RectTransform)card.parent);
                }
            }
            else if (slide.Dialogue != null && slide.Dialogue.PortraitId != Art.SummonerId)
            {
                // Cena de Confronto entre o Mago Invocador e o Mestre Guardião / Hyperion
                Color speakerCol = slide.Dialogue.Element == Element.Neutral ? accent : Theme.ElementColor(slide.Dialogue.Element);
                var vsOrb = Ui.Picture(stage, "CenterOrb", Art.VfxRing, 540, 96, 180, 180);
                vsOrb.color = new Color(accent.r, accent.g, accent.b, 0.50f);
                animatedRings.Add(vsOrb.rectTransform);

                var oppRing = Ui.Picture(stage, "OppRing", Art.VfxRing, 896, 56, 260, 260);
                oppRing.color = new Color(speakerCol.r, speakerCol.g, speakerCol.b, 0.55f);
                animatedRings.Add(oppRing.rectTransform);

                var oppCard = Ui.Frame(stage, "OppFrame", 936, 78, 180, 214, new Color(0.08f, 0.05f, 0.10f, 0.94f), speakerCol, 2.5f);
                Ui.Picture(oppCard, "OppArt", Art.Fighter(slide.Dialogue.PortraitId, slide.Dialogue.Element, slide.Dialogue.Class), 16, 10, 144, 144);
                Ui.Label(oppCard, slide.Dialogue.Speaker, 6, 156, 164, 22, 13, speakerCol, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(oppCard, slide.Dialogue.Title, 6, 178, 164, 20, 11, Theme.Muted, TextAnchor.MiddleCenter);
                animatedPortraits.Add((RectTransform)oppCard.parent);
            }

            // 3. Painel narrativo inferior (Narração + Fala do Personagem + Botão de Avanço)
            var storyPanel = Ui.Frame(Root, "StoryPanel", 48, 444, 1184, 248, new Color(0.05f, 0.04f, 0.08f, 0.95f), accent, 2.5f);

            var narrationBox = Ui.Frame(storyPanel, "NarrationBox", 16, 12, 1148, 64, new Color(0.09f, 0.07f, 0.14f, 0.92f), Theme.BorderSubtle, 1.5f);
            Ui.Label(narrationBox, slide.Narration, 18, 6, 1112, 52, 17, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Italic);

            if (slide.Dialogue != null)
            {
                var d = slide.Dialogue;
                Color dCol = d.Element == Element.Neutral ? Theme.Gold : Theme.ElementColor(d.Element);
                var portraitBox = Ui.Frame(storyPanel, "SpeakerPortrait", 20, 90, 132, 132, Theme.PanelLight, dCol, 2f);
                Ui.Picture(portraitBox, "PortraitPic", Art.Fighter(d.PortraitId, d.Element, d.Class), 6, 6, 116, 116);

                Ui.Label(storyPanel, d.Speaker.ToUpper(), 168, 90, 740, 28, 22, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
                Ui.Label(storyPanel, d.Title, 168, 118, 740, 22, 14, dCol, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(storyPanel, "\"" + d.Text + "\"", 168, 146, 760, 78, 18, Theme.Text, TextAnchor.UpperLeft);
            }

            bool isLast = _index >= _slides.Length - 1;
            string nextLabel = isLast ? "Continuar" : "Próxima cena";
            Ui.Button(storyPanel, nextLabel, 948, 116, 212, 68, NextSlide, Theme.ButtonPrimary, 20);
            Ui.Label(storyPanel, "[Espaço ou Clique no botão]", 948, 190, 212, 24, 12, Theme.Muted, TextAnchor.MiddleCenter);

            _animRoutine = App.Host.StartCoroutine(AnimateCutscene(bg.rectTransform, animatedRings, animatedPortraits));
        }

        private IEnumerator AnimateCutscene(RectTransform bg, List<RectTransform> rings, List<RectTransform> portraits)
        {
            float t = 0f;
            while (true)
            {
                yield return null;
                t += Time.unscaledDeltaTime;

                if (bg != null)
                {
                    float s = 1.02f + 0.03f * Mathf.Sin(t * 0.45f);
                    bg.localScale = new Vector3(s, s, 1f);
                }

                for (int i = 0; i < rings.Count; i++)
                {
                    if (rings[i] == null) continue;
                    float dir = i % 2 == 0 ? 1f : -1f;
                    rings[i].localRotation = Quaternion.Euler(0f, 0f, t * 24f * dir);
                    float pulse = 1f + 0.06f * Mathf.Sin(t * 2.2f + i);
                    rings[i].localScale = new Vector3(pulse, pulse, 1f);
                }

                for (int i = 0; i < portraits.Count; i++)
                {
                    if (portraits[i] == null) continue;
                    float bob = 1f + 0.02f * Mathf.Sin(t * 2.6f + i * 0.9f);
                    portraits[i].localScale = new Vector3(bob, bob, 1f);
                }

                if (t > 0.28f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                {
                    NextSlide();
                    yield break;
                }
            }
        }

        private void NextSlide()
        {
            if (_index >= _slides.Length) return;
            _index++;
            if (_index >= _slides.Length)
                FinishCutscene();
            else
                Rebuild();
        }

        private void FinishCutscene()
        {
            // Pular e o último "Continuar" no mesmo quadro não podem avançar a história duas vezes.
            if (_finished) return;
            _finished = true;
            CampaignStory.MarkCutsceneSeen(_cutsceneId);
            if (_onComplete != null) _onComplete();
            else App.ShowCampaign();
        }
    }
}
