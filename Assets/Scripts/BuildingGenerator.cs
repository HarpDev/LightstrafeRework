using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[ExecuteAlways]
[SelectionBase]
public class BuildingGenerator : MonoBehaviour
{
    public GameObject top;
    public GameObject[] slicePrefabs;
    public GameObject sliceContainer;

    public int slices = 15;

    public bool randomizeRotation = true;

    // Pattern functions as a list of prefab indexes top to bottom
    public long pattern = -1;

    private void Update()
    {
        if (Application.isPlaying) return;
        top.transform.localPosition = Vector3.up * 0.001f;
        
        var renderer = top.GetComponent<Renderer>();
        var offset = (top.transform.position.y - renderer.bounds.center.y) + (renderer.bounds.size.y / 2f);
        sliceContainer.transform.localPosition = Vector3.down * offset;
        
        PositionSlices();
    }

    public void PositionSlices()
    {
        var sliceBounds = new Bounds();
        var lastY = 0f;
        for (var i = 0; i < sliceContainer.transform.childCount; i++)
        {
            var pos = Vector3.zero;
            var child = sliceContainer.transform.GetChild(i);
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

        var sliceCollider = GetComponent<BoxCollider>();
        if (sliceCollider != null)
        {
            sliceCollider.center = transform.InverseTransformPoint(sliceBounds.center);
            sliceCollider.size = sliceBounds.size;
        }
    }

    public void ResizeTop()
    {
        var biggestBoundsX = 0f;
        var biggestBoundsZ = 0f;
        for (var i = 0; i < sliceContainer.transform.childCount; i++)
        {
            var child = sliceContainer.transform.GetChild(i);
            var childBounds = child.GetComponent<Renderer>().bounds;
            if (childBounds.size.x > biggestBoundsX) biggestBoundsX = childBounds.size.x;
            if (childBounds.size.z > biggestBoundsZ) biggestBoundsZ = childBounds.size.z;
        }

        var renderer = top.GetComponent<Renderer>();
        top.transform.localScale = Vector3.one;
        var minBoundsSize = Mathf.Max(biggestBoundsX, biggestBoundsZ);
        var topMaxBoundsSize = Mathf.Min(renderer.bounds.size.x, renderer.bounds.size.z);
        
        top.transform.localScale = Vector3.one * (minBoundsSize / topMaxBoundsSize);
    }

    public void Build()
    {
        var beforePosition = transform.position;
        var beforeRotation = transform.rotation;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        for (var i = sliceContainer.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(sliceContainer.transform.GetChild(i).gameObject);
        }

        var combine = new CombineInstance[slices + 1];

        var selectionDigits = ("" + pattern).Select(digit => int.Parse(digit.ToString())).ToArray();
        for (var i = 0; i < slices; i++)
        {
            var indexToBuild = Random.Range(0, slicePrefabs.Length);
            if (i < selectionDigits.Length)
            {
                if (selectionDigits[i] < slicePrefabs.Length && selectionDigits[i] >= 0) indexToBuild = selectionDigits[i];
            }

            var addedSlice = Instantiate(slicePrefabs[indexToBuild], sliceContainer.transform);
            if (randomizeRotation) addedSlice.transform.Rotate(0, 0, Random.Range(0, 4) * 90);
            addedSlice.isStatic = true;
        }
        PositionSlices();
        ResizeTop();
        combine[0].mesh = top.GetComponent<MeshFilter>().sharedMesh;
        combine[0].transform = top.transform.localToWorldMatrix;
        for (var i = 0; i < slices; i++)
        {
            var meshFilter = sliceContainer.transform.GetChild(i).GetComponent<MeshFilter>();
            combine[i + 1].mesh = meshFilter.sharedMesh;
            combine[i + 1].transform = meshFilter.transform.localToWorldMatrix;
        }

        var hitboxMesh = new Mesh
        {
            name = "hitbox"
        };
        hitboxMesh.indexFormat = IndexFormat.UInt32;
        hitboxMesh.CombineMeshes(combine);
        GetComponent<MeshCollider>().sharedMesh = hitboxMesh;
        top.isStatic = true;
        transform.position = beforePosition;
        transform.rotation = beforeRotation;
    }
}