using System.Collections.Generic;
using ComboTactics.Core;

namespace ComboTactics.Game
{
    /// <summary>Confere o cadastro de conteúdo. Erros aqui devem parar num teste, antes de chegar a um build.</summary>
    public static class ContentValidator
    {
        public static List<string> Validate(ContentDatabase content)
        {
            var errors = new List<string>();

            foreach (var c in content.Characters)
            {
                if (c.BaseHp <= 0 || c.BaseAttack <= 0 || c.BaseSpeed <= 0) errors.Add(c.Id + ": HP, Ataque e Velocidade precisam ser positivos.");
                if (c.BaseDefense < 0 || c.BaseDefense > 100) errors.Add(c.Id + ": Defesa fora de 0–100.");
                if (c.SkillIds.Count != 3) { errors.Add(c.Id + ": precisa de 3 habilidades."); continue; }
                for (int i = 0; i < c.SkillIds.Count; i++)
                {
                    SkillSpec skill;
                    if (!content.Skills.TryGetValue(c.SkillIds[i], out skill)) { errors.Add(c.Id + ": habilidade desconhecida " + c.SkillIds[i]); continue; }
                    if (skill.IsSpecial != (i == 2)) errors.Add(c.Id + ": as duas primeiras habilidades são normais e a terceira é o especial.");
                    if (skill.Power <= 0) errors.Add(skill.Id + ": poder precisa ser positivo.");
                    if (skill.EffectId != null && !content.Catalog.Effects.ContainsKey(skill.EffectId)) errors.Add(skill.Id + ": efeito desconhecido " + skill.EffectId);
                }
            }

            foreach (var item in content.Items)
            {
                if (item.Price <= 0) errors.Add(item.Id + ": preço precisa ser positivo.");
                if (item.Kind == ItemKind.ApplyEffect && (item.EffectId == null || !content.Catalog.Effects.ContainsKey(item.EffectId)))
                    errors.Add(item.Id + ": efeito desconhecido.");
            }

            foreach (var opponent in content.Opponents)
            {
                if (opponent.TeamIds.Count < 1 || opponent.TeamIds.Count > 3) errors.Add(opponent.Id + ": equipe precisa ter de 1 a 3 lutadores.");
                foreach (var id in opponent.TeamIds) if (content.Character(id) == null) errors.Add(opponent.Id + ": personagem desconhecido " + id);
                if (!content.AiProfiles.ContainsKey(opponent.AiProfileId)) errors.Add(opponent.Id + ": perfil de IA desconhecido " + opponent.AiProfileId);
                if (!content.Catalog.Arenas.ContainsKey(opponent.HomeArena)) errors.Add(opponent.Id + ": arena desconhecida.");
                CheckKit(content, opponent.Id, opponent.Items, errors);
            }

            foreach (var mission in content.Missions)
            {
                var recruit = content.Character(mission.RecruitId);
                if (recruit == null || !recruit.Playable) errors.Add(mission.Id + ": recruta precisa ser um personagem jogável.");
                if (mission.PlayerTeamSize < 1 || mission.PlayerTeamSize > 3) errors.Add(mission.Id + ": equipe do jogador precisa ter de 1 a 3 lutadores.");
                if (mission.EnemyTeamIds.Count < 1 || mission.EnemyTeamIds.Count > 3) errors.Add(mission.Id + ": equipe inimiga precisa ter de 1 a 3 lutadores.");
                foreach (var id in mission.EnemyTeamIds) if (content.Character(id) == null) errors.Add(mission.Id + ": personagem desconhecido " + id);
                if (!content.AiProfiles.ContainsKey(mission.AiProfileId)) errors.Add(mission.Id + ": perfil de IA desconhecido " + mission.AiProfileId);
                if (mission.RewardItemId != null && content.Item(mission.RewardItemId) == null) errors.Add(mission.Id + ": item de recompensa desconhecido.");
                if (mission.RequiredStagesCleared < 0 || mission.RequiredStagesCleared >= content.Opponents.Count) errors.Add(mission.Id + ": leva inválida.");
            }
            foreach (var c in content.Characters)
            {
                if (c.PhaseShift == null) continue;
                foreach (var id in c.PhaseShift.EffectIds)
                    if (!content.Catalog.Effects.ContainsKey(id)) errors.Add(c.Id + ": efeito da segunda fase desconhecido " + id);
            }

            CheckKit(content, "FairKit", content.FairKit, errors);
            CheckKit(content, "VsAiOpponentKit", content.VsAiOpponentKit, errors);
            if (content.PlayableCharacters.Count < 3) errors.Add("É preciso ao menos 3 personagens jogáveis.");
            if (content.Difficulties.Count == 0) errors.Add("Nenhuma dificuldade cadastrada para o modo contra IA.");
            if (content.Catalog.Arenas.Count != 4) errors.Add("As 4 arenas precisam estar cadastradas.");
            return errors;
        }

        private static void CheckKit(ContentDatabase content, string owner, IReadOnlyDictionary<string, int> kit, List<string> errors)
        {
            int total = 0;
            foreach (var pair in kit)
            {
                if (content.Item(pair.Key) == null) errors.Add(owner + ": item desconhecido " + pair.Key);
                total += pair.Value;
            }
            if (total > 3) errors.Add(owner + ": mais de 3 itens por batalha (R8).");
        }
    }
}
