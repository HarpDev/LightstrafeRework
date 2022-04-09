using UnityEngine;

public class AnimatedTexture : MonoBehaviour
{

    public Texture2D[] fzzySodaFrames;
    public Texture2D[] bigBlockRightArrowFrames;
    public Texture2D[] bigBlockLeftArrowFrames;

    private MusicPlayer music;

    private Material[] materials;
    
    private void Start()
    {
        music = Game.OnStartResolve<MusicPlayer>();
        materials = GetComponent<Renderer>().materials;
    }

    private int lastIndex;
    private void Update()
    {
        if (music == null) return;

        foreach (var mat in materials)
        {
            if (mat.name.StartsWith("bigBlockBlackYellowMat"))
            {
                var x = calcBounce(4, 0.6f);
                
                var offset = new Vector2(x, 0);
                
                mat.mainTextureOffset = offset;
                mat.SetTextureOffset("_EmissiveColorMap", offset);
            }
            if (mat.name.StartsWith("bigBlockColorfulMat"))
            {
                var x = calcElastic(10, 0.5f) + calcSine(25, 5);
                var y = calcElastic(7, 0.6f) + calcSine(15, 3);
                
                var offset = new Vector2(x, y);
                
                mat.mainTextureOffset = offset;
                mat.SetTextureOffset("_EmissiveColorMap", offset);
            }
            if (mat.name.StartsWith("fzzySodaMat"))
            {
                var time = music.Audio.timeSamples / (float)music.musicList[music.currentlyPlayingIndex].frequency;
                var fps = music.bpmList[music.currentlyPlayingIndex] / 60;
                var musicDelta = time * fps;
                var index = Mathf.RoundToInt(musicDelta);
                index %= fzzySodaFrames.Length;

                if (index != lastIndex && Time.timeScale > 0)
                {
                    mat.mainTexture = fzzySodaFrames[index];
                }
                lastIndex = index;
            }
            if (mat.name.StartsWith("bigBlockArrowMat"))
            {
                var time = music.Audio.timeSamples / (float)music.musicList[music.currentlyPlayingIndex].frequency;
                var fps = music.bpmList[music.currentlyPlayingIndex] / 60;
                var musicDelta = time * fps;
                var index = Mathf.RoundToInt(musicDelta);
                index %= bigBlockRightArrowFrames.Length;

                if (index != lastIndex && Time.timeScale > 0)
                {
                    mat.mainTexture = bigBlockRightArrowFrames[index];
                }
                lastIndex = index;
            }
            if (mat.name.StartsWith("bigBlockArrowLeftMat"))
            {
                var time = music.Audio.timeSamples / (float)music.musicList[music.currentlyPlayingIndex].frequency;
                var fps = music.bpmList[music.currentlyPlayingIndex] / 60;
                var musicDelta = time * fps;
                var index = Mathf.RoundToInt(musicDelta);
                index %= bigBlockLeftArrowFrames.Length;

                if (index != lastIndex && Time.timeScale > 0)
                {
                    mat.mainTexture = bigBlockLeftArrowFrames[index];
                }
                lastIndex = index;
            }
            if (mat.name.StartsWith("bigBlockRingMat"))
            {
                var x = calcRing(5, 3);
                
                var offset = new Vector2(x, x);
                offset += Vector2.one * 0.5f;

                mat.mainTextureOffset = -(offset / 2) + Vector2.one * 0.5f;
                mat.mainTextureScale = offset;
                mat.SetTextureOffset("_EmissiveColorMap", -(offset / 2) + Vector2.one * 0.5f);
                mat.SetTextureScale("_EmissiveColorMap", offset);
            }
        }
    }

    private float calcRing(float speed, float distance)
    {
        var position = (Time.time + distance) % speed;
        var amt = 0f;
        if (position > speed / 2)
        {
            var normalized = ((position - (speed / 2)) / (speed / 2));
            var ease = easeOutBounce(normalized);
            amt = 1 - ease;
        }
        else
        {
            var normalized = position / (speed / 2);
            var ease = easeOutElastic(normalized);
            amt = ease;
        }

        return amt * distance;
    }

    private float calcSine(float speed, float distance)
    {
        var position = (Time.time + distance) % speed;
        var amt = 0f;
        if (position > speed / 2)
        {
            var normalized = ((position - (speed / 2)) / (speed / 2));
            var ease = easeOutSine(normalized);
            amt = 1 - ease;
        }
        else
        {
            var normalized = position / (speed / 2);
            var ease = easeOutSine(normalized);
            amt = ease;
        }

        return amt * distance;
    }

    private float calcBounce(float speed, float distance)
    {
        var position = (Time.time + distance) % speed;
        var amt = 0f;
        if (position > speed / 2)
        {
            var normalized = ((position - (speed / 2)) / (speed / 2));
            var ease = easeOutBounce(normalized);
            amt = 1 - ease;
        }
        else
        {
            var normalized = position / (speed / 2);
            var ease = easeOutBounce(normalized);
            amt = ease;
        }

        return amt * distance;
    }

    private float calcElastic(float speed, float distance)
    {
        var position = (Time.time + distance) % speed;
        var amt = 0f;
        if (position > speed / 2)
        {
            var normalized = ((position - (speed / 2)) / (speed / 2));
            var ease = easeOutElastic(normalized);
            amt = 1 - ease;
        }
        else
        {
            var normalized = position / (speed / 2);
            var ease = easeOutElastic(normalized);
            amt = ease;
        }

        return amt * distance;
    }

    private float easeOutSine(float x)
    {
        return Mathf.Sin((x * Mathf.PI) / 2);
    }

    private float easeInOutElastic(float x)
    {
        var c5 = (2 * Mathf.PI) / 4.5f;

        return x == 0 ? 0 :
            x == 1 ? 1 :
            x < 0.5 ? -(Mathf.Pow(2, 20 * x - 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2 :
            (Mathf.Pow(2, -20 * x + 10) * Mathf.Sin((20 * x - 11.125f) * c5)) / 2 + 1;
    }

    private float easeOutElastic(float x)
    {
        var c4 = (2 * Mathf.PI) / 3;

        return x == 0 ? 0 : x == 1 ? 1 : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - .75f) * c4) + 1;
    }

    private float easeInOutBounce(float x)
    {
        return x < 0.5f ? (1 - easeOutBounce(1 - 2 * x)) / 2 : (1 + easeOutBounce(2 * x - 1)) / 2;
    }

    private float easeOutBounce(float x)
    {
        var n1 = 7.5625f;
        var d1 = 2.75f;

        if (x < 1 / d1)
        {
            return n1 * x * x;
        } else if (x < 2 / d1)
        {
            return n1 * (x -= 1.5f / d1) * x + 0.75f;
        } else if (x < 2.5f / d1)
        {
            return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        }
        else
        {
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }
    }
}
