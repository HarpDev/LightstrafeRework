using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(BuildingGenerator))]
public class BuildingGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        BuildingGenerator generator = (BuildingGenerator) target;
        if (GUILayout.Button("Build"))
        {
            generator.top.transform.localPosition = Vector3.up * 0.001f;
        
            var renderer = generator.top.GetComponent<Renderer>();
            var offset = (generator.top.transform.position.y - renderer.bounds.center.y) + (renderer.bounds.size.y / 2f);
            generator.sliceContainer.transform.localPosition = Vector3.down * offset;
            
            var beforePosition = generator.transform.position;
            var beforeRotation = generator.transform.rotation;
            generator.transform.position = Vector3.zero;
            generator.transform.rotation = Quaternion.identity;
            for (var i = generator.sliceContainer.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(generator.sliceContainer.transform.GetChild(i).gameObject);
            }

            var selectionDigits = ("" + generator.pattern).Select(digit => int.TryParse(digit.ToString(), out var n) ? n : 0)
                .ToArray();

            var combine = new CombineInstance[selectionDigits.Length + 1];

            for (var i = 0; i < selectionDigits.Length; i++)
            {
                var indexToBuild = Random.Range(0, generator.slicePrefabs.Length);
                if (i < selectionDigits.Length)
                {
                    if (selectionDigits[i] < generator.slicePrefabs.Length && selectionDigits[i] >= 0)
                        indexToBuild = selectionDigits[i];
                }

                var addedSlice = (GameObject)PrefabUtility.InstantiatePrefab(generator.slicePrefabs[indexToBuild], generator.sliceContainer.transform);
                if (generator.randomizeRotation) addedSlice.transform.Rotate(0, 0, Random.Range(0, 4) * 90);
                addedSlice.isStatic = true;
    
                generator.transform.position = beforePosition;
                PositionSlices(generator);
                if (addedSlice.transform.position.y < -40)
                {
                    generator.transform.position = Vector3.zero;
                    break;
                }
                generator.transform.position = Vector3.zero;
            }

            PositionSlices(generator);
            ResizeTop(generator);
            combine[0].mesh = generator.top.GetComponent<MeshFilter>().sharedMesh;
            combine[0].transform = generator.top.transform.localToWorldMatrix;
            for (var i = 0; i < generator.sliceContainer.transform.childCount; i++)
            {
                var meshFilter = generator.sliceContainer.transform.GetChild(i).GetComponent<MeshFilter>();
                //combine[i + 1].mesh = meshFilter.sharedMesh;
                //combine[i + 1].transform = meshFilter.transform.localToWorldMatrix;
            }

            var hitboxMesh = new Mesh
            {
                name = "hitbox"
            };
            hitboxMesh.indexFormat = IndexFormat.UInt32;
            hitboxMesh.CombineMeshes(combine);
            //GetComponent<MeshCollider>().sharedMesh = hitboxMesh;
            generator.top.isStatic = true;
            generator.transform.position = beforePosition;
            generator.transform.rotation = beforeRotation;
        }
    }

    public void ResizeTop(BuildingGenerator generator)
    {
        var biggestBoundsX = 0f;
        var biggestBoundsZ = 0f;
        for (var i = 0; i < generator.sliceContainer.transform.childCount; i++)
        {
            var child = generator.sliceContainer.transform.GetChild(i);
            var childBounds = child.GetComponent<Renderer>().bounds;
            if (childBounds.size.x > biggestBoundsX) biggestBoundsX = childBounds.size.x;
            if (childBounds.size.z > biggestBoundsZ) biggestBoundsZ = childBounds.size.z;
        }

        var renderer = generator.top.GetComponent<Renderer>();
        generator.top.transform.localScale = Vector3.one;
        var minBoundsSize = Mathf.Max(biggestBoundsX, biggestBoundsZ);
        var topMaxBoundsSize = Mathf.Min(renderer.bounds.size.x, renderer.bounds.size.z);
        
        generator.top.transform.localScale = Vector3.one * (minBoundsSize / topMaxBoundsSize);
    }


    public void PositionSlices(BuildingGenerator generator)
    {
        var rot = generator.transform.rotation;
        var euler = rot.eulerAngles;
        var prevY = euler.y;
        euler.y = 0;
        rot.eulerAngles = euler;
        generator.transform.rotation = rot;
        
        var sliceBounds = new Bounds();
        var lastY = 0f;
        for (var i = 0; i < generator.sliceContainer.transform.childCount; i++)
        {
            var pos = Vector3.zero;
            var child = generator.sliceContainer.transform.GetChild(i);

            var childBounds = child.GetComponent<Renderer>().bounds;
            if (i == 0)
            {
                sliceBounds = childBounds;
            }
            else
            {
                sliceBounds.Encapsulate(childBounds);
            }
            pos.y = lastY;
            child.localPosition = pos;
            lastY -= childBounds.size.y;
            
            
        }

        var sliceCollider = generator.transform.GetComponent<BoxCollider>();
        if (sliceCollider != null)
        {
            sliceCollider.center = generator.transform.InverseTransformPoint(sliceBounds.center);
            sliceCollider.size = sliceBounds.size;
        }
        
        euler.y = prevY;
        rot.eulerAngles = euler;
        generator.transform.rotation = rot;
    }
}