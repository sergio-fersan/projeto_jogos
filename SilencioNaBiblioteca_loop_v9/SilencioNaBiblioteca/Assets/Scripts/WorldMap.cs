using UnityEngine;

namespace SilencioBiblioteca
{
    // Mapa da biblioteca. Coordenadas "px" são as da arte BgBiblioteca (referência 1456x819).
    public static class WorldMap
    {
        public const float W = 16f;
        public const float H = 9.0048f; // 16 * 941 / 1672
        const float RefW = 1456f, RefH = 819f;

        public static Vector2 Px(float x, float y)
        {
            return new Vector2((x / RefW - 0.5f) * W, (0.5f - y / RefH) * H);
        }

        // limites do piso
        static readonly float[] Bounds = { 56f, 218f, 1402f, 706f };

        // móveis: x0, y0, x1, y1
        static readonly float[][] Obstacles =
        {
            new float[] { 548, 195, 925, 292 },   // balcão
            new float[] { 40, 170, 112, 248 },    // carrinho esquerdo
            new float[] { 1062, 165, 1165, 268 }, // carrinho direito
            new float[] { 1358, 252, 1412, 348 }, // estante pequena (direita)
            new float[] { 303, 318, 553, 486 },   // estante central esquerda
            new float[] { 903, 318, 1153, 492 },  // estante central direita
            new float[] { 42, 342, 236, 545 },    // mesa esquerda
            new float[] { 1210, 342, 1412, 550 }, // mesa direita
            new float[] { 322, 528, 622, 698 },   // estante inferior esquerda
            new float[] { 832, 528, 1122, 698 },  // estante inferior direita
            new float[] { 1108, 612, 1182, 716 }, // carrinho inferior
            new float[] { 42, 572, 292, 700 },    // computadores
            new float[] { 1212, 655, 1412, 700 }, // caixas
            new float[] { 1345, 105, 1410, 245 }, // planta canto
        };

        const float HalfX = 0.11f, HalfY = 0.05f; // "pé" do personagem (pequeno para andar com folga)
        const float Inset = 14f;                  // encolhe cada móvel (px) — a arte tem sombras/bordas

        public static bool Blocked(Vector2 foot)
        {
            Vector2 bmin = Px(Bounds[0], Bounds[3]);
            Vector2 bmax = Px(Bounds[2], Bounds[1]);
            if (foot.x - HalfX < bmin.x || foot.x + HalfX > bmax.x || foot.y - HalfY < bmin.y || foot.y + HalfY > bmax.y)
                return true;
            for (int i = 0; i < Obstacles.Length; i++)
            {
                var o = Obstacles[i];
                Vector2 omin = Px(o[0] + Inset, o[3] - Inset);
                Vector2 omax = Px(o[2] - Inset, o[1] + Inset);
                if (foot.x + HalfX > omin.x && foot.x - HalfX < omax.x && foot.y + HalfY > omin.y && foot.y - HalfY < omax.y)
                    return true;
            }
            return false;
        }

        // se o ponto estiver bloqueado, procura o ponto livre mais próximo
        public static Vector2 Free(Vector2 p)
        {
            if (!Blocked(p)) return p;
            for (float r = 0.1f; r < 6f; r += 0.1f)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI * 2f / 16f;
                    var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                    if (!Blocked(q)) return q;
                }
            return p;
        }
    }
}
