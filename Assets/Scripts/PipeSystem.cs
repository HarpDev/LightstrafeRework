using UnityEngine;

[ExecuteAlways]
public class PipeSystem : MonoBehaviour
{

    public Pipe pipePrefab;

    public int pipeCount;

    private Pipe[] pipes;

    public float curveRadius, pipeRadius;
    public int curveSegmentCount, pipeSegmentCount;
    public float ringDistance;

    public void Generate()
    {
        foreach (Transform pipe in transform)
        {
            DestroyImmediate(pipe.gameObject);
        }
        pipes = new Pipe[pipeCount];
        for (int i = 0; i < pipes.Length; i++)
        {
            Pipe pipe = pipes[i] = Instantiate(pipePrefab);
            pipe.transform.SetParent(transform, false);
            pipe.curveRadius = curveRadius;
            pipe.pipeRadius = pipeRadius;
            pipe.curveSegmentCount = curveSegmentCount;
            pipe.pipeSegmentCount = pipeSegmentCount;
            pipe.ringDistance = ringDistance;

            pipe.relativeRotation = Random.Range(0, curveSegmentCount) * 360f / pipeSegmentCount;

            pipe.Generate();

            if (i > 0)
            {
                pipe.AlignWith(pipes[i - 1]);
            }
            else
            {
                pipe.transform.rotation = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));
            }
        }
    }
}