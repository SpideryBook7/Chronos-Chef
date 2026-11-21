using System.Collections.Generic;
using UnityEngine;
namespace ChronosChef
{
    // A score and percussion stem share a DSP start so the mix can change mid-bar.
    public sealed class KitchenAudio : MonoBehaviour
    {
        AudioSource music, effects, percussion;
        float tension;
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
            music.clip = CreateMusic(); music.volume = MusicGain * .22f;
            percussion = gameObject.AddComponent<AudioSource>();
            percussion.loop = true; percussion.playOnAwake = false; percussion.spatialBlend = 0;
            percussion.clip = CreatePercussion(); percussion.volume = 0;
            double downbeat = AudioSettings.dspTime + .15;
            music.PlayScheduled(downbeat); percussion.PlayScheduled(downbeat);
            effects.volume = EffectsGain; game.Feedback += Play;
        }
        public void SetMusic(float value) { MusicGain = value; ApplyMix(); PlayerPrefs.SetFloat("ChronosChef.music", value); }
        public void SetEffects(float value) { EffectsGain = value; effects.volume = value; PlayerPrefs.SetFloat("ChronosChef.sfx", value); }
        public void Play(string key) { if (clips.TryGetValue(key, out var clip)) effects.PlayOneShot(clip, key == "click" ? .25f : .55f); }
        void Update()
        {
            if (!music || !owner) return;
            float target = owner.Running && !owner.Paused ? Mathf.Clamp01(owner.Anomaly / 100 + (owner.Level > 5 ? .28f : 0)) : 0;
            if (owner.Running && !owner.Paused && (owner.CloneRemaining > 0 || owner.Remaining < 20)) target = 1;
            if (owner.BorrowRemaining > 0) target = 0;
            tension = Mathf.MoveTowards(tension, target, Time.unscaledDeltaTime * .8f);
            ApplyMix();
        }
        void ApplyMix()
        {
            float gain = MusicGain * (owner.Paused ? .35f : 1);
            music.volume = gain * .22f;
            percussion.volume = gain * tension * .35f;
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
        static AudioClip CreateMusic()
        {
            const int rate = 22050; const float duration = 16;
            var data = new float[(int)(rate * duration)]; var random = new System.Random(417);
            float[] melody = { 523.25f, 0, 659.25f, 783.99f, 659.25f, 587.33f, 523.25f, 392, 440, 523.25f, 659.25f, 523.25f, 587.33f, 659.25f, 783.99f, 0 };
            float[] roots = { 130.81f, 110, 87.31f, 98 };
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, eighth = t % .25f, beat = t % .5f;
                int note = (int)(t * 4) % melody.Length; float frequency = melody[note];
                float lead = frequency == 0 ? 0 : Mathf.Sin(2 * Mathf.PI * frequency * eighth) * Mathf.Exp(-eighth * 13) * .22f;
                float bass = Mathf.Sin(2 * Mathf.PI * roots[(int)(t / 4) % 4] * beat) * Mathf.Exp(-beat * 5) * .20f;
                float kick = Mathf.Sin(2 * Mathf.PI * (55 * beat + 25 * (1 - Mathf.Exp(-beat * 25)) / 25)) * Mathf.Exp(-beat * 25) * .20f;
                float hat = (float)(random.NextDouble() * 2 - 1) * Mathf.Exp(-eighth * 90) * .035f;
                data[i] = (lead + bass + kick + hat) * Mathf.Min(1, t / .01f) * Mathf.Min(1, (duration - t) / .01f);
            }
            var clip = AudioClip.Create("Chronos_TimeKitchen_Loop", data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        static AudioClip CreatePercussion()
        {
            const int rate = 22050;
            var samples = new float[rate * 4];
            var noise = new System.Random(73);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                int eighth = (int)(t * 4);
                float phase = t - eighth * .25f;
                float tap = eighth % 3 == 0 ? .23f : .09f;
                float pan = Mathf.Sin(phase * 2 * Mathf.PI * 683) + .4f * Mathf.Sin(phase * 2 * Mathf.PI * 1137);
                samples[i] = tap * pan * Mathf.Exp(-phase * 52);
                if (eighth % 4 == 2) samples[i] += .21f * (float)(noise.NextDouble() * 2 - 1) * Mathf.Exp(-phase * 33);
                samples[i] *= Mathf.Min(1, phase * 750);
            }
            var clip = AudioClip.Create("Chronos_Pots_and_Plasma", samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
        void OnDestroy() { foreach (var clip in clips.Values) if (clip) Destroy(clip); if (music && music.clip) Destroy(music.clip); if (percussion && percussion.clip) Destroy(percussion.clip); }
    }
}
