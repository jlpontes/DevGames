using ComboTactics.Core;
using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Escolha da afinidade do mago (R11): Fogo, Água ou Planta. Cada carta mostra os 3 lutadores que entram
    /// no espectro e o que isso significa diante do primeiro guardião, que é sempre o do Vulcão.
    /// </summary>
    public sealed class AffinitySelectScreen : UiScreen
    {
        private static readonly Element[] Choices = { Element.Fire, Element.Water, Element.Plant };
        private Element _selected = Element.Neutral;

        protected override void Build()
        {
            var services = App.Services;
            Ui.Backdrop(Root, Art.Arena(ArenaId.Moon), 0.24f);

            Ui.Label(Root, "A AFINIDADE DE " + CampaignStory.SummonerName.ToUpperInvariant(), 0, 14, Theme.Width, 40, 30, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Ui.Label(Root, "Os 3 lutadores do elemento escolhido abrem o seu espectro. Os outros chegam vencendo guardiões ou nas missões.",
                0, 52, Theme.Width, 24, 15, Theme.Text, TextAnchor.MiddleCenter);

            for (int i = 0; i < Choices.Length; i++) DrawCard(Choices[i], 40 + i * 408, 96);

            bool ready = _selected != Element.Neutral;
            Ui.Button(Root, "Voltar", 40, 630, 180, 60, App.ShowMenu, Theme.Button, 19);
            string confirm = ready ? "Seguir com " + StoryScript.ElementTitle(_selected) : "Escolha um elemento";
            var go = Ui.Button(Root, confirm, 880, 622, 360, 76, () => App.ChooseAffinity(_selected), ready ? Theme.ButtonPrimary : Theme.Button, 24);
            go.interactable = ready;
            Ui.Label(Root, "A escolha vale para toda a jornada. Zerar o progresso no menu recomeça do zero.", 240, 630, 620, 60, 13, Theme.Muted, TextAnchor.MiddleLeft);
        }

        private void DrawCard(Element element, float x, float y)
        {
            var content = App.Services.Content;
            bool selected = element == _selected;
            Color color = Theme.ElementColor(element);
            var frame = Ui.Frame(Root, "Affinity_" + element, x, y, 392, 508,
                selected ? new Color(color.r * 0.35f, color.g * 0.35f, color.b * 0.35f, 0.97f) : Theme.PanelLight,
                selected ? Theme.Gold : color, selected ? 3f : 1.5f);

            var arena = Ui.ArenaPicture(frame, "Arena", StoryScript.HomeArena(element), 8, 8, 372, 130);
            arena.color = selected ? Color.white : new Color(0.7f, 0.7f, 0.75f, 1f);
            Ui.Label(frame, StoryScript.ElementTitle(element).ToUpperInvariant(), 8, 90, 372, 44, 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);

            var trio = StoryScript.Trio(content, element);
            for (int i = 0; i < trio.Length; i++)
            {
                var def = content.Character(trio[i]);
                float fx = 14 + i * 122;
                var portrait = Ui.Frame(frame, "Fighter" + i, fx, 150, 114, 150, Theme.Panel, color, 1.5f);
                Ui.Picture(portrait, "Sprite", Art.Fighter(def.Id, def.Element, def.Class), 7, 6, 96, 96);
                Ui.Label(portrait, def.Name, 4, 104, 102, 22, 12, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                Ui.Label(portrait, def.Appearance, 4, 124, 102, 18, 10, Theme.Muted, TextAnchor.MiddleCenter, FontStyle.Italic);
            }

            Ui.Label(frame, Advice(element), 18, 312, 356, 120, 14, Theme.Text, TextAnchor.UpperLeft);
            if (selected)
            {
                var badge = Ui.Frame(frame, "Chosen", 96, 448, 200, 40, Theme.ButtonPrimary, Theme.Gold, 1.5f);
                Ui.Label(badge, "ESCOLHIDO", 0, 0, 196, 36, 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            }

            // Botão invisível por cima da carta inteira: clicar em qualquer parte escolhe o elemento.
            var hit = Ui.Button(Root, "", x, y, 392, 508, () => { _selected = element; Rebuild(); }, new Color(0f, 0f, 0f, 0f));
            hit.gameObject.name = "Pick_" + element;
        }

        /// <summary>Como a afinidade se sai na ordem fixa dos guardiões: Vulcão, Mar, Floresta (R10).</summary>
        private static string Advice(Element element)
        {
            switch (element)
            {
                case Element.Fire:
                    return "Começa equilibrado contra o Vulcão, mas a Água do segundo guardião vence o Fogo. " +
                           "Recrute um lutador de Planta nas missões antes do Mar.\n\nDificuldade: maior no meio da jornada.";
                case Element.Water:
                    return "A Água vence o Fogo: o primeiro guardião fica mais fácil. " +
                           "Contra a Floresta, os lutadores do Fogo que você ganhar no Vulcão viram trunfo.\n\nDificuldade: a mais suave para começar.";
                default:
                    return "O Fogo vence a Planta: o primeiro guardião é o teste mais duro. " +
                           "A missão do Guerreiro de Água está aberta desde o início para equilibrar.\n\nDificuldade: maior no começo.";
            }
        }
    }
}
