using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RepeatingTexture : MonoBehaviour
{

    private void Update()
    {
        transform.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(1, 2);
    }
}
