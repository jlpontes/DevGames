using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Hub da Jornada Contínua do Modo História ("A Queda do Cristal Elemental" — GDD §2, Caderno p. 2, 3, 5, 8; R9–R11):
    /// Apresenta o protagonista (O Mago), sua Afinidade Elemental, a busca pelos 3 Fragmentos e pelo Núcleo Lunar,
    /// o estado de vida persistente das 9 Lendas no Espectro e 1 botão principal "Jogar / Continuar História".
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
            Element affinity = CampaignStory.GetPlayerAffinity();
            Color affinityCol = Theme.ElementColor(affinity);

            Ui.Backdrop(Root, Art.Arena(services.Campaign.Stages[currentActIdx].HomeArena), 0.26f);

            // 1. Cabeçalho do Protagonista (O Mago), Cristal Elemental e Afinidade
            var heroBar = Ui.Frame(Root, "HeroHeader", 36, 12, 968, 70, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Picture(heroBar, "SummonerPortrait", Art.Fighter(Art.SummonerId, Element.Neutral, CharClass.Mage), 10, 5, 56, 56);

            Texture crystalTex = services.Campaign.IsCompleted ? Art.CrystalWhole : Art.CrystalShattered;
            Ui.Picture(heroBar, "CrystalIcon", crystalTex, 70, 7, 52, 52);

            string titleStatus = services.Campaign.IsCompleted
                ? "O MAGO  ·  NOVO GUARDIÃO DO NÚCLEO"
                : "O MAGO  ·  JORNADA DO CRISTAL ELEMENTAL";
            Ui.Label(heroBar, titleStatus,
                128, 6, 480, 26, 20, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(heroBar, CampaignStory.CurrentActSummary(progress),
                128, 34, 826, 26, 12, Theme.Text, TextAnchor.MiddleLeft);

            var affinityBadge = Ui.Frame(heroBar, "AffinityBadge", 590, 7, 156, 24, Theme.PanelLight, affinityCol, 1f);
            Ui.Label(affinityBadge, "Afinidade: " + Theme.ElementName(affinity),
                4, 0, 148, 22, 11, affinityCol, TextAnchor.MiddleCenter, FontStyle.Bold);

            var saveBadge = Ui.Frame(heroBar, "SaveBadge", 754, 7, 202, 24, Theme.PanelLight, Theme.Good, 1f);
            Ui.Label(saveBadge, "✓ ESPECTRO SALVO (" + unlockedSet.Count + "/9 LENDAS)",
                4, 0, 194, 22, 11, Theme.Good, TextAnchor.MiddleCenter, FontStyle.Bold);

            var coins = Ui.Frame(Root, "Coins", 1018, 12, 226, 70, Theme.Panel, Theme.BorderGold, 2f);
            Ui.Label(coins, "MOEDAS DE CRISTAL", 0, 8, 222, 20, 12, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Ui.Label(coins, services.Profile.Coins.ToString(), 0, 28, 222, 34, 26, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            // 2. Linha do Tempo das 4 Fases (3 Zonas Elementais + Núcleo na Lua)
            var stages = services.Campaign.Stages;
            string[] relicLabels =
            {
                "🔥 Fragmento do Vulcão",
                "💧 Fragmento do Mar",
                "🌿 Fragmento da Floresta",
                "☾ Núcleo do Cristal"
            };

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

                var picture = Ui.Picture(card, "Arena", Art.Arena(opponent.HomeArena), 10, 10, 268, 122);
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

                string badgeText = isCurrent ? "★ PROVAÇÃO ATUAL" : cleared ? "✓ FRAGMENTO OBTIDO" : "ZONA " + (i + 1);
                var badge = Ui.Box(card, "StageBadge", 10, 10, 152, 24, new Color(0.05f, 0.04f, 0.07f, 0.90f));
                Ui.Label(badge.transform, badgeText, 0, 0, 152, 24, 11, isCurrent ? Theme.Gold : cleared ? Theme.Good : accent, TextAnchor.MiddleCenter, FontStyle.Bold);

                Ui.Label(card, chapter.ActTitle, 10, 138, 268, 24, 16, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
                Ui.Label(card, chapter.MasterName, 10, 162, 268, 22, 15, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(card, chapter.MapSynopsis, 12, 186, 264, 62, 12, Theme.Muted, TextAnchor.UpperCenter);

                // Relíquia e Lendas que se unem ao Espectro neste Ato
                var pactBox = Ui.Frame(card, "PactBox", 10, 252, 268, 56, Theme.Panel, Theme.BorderSubtle, 1f);
                if (chapter.NewlySummonedIds.Length > 0)
                {
                    string headerLabel = i == 0
                        ? "Espectro Inicial (" + Theme.ElementName(affinity) + " · 3 Lendas):"
                        : relicLabels[i - 1] + " (+3 Lendas no Espectro):";
                    Ui.Label(pactBox, headerLabel, 6, 2, 254, 18, 11, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                    for (int e = 0; e < chapter.NewlySummonedIds.Length; e++)
                    {
                        var eDef = content.Character(chapter.NewlySummonedIds[e]);
                        Ui.Picture(pactBox, "PactIcon" + e, Art.Fighter(eDef.Id, eDef.Element, eDef.Class), 42 + e * 68, 20, 32, 32);
                    }
                }
                else
                {
                    Ui.Label(pactBox, "Espectro Completo (9/9 Lendas)\nGuardião do Núcleo (2 Fases · Lua)",
                        6, 6, 254, 42, 11, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                }

                Ui.Label(card, "Arena " + arena.Name + " · Recompensa: " + opponent.Reward + " moedas",
                    10, 314, 268, 20, 12, accent, TextAnchor.MiddleCenter, FontStyle.Bold);

                int stageIndex = i;
                string stageBtnText = isCurrent ? "▶ Enfrentar Guardião" : cleared ? "🎬 Rever / Jogar Fase" : "🔒 Zona Bloqueada";
                var stageBtn = Ui.Button(card, stageBtnText, 24, 338, 240, 40,
                    () => App.PlayCampaignStage(stageIndex, cleared),
                    isCurrent ? Theme.ButtonPrimary : Theme.Button, 15);
                stageBtn.interactable = unlocked;
            }

            // 3. Faixa do Espectro Elemental (Mostra as 9 Lendas e o HP Persistente atual de cada uma!)
            var grimoireBar = Ui.Frame(Root, "GrimoireBar", 36, 494, 1208, 112, Theme.Panel, Theme.BorderGold, 2f);
            string restInfo = CampaignStory.GetLastRestReport();
            Ui.Label(grimoireBar,
                "ESPECTRO DO MAGO — VIDA PERSISTENTE ENTRE AS FASES   " +
                (string.IsNullOrEmpty(restInfo) ? "(Quem descansa no Espectro recupera +50% HP ao vencer uma fase)" : "·  " + restInfo),
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
                Ui.Label(mini, isUnlocked ? shortName : "🔒 Fase " + CampaignStory.GetUnlockChapterNumber(def.Id),
                    50, 6, 72, 20, 12, isUnlocked ? Theme.Text : Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(mini, isUnlocked ? (hp + "/" + spec.MaxHp) : "Zona Selada",
                    50, 26, 72, 18, 11, !isUnlocked ? Theme.Muted : hp <= 0 ? Theme.Bad : Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

                if (isUnlocked)
                {
                    Color barCol = ratio > 0.5f ? Theme.Good : ratio > 0.25f ? Theme.Gold : Theme.Bad;
                    Ui.Bar(mini, 6, 52, 110, 14, ratio, barCol, hp <= 0 ? "EXAUSTO" : "");
                }
            }

            // 4. Barra de Ação Principal (1 Botão Play Contínuo em Destaque!)
            Ui.Button(Root, "Menu Principal", 36, 620, 184, 68, App.ShowMenu, Theme.Button, 18);

            bool anyDamaged = CampaignStory.HasAnyDamagedElemental(progress, content, services.Economy,
                (id, st) => services.Profile.UpgradeLevel(id, st));
            var healBtn = Ui.Button(Root, "✦ Cristal: Curar Todos (" + CampaignStory.HealAllCost + "m)",
                234, 620, 264, 68, () =>
                {
                    if (!services.Profile.TrySpend(CampaignStory.HealAllCost))
                    {
                        App.Notify("Você precisa de " + CampaignStory.HealAllCost + " moedas para restaurar o Espectro no Santuário do Cristal.");
                        return;
                    }
                    services.Profile.Save();
                    CampaignStory.HealAllElementals();
                    App.Notify("Santuário do Cristal: todas as lendas do seu Espectro voltaram a 100% de HP!");
                    Rebuild();
                }, anyDamaged ? new Color(0.16f, 0.36f, 0.24f, 0.96f) : Theme.Button, 16);
            healBtn.interactable = anyDamaged;

            Ui.Button(Root, "Loja & Evolução", 512, 620, 204, 68, App.ShowStore, Theme.Button, 18);

            string playLabel = services.Campaign.IsCompleted
                ? "▶ JOGAR PROVAÇÃO FINAL (NÚCLEO)"
                : progress == 0 && !CampaignStory.HasSeenCutscene(0)
                    ? "▶ INICIAR HISTÓRIA (FASE 1)"
                    : "▶ CONTINUAR HISTÓRIA (" + currentChapter.ActTitle.Split('·')[0].Trim().ToUpper() + ")";

            var playBtn = Ui.Button(Root, "", 732, 616, 512, 76, App.ContinueCampaignStory, Theme.ButtonPrimary, 24);
            Ui.Label(playBtn.transform, playLabel, 12, 8, 488, 36, 23, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(playBtn.transform, "Fluxo Contínuo com Save Automático: Cutscene ➔ Espectro ➔ Batalha",
                12, 44, 488, 22, 13, new Color(1f, 0.92f, 0.78f, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }
}
