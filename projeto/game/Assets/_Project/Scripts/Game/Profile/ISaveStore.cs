namespace ComboTactics.Game
{
    /// <summary>
    /// Ponto de troca da persistência (ADR-005). Hoje é o save local em dois slots;
    /// um save na nuvem entra implementando esta mesma interface.
    /// </summary>
    public interface ISaveStore
    {
        /// <summary>Nunca lança exceção: sem save válido, devolve um perfil novo e preenche Warning.</summary>
        SaveData Load();
        /// <summary>Nunca lança exceção: se a gravação falhar, preenche Warning e o jogo segue.</summary>
        void Save(SaveData data);
        /// <summary>Aviso para mostrar ao jogador (save corrompido, navegador sem armazenamento). Null se está tudo bem.</summary>
        string Warning { get; }
    }

    /// <summary>Armazenamento chave → texto. No Unity é o PlayerPrefs (IndexedDB no WebGL).</summary>
    public interface IKeyValueStorage
    {
        /// <summary>Devolve null quando a chave não existe.</summary>
        string Get(string key);
        void Set(string key, string value);
    }

    /// <summary>Conversão do perfil para texto. No Unity é o JsonUtility.</summary>
    public interface ISaveSerializer
    {
        string Serialize(SaveData data);
        SaveData Deserialize(string text);
    }
}
