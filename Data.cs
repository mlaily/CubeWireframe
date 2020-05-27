using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace _3DFun
{
    public static class Data
    {
        public static Model<Vector3> CreateCube() => new Model<Vector3>
        {
            Vertices = new List<Vector3>
            {
                new Vector3(-1, +1, +1), // 0
                new Vector3(+1, +1, +1), // 1
                new Vector3(+1, -1, +1), // 2
                new Vector3(-1, -1, +1), // 3
                    //                      // 
                new Vector3(-1, +1, -1), // 4
                new Vector3(+1, +1, -1), // 5
                new Vector3(+1, -1, -1), // 6
                new Vector3(-1, -1, -1), // 7
            },
            TemplatedFaces = new List<IReadOnlyList<int>>
            {
                new[] { 0, 1, 2, 3 },
                new[] { 4, 5, 6, 7 },
                new[] { 0, 4, 7, 3 },
                new[] { 1, 5, 6, 2 },
                // Top and bottom faces are missing since we are only doing wireframes for now...
            }
        };
    }
}
