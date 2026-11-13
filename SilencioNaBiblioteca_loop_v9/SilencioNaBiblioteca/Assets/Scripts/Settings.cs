using System;
using UnityEngine;

namespace SilencioBiblioteca
{
    // Opções do jogador (salvas em PlayerPrefs).
    public static class Settings
    {
        public static float Master = 0.8f, Music = 0.8f, Sfx = 0.8f, TextSpeed = 1f;
        public static bool Shake = true, Hints = true, Fullscreen = true;
        public static event Action Changed;
        static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            Master = PlayerPrefs.GetFloat("sb_master", 0.8f);
            Music = PlayerPrefs.GetFloat("sb_music", 0.8f);
            Sfx = PlayerPrefs.GetFloat("sb_sfx", 0.8f);
            TextSpeed = PlayerPrefs.GetFloat("sb_textspeed", 1f);
            Shake = PlayerPrefs.GetInt("sb_shake", 1) == 1;
            Hints = PlayerPrefs.GetInt("sb_hints", 1) == 1;
            Fullscreen = PlayerPrefs.GetInt("sb_fs", Screen.fullScreen ? 1 : 0) == 1;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("sb_master", Master);
            PlayerPrefs.SetFloat("sb_music", Music);
            PlayerPrefs.SetFloat("sb_sfx", Sfx);
            PlayerPrefs.SetFloat("sb_textspeed", TextSpeed);
            PlayerPrefs.SetInt("sb_shake", Shake ? 1 : 0);
            PlayerPrefs.SetInt("sb_hints", Hints ? 1 : 0);
            PlayerPrefs.SetInt("sb_fs", Fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void Apply()
        {
            AudioListener.volume = Mathf.Clamp01(Master);
            if (Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
            var h = Changed; if (h != null) h();
        }

        public static void Reset()
        {
            Master = 0.8f; Music = 0.8f; Sfx = 0.8f; TextSpeed = 1f;
            Shake = true; Hints = true; Fullscreen = true;
            Save(); Apply();
        }
    }
}
