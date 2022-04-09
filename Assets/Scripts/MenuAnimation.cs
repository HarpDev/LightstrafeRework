using UnityEngine;

public class MenuAnimation : MonoBehaviour
{
    public Camera MenuCamera;
    public Transform OptionsPosition;
    public Transform OtherPosition;

    public float lerpSpeed = 2;

    public struct PosRot
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private PosRot startPosition;
    private PosRot nextPosition;
    private PosRot previousPosition;

    private float currentLerpValue;

    private CanvasManager canvasManager;

    private void Start()
    {
        canvasManager = Game.OnStartResolve<CanvasManager>();
        var trans = MenuCamera.transform;
        startPosition = new PosRot
        {
            position = trans.position,
            rotation = trans.rotation
        };
        nextPosition = startPosition;
        previousPosition = startPosition;
    }

    public void SendToOptionsPosition()
    {
        SendToTransform(OptionsPosition);
    }

    public void SendToOtherPosition()
    {
        SendToTransform(OtherPosition);
    }

    public void SendToStartPosition()
    {
        SendToPosRot(startPosition);
    }

    public void SendToTransform(Transform trans)
    {
        SendToPosRot(new PosRot
        {
            position = trans.position,
            rotation = trans.rotation
        });
    }

    public void SendToPosRot(PosRot posRot)
    {
        currentLerpValue = 0;
        var trans = MenuCamera.transform;
        var prev = new PosRot
        {
            position = trans.position,
            rotation = trans.rotation
        };
        previousPosition = prev;
        nextPosition = posRot;
    }

    private string previousCanvas;

    private void Update()
    {
        var currentCanvas = canvasManager.GetActiveCanvas().name;

        if (currentCanvas != previousCanvas)
        {
            if (currentCanvas.ToLower().Contains("options"))
            {
                SendToOptionsPosition();
            }
            else if (canvasManager.UiTree.Count > 0)
            {
                SendToOtherPosition();
            }
            else
            {
                SendToStartPosition();
            }
        }

        previousCanvas = currentCanvas;

        var x = currentLerpValue;
        var ease = 1 - Mathf.Pow(1 - x, 3);

        MenuCamera.transform.position =
            Vector3.Lerp(previousPosition.position, nextPosition.position, ease);
        MenuCamera.transform.rotation =
            Quaternion.Lerp(previousPosition.rotation, nextPosition.rotation, ease);
        currentLerpValue += Mathf.Min(1 - currentLerpValue, Time.deltaTime * lerpSpeed);
    }
}