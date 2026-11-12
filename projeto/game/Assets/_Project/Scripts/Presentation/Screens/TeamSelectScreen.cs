using System;
using System.Collections.Generic;
using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Seleção de Personagens: 3 dos 9 lutadores (o primeiro escolhido começa na arena) e, fora do PvP,
    /// até 3 itens do inventário (GDD R8). No PvP a tela aparece duas vezes: Equipe 1 e Equipe 2.
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

        protected override void Build()
        {
            var services = App.Services;
            var content = services.Content;
            int teamSize = services.Rules.MaxTeamSize;
            bool useUpgrades = !IsPvp || !services.Profile.FairPvp;

            var fatigued = _request.Mode == GameMode.Campaign
                ? CampaignStory.GetFatiguedCharacters(_request.StageIndex, services.Profile.CampaignStage)
                : new HashSet<string>();
            if (fatigued.Count > 0) Team.RemoveAll(id => fatigued.Contains(id));

            Texture backdropArt = _request.Mode == GameMode.Campaign
                ? Art.Arena(services.Campaign.Stages[_request.StageIndex].HomeArena)
                : null;
            Ui.Backdrop(Root, backdropArt, 0.30f);
            string title = IsPvp
                ? "Equipe " + (_player + 1) + " — Jogador " + (_player + 1)
                : _request.Mode == GameMode.Campaign
                    ? CampaignStory.Chapter(_request.StageIndex).ActTitle
                    : "Seleção de Personagens";
            Ui.Label(Root, title, 30, 12, 820, 44, 32, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            string sub = fatigued.Count > 0
                ? "Escolha " + teamSize + " lutadores descansados (quem lutou na vitória anterior descansa por 1 fase)."
                : "Escolha " + teamSize + " lutadores para a arena. O 1º escolhido inicia o combate.";
            Ui.Label(Root, sub, 30, 54, 820, 24, 15, Theme.Muted, TextAnchor.MiddleLeft);

            // Grade 3×3: linhas por classe, colunas por elemento.
            var characters = content.PlayableCharacters;
            for (int i = 0; i < characters.Count; i++)
            {
                var def = characters[i];
                string id = def.Id;
                var spec = StatsResolver.ForPlayer(def, content, services.Economy,
                    useUpgrades ? (Func<Stat, int>)(stat => services.Profile.UpgradeLevel(id, stat)) : null);
                int order = Team.IndexOf(def.Id);
                DrawCard(spec, def, 30 + (i % 3) * 274, 88 + (i / 3) * 172, order, fatigued.Contains(id));
            }

            // Painel da direita: resumo, oponente, itens e botões.
            var side = Ui.Frame(Root, "Side", 856, 88, 394, 516, Theme.Panel, Theme.BorderGold, 2f);
            float y = 12;
            Ui.Label(side, "EQUIPE (" + Team.Count + "/" + teamSize + ")", 16, y, 362, 26, 18, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            y += 30;
            for (int i = 0; i < teamSize; i++)
            {
                bool hasMember = i < Team.Count;
                var slot = Ui.Frame(side, "Slot" + i, 16, y, 362, 34, Theme.PanelLight,
                    hasMember ? Theme.ElementColor(content.Character(Team[i]).Element) : Theme.BorderSubtle, 1.5f);
                if (hasMember)
                {
                    var member = content.Character(Team[i]);
                    Ui.Picture(slot, "Mini", Art.Fighter(member.Id, member.Element, member.Class), 6, 3, 28, 28);
                    string role = i == 0 ? "1º (Líder)" : (i + 1) + "º (Reserva)";
                    Ui.Label(slot, role + " — " + member.Name, 40, 0, 314, 34, 15, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                }
                else
                {
                    Ui.Label(slot, (i + 1) + "º Vaga aberta", 12, 0, 338, 34, 15, Theme.Muted, TextAnchor.MiddleLeft);
                }
                y += 38;
            }

            y += 6;
            var oppBox = Ui.Frame(side, "OpponentBox", 16, y, 362, 86, Theme.PanelLight, Theme.BorderSubtle, 1f);
            Ui.Label(oppBox, OpponentInfo(), 10, 6, 342, 74, 14, Theme.Muted, TextAnchor.UpperLeft);
            y += 96;

            if (IsPvp)
            {
                Ui.Label(side, "SUPRIMENTOS DE ARENA", 16, y, 362, 26, 17, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(side, "Kit fixo do PvP: 1 Kit Médico, 1 Remédio e 1 Poção de Ataque.", 16, y + 28, 362, 50, 15, Theme.Muted, TextAnchor.UpperLeft);
            }
            else
            {
                int max = services.Rules.MaxItemsPerBattle;
                Ui.Label(side, "MOCHILA DE COMBATE (" + TotalItems() + "/" + max + ")", 16, y, 362, 26, 17, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 30;
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

            Ui.Button(Root, "Voltar", 30, 628, 190, 56, Back, Theme.Button, 20);
            bool ready = Team.Count == teamSize;
            string confirmLabel = IsPvp && _player == 0 ? "Próximo: Equipe 2" : "Entrar na Arena!";
            var confirm = Ui.Button(Root, confirmLabel, 930, 620, 320, 68, Confirm, ready ? Theme.ButtonPrimary : Theme.Button, 26);
            confirm.interactable = ready;
        }

        private void DrawCard(FighterSpec spec, CharacterDef def, float x, float y, int order, bool isFatigued)
        {
            bool selected = order >= 0;
            Color elemColor = Theme.ElementColor(def.Element);
            Color cardFill = isFatigued
                ? new Color(0.08f, 0.07f, 0.10f, 0.92f)
                : selected
                    ? new Color(0.22f, 0.28f, 0.14f, 0.96f)
                    : Theme.PanelLight;
            var card = Ui.Button(Root, "", x, y, 262, 160, () => Toggle(def.Id, isFatigued), cardFill);
            var t = card.transform;
            var portrait = Ui.Frame(t, "Portrait", 10, 10, 68, 68, new Color(0.06f, 0.05f, 0.05f, 0.9f),
                isFatigued ? Theme.BorderSubtle : selected ? Theme.Good : elemColor, 2f);
            var pic = Ui.Picture(portrait, "Icon", Art.Fighter(def.Id, def.Element, def.Class), 2, 2, 64, 64);
            if (isFatigued) pic.color = new Color(0.35f, 0.35f, 0.40f, 0.75f);
            if (!Art.HasSprite(def.Id))
                Ui.Label(portrait, Theme.ClassLetter(def.Class), 0, 0, 68, 68, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(t, def.Name, 86, 8, 170, 26, 18, isFatigued ? Theme.Muted : Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            Ui.Label(t, Theme.ElementName(def.Element) + " · " + Theme.ClassName(def.Class), 86, 32, 170, 20, 14, elemColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(t, def.Appearance, 86, 52, 170, 20, 12, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
            Ui.Label(t, "HP " + spec.MaxHp + "  ATQ " + spec.Attack + "  DEF " + spec.Defense + "  VEL " + spec.Speed,
                10, 82, 244, 22, 13, isFatigued ? Theme.Muted : Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(t, spec.Skills[0].Name + " · " + spec.Skills[1].Name, 10, 104, 244, 20, 12, Theme.Muted, TextAnchor.MiddleLeft);
            Ui.Label(t, "★ " + spec.Skills[2].Name, 10, 124, 244, 20, 12, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (isFatigued)
            {
                var restBanner = Ui.Frame(t, "RestBanner", 8, 122, 246, 30, new Color(0.18f, 0.06f, 0.06f, 0.96f), Theme.Bad, 1.5f);
                Ui.Label(restBanner, "EM DESCANSO (Lutou na fase anterior)", 4, 0, 234, 26, 11, Theme.Bad, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else if (selected)
            {
                var badge = Ui.Frame(t, "Order", 50, 54, 34, 26, Theme.ButtonPrimary, Theme.Gold, 1.5f);
                Ui.Label(badge, (order + 1) + "º", 0, 0, 34, 26, 15, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            }
        }

        private string OpponentInfo()
        {
            var services = App.Services;
            if (_request.Mode == GameMode.Campaign)
            {
                var opponent = services.Campaign.Stages[_request.StageIndex];
                var chapter = CampaignStory.Chapter(_request.StageIndex);
                var arena = services.Content.Catalog.Arenas[opponent.HomeArena];
                string bonus = _request.StageIndex == 3
                    ? "2 Fases (Despertar Cósmico em <50% HP)"
                    : "Atributos +" + Mathf.RoundToInt((float)chapter.ChallengingStatBonus * 100f) + "%";
                return "Mestre: " + chapter.MasterName + " (" + bonus + ")\n" +
                       "Arena: " + arena.Name + " (" + Theme.ArenaEffect(arena, services.Rules) + ")\n" +
                       "Regra: os 3 vencedores desta luta descansam na fase seguinte.";
            }
            if (_request.Mode == GameMode.VsAi)
                return "Oponente: IA " + services.Content.AiProfiles[_request.DifficultyId].Name + "\nEquipe e arena inicial sorteadas.";
            return "Arena inicial sorteada. A cada 3 rodadas, um novo sorteio.\n" +
                   (services.Profile.FairPvp ? "Modo Justo: atributos base." : "Modo Justo desligado: evoluções valem.");
        }

        private void Toggle(string id, bool isFatigued)
        {
            if (isFatigued)
            {
                App.Notify("Este lutador está em descanso após vencer a última arena! Escale outros guerreiros.");
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
