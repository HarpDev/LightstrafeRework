using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Random = UnityEngine.Random;

public class RandomGraffiti : MonoBehaviour
{

    public Material[] options;

    private void Start()
    {
        if (options.Length <= 0) return;
        var pick = options[Random.Range(0, options.Length)];
        GetComponent<DecalProjector>().material = pick;
    }
}
