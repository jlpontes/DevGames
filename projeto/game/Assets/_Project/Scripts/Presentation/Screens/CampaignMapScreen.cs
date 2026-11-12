using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Mapa da jornada do Modo História (Caderno p.2): o mago Kaelen, os 4 atos (três guardiões de fragmento e o
    /// Guardião do Núcleo), o espectro com o HP de cada lutador, o Altar, as missões e o botão "Continuar história".
    /// </summary>
    public sealed class CampaignMapScreen : UiScreen
    {
        protected override void Build()
        {
            var services = App.Services;
            var content = services.Content;
            int progress = services.Profile.CampaignStage;
            int currentActIdx = Mathf.Clamp(progress, 0, services.Campaign.Stages.Count - 1);
            var currentChapter = CampaignStory.Chapter(currentActIdx);
            var unlockedSet = CampaignStory.GetUnlockedElementals(progress);

            Ui.Backdrop(Root, Art.Arena(services.Campaign.Stages[currentActIdx].HomeArena), 0.26f);

            // 1. Cabeçalho do Protagonista (Kaelen, o Mago Invocador) e Save Automático
            var heroBar = Ui.Frame(Root, "HeroHeader", 36, 12, 968, 70, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Picture(heroBar, "SummonerPortrait", Art.Fighter(Art.SummonerId, Element.Neutral, CharClass.Mage), 10, 5, 56, 56);
            Texture crystalTex = services.Campaign.IsCompleted ? Art.CrystalWhole : Art.CrystalShattered;
            Ui.Picture(heroBar, "CrystalIcon", crystalTex, 70, 7, 52, 52);
            Ui.Label(heroBar, CampaignStory.SummonerName.ToUpper() + "  ·  " +
                (services.Campaign.IsCompleted ? "GUARDIÃO DO NÚCLEO" : "A JORNADA DO CRISTAL"),
                128, 6, 590, 28, 21, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(heroBar, CampaignStory.CurrentActSummary(progress),
                128, 34, 826, 26, 13, Theme.Text, TextAnchor.MiddleLeft);

            var saveBadge = Ui.Frame(heroBar, "SaveBadge", 734, 8, 220, 24, Theme.PanelLight, Theme.Good, 1f);
            Ui.Label(saveBadge, "ESPECTRO: " + unlockedSet.Count + " DE " + content.PlayableCharacters.Count + " LUTADORES",
                4, 0, 212, 22, 11, Theme.Good, TextAnchor.MiddleCenter, FontStyle.Bold);

            var coins = Ui.Frame(Root, "Coins", 1018, 12, 226, 70, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Label(coins, "MOEDAS DO CLÃ", 0, 8, 222, 20, 12, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(coins, services.Profile.Coins.ToString(), 0, 28, 222, 34, 26, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // 2. Linha do Tempo dos 4 Atos da História
            var stages = services.Campaign.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var opponent = stages[i];
                var chapter = CampaignStory.Chapter(i);
                bool unlocked = services.Campaign.IsUnlocked(i);
                bool cleared = services.Campaign.IsCleared(i);
                bool isCurrent = i == currentActIdx && !services.Campaign.IsCompleted;
                var arena = content.Catalog.Arenas[opponent.HomeArena];
                Color accent = arena.UsesSpeedBands ? Theme.Gold : Theme.ElementColor(arena.Favoured);
                Color border = isCurrent ? Theme.Gold : !unlocked ? new Color(0.24f, 0.22f, 0.28f, 0.7f) : cleared ? Theme.Good : accent;

                var card = Ui.Frame(Root, "Stage" + i, 36 + i * 304, 92, 292, 392,
                    isCurrent ? new Color(0.14f, 0.11f, 0.18f, 0.96f) : Theme.PanelLight, border, isCurrent ? 3f : 2f);

                var picture = Ui.ArenaPicture(card, "Arena", opponent.HomeArena, 10, 10, 268, 122);
                if (!unlocked) picture.color = new Color(0.25f, 0.25f, 0.28f, 1f);

                int count = opponent.TeamIds.Count;
                for (int t = 0; t < count; t++)
                {
                    var def = content.Character(opponent.TeamIds[t]);
                    float size = count == 1 ? 84f : 62f;
                    float spacing = 68f;
                    float startX = 144f - ((count - 1) * spacing) * 0.5f - size * 0.5f;
                    var icon = Ui.Picture(card, "Fighter" + t, Art.Fighter(def.Id, def.Element, def.Class), startX + t * spacing, 126f - size, size, size);
                    if (!unlocked) icon.color = new Color(0.18f, 0.18f, 0.22f, 0.9f);
                }

                string badgeText = isCurrent ? "★ CAPÍTULO ATUAL" : cleared ? "✓ CONCLUÍDO" : "ATO " + (i + 1);
                var badge = Ui.Box(card, "StageBadge", 10, 10, 142, 24, new Color(0.05f, 0.04f, 0.07f, 0.90f));
                Ui.Label(badge.transform, badgeText, 0, 0, 142, 24, 11, isCurrent ? Theme.Gold : cleared ? Theme.Good : accent, TextAnchor.MiddleCenter, FontStyle.Bold);

                Ui.Label(card, chapter.ActTitle, 10, 138, 268, 24, 17, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                Ui.Label(card, chapter.MasterName, 10, 162, 268, 22, 15, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(card, chapter.MapSynopsis, 12, 186, 264, 62, 12, Theme.Muted, TextAnchor.UpperCenter);

                // O que a vitória traz: os lutadores do guardião que ainda não estão no espectro (R11).
                var pactBox = Ui.Frame(card, "PactBox", 10, 252, 268, 56, Theme.Panel, Theme.BorderSubtle, 1f);
                if (chapter.NewlySummonedIds.Length > 0)
                {
                    Ui.Label(pactBox, "Ao vencer, entram no espectro:", 6, 2, 254, 18, 11, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                    for (int e = 0; e < chapter.NewlySummonedIds.Length; e++)
                    {
                        var eDef = content.Character(chapter.NewlySummonedIds[e]);
                        Ui.Picture(pactBox, "PactIcon" + e, Art.Fighter(eDef.Id, eDef.Element, eDef.Class), 42 + e * 68, 20, 32, 32);
                    }
                }
                else
                {
                    string pactText = opponent.UnlocksElement == Element.Neutral
                        ? "Guardião sem elemento, com 2 fases\n(desperta abaixo de 50% de HP)"
                        : chapter.SealName + "\nOs lutadores daqui já estão no seu espectro";
                    Ui.Label(pactBox, pactText, 6, 6, 254, 42, 11, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                }

                Ui.Label(card, "Arena " + arena.Name + " · Recompensa: " + opponent.Reward + " moedas",
                    10, 314, 268, 20, 12, accent, TextAnchor.MiddleCenter, FontStyle.Bold);

                int stageIndex = i;
                string stageBtnText = isCurrent ? "▶ Continuar aqui" : cleared ? "Rever / jogar de novo" : "Bloqueado";
                var stageBtn = Ui.Button(card, stageBtnText, 24, 338, 240, 40,
                    () => App.PlayCampaignStage(stageIndex, cleared),
                    isCurrent ? Theme.ButtonPrimary : Theme.Button, 15);
                stageBtn.interactable = unlocked;
            }

            // 3. Faixa do Grimório Elemental (Mostra os 9 Elementares e o HP Persistente atual de cada um!)
            var grimoireBar = Ui.Frame(Root, "GrimoireBar", 36, 494, 1208, 112, Theme.Panel, Theme.BorderGold, 2f);
            string restInfo = CampaignStory.GetLastRestReport();
            Ui.Label(grimoireBar,
                "ESPECTRO DE KAELEN — HP DA JORNADA   " +
                (string.IsNullOrEmpty(restInfo) ? "(o dano continua entre as fases; quem fica de fora de uma vitória recupera 50%)" : "·  " + restInfo),
                14, 4, 1180, 22, 13, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

            var allChars = content.PlayableCharacters;
            for (int i = 0; i < allChars.Count; i++)
            {
                var def = allChars[i];
                bool isUnlocked = unlockedSet.Contains(def.Id);
                var spec = StatsResolver.ForPlayer(def, content, services.Economy, st => services.Profile.UpgradeLevel(def.Id, st));
                int hp = CampaignStory.GetPersistentHp(def.Id, spec.MaxHp);
                float ratio = isUnlocked ? Mathf.Clamp01(hp / (float)spec.MaxHp) : 0f;
                Color elemCol = Theme.ElementColor(def.Element);

                var mini = Ui.Frame(grimoireBar, "Grim_" + def.Id, 12 + i * 132, 28, 126, 76,
                    isUnlocked ? Theme.PanelLight : new Color(0.05f, 0.05f, 0.08f, 0.85f),
                    isUnlocked ? elemCol : Theme.BorderSubtle, 1.5f);
                var pic = Ui.Picture(mini, "Icon", Art.Fighter(def.Id, def.Element, def.Class), 6, 6, 40, 40);
                if (!isUnlocked) pic.color = new Color(0.15f, 0.15f, 0.20f, 0.8f);

                string shortName = def.Name.Replace("Guerreiro de ", "G. ").Replace("Mago de ", "M. ").Replace("Tanque de ", "T. ");
                Ui.Label(mini, isUnlocked ? shortName : "Ato " + CampaignStory.GetUnlockChapterNumber(def.Id) + " / eco",
                    50, 6, 72, 20, 12, isUnlocked ? Theme.Text : Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(mini, isUnlocked ? (hp + "/" + spec.MaxHp) : "Fora do espectro",
                    50, 26, 72, 18, 11, !isUnlocked ? Theme.Muted : hp <= 0 ? Theme.Bad : Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

                if (isUnlocked)
                {
                    Color barCol = ratio > 0.5f ? Theme.Good : ratio > 0.25f ? Theme.Gold : Theme.Bad;
                    Ui.Bar(mini, 6, 52, 110, 14, ratio, barCol, hp <= 0 ? "EXAUSTO" : "");
                }
            }

            // 4. Barra de ação: menu, Altar, missões, loja e o botão principal da história.
            Ui.Button(Root, "Menu", 36, 620, 124, 68, App.ShowMenu, Theme.Button, 18);

            bool anyDamaged = CampaignStory.HasAnyDamagedElemental(progress, content, services.Economy,
                (id, st) => services.Profile.UpgradeLevel(id, st));
            var healBtn = Ui.Button(Root, "Altar: curar todos (" + CampaignStory.HealAllCost + " moedas)",
                170, 620, 236, 68, () =>
                {
                    if (!services.Profile.TrySpend(CampaignStory.HealAllCost))
                    {
                        App.Notify("Você precisa de " + CampaignStory.HealAllCost + " moedas para restaurar o Grimório no Altar.");
                        return;
                    }
                    services.Profile.Save();
                    CampaignStory.HealAllElementals();
                    App.Notify("Altar: todo o espectro voltou a 100% de HP.");
                    Rebuild();
                }, anyDamaged ? new Color(0.16f, 0.36f, 0.24f, 0.96f) : Theme.Button, 16);
            healBtn.interactable = anyDamaged;

            int openMissions = services.Missions.OpenRecruitCount();
            var missionsBtn = Ui.Button(Root, "", 416, 620, 176, 68, App.ShowMissions,
                openMissions > 0 ? new Color(0.20f, 0.30f, 0.46f, 0.96f) : Theme.Button, 18);
            Ui.Label(missionsBtn.transform, "Missões", 6, 8, 164, 30, 19, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(missionsBtn.transform, openMissions > 0 ? openMissions + " recruta(s) à espera" : "Ecos do Cristal",
                6, 38, 164, 22, 12, openMissions > 0 ? Theme.Gold : Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);

            Ui.Button(Root, "Loja", 602, 620, 120, 68, App.ShowStore, Theme.Button, 18);

            string playLabel = services.Campaign.IsCompleted
                ? "▶ JOGAR O ATO FINAL DE NOVO"
                : progress == 0 && !CampaignStory.HasSeenCutscene(0)
                    ? "▶ INICIAR HISTÓRIA (ATO I)"
                    : "▶ CONTINUAR HISTÓRIA (" + currentChapter.ActTitle.Split('·')[0].Trim().ToUpper() + ")";

            var playBtn = Ui.Button(Root, "", 732, 616, 512, 76, App.ContinueCampaignStory, Theme.ButtonPrimary, 24);
            Ui.Label(playBtn.transform, playLabel, 12, 8, 488, 36, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(playBtn.transform, "História, equipe e batalha · o progresso é salvo sozinho",
                12, 44, 488, 22, 13, new Color(1f, 0.92f, 0.78f, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }
}
