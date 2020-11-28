using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pinocchio
{

    public class Vertex
    {
        public Vector3 p;
        public List<Edge> edges;
        public List<Triangle> triangles;
        public Vertex updated;

        // reference index to original vertex
        public int index;

        public Vertex(Vector3 p) : this(p, -1)
        {
        }

        public Vertex(Vector3 p, int index)
        {
            this.p = p;
            this.index = index;
            this.edges = new List<Edge>();
            this.triangles = new List<Triangle>();
        }

        public void AddEdge(Edge e)
        {
            if (e.a.p == p)
                edges.Add(e);
            else
                edges.Add(new Edge(this, e.a));

        }

        public void AddTriangle(Triangle f)
        {
            triangles.Add(f);
        }

        public double GetArea()
        {
            double result = 0;
            for(int i = 0; i < edges.Count; i++)
            {
                var v1 = edges[i].b.p - p;
                var v2 = edges[(i+1)% edges.Count].b.p - p;
                result += Vector3.Cross(v1, v2).magnitude;
            }
            return result;
        }

    }

}

