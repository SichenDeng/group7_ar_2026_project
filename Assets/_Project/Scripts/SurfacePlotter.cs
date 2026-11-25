using System.Collections.Generic;
using UnityEngine;

namespace ARFunctionLab
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SurfacePlotter : MonoBehaviour
    {
        public SurfaceSettings settings = new SurfaceSettings();
        public CrossSectionSettings crossSection = new CrossSectionSettings();
        public ParsedFormula CurrentFormula { get; private set; }
        public SurfaceResult LastResult { get; private set; }
        public event System.Action SurfaceChanged;
        /// <summary>Raised when only the cutting plane moved, so the mesh is not regenerated.</summary>
        public event System.Action CrossSectionChanged;
        private Mesh ownedMesh;

        public bool TryPlot(string source, out string message)
        {
            Dictionary<string, double> parameters;
            if (!settings.TryGetParameters(out parameters, out message)) return false;
            FormulaParser parser = new FormulaParser();
            FormulaError error;
            ParsedFormula formula;
            if (!parser.TryParse(source, parameters.Keys, out formula, out error))
            {
                message = error.Message + " (position " + (error.Start + 1) + ")";
                return false;
            }
            return Generate(formula, settings, parameters, out message);
        }

        public bool Replot(out string message)
        {
            Dictionary<string, double> parameters;
            if (!settings.TryGetParameters(out parameters, out message)) return false;
            return Generate(CurrentFormula, settings, parameters, out message);
        }

        public bool TryExpandDomain(out string message)
        {
            return TrySetDomain(settings.xMin * 2, settings.xMax * 2, settings.yMin * 2, settings.yMax * 2, out message);
        }

        public bool TryShrinkDomain(out string message)
        {
            return TrySetDomain(settings.xMin * 0.5, settings.xMax * 0.5, settings.yMin * 0.5, settings.yMax * 0.5, out message);
        }

        public bool TrySetDomain(double xMin, double xMax, double yMin, double yMax, out string message)
        {
            SurfaceSettings candidate = settings.Copy();
            candidate.xMin = xMin;
            candidate.xMax = xMax;
            candidate.yMin = yMin;
            candidate.yMax = yMax;
            return TryApplySettings(candidate, out message);
        }

        public bool TrySetResolution(int cells, out string message)
        {
            SurfaceSettings candidate = settings.Copy();
            candidate.cells = cells;
            return TryApplySettings(candidate, out message);
        }

        private bool TryApplySettings(SurfaceSettings candidate, out string message)
        {
            Dictionary<string, double> parameters;
            if (!candidate.TryGetParameters(out parameters, out message)) return false;
            return Generate(CurrentFormula, candidate, parameters, out message);
        }

        private bool Generate(ParsedFormula formula, SurfaceSettings candidate, Dictionary<string, double> parameters, out string message)
        {
            SurfaceResult result;
            if (!new SurfaceGenerator().TryGenerate(formula, candidate, parameters, out result, out message)) return false;
            Mesh oldMesh = ownedMesh;
            ownedMesh = result.Mesh;
            GetComponent<MeshFilter>().sharedMesh = ownedMesh;
            CurrentFormula = formula;
            LastResult = result;
            settings = candidate;
            // A new domain can leave the cutting plane outside the plotted area.
            crossSection.ClampToDomain(settings);
            Release(oldMesh);
            message = result.Mesh.vertexCount.ToString("N0") + " vertices  |  " + result.Triangles.ToString("N0") + " triangles  |  " + result.Milliseconds.ToString("F1") + " ms";
            if (result.UndefinedSamples > 0 || result.ExcludedSamples > 0)
                message += "\nSkipped: " + result.UndefinedSamples + " undefined, " + result.ExcludedSamples + " outside height limit.";
            if (SurfaceChanged != null) SurfaceChanged();
            return true;
        }

        public void ToggleCrossSection()
        {
            crossSection.enabled = !crossSection.enabled;
            if (crossSection.enabled) crossSection.ClampToDomain(settings);
            RaiseCrossSectionChanged();
        }

        public void SetCrossSectionAxis(CrossSectionAxis axis)
        {
            if (crossSection.axis == axis) return;
            crossSection.axis = axis;
            crossSection.ClampToDomain(settings);
            RaiseCrossSectionChanged();
        }

        /// <summary>Moves the plane by one grid tick, reusing the axis step the grid already draws.</summary>
        public void StepCrossSection(int direction)
        {
            double min;
            double max;
            CrossSectionSettings.GetFixedRange(settings, crossSection.axis, out min, out max);
            double step = GraphDisplay.TickStep(max - min);
            if (step <= 0 || !FunctionCatalog.IsFinite(step)) return;
            double ticks = crossSection.value / step;
            double rounded = System.Math.Round(ticks);
            // From an off-grid value the first press snaps onto the grid in that direction.
            double next = System.Math.Abs(ticks - rounded) > 1e-6
                ? (direction > 0 ? System.Math.Ceiling(ticks) : System.Math.Floor(ticks)) * step
                : (rounded + direction) * step;
            SetCrossSectionValue(next);
        }

        public void SetCrossSectionValue(double value)
        {
            crossSection.value = value;
            crossSection.ClampToDomain(settings);
            RaiseCrossSectionChanged();
        }

        private void RaiseCrossSectionChanged()
        {
            if (CrossSectionChanged != null) CrossSectionChanged();
        }

        private void Release(Mesh mesh)
        {
            if (mesh == null) return;
            if (Application.isPlaying) Destroy(mesh);
            else DestroyImmediate(mesh);
        }

        private void OnDestroy() { Release(ownedMesh); }
    }
}
