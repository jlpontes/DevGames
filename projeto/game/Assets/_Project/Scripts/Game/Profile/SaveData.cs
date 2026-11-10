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
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public List<ItemStack> items = new List<ItemStack>();
        public List<UpgradeEntry> upgrades = new List<UpgradeEntry>();
        /// <summary>Quantas fases da campanha já foram vencidas (0 a 4).</summary>
        public int campaignStage;
        public SettingsData settings = new SettingsData();
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
