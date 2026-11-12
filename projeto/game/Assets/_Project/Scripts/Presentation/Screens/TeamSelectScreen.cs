using System;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Seleção de Personagens / Acampamento do Invocador:
    /// No Modo História mostra os Elementares despertados no Grimório, o HP persistente de cada um,
    /// o Altar de Cura com moedas e a preparação de itens antes de entrar na arena.
    /// </summary>
    public sealed class TeamSelectScreen : UiScreen
    {
        private readonly MatchRequest _request;
        private readonly int _player;

        public TeamSelectScreen(MatchRequest request, int player)
        {
            _request = request;
            _player = player;
        }

        private List<string> Team { get { return _player == 0 ? _request.Team : _request.Team2; } }
        private bool IsPvp { get { return _request.Mode == GameMode.LocalPvp; } }
        private bool IsCampaign { get { return _request.Mode == GameMode.Campaign; } }

        protected override void Build()
        {
            var services = App.Services;
            var content = services.Content;
            int teamSize = services.Rules.MaxTeamSize;
            bool useUpgrades = !IsPvp || !services.Profile.FairPvp;

            // Na Campanha, verifica quais Elementares já foram despertados no Grimório e remove da seleção quem estiver sem HP ou selado.
            int grimoireProgress = Mathf.Max(services.Profile.CampaignStage, _request.StageIndex);
            var unlockedSet = IsCampaign ? CampaignStory.GetUnlockedElementals(grimoireProgress) : null;
            if (IsCampaign)
            {
                Team.RemoveAll(id =>
                {
                    if (!unlockedSet.Contains(id)) return true;
                    var def = content.Character(id);
                    var sp = StatsResolver.ForPlayer(def, content, services.Economy, st => services.Profile.UpgradeLevel(id, st));
                    return CampaignStory.GetPersistentHp(id, sp.MaxHp) <= 0;
                });
            }

            Texture backdropArt = IsCampaign
                ? Art.Arena(services.Campaign.Stages[_request.StageIndex].HomeArena)
                : null;
            Ui.Backdrop(Root, backdropArt, 0.30f);

            string title = IsPvp
                ? "Equipe " + (_player + 1) + " — Jogador " + (_player + 1)
                : IsCampaign
                    ? CampaignStory.Chapter(_request.StageIndex).ActTitle + " · Espectro do Mago"
                    : "Seleção de Personagens";
            Ui.Label(Root, title, 30, 10, 780, 40, 30, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);

            string sub = IsCampaign
                ? "O Mago: escolha 3 lendas vivas do seu Espectro. Quem descansar recupera +50% HP ao vencer a fase!"
                : "Escolha " + teamSize + " lutadores para a arena. O 1º escolhido inicia o combate.";
            Ui.Label(Root, sub, 30, 48, 790, 24, 14, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);

            if (IsCampaign)
            {
                var coinsBox = Ui.Frame(Root, "CoinsTop", 1044, 14, 206, 44, Theme.Panel, Theme.BorderGold, 1.5f);
                Ui.Label(coinsBox, "Moedas: " + services.Profile.Coins, 0, 0, 202, 40, 19, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            }

            // Grade 3×3: linhas por classe, colunas por elemento.
            var characters = content.PlayableCharacters;
            for (int i = 0; i < characters.Count; i++)
            {
                var def = characters[i];
                string id = def.Id;
                var spec = StatsResolver.ForPlayer(def, content, services.Economy,
                    useUpgrades ? (Func<Stat, int>)(stat => services.Profile.UpgradeLevel(id, stat)) : null);
                int order = Team.IndexOf(def.Id);
                bool unlocked = !IsCampaign || unlockedSet.Contains(id);
                int currentHp = IsCampaign ? CampaignStory.GetPersistentHp(id, spec.MaxHp) : spec.MaxHp;
                DrawCard(spec, def, 30 + (i % 3) * 274, 80 + (i / 3) * 174, order, unlocked, currentHp);
            }

            // Painel da direita: resumo, oponente, itens e botões.
            var side = Ui.Frame(Root, "Side", 856, 80, 394, 524, Theme.Panel, Theme.BorderGold, 2f);
            float y = 10;
            Ui.Label(side, "CÍRCULO DE INVOCAÇÃO (" + Team.Count + "/" + teamSize + ")", 16, y, 362, 24, 17, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            y += 28;
            for (int i = 0; i < teamSize; i++)
            {
                bool hasMember = i < Team.Count;
                var slot = Ui.Frame(side, "Slot" + i, 16, y, 362, 34, Theme.PanelLight,
                    hasMember ? Theme.ElementColor(content.Character(Team[i]).Element) : Theme.BorderSubtle, 1.5f);
                if (hasMember)
                {
                    var member = content.Character(Team[i]);
                    var mSpec = StatsResolver.ForPlayer(member, content, services.Economy,
                        useUpgrades ? (Func<Stat, int>)(stat => services.Profile.UpgradeLevel(member.Id, stat)) : null);
                    int mHp = IsCampaign ? CampaignStory.GetPersistentHp(member.Id, mSpec.MaxHp) : mSpec.MaxHp;
                    Ui.Picture(slot, "Mini", Art.Fighter(member.Id, member.Element, member.Class), 6, 3, 28, 28);
                    string role = i == 0 ? "1º (Líder)" : (i + 1) + "º (Reserva)";
                    Ui.Label(slot, role + " — " + member.Name + " (" + mHp + "/" + mSpec.MaxHp + " HP)",
                        40, 0, 314, 34, 14, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                }
                else
                {
                    Ui.Label(slot, (i + 1) + "º Vaga de invocação aberta", 12, 0, 338, 34, 14, Theme.Muted, TextAnchor.MiddleLeft);
                }
                y += 38;
            }

            y += 6;
            var oppBox = Ui.Frame(side, "OpponentBox", 16, y, 362, 92, Theme.PanelLight, Theme.BorderSubtle, 1f);
            Ui.Label(oppBox, OpponentInfo(), 10, 6, 342, 80, 13, Theme.Muted, TextAnchor.UpperLeft);
            y += 100;

            if (IsPvp)
            {
                Ui.Label(side, "SUPRIMENTOS DE ARENA", 16, y, 362, 26, 17, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(side, "Kit fixo do PvP: 1 Kit Médico, 1 Remédio e 1 Poção de Ataque.", 16, y + 28, 362, 50, 15, Theme.Muted, TextAnchor.UpperLeft);
            }
            else
            {
                int max = services.Rules.MaxItemsPerBattle;
                Ui.Label(side, "MOCHILA DE COMBATE (" + TotalItems() + "/" + max + ")", 16, y, 362, 24, 16, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 28;
                foreach (var item in content.Items)
                {
                    string itemId = item.Id;
                    int owned = services.Profile.ItemCount(itemId);
                    int taken = Taken(itemId);
                    var row = Ui.Frame(side, "Item_" + itemId, 16, y, 362, 38, Theme.PanelLight, taken > 0 ? Theme.BorderGold : Theme.BorderSubtle, 1f);
                    Ui.Label(row, item.Name + " (" + owned + ")", 10, 0, 200, 38, 15, owned > 0 ? Theme.Text : Theme.Muted, TextAnchor.MiddleLeft);
                    var minus = Ui.Button(row, "−", 220, 3, 36, 32, () => ChangeItem(itemId, -1), Theme.Button, 20);
                    minus.interactable = taken > 0;
                    Ui.Label(row, taken.ToString(), 258, 0, 38, 38, 18, taken > 0 ? Theme.Gold : Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    var plus = Ui.Button(row, "+", 298, 3, 36, 32, () => ChangeItem(itemId, 1), Theme.Button, 20);
                    plus.interactable = taken < owned && TotalItems() < max;
                    y += 42;
                }
            }

            // Barra inferior: Voltar, Altar de Cura + Cutscene (na Campanha) e Invocar na Arena
            Ui.Button(Root, IsCampaign ? "Jornada" : "Voltar", 30, 620, 168, 60, Back, Theme.Button, 19);

            if (IsCampaign)
            {
                Ui.Button(Root, "🎬 Rever Cutscene", 212, 620, 196, 60,
                    () => App.PlayCampaignStage(_request.StageIndex, true), Theme.Button, 16);

                bool anyDamaged = CampaignStory.HasAnyDamagedElemental(grimoireProgress, content, services.Economy,
                    (id, st) => services.Profile.UpgradeLevel(id, st));
                var healAllBtn = Ui.Button(Root,
                    "✦ Altar: Curar Todos (" + CampaignStory.HealAllCost + "m)",
                    422, 620, 272, 60, HealAllInSanctuary,
                    anyDamaged ? new Color(0.16f, 0.36f, 0.24f, 0.96f) : Theme.Button, 16);
                healAllBtn.interactable = anyDamaged;

                Ui.Button(Root, "Loja & Evolução", 708, 620, 196, 60, App.ShowStore, Theme.Button, 16);
            }

            bool ready = Team.Count == teamSize;
            string confirmLabel = IsPvp && _player == 0 ? "Próximo: Equipe 2" : "Invocar na Arena! ⚔";
            var confirm = Ui.Button(Root, confirmLabel, 920, 616, 330, 68, Confirm, ready ? Theme.ButtonPrimary : Theme.Button, 24);
            confirm.interactable = ready;
        }

        private void DrawCard(FighterSpec spec, CharacterDef def, float x, float y, int order, bool isUnlocked, int currentHp)
        {
            bool selected = order >= 0;
            bool isExhausted = isUnlocked && currentHp <= 0;
            bool isDamaged = isUnlocked && currentHp < spec.MaxHp;
            Color elemColor = Theme.ElementColor(def.Element);

            Color cardFill = !isUnlocked
                ? new Color(0.06f, 0.05f, 0.08f, 0.90f)
                : isExhausted
                    ? new Color(0.14f, 0.06f, 0.07f, 0.94f)
                    : selected
                        ? new Color(0.22f, 0.28f, 0.14f, 0.96f)
                        : Theme.PanelLight;

            var card = Ui.Button(Root, "", x, y, 262, 162, () => Toggle(def.Id, isUnlocked, isExhausted), cardFill);
            var t = card.transform;
            var portrait = Ui.Frame(t, "Portrait", 10, 10, 68, 68, new Color(0.06f, 0.05f, 0.05f, 0.9f),
                !isUnlocked || isExhausted ? Theme.BorderSubtle : selected ? Theme.Good : elemColor, 2f);
            var pic = Ui.Picture(portrait, "Icon", Art.Fighter(def.Id, def.Element, def.Class), 2, 2, 64, 64);
            if (!isUnlocked) pic.color = new Color(0.14f, 0.14f, 0.18f, 0.85f);
            else if (isExhausted) pic.color = new Color(0.45f, 0.25f, 0.25f, 0.80f);

            if (!Art.HasSprite(def.Id))
                Ui.Label(portrait, Theme.ClassLetter(def.Class), 0, 0, 68, 68, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            Ui.Label(t, isUnlocked ? def.Name : "??? (Espírito Selado)", 86, 8, 170, 24, 17,
                !isUnlocked || isExhausted ? Theme.Muted : Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(t, Theme.ElementName(def.Element) + " · " + Theme.ClassName(def.Class), 86, 30, 170, 20, 13, elemColor, TextAnchor.MiddleLeft, FontStyle.Bold);

            if (IsCampaign && isUnlocked)
            {
                // Barra de HP persistente do Guerreiro Elemental no Grimório
                float ratio = Mathf.Clamp01(currentHp / (float)spec.MaxHp);
                Color hpCol = ratio > 0.5f ? Theme.Good : ratio > 0.25f ? Theme.Gold : Theme.Bad;
                Ui.Bar(t, 86, 54, 164, 18, ratio, hpCol, "HP " + currentHp + "/" + spec.MaxHp);
            }
            else
            {
                Ui.Label(t, def.Appearance, 86, 52, 170, 20, 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
            }

            Ui.Label(t, "HP " + spec.MaxHp + "  ATQ " + spec.Attack + "  DEF " + spec.Defense + "  VEL " + spec.Speed,
                10, 82, 244, 20, 12, !isUnlocked || isExhausted ? Theme.Muted : Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(t, spec.Skills[0].Name + " · " + spec.Skills[1].Name, 10, 102, 244, 18, 11, Theme.Muted, TextAnchor.MiddleLeft);
            Ui.Label(t, "★ " + spec.Skills[2].Name, 10, 120, 244, 18, 11, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);

            if (!isUnlocked)
            {
                int actNum = CampaignStory.GetUnlockChapterNumber(def.Id);
                var lockBanner = Ui.Frame(t, "LockBanner", 8, 124, 246, 30, new Color(0.08f, 0.06f, 0.14f, 0.96f), Theme.BorderGold, 1.5f);
                Ui.Label(lockBanner, "🔒 UNE-SE AO ESPECTRO NA FASE " + actNum, 4, 0, 234, 26, 11, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else if (IsCampaign && isDamaged)
            {
                string id = def.Id;
                string healText = isExhausted
                    ? "EXAUSTO · Curar (" + CampaignStory.HealSingleCost + "m)"
                    : "✦ Curar 100% (" + CampaignStory.HealSingleCost + "m)";
                Ui.Button(t, healText, 10, 134, 242, 24, () => HealSingleInSanctuary(id, def.Name),
                    isExhausted ? Theme.ButtonDanger : new Color(0.16f, 0.36f, 0.24f, 0.95f), 11);
            }

            if (selected)
            {
                var badge = Ui.Frame(t, "Order", 50, 54, 34, 26, Theme.ButtonPrimary, Theme.Gold, 1.5f);
                Ui.Label(badge, (order + 1) + "º", 0, 0, 34, 26, 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            }
        }

        private void HealSingleInSanctuary(string charId, string charName)
        {
            if (!App.Services.Profile.TrySpend(CampaignStory.HealSingleCost))
            {
                App.Notify("Moedas insuficientes! Você precisa de " + CampaignStory.HealSingleCost + " moedas para canalizar cura no Altar.");
                return;
            }
            App.Services.Profile.Save();
            CampaignStory.HealElementalFull(charId);
            App.Notify(charName + " teve 100% do HP restaurado pelo Altar do Grimório!");
            Rebuild();
        }

        private void HealAllInSanctuary()
        {
            if (!App.Services.Profile.TrySpend(CampaignStory.HealAllCost))
            {
                App.Notify("Moedas insuficientes! São necessárias " + CampaignStory.HealAllCost + " moedas para curar todo o Grimório.");
                return;
            }
            App.Services.Profile.Save();
            CampaignStory.HealAllElementals();
            App.Notify("Todos os Elementares do seu Grimório foram 100% restaurados!");
            Rebuild();
        }

        private string OpponentInfo()
        {
            var services = App.Services;
            if (IsCampaign)
            {
                var opponent = services.Campaign.Stages[_request.StageIndex];
                var chapter = CampaignStory.Chapter(_request.StageIndex);
                var arena = services.Content.Catalog.Arenas[opponent.HomeArena];
                string bonus = _request.StageIndex == 3
                    ? "Boss 2 Fases (Desperta <50% HP)"
                    : "Atributos +" + Mathf.RoundToInt((float)chapter.ChallengingStatBonus * 100f) + "%";
                return "Guardião: " + chapter.MasterName + " (" + bonus + ")\n" +
                       "Zona: " + arena.Name + " (" + Theme.ArenaEffect(arena, services.Rules) + ")\n" +
                       "Dano Persistente: o HP final fica salvo; reservas no Espectro curam +50% HP!";
            }
            if (_request.Mode == GameMode.VsAi)
                return "Oponente: IA " + services.Content.AiProfiles[_request.DifficultyId].Name + "\nEquipe e arena inicial sorteadas.";
            return "Arena inicial sorteada. A cada 3 rodadas, um novo sorteio.\n" +
                   (services.Profile.FairPvp ? "Modo Justo: atributos base." : "Modo Justo desligado: evoluções valem.");
        }

        private void Toggle(string id, bool isUnlocked, bool isExhausted)
        {
            if (!isUnlocked)
            {
                int actNum = CampaignStory.GetUnlockChapterNumber(id);
                App.Notify("Este Espírito Elemental ainda está selado! Avance até o Ato " + actNum + " para despertá-lo no Grimório.");
                return;
            }
            if (isExhausted)
            {
                App.Notify("Este Elementar está com 0 HP! Deixe-o descansar por 1 vitória (+50% HP) ou cure-o no Altar.");
                return;
            }
            if (Team.Contains(id)) Team.Remove(id);
            else if (Team.Count < App.Services.Rules.MaxTeamSize) Team.Add(id);
            else App.Notify("A equipe já tem " + App.Services.Rules.MaxTeamSize + " lutadores. Clique em um deles para tirar.");
            Rebuild();
        }

        private int Taken(string itemId)
        {
            int count;
            return _request.Items.TryGetValue(itemId, out count) ? count : 0;
        }

        private int TotalItems()
        {
            int total = 0;
            foreach (var pair in _request.Items) total += pair.Value;
            return total;
        }

        private void ChangeItem(string itemId, int delta)
        {
            int value = Mathf.Max(0, Taken(itemId) + delta);
            if (value == 0) _request.Items.Remove(itemId);
            else _request.Items[itemId] = value;
            Rebuild();
        }

        private void Back()
        {
            if (IsPvp && _player == 1) App.ShowTeamSelect(_request, 0);
            else if (_request.Mode == GameMode.VsAi) App.ShowVsAi();
            else if (IsPvp) App.ShowPvp();
            else App.ShowCampaign();
        }

        private void Confirm()
        {
            if (IsPvp && _player == 0) App.ShowTeamSelect(_request, 1);
            else App.StartBattle(_request);
        }
    }
}
