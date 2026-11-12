using System;
using System.Collections.Generic;
using System.Globalization;

namespace ComboTactics.Game
{
    /// <summary>
    /// Save em dois slots alternados com contador e checksum (SYSTEM-DESIGN §4.3).
    /// Se uma gravação for interrompida, o slot anterior continua válido.
    /// </summary>
    public sealed class TwoSlotSaveStore : ISaveStore
    {
        public const string SlotA = "save_a";
        public const string SlotB = "save_b";
        public const string BackupKey = "save_backup";

        private readonly IKeyValueStorage _storage;
        private readonly ISaveSerializer _serializer;
        private long _counter;
        private string _lastSlot;

        public string Warning { get; private set; }

        public TwoSlotSaveStore(IKeyValueStorage storage, ISaveSerializer serializer)
        {
            _storage = storage;
            _serializer = serializer;
        }

        public SaveData Load()
        {
            Warning = null;
            string rawA = SafeGet(SlotA);
            string rawB = SafeGet(SlotB);

            long counterA, counterB;
            SaveData a = Decode(rawA, out counterA);
            SaveData b = Decode(rawB, out counterB);

            if (a != null && (b == null || counterA >= counterB)) { _counter = counterA; _lastSlot = SlotA; return a; }
            if (b != null) { _counter = counterB; _lastSlot = SlotB; return b; }

            _counter = 0;
            _lastSlot = null;
            string leftover = !string.IsNullOrEmpty(rawA) ? rawA : rawB;
            if (!string.IsNullOrEmpty(leftover))
            {
                // Havia um save, mas nenhum slot é válido: guarda o texto original e começa de novo.
                try { _storage.Set(BackupKey, leftover); } catch (Exception) { }
                Warning = "Não foi possível ler o progresso salvo. Um perfil novo foi criado.";
            }
            return new SaveData();
        }

        public void Save(SaveData data)
        {
            try
            {
                string json = _serializer.Serialize(data);
                long next = _counter + 1;
                string slot = _lastSlot == SlotA ? SlotB : SlotA;
                _storage.Set(slot, next.ToString(CultureInfo.InvariantCulture) + "|" + Checksum(json) + "|" + json);
                _counter = next;
                _lastSlot = slot;
            }
            catch (Exception)
            {
                Warning = "O progresso não pode ser salvo neste navegador. Você pode continuar jogando.";
            }
        }

        private string SafeGet(string key)
        {
            try { return _storage.Get(key); } catch (Exception) { return null; }
        }

        private SaveData Decode(string raw, out long counter)
        {
            counter = 0;
            if (string.IsNullOrEmpty(raw)) return null;
            int first = raw.IndexOf('|');
            int second = first < 0 ? -1 : raw.IndexOf('|', first + 1);
            if (second < 0) return null;
            if (!long.TryParse(raw.Substring(0, first), NumberStyles.Integer, CultureInfo.InvariantCulture, out counter)) return null;

            string json = raw.Substring(second + 1);
            if (raw.Substring(first + 1, second - first - 1) != Checksum(json)) return null;
            try
            {
                SaveData data = _serializer.Deserialize(json);
                if (data == null || data.version < 1 || data.version > SaveData.CurrentVersion) return null;
                return Migrate(data);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Converte versões antigas, uma por vez.</summary>
        private static SaveData Migrate(SaveData data)
        {
            if (data.version < 2)
            {
                // v1 → v2: campos novos começam vazios. A afinidade será escolhida ao abrir o Modo História;
                // os guardiões já vencidos continuam liberando os seus lutadores, porque o espectro é calculado.
                data.affinity = "";
                data.recruits = new List<string>();
                data.journeyHp = new List<HpEntry>();
                data.seenStory = new List<string>();
                data.missionsDone = new List<string>();
            }
            data.version = SaveData.CurrentVersion;
            return data;
        }

        /// <summary>FNV-1a de 32 bits em hexadecimal.</summary>
        public static string Checksum(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }
                return hash.ToString("x8", CultureInfo.InvariantCulture);
            }
        }
    }
}
