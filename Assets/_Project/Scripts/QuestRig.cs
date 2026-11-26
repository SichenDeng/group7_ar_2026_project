using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace ARFunctionLab
{
    public class QuestRig : MonoBehaviour
    {
        public IEnumerator PlaceContent(Camera camera, Transform panel, Transform graph)
        {
            panel.gameObject.SetActive(false);
            graph.gameObject.SetActive(false);
            bool tracked = false;
            while (!tracked)
            {
                UnityEngine.XR.InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                head.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out tracked);
                yield return null;
            }
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            panel.position = camera.transform.position + forward * 1.5f - right * 0.55f - Vector3.up * 0.18f;
            panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
            graph.position = camera.transform.position + forward * 1.8f + right * 0.55f - Vector3.up * 0.3f;
            graph.rotation = Quaternion.LookRotation(forward, Vector3.up);
            panel.gameObject.SetActive(true);
            graph.gameObject.SetActive(true);
        }

        public Camera Build(Material rayMaterial)
        {
            GameObject rig = new GameObject("XR Origin");
            rig.SetActive(false);
            rig.transform.SetParent(transform, false);
            XROrigin origin = rig.AddComponent<XROrigin>();
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            GameObject offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);
            origin.CameraFloorOffsetObject = offset;
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(offset.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30;
            camera.allowHDR = false;
            cameraObject.AddComponent<AudioListener>();
            AddTracking(cameraObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");
            cameraObject.AddComponent<ARCameraManager>();
            origin.Camera = camera;
            XRInteractionManager manager = rig.AddComponent<XRInteractionManager>();
            Controller(offset.transform, "LeftHand", manager, rayMaterial);
            Controller(offset.transform, "RightHand", manager, rayMaterial);
            new GameObject("AR Session", typeof(ARSession));
            rig.SetActive(true);
            return camera;
        }

        private void AddTracking(GameObject target, string position, string rotation)
        {
            TrackedPoseDriver driver = target.AddComponent<TrackedPoseDriver>();
            driver.ignoreTrackingState = true;
            driver.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, position));
            driver.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, rotation));
        }

        private void Controller(Transform parent, string hand, XRInteractionManager manager, Material material)
        {
            GameObject controller = new GameObject(hand + " Ray");
            controller.transform.SetParent(parent, false);
            string device = "<XRController>{" + hand + "}";
            AddTracking(controller, device + "/pointerPosition", device + "/pointerRotation");
            XRRayInteractor ray = controller.AddComponent<XRRayInteractor>();
            ray.interactionManager = manager;
            ray.enableUIInteraction = true;
            ray.maxRaycastDistance = 6;

            // UI presses and 3D selections are separate inputs in XRI. The trigger was already
            // wired to UI, but the cutting-plane interactable also needs the select input.
            ray.selectInput = new XRInputButtonReader();
            ray.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
            ray.selectInput.inputActionPerformed = new InputAction(hand + " Select", InputActionType.Button, device + "/triggerPressed");
            ray.selectInput.inputActionValue = new InputAction(hand + " Select Value", InputActionType.Value, device + "/trigger");

            ray.uiPressInput = new XRInputButtonReader();
            ray.uiPressInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
            ray.uiPressInput.inputActionPerformed = new InputAction(hand + " UI Press", InputActionType.Button, device + "/triggerPressed");
            ray.uiPressInput.inputActionValue = new InputAction(hand + " UI Value", InputActionType.Value, device + "/trigger");
            LineRenderer line = controller.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startWidth = 0.003f;
            line.endWidth = 0.001f;
            controller.AddComponent<XRInteractorLineVisual>();
        }
    }
}
