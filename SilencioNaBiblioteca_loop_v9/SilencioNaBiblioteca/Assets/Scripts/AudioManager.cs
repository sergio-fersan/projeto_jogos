using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SilencioBiblioteca
{
    // Música (blues no início, jazz durante o jogo, vitória no final) e alarme. Arquivos em Resources/Audio.
    public class AudioManager : MonoBehaviour
    {
        AudioSource _music, _sfx;
        string _cur = "";
        float _musicBase = 0.5f, _sfxBase = 0.7f;   // volume pedido pela história (0..1), antes das opções
        Coroutine _co, _coSfx;
        readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();

        // volume final = volume pedido * opção do jogador, sempre dentro de 0..1
        float MusicVol { get { return Mathf.Clamp01(_musicBase * Settings.Music); } }
        float SfxVol { get { return Mathf.Clamp01(_sfxBase * Settings.Sfx); } }

        public void Init()
        {
            EnsureAudioListener();

            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false; _music.spatialBlend = 0f; _music.volume = MusicVol;
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false; _sfx.spatialBlend = 0f; _sfx.volume = SfxVol;

            // garante que o volume geral salvo já vale para o listener recém-criado
            AudioListener.volume = Mathf.Clamp01(Settings.Master);
            Settings.Changed += Refresh;
        }

        void OnDestroy()
        {
            Settings.Changed -= Refresh;
            if (_co != null) StopCoroutine(_co);
            if (_coSfx != null) StopCoroutine(_coSfx);
        }

        // Sem um AudioListener na cena o Unity não emite som nenhum (nem música, nem alarme).
        static void EnsureAudioListener()
        {
            if (FindAnyObjectByType<AudioListener>() != null) return;
            var cam = Camera.main;
            if (cam != null) cam.gameObject.AddComponent<AudioListener>();
            else new GameObject("AudioListener").AddComponent<AudioListener>();
        }

        // aplica as opções de volume ao que já está tocando (quando não há fade em andamento,
        // porque durante o fade o próprio fade já relê as opções a cada frame)
        void Refresh()
        {
            if (_music != null && _co == null) _music.volume = MusicVol;
            if (_sfx != null && _coSfx == null) _sfx.volume = SfxVol;
        }

        AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            AudioClip c;
            if (_cache.TryGetValue(name, out c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + name);
            if (c == null) Debug.LogWarning("[Audio] Não encontrado: Resources/Audio/" + name);
            _cache[name] = c;
            return c;
        }

        public void PlayMusic(string name, float vol, bool loop, float fade)
        {
            if (_music == null) return;
            _musicBase = Mathf.Clamp01(vol);

            // mesma faixa já tocando: só ajusta o volume, sem reiniciar a música
            if (_cur == name && _music.isPlaying)
            {
                if (_co == null) _music.volume = MusicVol;
                return;
            }

            _cur = name;
            if (_co != null) { StopCoroutine(_co); _co = null; }
            _co = StartCoroutine(SwapRoutine(name, loop, fade));
        }

        IEnumerator SwapRoutine(string name, bool loop, float fade)
        {
            float half = Mathf.Max(0.01f, fade * 0.5f);
            if (_music.isPlaying)
            {
                float v = _music.volume, t = 0f;
                while (t < half) { t += Time.unscaledDeltaTime; _music.volume = Mathf.Lerp(v, 0f, t / half); yield return null; }
                _music.Stop();
            }
            var c = Clip(name);
            if (c == null) { _co = null; _cur = ""; yield break; }

            _music.clip = c; _music.loop = loop; _music.volume = 0f; _music.Play();
            float t2 = 0f;
            while (t2 < half) { t2 += Time.unscaledDeltaTime; _music.volume = Mathf.Lerp(0f, MusicVol, t2 / half); yield return null; }
            _music.volume = MusicVol;
            _co = null;
        }

        public void StopMusic(float fade)
        {
            _cur = "";
            if (_co != null) { StopCoroutine(_co); _co = null; }
            if (_music == null) return;
            if (!_music.isPlaying || fade <= 0.01f)
            {
                if (_music.isPlaying) _music.Stop();
                _music.volume = MusicVol;
                return;
            }
            _co = StartCoroutine(FadeOut(_music, fade, false));
        }

        public void PlayAlarm(float vol)
        {
            if (_sfx == null) return;
            var c = Clip("alarme");
            if (c == null) return;
            if (_coSfx != null) { StopCoroutine(_coSfx); _coSfx = null; }
            _sfxBase = Mathf.Clamp01(vol);
            _sfx.clip = c; _sfx.loop = true; _sfx.volume = SfxVol; _sfx.Play();
        }

        public void StopAlarm(float fade)
        {
            if (_sfx == null) return;
            if (_coSfx != null) { StopCoroutine(_coSfx); _coSfx = null; }
            if (!_sfx.isPlaying) return;
            if (fade <= 0.01f)
            {
                _sfx.Stop();
                _sfx.volume = SfxVol;
                return;
            }
            _coSfx = StartCoroutine(FadeOut(_sfx, fade, true));
        }

        IEnumerator FadeOut(AudioSource s, float fade, bool sfx)
        {
            float v = s.volume, t = 0f, d = Mathf.Max(0.01f, fade);
            while (t < d) { t += Time.unscaledDeltaTime; s.volume = Mathf.Lerp(v, 0f, t / d); yield return null; }
            s.Stop();
            // libera o handle para que as opções de volume voltem a ser aplicadas
            if (sfx) { s.volume = SfxVol; _coSfx = null; }
            else { s.volume = MusicVol; _co = null; }
        }
    }
}
