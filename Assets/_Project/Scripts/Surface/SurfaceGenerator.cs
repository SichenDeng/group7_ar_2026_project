using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARFunctionLab
{
    public class SurfaceResult
    {
        public Mesh Mesh;
        public int TotalSamples;
        public int UndefinedSamples;
        public int ExcludedSamples;
        public int Triangles;
        public double Milliseconds;
    }

    public class SurfaceGenerator
    {
        public bool TryGenerate(ParsedFormula formula, SurfaceSettings settings, IDictionary<string, double> parameters, out SurfaceResult result, out string message)
        {
            result = null;
            if (!settings.Validate(out message)) return false;
            if (formula == null) { message = "Generate a valid formula first."; return false; }
            if (!formula.ValidateParameters(parameters, out message)) return false;
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            int size = settings.cells + 1;
            int count = size * size;
            Vector3[] samples = new Vector3[count];
            bool[] valid = new bool[count];
            int[] vertexMap = new int[count];
            int undefined = 0;
            int excluded = 0;
            for (int j = 0; j < size; j++)
            {
                double y = settings.yMin + (settings.yMax - settings.yMin) * j / settings.cells;
                for (int i = 0; i < size; i++)
                {
                    int index = j * size + i;
                    vertexMap[index] = -1;
                    double x = settings.xMin + (settings.xMax - settings.xMin) * i / settings.cells;
                    double z;
                    EvaluationError error;
                    if (!formula.TryEvaluate(x, y, parameters, out z, out error)) { undefined++; continue; }
                    if (Math.Abs(z) > settings.maximumAbsoluteHeight) { excluded++; continue; }
                    samples[index] = GraphCoordinates.ToUnity(x, y, z);
                    valid[index] = true;
                }
            }
            List<Vector3> vertices = new List<Vector3>(count);
            List<Vector2> uv = new List<Vector2>(count);
            List<int> triangles = new List<int>(settings.cells * settings.cells * 6);
            for (int j = 0; j < settings.cells; j++)
            {
                for (int i = 0; i < settings.cells; i++)
                {
                    int a = j * size + i;
                    int b = a + 1;
                    int c = a + size;
                    int d = c + 1;
                    AddTriangle(a, c, b, valid, samples, vertexMap, vertices, uv, triangles, size);
                    AddTriangle(b, c, d, valid, samples, vertexMap, vertices, uv, triangles, size);
                }
            }
            if (triangles.Count == 0)
            {
                message = "No visible triangles in this domain. Check the formula, domain or height limit.";
                return false;
            }
            Mesh mesh = new Mesh();
            mesh.name = "Function Surface";
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            timer.Stop();
            result = new SurfaceResult
            {
                Mesh = mesh,
                TotalSamples = count,
                UndefinedSamples = undefined,
                ExcludedSamples = excluded,
                Triangles = triangles.Count / 3,
                Milliseconds = timer.Elapsed.TotalMilliseconds
            };
            message = "";
            return true;
        }

        private void AddTriangle(int a, int b, int c, bool[] valid, Vector3[] samples, int[] map, List<Vector3> vertices, List<Vector2> uv, List<int> triangles, int size)
        {
            if (!valid[a] || !valid[b] || !valid[c]) return;
            AddVertex(a, samples, map, vertices, uv, triangles, size);
            AddVertex(b, samples, map, vertices, uv, triangles, size);
            AddVertex(c, samples, map, vertices, uv, triangles, size);
        }

        private void AddVertex(int index, Vector3[] samples, int[] map, List<Vector3> vertices, List<Vector2> uv, List<int> triangles, int size)
        {
            if (map[index] < 0)
            {
                map[index] = vertices.Count;
                vertices.Add(samples[index]);
                uv.Add(new Vector2((float)(index % size) / (size - 1), (float)(index / size) / (size - 1)));
            }
            triangles.Add(map[index]);
        }
    }
}
