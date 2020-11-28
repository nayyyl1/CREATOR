using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMesh : MonoBehaviour
{
    public Transform[] bones;
    //public BoneWeight[] boneWeights;//Bone weight for each vertices maximum 4 for each
    Matrix4x4[] bindPoses;

    Matrix mH;//the diagonal matrix with Hjj being the heat contribution weight of the nearest bone to vertex j.
    Matrix[] mP;//a vector with pij = 1 if the nearest bone to vertex j is i and pij = 0 otherwise
    Matrix[] mW;//weight matrix mW[i] is the weight matrix for ith bone

    public SkinnedMesh(Mesh objMesh,GameObject[] objBones)
    {
        Pinocchio.Model pModel = new Pinocchio.Model(objMesh);
        var objVers = pModel.vertices.ToArray();
        bones = new Transform[objBones.Length];
        bindPoses = new Matrix4x4[objBones.Length];

        mH = Matrix.ZeroMatrix(objVers.Length, objVers.Length);
        mP = new Matrix[objBones.Length];
        mW = new Matrix[objBones.Length];
        for(int i = 0; i < objBones.Length; i++)
        {
            mP[i] = Matrix.ZeroMatrix(objVers.Length, 1);
            mW[i] = Matrix.ZeroMatrix(objVers.Length, 1);
        }//initialize matrix

        for (int i = 0; i < objBones.Length; i++)
        {
            bones[i] = objBones[i].transform;
            bones[i].localRotation = Quaternion.identity;
            bindPoses[i] = bones[i].worldToLocalMatrix * bones[i].parent.localToWorldMatrix;
        }//initialize bones


        //Calculation mW[i] = (mH - L)^-1 HP
        Matrix mM = Matrix.ZeroMatrix(objVers.Length, objVers.Length);
        Matrix mC = Matrix.ZeroMatrix(objVers.Length, objVers.Length);
        for ( int i = 0; i < objVers.Length; i++)
        {
            //1. Find mH by calculating distance construct mH and mP
            FindNearestBone(objVers[i].p, objBones,i);
            //2. Calculate Laplacian for this vertex L= MC
            mM[i, i] = 1/objVers[i].GetArea();
            var sum = 0f;
            for (int j = 0; j < objVers[i].edges.Count; j++)
            {
                var theEdge = objVers[i].edges;
                int n1 = (j - 1) % objVers[i].edges.Count;
                int n2 = (j + 1) % objVers[i].edges.Count;

                Vector3 v1 = objVers[i].p - theEdge[n1].b.p;
                Vector3 v2 = theEdge[j].b.p - theEdge[n1].b.p;
                Vector3 v3 = objVers[i].p - theEdge[n2].b.p;
                Vector3 v4 = theEdge[j].b.p - theEdge[n2].b.p;

                float cos1 = Vector3.Dot(v1, v2) / (v1.magnitude * v2.magnitude);
                float cos2 = Vector3.Dot(v3, v4) / (v3.magnitude * v4.magnitude);

                float cot1 = cos1 / Mathf.Sqrt(1 - Mathf.Pow(cos1, 2));
                float cot2 = cos2 / Mathf.Sqrt(1 - Mathf.Pow(cos2, 2));

                mC[i, theEdge[j].b.index] = (-1 / 2) * (cot1 + cot2);

                sum -= (-1 / 2) * (cot1 + cot2);

            }
            mC[i, i] = sum;
        }
        var mL = mM * mC;
        var mFactor = mH - mL;
        for(int i = 0; i < objBones.Length; i++)
        {
            var mHP = mH * mP[i];
            mW[i] = mFactor.Invert() * mHP;
        }
        Debug.Log(objMesh.bounds.size.x);
    }


    void FindNearestBone(Vector3 vertex, GameObject[] objBones,int vertexIdx)//might be wrong for local position
    {
        var shortestDist = 200f;
        var nearestBoneIdx = -1;
        var equalBones = new List<int>();
        for (int i = 0; i < objBones.Length; i++)
        {
            var dist = 200f;
            if (bones[i].childCount == 0) dist = Vector3.Distance(vertex, bones[i].position);
            else dist = DistanceToBone(bones[i].position, bones[i].GetChild(0).position, vertex);
            Debug.Log(dist);
            if (shortestDist > dist)
            {
                shortestDist = dist;
                nearestBoneIdx = i;
                equalBones.Clear();
            }
            else if(shortestDist == dist)
            {
                if(equalBones.Count==0) equalBones.Add(nearestBoneIdx);
                equalBones.Add(i);
            }
        }
        if (equalBones.Count == 0)
        {
            mH[vertexIdx, vertexIdx] = 1 / (shortestDist * shortestDist);
            mP[nearestBoneIdx][vertexIdx, 0] = 1;
        }
        else
        {
            mH[vertexIdx, vertexIdx] = equalBones.Count / (shortestDist * shortestDist);
            mP[nearestBoneIdx][vertexIdx, 0] = 1 / equalBones.Count;
        }
    }

    void CalculateLaplacian(Pinocchio.Model objMesh)
    {
        for(int i = 0; i < objMesh.vertices.Count; i++)
        {

        }
    }

    public float DistanceToBone(Vector3 origin, Vector3 end, Vector3 point)
    {
        //Get heading
        Vector3 heading = (end - origin);
        float magnitudeMax = heading.magnitude;
        heading.Normalize();

        //Do projection from the point but clamp it
        Vector3 lhs = point - origin;
        float dotP = Vector3.Dot(lhs, heading);
        dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
        return Vector3.Distance(point,origin + heading * dotP);
    }

}
