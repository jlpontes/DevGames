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
    /// Diretor de Cutscenes do Modo História (GDD & Caderno de Projeto p. 2, 3, 5, 8; R9–R11):
    /// Encena a história do Cristal Elemental despedaçado que originou as zonas (Vulcão, Mar, Floresta e Lua),
    /// permite ao Mago escolher sua Afinidade Elemental inicial, exibe os Fragmentos conquistados
    /// e apresenta as provações contra os Guardiões até se tornar o Novo Guardião do Núcleo.
    /// </summary>
    public sealed class CutsceneScreen : UiScreen
    {
        private readonly CutsceneSlide[] _fallbackSlides;
        private readonly int _cutsceneId;
        private readonly Action _onComplete;
        private int _index;
        private bool _finished;
        private Coroutine _animRoutine;
        private bool _typingComplete;
        private Text _dialogueTextLabel;
        private string _fullDialogueText = "";

        public CutsceneScreen(CutsceneSlide[] slides, int cutsceneId, Action onComplete)
        {
            _fallbackSlides = slides ?? new CutsceneSlide[0];
            _cutsceneId = cutsceneId;
            _onComplete = onComplete;
        }

        private CutsceneSlide[] ActiveSlides
        {
            get
            {
                if (_cutsceneId >= 0 && _cutsceneId <= 3)
                {
                    var dynamicSlides = CampaignStory.Chapter(_cutsceneId).ActCutscene;
                    if (dynamicSlides != null && dynamicSlides.Length > 0)
                        return dynamicSlides;
                }
                return _fallbackSlides;
            }
        }

        public override void Close()
        {
            if (_animRoutine != null) App.Host.StopCoroutine(_animRoutine);
            _animRoutine = null;
        }

        protected override void Build()
        {
            var slides = ActiveSlides;
            if (slides.Length == 0 || _index >= slides.Length)
            {
                FinishCutscene();
                return;
            }

            var slide = slides[_index];
            Color accent = Theme.ArenaColor(slide.BackdropArena);
            Element playerAffinity = CampaignStory.GetPlayerAffinity();
            Color affinityColor = Theme.ElementColor(playerAffinity);

            // 1. Cenário de fundo com vinheta atmosférica e partículas da zona elemental
            var bg = Ui.Picture(Root, "CutsceneBg", Art.Arena(slide.BackdropArena), -36, -22, Theme.Width + 72, Theme.Height + 44);
            bg.color = new Color(0.80f, 0.78f, 0.86f, 1f);
            Ui.AddAmbientSparks(Root, bg.rectTransform, accent, 28);

            // Vinheta superior e inferior de cinema
            Ui.Box(Root, "TopBar", 0, 0, Theme.Width, 68, new Color(0.03f, 0.02f, 0.05f, 0.94f));
            Ui.Box(Root, "TopDivider", 0, 68, Theme.Width, 2.5f, accent);

            Ui.Label(Root, "CRÔNICAS DO CRISTAL  ·  CENA " + (_index + 1) + " / " + slides.Length,
                24, 8, 290, 24, 13, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            DrawRelicTracker(24, 34);

            Ui.Label(Root, slide.HeaderTitle,
                330, 12, 620, 44, 22, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            if (_index > 0)
            {
                Ui.Button(Root, "◂ Anterior", 956, 14, 112, 40, PrevSlide, Theme.Button, 14);
            }
            Ui.Button(Root, "Pular", 1078, 14, 178, 40, FinishCutscene, Theme.Button, 14);

            // 2. Palco Cinematográfico Central (varia conforme CutsceneVisualKind)
            var stage = Ui.Node(Root, "StageArea", 0, 72, Theme.Width, 366);
            var animatedRings = new List<RectTransform>();
            var animatedPortraits = new List<RectTransform>();

            switch (slide.VisualKind)
            {
                case CutsceneVisualKind.CrystalWhole:
                    BuildCrystalWholeStage(stage, slide, accent, affinityColor, animatedRings, animatedPortraits);
                    break;
                case CutsceneVisualKind.CrystalShatter:
                    BuildCrystalShatterStage(stage, accent, animatedRings, animatedPortraits);
                    break;
                case CutsceneVisualKind.AffinityChoice:
                    BuildAffinityChoiceStage(stage, playerAffinity, animatedRings, animatedPortraits);
                    break;
                case CutsceneVisualKind.SpectrumUnlock:
                    BuildSpectrumUnlockStage(stage, slide, affinityColor, animatedRings, animatedPortraits);
                    break;
                default:
                    BuildGuardianConfrontationStage(stage, slide, accent, playerAffinity, affinityColor, animatedRings, animatedPortraits);
                    break;
            }

            // 3. Painel Narrativo Inferior (Narração do GDD + Diálogo com Typewriter + Avanço)
            var storyPanel = Ui.Frame(Root, "StoryPanel", 40, 442, 1200, 252, new Color(0.05f, 0.04f, 0.08f, 0.96f), accent, 2.5f);

            var narrationBox = Ui.Frame(storyPanel, "NarrationBox", 16, 12, 1164, 62, new Color(0.09f, 0.07f, 0.14f, 0.94f), Theme.BorderSubtle, 1.5f);
            Ui.Label(narrationBox, slide.Narration, 16, 4, 1132, 54, 16, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Italic);

            _dialogueTextLabel = null;
            _fullDialogueText = "";
            _typingComplete = true;

            if (slide.Dialogue != null)
            {
                var d = slide.Dialogue;
                Color dCol = d.Element == Element.Neutral ? Theme.Gold : Theme.ElementColor(d.Element);
                var portraitBox = Ui.Frame(storyPanel, "SpeakerPortrait", 18, 86, 136, 136, Theme.PanelLight, dCol, 2.5f);
                Ui.Picture(portraitBox, "PortraitPic", Art.Fighter(d.PortraitId, d.Element, d.Class), 6, 6, 120, 120);

                Ui.Label(storyPanel, d.Speaker.ToUpper(), 170, 86, 760, 28, 21, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
                Ui.Label(storyPanel, d.Title, 170, 114, 760, 22, 14, dCol, TextAnchor.MiddleLeft, FontStyle.Bold);

                _fullDialogueText = "\"" + d.Text + "\"";
                _typingComplete = false;
                _dialogueTextLabel = Ui.Label(storyPanel, "", 170, 140, 770, 86, 17, Theme.Text, TextAnchor.UpperLeft);
            }

            bool isLast = _index >= slides.Length - 1;
            string nextLabel = isLast
                ? (_cutsceneId == 4 ? "Concluir jornada" : "Continuar")
                : "Próxima cena";
            Ui.Button(storyPanel, nextLabel, 958, 108, 220, 68, OnAdvancePressed, Theme.ButtonPrimary, 20);
            Ui.Label(storyPanel, "[Espaço, Enter ou Clique]", 958, 182, 220, 22, 12, Theme.Muted, TextAnchor.MiddleCenter);
            Ui.Label(storyPanel, AffinityText(playerAffinity),
                958, 206, 220, 20, 12, affinityColor, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Véu de transição suave na entrada do slide
            var fadeVeil = Ui.Box(Root, "FadeVeil", 0, 0, Theme.Width, Theme.Height, new Color(0.02f, 0.01f, 0.04f, 0.65f));
            fadeVeil.raycastTarget = false;

            _animRoutine = App.Host.StartCoroutine(AnimateCutscene(bg.rectTransform, animatedRings, animatedPortraits, fadeVeil));
        }

        // ── Rastreador de Relíquias (Fragmentos do Cristal Elemental) ────────────

        private void DrawRelicTracker(float x, float y)
        {
            int progress = App.Services.Profile.CampaignStage;
            int effectiveStage = Mathf.Max(progress, _cutsceneId);

            string[] labels = { "🔥 Vulcão", "💧 Mar", "🌿 Floresta", "☾ Núcleo" };
            Color[] colors =
            {
                Theme.ElementColor(Element.Fire),
                Theme.ElementColor(Element.Water),
                Theme.ElementColor(Element.Plant),
                Theme.Gold
            };

            for (int i = 0; i < 4; i++)
            {
                bool collected = effectiveStage > i || _cutsceneId == 4;
                Color col = collected ? colors[i] : Theme.Muted;
                var pill = Ui.Frame(Root, "Relic" + i, x + i * 74, y, 70, 24,
                    collected ? new Color(col.r * 0.22f, col.g * 0.22f, col.b * 0.22f, 0.95f) : new Color(0.06f, 0.05f, 0.09f, 0.80f),
                    collected ? col : Theme.BorderSubtle, 1f);
                Ui.Label(pill, labels[i], 2, 0, 64, 22, 10, col, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        // ── Composição 1: O Cristal Elemental Inteiro (Prólogo & Epílogo) ────────

        private void BuildCrystalWholeStage(
            RectTransform stage,
            CutsceneSlide slide,
            Color accent,
            Color affinityColor,
            List<RectTransform> animatedRings,
            List<RectTransform> animatedPortraits)
        {
            DrawMageCard(stage, 64, 66, affinityColor, animatedRings, animatedPortraits);

            // Altar central do Cristal Elemental em harmonia
            var centerPillar = Ui.Picture(stage, "CrystalPillar", Art.VfxPillar, 456, 8, 368, 350);
            centerPillar.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.42f);

            var outerRing = Ui.Picture(stage, "CrystalOuterRing", Art.VfxRing, 466, 18, 348, 348);
            outerRing.color = new Color(Theme.Gold.r, Theme.Gold.g, Theme.Gold.b, 0.65f);
            animatedRings.Add(outerRing.rectTransform);

            var innerRing = Ui.Picture(stage, "CrystalInnerRing", Art.VfxRing, 504, 56, 272, 272);
            innerRing.color = new Color(accent.r, accent.g, accent.b, 0.58f);
            animatedRings.Add(innerRing.rectTransform);

            var crystalPic = Ui.Picture(stage, "CrystalWholeArt", Art.CrystalWhole, 500, 44, 280, 280);
            animatedPortraits.Add(crystalPic.rectTransform);

            var titleBanner = Ui.Frame(stage, "CrystalBanner", 390, 314, 500, 40, new Color(0.07f, 0.05f, 0.12f, 0.94f), Theme.Gold, 1.5f);
            string bannerText = _cutsceneId == 4
                ? "✦ CRISTAL ELEMENTAL REUNIFICADO · EQUILÍBRIO RESTAURADO ✦"
                : "✦ O CRISTAL ELEMENTAL · FOGO, ÁGUA E PLANTA EM EQUILÍBRIO ✦";
            Ui.Label(titleBanner, bannerText, 8, 0, 480, 36, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // Lendas elementais em comunhão à direita
            string[] previewIds = (slide.SummonedElementals != null && slide.SummonedElementals.Length > 0)
                ? slide.SummonedElementals
                : new[] { "fire_warrior", "water_mage", "plant_tank" };

            var rightFrame = Ui.Frame(stage, "HarmonyLegends", 912, 48, 312, 276, new Color(0.06f, 0.05f, 0.10f, 0.92f), Theme.BorderGold, 2f);
            Ui.Label(rightFrame, "AS LENDAS DAS ZONAS", 8, 8, 292, 22, 14, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(rightFrame, "Guardiãs da harmonia primordial", 8, 28, 292, 18, 11, Theme.Muted, TextAnchor.MiddleCenter);

            for (int i = 0; i < previewIds.Length && i < 3; i++)
            {
                var def = App.Services.Content.Character(previewIds[i]);
                if (def == null) continue;
                Color c = Theme.ElementColor(def.Element);
                var row = Ui.Frame(rightFrame, "Leg" + i, 12, 54 + i * 70, 284, 62, Theme.PanelLight, c, 1.5f);
                Ui.Picture(row, "Icon", Art.Fighter(def.Id, def.Element, def.Class), 6, 4, 50, 50);
                Ui.Label(row, def.Appearance, 64, 6, 212, 24, 15, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(row, def.Name + " (" + Theme.ElementName(def.Element) + ")", 64, 30, 212, 20, 12, c, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
        }

        // ── Composição 2: A Ruptura do Cristal e Criação das 4 Zonas (GDD §2) ────

        private void BuildCrystalShatterStage(
            RectTransform stage,
            Color accent,
            List<RectTransform> animatedRings,
            List<RectTransform> animatedPortraits)
        {
            // Esquerda/Centro: O Cristal Elemental despedaçando-se em 3 fragmentos + Núcleo
            var shockRing1 = Ui.Picture(stage, "ShockRing1", Art.VfxRing, 56, 22, 330, 330);
            shockRing1.color = new Color(0.95f, 0.32f, 0.24f, 0.62f);
            animatedRings.Add(shockRing1.rectTransform);

            var shockRing2 = Ui.Picture(stage, "ShockRing2", Art.VfxRing, 92, 58, 258, 258);
            shockRing2.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            animatedRings.Add(shockRing2.rectTransform);

            var shatteredPic = Ui.Picture(stage, "CrystalShatteredArt", Art.CrystalShattered, 80, 38, 284, 284);
            animatedPortraits.Add(shatteredPic.rectTransform);

            var shatterBadge = Ui.Frame(stage, "ShatterBadge", 38, 314, 368, 40, new Color(0.12f, 0.04f, 0.08f, 0.95f), Theme.Bad, 1.5f);
            Ui.Label(shatterBadge, "⚡ O CRISTAL ELEMENTAL SE DESPEDAÇA! ⚡",
                6, 0, 352, 36, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // Direita: As 4 Zonas geradas pela queda dos Fragmentos e do Núcleo (Caderno p. 2 & 8)
            var zonesHeader = Ui.Frame(stage, "ZonesHeader", 432, 12, 808, 36, new Color(0.07f, 0.05f, 0.12f, 0.94f), Theme.Gold, 1.5f);
            Ui.Label(zonesHeader, "CADA PARTE DO CRISTAL CAIU EM UM CANTO DO MUNDO E GEROU UMA ZONA ELEMENTAL:",
                8, 0, 788, 32, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);

            ArenaId[] zoneArenas = { ArenaId.Volcano, ArenaId.Sea, ArenaId.Forest, ArenaId.Moon };
            string[] zoneTitles =
            {
                "🔥 FRAGMENTO DE FOGO ➔ VULCÃO",
                "💧 FRAGMENTO DE ÁGUA ➔ MAR",
                "🌿 FRAGMENTO DE PLANTA ➔ FLORESTA",
                "☾ NÚCLEO DO CRISTAL ➔ LUA"
            };
            string[] zoneLore =
            {
                "Despertou Mula sem cabeça, Bruxo-fênix e Dragão (+10% Fogo)",
                "Despertou Guerreiro-tubarão, Poseidon e Baleia (+10% Água)",
                "Despertou Curupira, Cogumelo e Árvore (+10% Planta)",
                "Gerou o Guardião do Núcleo e a gravidade lunar (VEL < 50)"
            };

            for (int i = 0; i < 4; i++)
            {
                float colX = 432 + (i % 2) * 408;
                float rowY = 58 + (i / 2) * 150;
                Color zCol = Theme.ArenaColor(zoneArenas[i]);

                var zCard = Ui.Frame(stage, "ZoneOrigin" + i, colX, rowY, 396, 140, new Color(0.06f, 0.05f, 0.10f, 0.95f), zCol, 2f);
                Ui.ArenaPicture(zCard, "ArenaThumb", zoneArenas[i], 8, 8, 148, 120);
                Ui.Label(zCard, zoneTitles[i], 164, 10, 222, 24, 13, zCol, TextAnchor.MiddleLeft, FontStyle.Bold, true);
                Ui.Label(zCard, zoneLore[i], 164, 38, 222, 54, 12, Theme.Text, TextAnchor.UpperLeft);
                Ui.Label(zCard, i < 3 ? "Protegido pelo Guardião da Zona" : "Protegido pelo Guardião do Núcleo",
                    164, 100, 222, 24, 11, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Italic);
            }
        }

        // ── Composição 3: Escolha Interativa da Afinidade Elemental (Caderno p. 5) ─

        private void BuildAffinityChoiceStage(
            RectTransform stage,
            Element currentAffinity,
            List<RectTransform> animatedRings,
            List<RectTransform> animatedPortraits)
        {
            Color affCol = Theme.ElementColor(currentAffinity);
            DrawMageCard(stage, 44, 64, affCol, animatedRings, animatedPortraits);

            var header = Ui.Frame(stage, "AffinityBanner", 326, 8, 914, 40, new Color(0.07f, 0.05f, 0.13f, 0.95f), Theme.Gold, 1.5f);
            Ui.Label(header, currentAffinity == Element.Neutral ? "TRÊS AFINIDADES, TRÊS CAMINHOS · A ESCOLHA DEFINE OS 3 PRIMEIROS LUTADORES DO ESPECTRO" : "A AFINIDADE DO MAGO DEFINE OS 3 PRIMEIROS LUTADORES DO ESPECTRO",
                8, 0, 894, 36, 14, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            Element[] elements = { Element.Fire, Element.Water, Element.Plant };
            string[] titles = { "AFINIDADE: FOGO", "AFINIDADE: ÁGUA", "AFINIDADE: PLANTA" };
            string[] rules =
            {
                "Vantagem contra Planta (+30%)\nRessonância na Arena Vulcão",
                "Vantagem contra Fogo (+30%)\nRessonância na Arena Mar",
                "Vantagem contra Água (+30%)\nRessonância na Arena Floresta"
            };

            for (int i = 0; i < 3; i++)
            {
                Element elem = elements[i];
                bool isSelected = elem == currentAffinity;
                Color eCol = Theme.ElementColor(elem);
                float cx = 326 + i * 308;

                var card = Ui.Frame(stage, "AffinityCard" + i, cx, 56, 298, 298,
                    isSelected ? new Color(0.14f, 0.11f, 0.20f, 0.98f) : new Color(0.06f, 0.05f, 0.10f, 0.92f),
                    isSelected ? Theme.Gold : eCol, isSelected ? 3f : 1.5f);

                var topStrip = Ui.Box(card, "TopStrip", 6, 6, 280, 28,
                    new Color(eCol.r * 0.28f, eCol.g * 0.28f, eCol.b * 0.28f, 0.95f));
                Ui.Label(topStrip.transform, titles[i], 0, 0, 280, 28, 14, isSelected ? Theme.Gold : eCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);

                Ui.Label(card, rules[i], 10, 38, 272, 38, 11, Theme.Muted, TextAnchor.MiddleCenter);

                string[] ids = CampaignStory.GetElementalsFor(elem);
                for (int c = 0; c < ids.Length; c++)
                {
                    var def = App.Services.Content.Character(ids[c]);
                    if (def == null) continue;
                    var mini = Ui.Frame(card, "Char" + c, 12 + c * 90, 82, 84, 148, Theme.PanelLight, isSelected ? eCol : Theme.BorderSubtle, 1f);
                    Ui.Picture(mini, "Pic", Art.Fighter(def.Id, def.Element, def.Class), 8, 6, 66, 66);
                    Ui.Label(mini, def.Appearance, 4, 74, 74, 34, 10, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    Ui.Label(mini, Theme.ClassName(def.Class), 4, 112, 74, 24, 10, eCol, TextAnchor.MiddleCenter);
                }

                // A escolha em si acontece na tela de afinidade, logo depois do prólogo: aqui as cartas só apresentam os caminhos.
                string tag = isSelected ? "AFINIDADE DO MAGO" : currentAffinity == Element.Neutral ? "UM CAMINHO POSSÍVEL" : "OUTRO CAMINHO";
                var tagBox = Ui.Frame(card, "Tag", 18, 240, 256, 44, isSelected ? Theme.ButtonPrimary : Theme.PanelLight, isSelected ? Theme.Gold : eCol, 1.5f);
                Ui.Label(tagBox, tag, 0, 0, 252, 40, 14, isSelected ? Color.white : eCol, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        // ── Composição 4: Desbloqueio de Novas Lendas no Espectro (GDD R11) ──────

        private void BuildSpectrumUnlockStage(
            RectTransform stage,
            CutsceneSlide slide,
            Color affinityColor,
            List<RectTransform> animatedRings,
            List<RectTransform> animatedPortraits)
        {
            DrawMageCard(stage, 54, 66, affinityColor, animatedRings, animatedPortraits);

            var pactBanner = Ui.Frame(stage, "SpectrumBanner", 356, 10, 868, 42, new Color(0.07f, 0.05f, 0.12f, 0.94f), Theme.Gold, 1.5f);
            Ui.Label(pactBanner, "NOVOS LUTADORES NO ESPECTRO DE " + CampaignStory.SummonerName.ToUpperInvariant(),
                10, 0, 844, 38, 16, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            for (int i = 0; i < slide.SummonedElementals.Length; i++)
            {
                var def = App.Services.Content.Character(slide.SummonedElementals[i]);
                if (def == null) continue;
                Color elemCol = Theme.ElementColor(def.Element);
                float cx = 356 + i * 292;

                var pillar = Ui.Picture(stage, "Pillar" + i, Art.VfxPillar, cx + 26, 46, 224, 304);
                pillar.color = new Color(elemCol.r, elemCol.g, elemCol.b, 0.45f);

                var ring = Ui.Picture(stage, "Ring" + i, Art.VfxRing, cx + 20, 66, 236, 236);
                ring.color = new Color(elemCol.r, elemCol.g, elemCol.b, 0.68f);
                animatedRings.Add(ring.rectTransform);

                var card = Ui.Frame(stage, "ElemCard" + i, cx + 14, 62, 248, 266, new Color(0.06f, 0.06f, 0.10f, 0.96f), elemCol, 2.5f);
                var badge = Ui.Box(card, "NewBadge", 10, 8, 224, 24, new Color(elemCol.r * 0.28f, elemCol.g * 0.28f, elemCol.b * 0.28f, 0.95f));
                Ui.Label(badge.transform, "NO ESPECTRO · HP CHEIO", 0, 0, 224, 24, 11, elemCol, TextAnchor.MiddleCenter, FontStyle.Bold);

                Ui.Picture(card, "Sprite", Art.Fighter(def.Id, def.Element, def.Class), 58, 36, 128, 128);
                Ui.Label(card, def.Appearance.ToUpper(), 8, 168, 228, 24, 16, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                Ui.Label(card, def.Name + " · " + Theme.ElementName(def.Element),
                    8, 194, 228, 20, 13, elemCol, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(card, "Pronto para o chamado do mago",
                    8, 220, 228, 22, 11, Theme.Good, TextAnchor.MiddleCenter, FontStyle.Italic);
                animatedPortraits.Add((RectTransform)card.parent);
            }
        }

        // ── Composição 5: Confronto com o Guardião da Zona / Guardião do Núcleo ──

        private void BuildGuardianConfrontationStage(
            RectTransform stage,
            CutsceneSlide slide,
            Color accent,
            Element playerAffinity,
            Color affinityColor,
            List<RectTransform> animatedRings,
            List<RectTransform> animatedPortraits)
        {
            DrawMageCard(stage, 76, 66, affinityColor, animatedRings, animatedPortraits);

            // Cristal / Ressonância no centro do palco entre o Mago e o Guardião
            var centerRing = Ui.Picture(stage, "CenterOrb", Art.VfxRing, 510, 48, 260, 260);
            centerRing.color = new Color(accent.r, accent.g, accent.b, 0.55f);
            animatedRings.Add(centerRing.rectTransform);

            Texture centerCrystal = _cutsceneId == 3 ? Art.CrystalWhole : Art.CrystalShattered;
            var crystalCenter = Ui.Picture(stage, "CenterCrystal", centerCrystal, 550, 88, 180, 180);
            animatedPortraits.Add(crystalCenter.rectTransform);

            var trialBadge = Ui.Frame(stage, "TrialBadge", 450, 286, 380, 38, new Color(0.07f, 0.05f, 0.12f, 0.94f), accent, 1.5f);
            string trialText = _cutsceneId == 3
                ? "☾ JULGAMENTO DO NÚCLEO NA LUA ☾"
                : "✦ PROVAÇÃO DO GUARDIÃO DA ZONA ✦";
            Ui.Label(trialBadge, trialText, 8, 0, 360, 34, 13, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // Retrato do Guardião à direita
            string oppPortrait = (slide.Dialogue != null && slide.Dialogue.PortraitId != Art.SummonerId)
                ? slide.Dialogue.PortraitId
                : (_cutsceneId == 0 ? "fire_tank" : _cutsceneId == 1 ? "water_mage" : _cutsceneId == 2 ? "plant_tank" : BuiltInContent.BossId);
            Element oppElem = (slide.Dialogue != null && slide.Dialogue.PortraitId != Art.SummonerId)
                ? slide.Dialogue.Element
                : (_cutsceneId == 0 ? Element.Fire : _cutsceneId == 1 ? Element.Water : _cutsceneId == 2 ? Element.Plant : Element.Neutral);
            CharClass oppClass = (slide.Dialogue != null && slide.Dialogue.PortraitId != Art.SummonerId)
                ? slide.Dialogue.Class
                : (_cutsceneId == 3 ? CharClass.Neutral : CharClass.Tank);
            string oppName = (slide.Dialogue != null && slide.Dialogue.PortraitId != Art.SummonerId)
                ? slide.Dialogue.Speaker
                : CampaignStory.Chapter(Mathf.Clamp(_cutsceneId, 0, 3)).MasterName;
            string oppTitle = (slide.Dialogue != null && slide.Dialogue.PortraitId != Art.SummonerId)
                ? slide.Dialogue.Title
                : CampaignStory.Chapter(Mathf.Clamp(_cutsceneId, 0, 3)).MasterTitle;

            Color speakerCol = oppElem == Element.Neutral ? Theme.Gold : Theme.ElementColor(oppElem);

            var oppRing = Ui.Picture(stage, "OppRing", Art.VfxRing, 886, 46, 280, 280);
            oppRing.color = new Color(speakerCol.r, speakerCol.g, speakerCol.b, 0.58f);
            animatedRings.Add(oppRing.rectTransform);

            var oppCard = Ui.Frame(stage, "OppFrame", 922, 66, 208, 232, new Color(0.08f, 0.05f, 0.10f, 0.95f), speakerCol, 2.5f);
            Ui.Picture(oppCard, "OppArt", Art.Fighter(oppPortrait, oppElem, oppClass), 26, 10, 152, 152);
            Ui.Label(oppCard, oppName.ToUpper(), 6, 166, 192, 24, 13, speakerCol, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(oppCard, oppTitle, 6, 190, 192, 32, 11, Theme.Muted, TextAnchor.MiddleCenter);
            animatedPortraits.Add((RectTransform)oppCard.parent);
        }

        private void DrawMageCard(
            RectTransform stage,
            float x,
            float y,
            Color affinityColor,
            List<RectTransform> animatedRings,
            List<RectTransform> animatedPortraits)
        {
            var summonerRing = Ui.Picture(stage, "SummonerRing", Art.VfxRing, x - 36, y - 20, 272, 272);
            summonerRing.color = new Color(affinityColor.r, affinityColor.g, affinityColor.b, 0.55f);
            animatedRings.Add(summonerRing.rectTransform);

            var summonerCard = Ui.Frame(stage, "SummonerFrame", x, y, 200, 232, new Color(0.08f, 0.05f, 0.14f, 0.95f), Theme.Gold, 2.5f);
            Ui.Picture(summonerCard, "SummonerArt", Art.Fighter(Art.SummonerId, Element.Neutral, CharClass.Mage), 22, 10, 152, 152);
            Ui.Label(summonerCard, CampaignStory.SummonerName.ToUpperInvariant() + " (VOCÊ)", 6, 166, 184, 22, 14, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(summonerCard, AffinityText(CampaignStory.GetPlayerAffinity()),
                6, 190, 184, 22, 12, affinityColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            animatedPortraits.Add((RectTransform)summonerCard.parent);
        }

        // ── Animação Cinematográfica & Typewriter ────────────────────────────────

        private IEnumerator AnimateCutscene(RectTransform bg, List<RectTransform> rings, List<RectTransform> portraits, Image fadeVeil)
        {
            float t = 0f;
            const float charsPerSecond = 78f;

            while (true)
            {
                yield return null;
                t += Time.unscaledDeltaTime;

                if (fadeVeil != null)
                {
                    float alpha = Mathf.Clamp01(1f - t / 0.26f) * 0.65f;
                    fadeVeil.color = new Color(0.02f, 0.01f, 0.04f, alpha);
                }

                if (!_typingComplete && _dialogueTextLabel != null)
                {
                    int charCount = Mathf.Clamp(Mathf.FloorToInt(t * charsPerSecond), 0, _fullDialogueText.Length);
                    _dialogueTextLabel.text = _fullDialogueText.Substring(0, charCount);
                    if (charCount >= _fullDialogueText.Length)
                        _typingComplete = true;
                }

                if (bg != null)
                {
                    float s = 1.02f + 0.025f * Mathf.Sin(t * 0.45f);
                    bg.localScale = new Vector3(s, s, 1f);
                }

                for (int i = 0; i < rings.Count; i++)
                {
                    if (rings[i] == null) continue;
                    float dir = i % 2 == 0 ? 1f : -1f;
                    rings[i].localRotation = Quaternion.Euler(0f, 0f, t * 22f * dir);
                    float pulse = 1f + 0.055f * Mathf.Sin(t * 2.1f + i);
                    rings[i].localScale = new Vector3(pulse, pulse, 1f);
                }

                for (int i = 0; i < portraits.Count; i++)
                {
                    if (portraits[i] == null) continue;
                    float bob = 1f + 0.022f * Mathf.Sin(t * 2.4f + i * 0.9f);
                    portraits[i].localScale = new Vector3(bob, bob, 1f);
                }

                if (t > 0.24f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                {
                    OnAdvancePressed();
                    if (_index >= ActiveSlides.Length) yield break;
                }
            }
        }

        private void OnAdvancePressed()
        {
            if (!_typingComplete && _dialogueTextLabel != null)
            {
                _typingComplete = true;
                _dialogueTextLabel.text = _fullDialogueText;
                return;
            }
            NextSlide();
        }

        private void PrevSlide()
        {
            if (_index > 0)
            {
                _index--;
                Rebuild();
            }
        }

        private static string AffinityText(Element affinity)
        {
            return affinity == Element.Neutral ? "Afinidade a escolher" : "Afinidade: " + Theme.ElementName(affinity);
        }

        private void NextSlide()
        {
            if (_finished) return;
            _index++;
            if (_index >= ActiveSlides.Length)
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
