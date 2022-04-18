using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class RainSoundController : MonoBehaviour
{
    private AudioSource source;
    private Player player;
    private void Start()
    {
        player = Game.OnStartResolve<Player>();
        source = GetComponent<AudioSource>();
    }
    private float volume;

    private void Update()
    {
        source.volume = GameSettings.SoundVolume * volume * 0.4f;
        source.pitch = Time.timeScale;
    }

    private void FixedUpdate()
    {
        if (Physics.Raycast(player.transform.position, Vector3.down, out var hit, 50, 1, QueryTriggerInteraction.Ignore))
        {
            volume = Mathf.Lerp(volume, (50 - hit.distance) / 50, Time.fixedDeltaTime);
        } else
        {
            volume = Mathf.Lerp(volume, 0, Time.fixedDeltaTime / 4);
        }
    }
}
