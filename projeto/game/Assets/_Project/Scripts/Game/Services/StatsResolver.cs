using System;
using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>Converte o cadastro de um personagem no lutador que entra na batalha, com os atributos efetivos.</summary>
    public static class StatsResolver
    {
        /// <summary>Jogador: base × (1 + 3% × nível da evolução). levelOf pode ser null para atributos base (Modo Justo, R6).</summary>
        public static FighterSpec ForPlayer(CharacterDef def, ContentDatabase content, EconomyConfig economy, Func<Stat, int> levelOf)
        {
            return Build(def, content, stat => 1.0 + (levelOf == null ? 0 : economy.UpgradePercentPerLevel * levelOf(stat)));
        }

        /// <summary>Oponente da campanha: base × (1 + bônus da fase) (R10).</summary>
        public static FighterSpec ForOpponent(CharacterDef def, ContentDatabase content, double statBonus)
        {
            return Build(def, content, stat => 1.0 + statBonus);
        }

        public static int Scale(int baseValue, double multiplier)
        {
            return (int)Math.Round(baseValue * multiplier, MidpointRounding.AwayFromZero);
        }

        private static FighterSpec Build(CharacterDef def, ContentDatabase content, Func<Stat, double> multiplier)
        {
            var skills = new List<SkillSpec>();
            foreach (var id in def.SkillIds) skills.Add(content.Skills[id]);
            return new FighterSpec(def.Id, def.Name, def.Element, def.Class,
                Scale(def.BaseHp, multiplier(Stat.Hp)),
                Scale(def.BaseAttack, multiplier(Stat.Attack)),
                Scale(def.BaseDefense, multiplier(Stat.Defense)),
                Scale(def.BaseSpeed, multiplier(Stat.Speed)),
                skills);
        }
    }
}
