using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputDisplay : MonoBehaviour
{
    public Image forward;
    public Image back;
    public Image right;
    public Image left;

    private PlayerInput input;
    private void Start()
    {
        input = Game.OnStartResolve<PlayerInput>();
    }
    private void Update()
    {
        forward.enabled = input.IsKeyPressed(PlayerInput.MoveForward);
        back.enabled = input.IsKeyPressed(PlayerInput.MoveBackward);
        right.enabled = input.IsKeyPressed(PlayerInput.MoveRight);
        left.enabled = input.IsKeyPressed(PlayerInput.MoveLeft);
    }
}
