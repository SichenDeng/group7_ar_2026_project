using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ARFunctionLab
{
    public class LabBootstrap : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Material surfaceMaterial;
        public Material lineMaterial;
        public SurfaceSettings surfaceSettings = new SurfaceSettings();
        public FormulaInputUI InputUI { get; private set; }

        private void Start()
        {
            UIFactory.Font = font;
            bool quest = Application.platform == RuntimePlatform.Android;
            Camera camera;
            if (quest) camera = gameObject.AddComponent<QuestRig>().Build(lineMaterial);
            else
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(6, 4.5f, -7);
                camera.transform.LookAt(Vector3.zero);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.075f, 0.1f, 0.15f);
                camera.fieldOfView = 40;
                camera.rect = new Rect(0.47f, 0, 0.53f, 1);
            }
            GameObject lightObject = new GameObject("Key Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(35, -35, 0);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
            GameObject graph = new GameObject("GraphRoot");
            if (quest)
            {
                graph.transform.position = new Vector3(0.55f, 1.25f, 1.8f);
            }
            GameObject content = new GameObject("GraphContent");
            content.transform.SetParent(graph.transform, false);
            GameObject surface = new GameObject("Surface");
            surface.transform.SetParent(content.transform, false);
            SurfacePlotter plotter = surface.AddComponent<SurfacePlotter>();
            plotter.settings = surfaceSettings;
            surface.GetComponent<MeshRenderer>().sharedMaterial = surfaceMaterial;
            GraphDisplay display = content.AddComponent<GraphDisplay>();
            display.lineMaterial = lineMaterial;
            CrossSectionDisplay crossSection = content.AddComponent<CrossSectionDisplay>();
            crossSection.lineMaterial = lineMaterial;
            GraphViewport viewport = graph.AddComponent<GraphViewport>();
            viewport.Initialize(camera, plotter, display, crossSection, content.transform, quest);
            GameObject events = new GameObject("EventSystem", typeof(EventSystem));
            if (quest) events.AddComponent<XRUIInputModule>();
            else events.AddComponent<InputSystemUIInputModule>();
            GameObject canvasObject = new GameObject("Formula Canvas", typeof(RectTransform), typeof(Canvas));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.worldCamera = camera;
            if (quest)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform rect = canvasObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(760, 940);
                rect.position = new Vector3(-0.55f, 1.35f, 1.5f);
                rect.localScale = Vector3.one * 0.00105f;
                canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1600, 940);
                scaler.matchWidthOrHeight = 1;
                canvasObject.AddComponent<GraphicRaycaster>();
            }
            RectTransform panel = UIFactory.Rect("Formula Input", canvasObject.transform, 28, 26, 704, 844);
            InputUI = panel.gameObject.AddComponent<FormulaInputUI>();
            InputUI.plotter = plotter;
            InputUI.crossSection = crossSection;
            InputUI.graphRoot = graph.transform;
            viewport.Sidebar = panel;
            InputUI.Build();
            if (quest) StartCoroutine(GetComponent<QuestRig>().PlaceContent(camera, canvasObject.transform, graph.transform));
            RectTransform controls = UIFactory.Rect("View controls", canvasObject.transform, 28, 883, 380, 44);
            viewport.Controls = controls;
            if (!quest) controls.pivot = new Vector2(0.5f, 0);
            UIFactory.Button("Rotate left", controls, 0, 0, 106, 44, "Rotate −", delegate { viewport.Rotate(-15); });
            UIFactory.Button("Rotate right", controls, 116, 0, 106, 44, "Rotate +", delegate { viewport.Rotate(15); });
            UIFactory.Button("Reset view", controls, 232, 0, 148, 44, "Reset view", delegate
            {
                if (quest) StartCoroutine(GetComponent<QuestRig>().PlaceContent(camera, canvasObject.transform, graph.transform));
                else viewport.ResetView();
            });
        }
    }
}
