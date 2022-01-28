using UnityEngine;

[ExecuteAlways]
[SelectionBase]
public class BuildingGenerator : MonoBehaviour
{

    public GameObject top;
    public GameObject slice;
    public GameObject sliceContainer;

    public int slices = 10;

    private void Update()
    {
        top.transform.localPosition = Vector3.zero + (Vector3.up * 0.001f);
        slice.transform.localPosition = Vector3.zero;
        var sliceSize = slice.GetComponent<Renderer>().bounds.size.y;

        if (sliceContainer.transform.childCount < slices)
        {
            for (var i = 0; i < slices - sliceContainer.transform.childCount; i++)
            {
                Instantiate(slice, sliceContainer.transform);
            }
        }

        if (sliceContainer.transform.childCount > slices)
        {
            for (var i = 0; i < sliceContainer.transform.childCount - slices; i++)
            {
                DestroyImmediate(sliceContainer.transform.GetChild(i).gameObject);
            }
        }

        for (var i = 0; i < slices; i++)
        {
            var pos = Vector3.zero;
            pos.y -= (i + 1) * sliceSize;
            var child = sliceContainer.transform.GetChild(i);
            child.localPosition = pos;
        }
    }
}
