using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{

    public Transform transformToFollow;
    public Camera viewModel;

    private void Update()
    {
        var screen = viewModel.WorldToViewportPoint(transformToFollow.position);
        transform.position = Game.Player.camera.ViewportToWorldPoint(screen);
    }
}
