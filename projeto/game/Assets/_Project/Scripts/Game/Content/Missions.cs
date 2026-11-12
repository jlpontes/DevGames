using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>
    /// Missão secundária ("Ecos do Cristal"): um lutador que se desgarrou da sua zona desafia o mago sob uma regra
    /// especial. Vencer cumprindo os objetivos traz o lutador para o espectro antes do guardião da zona dele.
    /// </summary>
    public sealed class MissionDef
    {
        public readonly string Id;
        public readonly string Title;
        /// <summary>Lutador que entra no espectro na primeira conclusão.</summary>
        public readonly string RecruitId;
        /// <summary>Quantos guardiões precisam ter sido vencidos para a missão aparecer.</summary>
        public readonly int RequiredStagesCleared;
        public readonly string Summary;
        public readonly IReadOnlyList<string> EnemyTeamIds;
        public readonly double EnemyStatBonus;
        public readonly string AiProfileId;
        public readonly ArenaId Arena;
        /// <summary>Verdadeiro: a arena não é sorteada durante a missão.</summary>
        public readonly bool FixedArena;
        public readonly int PlayerTeamSize;
        public readonly bool AllowItems;
        /// <summary>Vencer até esta rodada (0 = sem limite).</summary>
        public readonly int MaxRounds;
        /// <summary>Vencer sem que nenhum lutador do jogador caia.</summary>
        public readonly bool NoLosses;
        public readonly int RewardCoins;
        /// <summary>Item extra da primeira conclusão (null = nenhum).</summary>
        public readonly string RewardItemId;

        public MissionDef(string id, string title, string recruitId, int requiredStagesCleared, string summary,
            IReadOnlyList<string> enemyTeamIds, double enemyStatBonus, string aiProfileId, ArenaId arena, bool fixedArena,
            int playerTeamSize, bool allowItems, int maxRounds, bool noLosses, int rewardCoins, string rewardItemId)
        {
            Id = id; Title = title; RecruitId = recruitId; RequiredStagesCleared = requiredStagesCleared; Summary = summary;
            EnemyTeamIds = enemyTeamIds; EnemyStatBonus = enemyStatBonus; AiProfileId = aiProfileId; Arena = arena;
            FixedArena = fixedArena; PlayerTeamSize = playerTeamSize; AllowItems = allowItems; MaxRounds = maxRounds;
            NoLosses = noLosses; RewardCoins = rewardCoins; RewardItemId = rewardItemId;
        }

        /// <summary>As regras da missão em texto, para o quadro de missões e a tela de seleção.</summary>
        public List<string> RuleTexts()
        {
            var rules = new List<string>();
            rules.Add(PlayerTeamSize == 1 ? "Duelo: leve 1 lutador" : "Leve " + PlayerTeamSize + " lutadores");
            if (!AllowItems) rules.Add("Sem itens");
            if (FixedArena) rules.Add("Arena fixa (sem sorteio)");
            if (MaxRounds > 0) rules.Add("Vença até a rodada " + MaxRounds);
            if (NoLosses) rules.Add("Nenhum lutador seu pode cair");
            return rules;
        }
    }
}
