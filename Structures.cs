using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace _3DFun
{
    public class Model<TVertex>
    {

        public IReadOnlyList<TVertex> Vertices { get; set; }
        /// <summary>
        /// A face is a polygon, defined by a list of vertices index.
        /// The start and the end are automatically connected.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<int>> TemplatedFaces
        {
            get => _templatedFaces;
            set
            {
                _templatedFaces = value;
                TemplatedWireframeEdges = GetTemplatedEdgesForWireframe();
            }
        }
        private IReadOnlyList<IReadOnlyList<int>> _templatedFaces;

        public IReadOnlyList<Edge<int>> TemplatedWireframeEdges { get; private set; }

        private IReadOnlyList<Edge<int>> GetTemplatedEdgesForWireframe() => EnumerateAllTemplatedEdges().ToHashSet().ToList();

        public IEnumerable<Edge<int>> EnumerateAllTemplatedEdges()
        {
            foreach (var polygon in TemplatedFaces)
            {
                for (int i = 0; i < polygon.Count - 1; i++)
                {
                    yield return new Edge<int>(polygon[i], polygon[i + 1]);
                }
                // Implicit last edge
                yield return new Edge<int>(polygon[0], polygon[polygon.Count - 1]);
            }
        }
    }

    [DebuggerDisplay("{" + nameof(DebuggerFormatted) + ",nq}")]
    public class Edge<TVertex>
    {
        public TVertex A { get; }
        public TVertex B { get; }

        public Edge(TVertex a, TVertex b)
        {
            A = a;
            B = b;
        }

        public void Deconstruct(out TVertex A, out TVertex B)
        {
            A = this.A;
            B = this.B;
        }

        private string DebuggerFormatted => $"{A:#0} {B:#0}";

        // Commutative equality
        public override bool Equals(object obj) => obj is Edge<TVertex> edge &&
                   ((EqualityComparer<TVertex>.Default.Equals(A, edge.A) && EqualityComparer<TVertex>.Default.Equals(B, edge.B))
                 || (EqualityComparer<TVertex>.Default.Equals(A, edge.B) && EqualityComparer<TVertex>.Default.Equals(B, edge.A)));
        // Commutative hashcode
        public override int GetHashCode() => HashCode.Combine(A) + HashCode.Combine(B);
        public static bool operator ==(Edge<TVertex> left, Edge<TVertex> right) => EqualityComparer<Edge<TVertex>>.Default.Equals(left, right);
        public static bool operator !=(Edge<TVertex> left, Edge<TVertex> right) => !(left == right);
    }

    public static class Model
    {
        public static Vector3 GetCenter(Model<Vector3> model)
            => new Vector3(
                model.Vertices.Sum(x => x.X) / model.Vertices.Count,
                model.Vertices.Sum(x => x.Y) / model.Vertices.Count,
                model.Vertices.Sum(x => x.Z) / model.Vertices.Count);
    }
}
