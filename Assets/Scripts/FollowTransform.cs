using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{

    public Transform transformToFollow;
    public Camera viewModel;
    private Player player;

    private void Start()
    {
        player = Game.OnStartResolve<Player>();
    }

    private void Update()
    {
        var screen = viewModel.WorldToViewportPoint(transformToFollow.position);
        transform.position = player.camera.ViewportToWorldPoint(screen);
    }
}
