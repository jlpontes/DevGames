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

            Ui.Box(Root, "Background", 0, 0, Theme.Width, Theme.Height, Theme.PanelSolid);
            string title = IsPvp ? "Equipe " + (_player + 1) + " — Jogador " + (_player + 1) : "Seleção de Personagens";
            Ui.Label(Root, title, 30, 12, 800, 46, 32, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(Root, "Escolha " + teamSize + " lutadores. O primeiro escolhido começa na arena.", 30, 56, 800, 24, 16, Theme.Muted, TextAnchor.MiddleLeft);

            // Grade 3×3: linhas por classe, colunas por elemento.
            var characters = content.PlayableCharacters;
            for (int i = 0; i < characters.Count; i++)
            {
                var def = characters[i];
                string id = def.Id;
                var spec = StatsResolver.ForPlayer(def, content, services.Economy,
                    useUpgrades ? (Func<Stat, int>)(stat => services.Profile.UpgradeLevel(id, stat)) : null);
                int order = Team.IndexOf(def.Id);
                DrawCard(spec, def, 30 + (i % 3) * 274, 92 + (i / 3) * 172, order);
            }

            // Painel da direita: resumo, oponente, itens e botões.
            var side = Ui.Box(Root, "Side", 860, 92, 390, 506, Theme.PanelLight);
            float y = 14;
            Ui.Label(side.transform, "Equipe (" + Team.Count + "/" + teamSize + ")", 16, y, 358, 28, 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            y += 32;
            for (int i = 0; i < teamSize; i++)
            {
                string line = (i + 1) + "º  " + (i < Team.Count ? content.Character(Team[i]).Name : "—") + (i == 0 && Team.Count > 0 ? "  (começa)" : "");
                Ui.Label(side.transform, line, 16, y, 358, 24, 17, i < Team.Count ? Theme.Text : Theme.Muted, TextAnchor.MiddleLeft);
                y += 24;
            }

            y += 10;
            Ui.Label(side.transform, OpponentInfo(), 16, y, 358, 92, 15, Theme.Muted, TextAnchor.UpperLeft);
            y += 100;

            if (IsPvp)
            {
                Ui.Label(side.transform, "Itens", 16, y, 358, 28, 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                Ui.Label(side.transform, "Kit fixo do PvP: 1 Kit Médico, 1 Remédio e 1 Poção de Ataque.", 16, y + 30, 358, 50, 16, Theme.Muted, TextAnchor.UpperLeft);
            }
            else
            {
                int max = services.Rules.MaxItemsPerBattle;
                Ui.Label(side.transform, "Itens na mochila (" + TotalItems() + "/" + max + ")", 16, y, 358, 28, 20, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
                y += 34;
                foreach (var item in content.Items)
                {
                    string itemId = item.Id;
                    int owned = services.Profile.ItemCount(itemId);
                    int taken = Taken(itemId);
                    Ui.Label(side.transform, item.Name + "  (tem " + owned + ")", 16, y, 210, 36, 16, owned > 0 ? Theme.Text : Theme.Muted, TextAnchor.MiddleLeft);
                    var minus = Ui.Button(side.transform, "−", 232, y + 2, 38, 32, () => ChangeItem(itemId, -1), Theme.Button, 22);
                    minus.interactable = taken > 0;
                    Ui.Label(side.transform, taken.ToString(), 272, y, 40, 36, 20, Theme.Text);
                    var plus = Ui.Button(side.transform, "+", 314, y + 2, 38, 32, () => ChangeItem(itemId, 1), Theme.Button, 22);
                    plus.interactable = taken < owned && TotalItems() < max;
                    y += 40;
                }
            }

            Ui.Button(Root, "Voltar", 30, 636, 180, 52, Back, Theme.Button);
            bool ready = Team.Count == teamSize;
            string confirmLabel = IsPvp && _player == 0 ? "Próximo: Equipe 2" : "Lutar!";
            var confirm = Ui.Button(Root, confirmLabel, 950, 624, 300, 70, Confirm, ready ? Theme.ButtonPrimary : Theme.Button, 28);
            confirm.interactable = ready;
        }

        private void DrawCard(FighterSpec spec, CharacterDef def, float x, float y, int order)
        {
            bool selected = order >= 0;
            var card = Ui.Button(Root, "", x, y, 262, 160, () => Toggle(def.Id), selected ? new Color(0.30f, 0.36f, 0.20f, 1f) : Theme.PanelLight);
            var t = card.transform;
            Ui.Picture(t, "Icon", Art.Fighter(def.Id, def.Element, def.Class), 10, 10, 64, 64);
            if (!Art.HasSprite(def.Id))
                Ui.Label(t, Theme.ClassLetter(def.Class), 10, 10, 64, 64, 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(t, def.Name, 82, 8, 174, 26, 18, Theme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            Ui.Label(t, Theme.ElementName(def.Element) + " · " + Theme.ClassName(def.Class), 82, 32, 174, 20, 14, Theme.ElementColor(def.Element), TextAnchor.MiddleLeft);
            Ui.Label(t, def.Appearance, 82, 52, 174, 20, 13, Theme.Muted, TextAnchor.MiddleLeft, FontStyle.Italic);
            Ui.Label(t, "HP " + spec.MaxHp + "   ATQ " + spec.Attack + "   DEF " + spec.Defense + "   VEL " + spec.Speed,
                10, 80, 244, 22, 14, Theme.Text, TextAnchor.MiddleLeft);
            Ui.Label(t, spec.Skills[0].Name + " · " + spec.Skills[1].Name, 10, 102, 244, 20, 12, Theme.Muted, TextAnchor.MiddleLeft);
            Ui.Label(t, "Especial: " + spec.Skills[2].Name, 10, 122, 244, 20, 12, Theme.Gold, TextAnchor.MiddleLeft);
            if (selected)
            {
                // Selo com a ordem de entrada, no canto do ícone para não cobrir o nome.
                var badge = Ui.Box(t, "Order", 48, 50, 34, 26, Theme.ButtonPrimary);
                Ui.Label(badge.transform, (order + 1) + "º", 0, 0, 34, 26, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        private string OpponentInfo()
        {
            var services = App.Services;
            if (_request.Mode == GameMode.Campaign)
            {
                var opponent = services.Campaign.Stages[_request.StageIndex];
                var arena = services.Content.Catalog.Arenas[opponent.HomeArena];
                return "Oponente: " + opponent.Name + "\n" + opponent.Description + "\nA batalha começa na arena " + arena.Name +
                       " (" + Theme.ArenaEffect(arena, services.Rules) + ").";
            }
            if (_request.Mode == GameMode.VsAi)
                return "Oponente: IA " + services.Content.AiProfiles[_request.DifficultyId].Name + "\nEquipe e arena inicial sorteadas.";
            return "Arena inicial sorteada. A cada 3 rodadas, um novo sorteio.\n" +
                   (services.Profile.FairPvp ? "Modo Justo: atributos base." : "Modo Justo desligado: evoluções valem.");
        }

        private void Toggle(string id)
        {
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
