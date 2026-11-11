using System.Collections.Generic;
using UnityEngine;

namespace InventoryShop
{
    public sealed class GameAudio : MonoBehaviour
    {
        private static GameAudio current;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource music, effects;
        private const string VolumeKey = "InventoryShop.Volume";

        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 0.65f);
            set
            {
                PlayerPrefs.SetFloat(VolumeKey, value);
                AudioListener.volume = value;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (current != null) return;
            var node = new GameObject("InventoryShop Audio", typeof(AudioListener));
            DontDestroyOnLoad(node);
            current = node.AddComponent<GameAudio>();
            current.effects = node.AddComponent<AudioSource>();
            current.music = node.AddComponent<AudioSource>();
            AudioListener.volume = Volume;
            current.music.clip = Resources.Load<AudioClip>("ShopAudio/Music");
            current.music.loop = true;
            current.music.volume = 0.48f;
            if (current.music.clip != null) current.music.Play();
        }

        public static void Play(string name)
        {
            if (current == null) return;
            if (!current.clips.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("ShopAudio/" + name);
                current.clips.Add(name, clip);
            }
            if (clip != null) current.effects.PlayOneShot(clip);
        }
    }
}
