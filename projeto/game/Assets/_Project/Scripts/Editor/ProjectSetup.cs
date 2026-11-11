using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ComboTactics.EditorTools
{
    /// <summary>
    /// Deixa o projeto pronto para Play e para o build WebGL sem passos manuais: cria a cena Boot (vazia; o GameRoot
    /// monta o jogo por código), registra a cena no Build Settings e aplica o checklist de WebGL (ARCHITECTURE §5).
    /// Roda sozinho na primeira vez que o projeto abre e pelo menu Combo Tactics.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string SceneFolder = "Assets/_Project/Scenes";
        private const string ScenePath = SceneFolder + "/Boot.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += RunOnceIfNeeded;
        }

        private static void RunOnceIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ConfigureCharacterTextures();
            if (File.Exists(ScenePath)) return;

            // Criar a cena Boot troca a cena aberta; só faz isso sozinho se não houver trabalho não salvo.
            var open = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(open.path) || open.isDirty)
            {
                Debug.Log("Combo Tactics: use o menu Combo Tactics > Configurar projeto para criar a cena Boot.");
                return;
            }
            Configure();
        }

        [MenuItem("Combo Tactics/Configurar projeto")]
        public static void Configure()
        {
            if (!File.Exists(ScenePath))
            {
                if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Combo Tactics";
            PlayerSettings.productName = "Combo Tactics";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            // GitHub Pages não envia Content-Encoding; o fallback faz o Brotli funcionar lá e no itch.io.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;

            ConfigureCharacterTextures();

            AssetDatabase.SaveAssets();
            Debug.Log("Combo Tactics: projeto configurado. Abra " + ScenePath + " (ou qualquer cena) e aperte Play.");
        }

        private static void ConfigureCharacterTextures()
        {
            const string folder = "Assets/_Project/Resources/Art/Characters";
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (!importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }
            }
        }

        [MenuItem("Combo Tactics/Apagar save local")]
        public static void ClearSave()
        {
            PlayerPrefs.DeleteKey("save_a");
            PlayerPrefs.DeleteKey("save_b");
            PlayerPrefs.DeleteKey("save_backup");
            PlayerPrefs.Save();
            Debug.Log("Combo Tactics: save local apagado.");
        }
    }
}
