using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ARFunctionLab
{
    /// <summary>
    /// Lets the cutting plane be dragged directly, by controller ray on Quest and by mouse on the
    /// desktop. Both paths feed the same ray into <see cref="CrossSectionDrag"/>, so the behaviour
    /// is identical and the headset is not needed to develop or demonstrate the interaction.
    /// Hand tracking would add a third source without changing anything below the ray.
    /// </summary>
    public class CrossSectionInteractor : MonoBehaviour
    {
        private const double SnapThreshold = 0.15;

        public SurfacePlotter plotter;
        public CrossSectionDisplay display;
        public Transform graphContent;
        public Camera viewCamera;
        public bool quest;

        private bool dragging;
        private double grabOffset;
        private Vector3 dragAnchor;
        private CrossSectionAxis dragAxis;
        private XRSimpleInteractable handleInteractable;
        private XRSimpleInteractable planeInteractable;

        private void Update()
        {
            if (plotter == null || display == null || graphContent == null) return;
            if (!plotter.crossSection.enabled)
            {
                dragging = false;
                return;
            }
            if (quest) UpdateXR();
            else UpdateMouse();
        }

        // ---- Desktop ------------------------------------------------------

        private void UpdateMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || viewCamera == null) return;

            if (dragging && !mouse.leftButton.isPressed) dragging = false;

            if (!dragging && mouse.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
                if (PointsAtTheCut(ray)) Begin(ray.origin, ray.direction);
                return;
            }

            if (dragging)
            {
                Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
                Continue(ray.origin, ray.direction);
            }
        }

        private bool PointsAtTheCut(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, 10000f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == display.HandleCollider || hit == display.PlaneCollider) return true;
            }
            return false;
        }

        // ---- Quest --------------------------------------------------------

        private void UpdateXR()
        {
            AttachInteractables();
            Vector3 origin;
            Vector3 direction;
            if (!TryGetSelectingRay(out origin, out direction))
            {
                dragging = false;
                return;
            }
            if (!dragging) Begin(origin, direction);
            else Continue(origin, direction);
        }

        /// <summary>
        /// The grab targets are created on the first Build, so they are wired up lazily rather than
        /// during bootstrap.
        /// </summary>
        private void AttachInteractables()
        {
            if (handleInteractable == null && display.HandleCollider != null)
                handleInteractable = display.HandleCollider.gameObject.AddComponent<XRSimpleInteractable>();
            if (planeInteractable == null && display.PlaneCollider != null)
                planeInteractable = display.PlaneCollider.gameObject.AddComponent<XRSimpleInteractable>();
        }

        private bool TryGetSelectingRay(out Vector3 origin, out Vector3 direction)
        {
            origin = Vector3.zero;
            direction = Vector3.zero;
            IXRSelectInteractor interactor = FirstSelecting(handleInteractable) ?? FirstSelecting(planeInteractable);
            if (interactor == null) return false;

            XRRayInteractor rayInteractor = interactor as XRRayInteractor;
            Transform source = rayInteractor != null && rayInteractor.rayOriginTransform != null
                ? rayInteractor.rayOriginTransform
                : interactor.transform;
            if (source == null) return false;
            origin = source.position;
            direction = source.forward;
            return true;
        }

        private static IXRSelectInteractor FirstSelecting(XRSimpleInteractable interactable)
        {
            if (interactable == null || !interactable.isSelected) return null;
            if (interactable.interactorsSelecting.Count == 0) return null;
            return interactable.interactorsSelecting[0];
        }

        // ---- Shared drag --------------------------------------------------

        private void Begin(Vector3 worldOrigin, Vector3 worldDirection)
        {
            // The slide line is fixed for the whole gesture, so the plane cannot drift as it moves.
            dragAxis = plotter.crossSection.axis;
            dragAnchor = display.HandleCentre;
            double resolved;
            if (!Resolve(worldOrigin, worldDirection, out resolved)) return;
            dragging = true;
            // Holding the offset means the plane does not jump to the pointer on the first frame.
            grabOffset = plotter.crossSection.value - resolved;
        }

        private void Continue(Vector3 worldOrigin, Vector3 worldDirection)
        {
            if (dragAxis != plotter.crossSection.axis) { dragging = false; return; }
            double resolved;
            if (!Resolve(worldOrigin, worldDirection, out resolved)) return;

            double min;
            double max;
            CrossSectionSettings.GetFixedRange(plotter.settings, dragAxis, out min, out max);
            double step = GraphDisplay.TickStep(max - min);
            double target = CrossSectionDrag.Snap(resolved + grabOffset, step, SnapThreshold);
            if (target == plotter.crossSection.value) return;
            plotter.SetCrossSectionValue(target);
        }

        private bool Resolve(Vector3 worldOrigin, Vector3 worldDirection, out double value)
        {
            Vector3 localOrigin = graphContent.InverseTransformPoint(worldOrigin);
            Vector3 localDirection = graphContent.InverseTransformDirection(worldDirection);
            return CrossSectionDrag.TryResolve(localOrigin, localDirection, dragAnchor, dragAxis, out value);
        }
    }
}
