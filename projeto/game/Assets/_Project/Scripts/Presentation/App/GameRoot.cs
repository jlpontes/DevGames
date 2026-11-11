using System;
using ComboTactics.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComboTactics.UI
{
    /// <summary>
    /// Ponto de entrada do jogo. Cria a si mesmo ao dar Play em qualquer cena, monta o canvas,
    /// os serviços (GameServices) e abre o menu. Sobrevive às trocas de cena.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private static GameRoot _instance;

        public App App { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("GameRoot");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GameRoot>();
        }

        private void Awake()
        {
            EnsureCamera();
            EnsureEventSystem();
            var stage = BuildCanvas();

            // Ponto de troca da persistência: um save na nuvem entra aqui, implementando ISaveStore.
            ISaveStore saveStore = new TwoSlotSaveStore(new PlayerPrefsStorage(), new JsonSaveSerializer());
            var services = new GameServices(saveStore);
            App = new App(services, this, stage);

            try
            {
                var problems = ContentValidator.Validate(services.Content);
                if (problems.Count > 0) throw new InvalidOperationException("Conteúdo inválido:\n" + string.Join("\n", problems));
                App.ShowMenu();
            }
            catch (Exception e)
            {
                App.Errors.Report(e, "inicialização");
            }
        }

        private void OnDestroy()
        {
            if (App != null) App.Errors.Dispose();
        }

        /// <summary>A interface não precisa de câmera, mas sem nenhuma o Unity deixa a tela sem limpar.</summary>
        private void EnsureCamera()
        {
            if (Camera.allCamerasCount > 0) return;
            var go = new GameObject("Main Camera");
            go.transform.SetParent(transform, false);
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.cullingMask = 0;
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.transform.SetParent(transform, false);
#if ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<StandaloneInputModule>();
#else
            // Projeto configurado só com o Input System novo: usa o módulo de UI dele, se o pacote estiver instalado.
            var moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null) go.AddComponent(moduleType);
            else go.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>Canvas que escala um palco fixo de 1280×720, com faixas pretas se a tela tiver outra proporção.</summary>
        private RectTransform BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Theme.Width, Theme.Height);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var letterbox = new GameObject("Letterbox", typeof(RectTransform)).AddComponent<Image>();
            letterbox.transform.SetParent(canvasGo.transform, false);
            letterbox.color = Color.black;
            letterbox.raycastTarget = false;
            Ui.Stretch(letterbox.rectTransform);

            var stage = new GameObject("Stage", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            stage.SetParent(canvasGo.transform, false);
            stage.anchorMin = new Vector2(0.5f, 0.5f);
            stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.anchoredPosition = Vector2.zero;
            stage.sizeDelta = new Vector2(Theme.Width, Theme.Height);
            return stage;
        }
    }
}
