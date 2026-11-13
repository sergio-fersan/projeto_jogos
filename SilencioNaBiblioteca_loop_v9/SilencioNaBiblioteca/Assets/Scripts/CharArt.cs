using System.Collections.Generic;
using UnityEngine;

namespace SilencioBiblioteca
{
    // Carrega os frames dos personagens (Resources/Art/Chars) e cria Sprites em runtime.
    // Independe das configurações de importação: sempre usa Point e pivô nos pés.
    public static class CharArt
    {
        public const float Ppu = 130f; // quadro de 168px ~ 1,3 unidades de altura
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name, bool portrait)
        {
            Sprite s;
            if (_cache.TryGetValue(name, out s)) return s;
            var tex = Resources.Load<Texture2D>("Art/Chars/" + name);
            if (tex == null)
            {
                Debug.LogWarning("[CharArt] Sprite não encontrado: Art/Chars/" + name);
                _cache[name] = null;
                return null;
            }
            tex.filterMode = FilterMode.Point;
            var rect = new Rect(0, 0, tex.width, tex.height);
            s = portrait
                ? Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect)
                : Sprite.Create(tex, rect, new Vector2(0.5f, 0f), Ppu, 0, SpriteMeshType.FullRect);
            _cache[name] = s;
            return s;
        }

        public static Sprite Body(string id, int frame) { return Get(id + "_" + frame, false); }
    }
}
