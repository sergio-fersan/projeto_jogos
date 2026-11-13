using UnityEngine;

namespace SilencioBiblioteca
{
    // Personagem top-down: 7 frames (0 frente, 1-3 lado direito, 4-6 costas). Lado esquerdo = flipX.
    public class Actor : MonoBehaviour
    {
        public string id;
        SpriteRenderer _sr;
        readonly Sprite[] _f = new Sprite[7];
        Vector2 _facing = Vector2.down;
        float _animT;
        static readonly int[] SideWalk = { 2, 1, 3, 1 };
        static readonly int[] UpWalk = { 5, 6 };

        public Vector2 Pos { get { return transform.position; } }

        public void Init(string charId, string goName)
        {
            id = charId;
            gameObject.name = goName;
            var child = new GameObject("Sprite");
            child.transform.SetParent(transform, false);
            _sr = child.AddComponent<SpriteRenderer>();
            for (int i = 0; i < 7; i++) _f[i] = CharArt.Body(charId, i);
            _sr.sprite = _f[0];
            Sort();
        }

        public void SetPos(Vector2 p)
        {
            transform.position = new Vector3(p.x, p.y, 0f);
            Sort();
        }

        void Sort() { if (_sr != null) _sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f); }

        // vel em unidades/s (zero = parado)
        public void Animate(Vector2 vel, float dt)
        {
            if (_sr == null) return;
            bool moving = vel.sqrMagnitude > 0.04f;
            if (moving) _facing = Vector2.Lerp(_facing, vel.normalized, 0.5f).normalized;
            bool side = Mathf.Abs(_facing.x) >= Mathf.Abs(_facing.y);
            bool up = !side && _facing.y > 0f;
            _sr.flipX = side && _facing.x < 0f;
            if (moving) _animT += dt * 7f; else _animT = 0f;
            int step = Mathf.FloorToInt(_animT);
            Sprite s;
            if (side) s = _f[moving ? SideWalk[step % SideWalk.Length] : 1];
            else if (up) s = _f[moving ? UpWalk[step % UpWalk.Length] : 5];
            else s = _f[0];
            if (s != null) _sr.sprite = s;
            float bob = (moving && !side) ? Mathf.Abs(Mathf.Sin(_animT * Mathf.PI)) * 0.05f : 0f;
            _sr.transform.localPosition = new Vector3(0f, bob, 0f);
        }
    }
}
