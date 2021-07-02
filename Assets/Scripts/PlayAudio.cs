using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudio : StateMachineBehaviour
{

    public float delay;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.gameObject.GetComponent<AudioSource>().PlayDelayed(delay);
    }
}
