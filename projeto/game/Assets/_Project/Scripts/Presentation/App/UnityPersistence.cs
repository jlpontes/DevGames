using ComboTactics.Game;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>PlayerPrefs como armazenamento do save. No WebGL ele fica no IndexedDB do navegador (ADR-005).</summary>
    public sealed class PlayerPrefsStorage : IKeyValueStorage
    {
        public string Get(string key)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        }

        public void Set(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            // Sem o Save() explícito, o WebGL pode perder a gravação quando a aba fecha.
            PlayerPrefs.Save();
        }
    }

    public sealed class JsonSaveSerializer : ISaveSerializer
    {
        public string Serialize(SaveData data) { return JsonUtility.ToJson(data); }
        public SaveData Deserialize(string text) { return JsonUtility.FromJson<SaveData>(text); }
    }
}
