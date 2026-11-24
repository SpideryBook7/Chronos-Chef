using System.Collections.Generic;
using UnityEngine;
namespace ChronosChef
{
    public sealed class KitchenAudio : MonoBehaviour
    {
        AudioSource music, effects, shiftMusic;
        float shiftBlend;
        KitchenGame owner;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public float MusicGain { get; private set; }
        public float EffectsGain { get; private set; }
        public void Initialize(KitchenGame game)
        {
            owner = game;
            MusicGain = PlayerPrefs.GetFloat("ChronosChef.music", .35f); EffectsGain = PlayerPrefs.GetFloat("ChronosChef.sfx", .65f);
            if (!Object.FindFirstObjectByType<AudioListener>()) Camera.main.gameObject.AddComponent<AudioListener>();
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.playOnAwake = false; music.spatialBlend = 0;
            effects = gameObject.AddComponent<AudioSource>(); effects.playOnAwake = false; effects.spatialBlend = 0;
            foreach (var key in new[] { "click", "good", "cut", "cook", "ready", "serve", "bad", "warn", "paradox", "win", "borrow" }) clips[key] = Synthesize(key);
            music.clip = Resources.Load<AudioClip>("MenuScore");
            shiftMusic = gameObject.AddComponent<AudioSource>();
            shiftMusic.loop = true; shiftMusic.playOnAwake = false; shiftMusic.spatialBlend = 0;
            shiftMusic.clip = Resources.Load<AudioClip>("ShiftScore");
            ApplyMix();
            music.Play();
            effects.volume = EffectsGain; game.Feedback += Play;
        }
        public void SetMusic(float value) { MusicGain = value; ApplyMix(); PlayerPrefs.SetFloat("ChronosChef.music", value); }
        public void SetEffects(float value) { EffectsGain = value; effects.volume = value; PlayerPrefs.SetFloat("ChronosChef.sfx", value); }
        public void Play(string key) { if (clips.TryGetValue(key, out var clip)) effects.PlayOneShot(clip, key == "click" ? .25f : .55f); }
        void Update()
        {
            if (!music || !owner) return;
            var incoming=owner.Running ? shiftMusic : music;
            if(!incoming.isPlaying) incoming.Play();
            shiftBlend=Mathf.MoveTowards(shiftBlend,owner.Running?1:0,Time.unscaledDeltaTime*1.5f);
            if(shiftBlend==1 && music.isPlaying) music.Stop();
            if(shiftBlend==0 && shiftMusic.isPlaying) shiftMusic.Stop();
            ApplyMix();
        }
        void ApplyMix()
        {
            float gain = MusicGain * (owner.Paused ? .35f : 1);
            music.volume = gain * .55f * (1-shiftBlend);
            shiftMusic.volume = gain * .55f * shiftBlend;
        }
        static AudioClip Synthesize(string key)
        {
            const int rate = 22050;
            float seconds = key == "borrow" ? .85f : key == "win" || key == "paradox" ? .6f : key == "serve" || key == "cook" || key == "cut" ? .36f : .12f;
            var data = new float[(int)(rate * seconds)];
            float phase = 0;
            float[] triad = { 523.25f, 659.25f, 783.99f };
            var noise = new System.Random(321);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, p = t / seconds;
                float frequency = key == "bad" || key == "warn" ? Mathf.Lerp(330, 130, p) : key == "paradox" ? 150 + Mathf.Sin(t * 80) * 90 : 660 + p * 440;
                if (key == "serve" || key == "win") frequency = triad[Mathf.Min(2, (int)(p * 3))];
                if (key == "borrow") frequency = Mathf.Lerp(1220, 145, p) + 35 * Mathf.Sin(t * 38);
                if (key == "cut") frequency = 900 - 710 * Mathf.Repeat(p * 3, 1);
                if (key == "cook") frequency = 82 + 38 * Mathf.Sin(t * 24);
                phase += frequency * 2 * Mathf.PI / rate;
                float envelope = Mathf.Pow(1 - p, 2);
                envelope *= Mathf.Min(1, t / .005f) * Mathf.Min(1, (seconds - t) / .005f);
                data[i] = (Mathf.Sin(phase) + .2f * Mathf.Sin(phase * 2)) * envelope * .35f;
                if (key == "cook") data[i] += (float)(noise.NextDouble() * 2 - 1) * .3f * envelope;
                if (key == "cut") data[i] *= Mathf.Exp(-7 * Mathf.Repeat(p * 3, 1));
            }
            var clip = AudioClip.Create("Chronos_" + key, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        void OnDestroy()
        {
            if(owner) owner.Feedback -= Play;
            foreach(var clip in clips.Values) if(clip) Destroy(clip);
        }
    }
}
