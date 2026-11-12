using System;
using System.Collections.Generic;

namespace ComboTactics.Game
{
    /// <summary>
    /// Perfil persistente (ADR-005). Só campos públicos e tipos simples, para o JsonUtility do Unity serializar.
    /// O save cita conteúdo apenas por id em texto; ids desconhecidos são ignorados ao carregar.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>v2: afinidade, espectro, HP da jornada, cartões vistos e missões (11/2026).</summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int coins;
        public List<ItemStack> items = new List<ItemStack>();
        public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();
        /// <summary>Quantas fases da campanha já foram vencidas (0 a 4).</summary>
        public int campaignStage;
        public SettingsData settings = new SettingsData();

        /// <summary>Afinidade do mago: "fire", "water", "plant" ou vazio enquanto não foi escolhida (R11).</summary>
        public string affinity = "";
        /// <summary>Lutadores trazidos para o espectro por missões secundárias.</summary>
        public List<string> recruits = new List<string>();
        /// <summary>HP de cada lutador na jornada da campanha; quem não aparece está com HP cheio.</summary>
        public List<HpEntry> journeyHp = new List<HpEntry>();
        /// <summary>Cartões de história já vistos ("prologue", "chapter_0", "finale"...).</summary>
        public List<string> seenStory = new List<string>();
        /// <summary>Missões secundárias concluídas.</summary>
        public List<string> missionsDone = new List<string>();
    }

    [Serializable]
    public class HpEntry
    {
        public string charId;
        public int hp;
    }

    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class UpgradeEntry
    {
        public string charId;
        public int hp;
        public int atk;
        public int def;
        public int spd;
    }

    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public bool fairPvp = true;
    }
}
