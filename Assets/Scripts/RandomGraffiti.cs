using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

public class RandomGraffiti : MonoBehaviour
{

    public GameObject[] options;

    public float noGraffitiChance = 50f;

    private void Start()
    {
        GetComponent<DecalProjector>().enabled = false;
        if (options.Length <= 0) return;
        if (Random.Range(0, 100) < noGraffitiChance) return;
        var pick = options[Random.Range(0, options.Length)];
        var decal = Instantiate(pick, transform);
        decal.transform.localPosition = Vector3.zero;
        decal.transform.localRotation = Quaternion.identity;
    }
}
