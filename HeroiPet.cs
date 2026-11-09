// HeroiPet - um herozinho pixel-art que vive na sua area de trabalho,
// anda sozinho, enfrenta inimigos, coleta rupias e cura com coracoes.
// Compilar: build.bat   (usa o csc.exe que ja vem no Windows)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HeroiPet
{
    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            bool created;
            using (Mutex m = new Mutex(true, "HeroiPetDesktopHero", out created))
            {
                if (!created) return;
                try { SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles();
                Application.Run(new PetForm());
            }
        }
    }

    class Sprite { public Bitmap R, L; public int W, H; }

    // Janelas auxiliares (aviso, morte, creditos) ficam no fundo, junto com o heroi: nunca por cima de outros aplicativos
    static class ZOrder
    {
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        public static void Bottom(IntPtr h) { SetWindowPos(h, new IntPtr(1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0200); }     // HWND_BOTTOM, NOMOVE|NOSIZE|NOACTIVATE|NOOWNERZORDER
    }

    static class Art
    {
        static Dictionary<char, Color> Pal = new Dictionary<char, Color> {
            {'K', Color.FromArgb(28, 22, 36)},
            {'G', Color.FromArgb(58, 165, 62)},
            {'g', Color.FromArgb(30, 110, 45)},
            {'Y', Color.FromArgb(240, 200, 80)},
            {'S', Color.FromArgb(250, 208, 165)},
            {'V', Color.FromArgb(222, 158, 20)},
            {'B', Color.FromArgb(135, 85, 45)},
            {'W', Color.FromArgb(245, 245, 245)},
            {'R', Color.FromArgb(215, 55, 55)},
            {'P', Color.FromArgb(150, 80, 205)},
            {'E', Color.FromArgb(255, 235, 70)},
            {'U', Color.FromArgb(70, 125, 235)},
            {'L', Color.FromArgb(170, 215, 255)},
            {'A', Color.FromArgb(165, 165, 175)},
            {'M', Color.FromArgb(235, 255, 235)},
            {'N', Color.FromArgb(120, 205, 85)},
            {'b', Color.FromArgb(95, 60, 30)},
            {'r', Color.FromArgb(150, 40, 45)},
            {'T', Color.FromArgb(228, 190, 125)},
            {'t', Color.FromArgb(196, 150, 90)},
            {'D', Color.FromArgb(45, 34, 48)},
            {'O', Color.FromArgb(250, 150, 40)},
            {'p', Color.FromArgb(96, 50, 140)},
            {'C', Color.FromArgb(255, 205, 40)},       // dourado (coroa, cinto do rei)
            {'F', Color.FromArgb(245, 245, 250)},      // pele branca do manto do rei
            {'J', Color.FromArgb(245, 245, 245)},      // pernas (branco; vira calca marrom com o bau)
            {'Z', Color.FromArgb(135, 85, 45)},        // sapatos (marrom; viram roxos com o bau)
            {'H', Color.FromArgb(58, 165, 62)},       // gorro do heroi (verde; vira roxo na evolucao 2)
            {'Q', Color.FromArgb(58, 165, 62)},       // roupa do heroi (verde; vira azul na evolucao 3)
            {'k', Color.FromArgb(226, 106, 92)},
            {'h', Color.FromArgb(240, 232, 190)}
        };

        // separa o verde do gorro (linhas 0-8) do verde da roupa (linhas 9+) para poder recolorir cada um na evolucao
        public static bool Crown, Robe;     // equipado: coroa de ouro / roupa de rei (com calca marrom e sapatos roxos)
        public static string[] HeroRecolor(string[] rows)
        {
            List<string> r = new List<string>();
            if (Crown) { r.Add("....C.CC.C...."); r.Add("....CCCCCC...."); r.Add("....CRCCRC...."); }      // coroa sobre o gorro
            for (int i = 0; i < rows.Length; i++)
            {
                string s = rows[i].Replace('Y', 'V').Replace('G', i < 9 ? (Crown ? 'V' : 'H') : 'Q');      // cabelo castanho ('V') para contrastar com o rosto; com a coroa: sem gorro (cabelo aparece) e a coroa fica sobre a cabeca
                if (Crown && (i == 4 || i == 5) && s.StartsWith("KV")) s = ".K" + s.Substring(2);      // sem gorro: tira a pontinha do canto esquerdo da cabeca
                if (Robe)
                {
                    if (i == 9) s = s.Replace('Q', 'F');          // gola de pele
                    else if (i == 13) s = s.Replace('B', 'C');    // cinto dourado
                    else if (i == 14) s = s.Replace('W', 'J');    // calca
                    else if (i == 15) s = s.Replace('B', 'Z');    // sapatos
                }
                r.Add(s);
            }
            return r.ToArray();
        }

        public static Dictionary<char, Color> Ov(char c, Color col)
        {
            Dictionary<char, Color> d = new Dictionary<char, Color>();
            d[c] = col;
            return d;
        }

        public static Sprite Make(string[] rows, int s, Dictionary<char, Color> over)
        {
            int w = 0;
            foreach (string r in rows) if (r.Length > w) w = r.Length;
            int h = rows.Length;
            Bitmap b = new Bitmap(w * s, h * s, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < rows[y].Length; x++)
                    {
                        char c = rows[y][x];
                        if (c == '.' || c == ' ') continue;
                        Color col;
                        if (over == null || !over.TryGetValue(c, out col))
                            if (!Pal.TryGetValue(c, out col)) continue;
                        using (SolidBrush br = new SolidBrush(col))
                            g.FillRectangle(br, x * s, y * s, s, s);
                    }
            }
            Bitmap l = (Bitmap)b.Clone();
            l.RotateFlip(RotateFlipType.RotateNoneFlipX);
            Sprite sp = new Sprite();
            sp.R = b; sp.L = l; sp.W = b.Width; sp.H = b.Height;
            return sp;
        }

        // ---------- Heroi (olhando para a direita) ----------
        public static string[] HeroA = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            "KGGGYYYYYYSSK.",
            "KGGYYYYYYYSSSK",
            ".KYYYYYYYSKSSK",
            "..KYYYYYYSSSK.",
            "..KYYYYYSSSK..",
            "..KYYKGGGGKK..",
            "..KGGGGGGGGGK.",
            "..KGGGGGGGGGK.",
            "..KGGGGGGGGSK.",
            "..KBBBBBBBBBK.",
            "...KWWK.KWWK..",
            "...KBBK.KBBK.."
        };
        public static string[] HeroB = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            "KGGGYYYYYYSSK.",
            "KGGYYYYYYYSSSK",
            ".KYYYYYYYSKSSK",
            "..KYYYYYYSSSK.",
            "..KYYYYYSSSK..",
            "..KYYKGGGGKK..",
            "..KGGGGGGGGGK.",
            "..KGGGGGGGGGK.",
            "..KGGGGGGGGSK.",
            "..KBBBBBBBBBK.",
            "....KWWKWWK...",
            "....KBBKBBK..."
        };

        // ---------- Heroi dormindo: o desenho de frente (olhos fechados, maos na barriga)
        // e girado 90 graus, entao fica de barriga para cima, com exatamente o mesmo tamanho de em pe
        public static string[] HeroFrontSleep = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            "KYYYYYYYYYYYYK",
            "KYYYSSSSSSYYYK",
            ".KYSKKSSKKSYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KGGGGSSGGGGK.",
            "..KBBBBBBBBK..",
            "..KWWWKKWWWK..",
            "..KBBBK.KBBBK."
        };

        // ---------- Macieira (gerada por codigo) ----------
        public static int[,] AppleSlots = { { 7, 5 }, { 13, 6 }, { 10, 10 }, { 4, 12 }, { 15, 12 } };
        static bool Disc(int x, int y, double cx, double cy, double r)
        { return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r; }
        static bool Canopy(int x, int y)
        { return Disc(x, y, 10, 8, 7.3) || Disc(x, y, 5, 11, 5.3) || Disc(x, y, 15, 11, 5.3); }
        public static string[] TreeRows()
        {
            int w = 20, h = 24;
            char[][] g = new char[h][];
            for (int y = 0; y < h; y++) { g[y] = new char[w]; for (int x = 0; x < w; x++) g[y][x] = '.'; }
            for (int y = 15; y <= 22; y++)                      // tronco
            {
                int fl = y >= 21 ? 1 : 0;
                g[y][7 - fl] = 'K'; g[y][12 + fl] = 'K';
                for (int x = 8 - fl; x <= 11 + fl; x++) g[y][x] = x >= 10 + fl ? 'b' : 'B';
            }
            for (int x = 6; x <= 13; x++) g[23][x] = 'K';
            for (int y = 0; y < h; y++)                         // copa
                for (int x = 0; x < w; x++)
                {
                    if (!Canopy(x, y)) continue;
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                        || !Canopy(x - 1, y) || !Canopy(x + 1, y) || !Canopy(x, y - 1) || !Canopy(x, y + 1);
                    char c = 'G';
                    if (x + y < 17) c = 'N'; else if (y >= 13 && x >= 9) c = 'g';
                    g[y][x] = edge ? 'K' : c;
                }
            string[] rows = new string[h];
            for (int y = 0; y < h; y++) rows[y] = new string(g[y]);
            return rows;
        }
                // ---------- Dragao (gerado por codigo; olha para a direita) ----------
        static void EllF(char[][] g, double cx, double cy, double rx, double ry, char c)
        {
            for (int y = 0; y < g.Length; y++)
                for (int x = 0; x < g[y].Length; x++)
                {
                    double dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1.0) g[y][x] = c;
                }
        }
        static void TriF(char[][] g, double ax, double ay, double bx, double by, double cx, double cy, char c)
        {
            for (int y = 0; y < g.Length; y++)
                for (int x = 0; x < g[y].Length; x++)
                {
                    double d1 = (x - bx) * (ay - by) - (ax - bx) * (y - by);
                    double d2 = (x - cx) * (by - cy) - (bx - cx) * (y - cy);
                    double d3 = (x - ax) * (cy - ay) - (cx - ax) * (y - ay);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos)) g[y][x] = c;
                }
        }
        public static string[] DragonRows(bool open)
        {
            int w = 48, h = 40, ox = 3, oy = 3;
            char[][] g = new char[h][];
            for (int y = 0; y < h; y++) { g[y] = new char[w]; for (int x = 0; x < w; x++) g[y][x] = '.'; }
            // asa (atras)
            TriF(g, 6 + ox, 18 + oy, 13 + ox, 1 + oy, 25 + ox, 17 + oy, 'p');
            TriF(g, 9 + ox, 16 + oy, 13.5 + ox, 5 + oy, 21 + ox, 15 + oy, 'P');
            // cauda
            EllF(g, 6 + ox, 27 + oy, 3.4, 2.8, 'P'); EllF(g, 3.5 + ox, 29.8 + oy, 2.5, 2.1, 'P'); EllF(g, 1.4 + ox, 31.5 + oy, 1.7, 1.5, 'P');
            TriF(g, -1 + ox, 29.5 + oy, 3 + ox, 31 + oy, 0 + ox, 34 + oy, 'W');
            // pernas
            EllF(g, 12 + ox, 31.5 + oy, 3.6, 2.8, 'p'); EllF(g, 23.5 + ox, 31.5 + oy, 3.8, 2.8, 'P');
            // corpo e barriga
            EllF(g, 17 + ox, 22 + oy, 11.8, 9.6, 'P');
            EllF(g, 20.5 + ox, 24.5 + oy, 6.6, 6.6, 'Y');
            // pescoco
            EllF(g, 25 + ox, 15.5 + oy, 4.6, 5.2, 'P'); EllF(g, 28.5 + ox, 11 + oy, 4.6, 4.6, 'P');
            // cabeca
            if (!open)
            {
                EllF(g, 31.5 + ox, 9 + oy, 6.6, 5.0, 'P'); EllF(g, 36.5 + ox, 11 + oy, 4.6, 3.4, 'P');
                EllF(g, 35.5 + ox, 13.4 + oy, 4.4, 1.2, 'p');
            }
            else
            {
                EllF(g, 31.5 + ox, 8 + oy, 6.6, 4.3, 'P'); EllF(g, 36.5 + ox, 9.2 + oy, 4.6, 2.5, 'P');
                EllF(g, 36.8 + ox, 13.2 + oy, 4.6, 2.5, 'D'); EllF(g, 38 + ox, 13.2 + oy, 2.4, 1.3, 'O');
                EllF(g, 35.8 + ox, 16.6 + oy, 4.6, 1.9, 'p');
                for (int tx = 34; tx <= 40; tx += 2) { g[11 + oy][tx + ox] = 'W'; g[15 + oy][tx + ox] = 'W'; }
            }
            // chifres, olho, narina
            EllF(g, 28.5 + ox, 3.6 + oy, 1.1, 1.6, 'W'); EllF(g, 27 + ox, 1.8 + oy, 0.9, 1.2, 'W');
            EllF(g, 33.5 + ox, 3.4 + oy, 1.1, 1.6, 'W'); EllF(g, 35 + ox, 1.6 + oy, 0.9, 1.2, 'W');
            EllF(g, 33.4 + ox, 7 + oy, 1.7, 1.7, 'E'); g[7 + oy][34 + ox] = 'K'; g[8 + oy][34 + ox] = 'K';
            g[10 + oy][40 + ox] = 'K';
            // espinhos nas costas
            EllF(g, 10 + ox, 14 + oy, 1.3, 1.3, 'W'); EllF(g, 14 + ox, 12.4 + oy, 1.3, 1.3, 'W'); EllF(g, 18 + ox, 12 + oy, 1.3, 1.3, 'W'); EllF(g, 22 + ox, 13 + oy, 1.3, 1.3, 'W');
            // garras
            EllF(g, 25.5 + ox, 34.4 + oy, 0.8, 0.8, 'W'); EllF(g, 27.5 + ox, 34.2 + oy, 0.8, 0.8, 'W'); EllF(g, 14 + ox, 34.2 + oy, 0.8, 0.8, 'W');
            // contorno externo
            char[][] o = new char[h][];
            for (int y = 0; y < h; y++) { o[y] = (char[])g[y].Clone(); }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (g[y][x] != '.') continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int yy = y + dy, xx = x + dx;
                            if (yy >= 0 && yy < h && xx >= 0 && xx < w && g[yy][xx] != '.') { near = true; break; }
                        }
                    if (near) o[y][x] = 'K';
                }
            string[] rows = new string[h];
            for (int y = 0; y < h; y++) rows[y] = new string(o[y]);
            return rows;
        }

        public static string[] FireballRows(int frame)
        {
            if (frame == 0) return new string[] {
                "...KK...",
                "..KOOK..",
                ".KOYYOK.",
                "KOYEEYOK",
                "KOYEEYOK",
                ".KOYYOK.",
                "..KOOK..",
                "...KK..." };
            return new string[] {
                "..K..K..",
                "..KOOK..",
                ".KOYYOK.",
                "KOYEEYOK",
                "KOYEEYOK",
                ".KOYYOK.",
                "..KOOK..",
                "..K..K.." };
        }

        public static string[] AppleBig = {       // maca do indicador do topo (mesma altura da rupia, contorno fino)
            "...B.GG.",
            ".KKKKKK.",
            "KRRRRRRK",
            "KRMRRRRK",
            "KRRRRRRK",
            "KRRRRRRK",
            ".KRRRRK.",
            "..KKKK.."
        };
        public static string[] AppleSm = {
            ".BG.",
            "KRRK",
            "KRRK",
            ".KK."
        };

        // ---------- Heroi de frente ----------
        public static string[] HeroFront = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            "KYYYYYYYYYYYYK",
            "KYYYSSSSSSYYYK",
            ".KYYSKSSKSYYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "..KGGGGGGGGK..",
            ".KSGGGGGGGGSK.",
            ".KSGGGGGGGGSK.",
            "..KBBBBBBBBK..",
            "..KWWWKKWWWK..",
            "..KBBBK.KBBBK."
        };
        // igual, mas sem a mao esquerda (o braco levantado e desenhado a parte)
        public static string[] HeroFrontWave = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            "KYYYYYYYYYYYYK",
            "KYYYSSSSSSYYYK",
            ".KYYSKSSKSYYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "..KGGGGGGGGK..",
            ".KSGGGGGGGGGK.",
            ".KSGGGGGGGGGK.",
            "..KBBBBBBBBK..",
            "..KWWWKKWWWK..",
            "..KBBBK.KBBBK."
        };

        // ---------- Inimigos ----------
        public static string[] OctoA = {
            "...KKKKKK...",
            "..KRRRRRRK..",
            ".KRRRRRRRRK.",
            "KRRWWRRWWRRK",
            "KRRWKRRWKRRK",
            "KRRRRRRRRRRK",
            "KRRRKKKKRRRK",
            ".KRRRRRRRRK.",
            "..KKRRRRKK..",
            ".KR.K..K.RK."
        };
        public static string[] OctoB = {
            "...KKKKKK...",
            "..KRRRRRRK..",
            ".KRRRRRRRRK.",
            "KRRWWRRWWRRK",
            "KRRWKRRWKRRK",
            "KRRRRRRRRRRK",
            "KRRRKKKKRRRK",
            ".KRRRRRRRRK.",
            "..KKRRRRKK..",
            "..KR.KK.RK.."
        };
        // Bokoblin: goblin de pele rosada, chifres e porrete (arte propria)
        public static string[] BokoA = {
            "..h.......h...",
            "..KKKKKKKKK...",
            ".KkkkkkkkkK...",
            ".KkWKkkWKkkK..",
            ".KkkkkkkkkK...",
            ".KkkKWKWKkkK..",
            "..KkkkkkkkK.BB",
            ".KkBBBBBBkK.bB",
            ".KkBBBBBBkK.bB",
            ".KkkBBBBkkK.bB",
            "..KKBBBBKK..bB",
            "...KkK..KkK...",
            "...KkK..KkK...",
            "..KKKK..KKKK.."
        };
        public static string[] BokoB = {
            "..h.......h...",
            "..KKKKKKKKK...",
            ".KkkkkkkkkK...",
            ".KkWKkkWKkkK..",
            ".KkkkkkkkkK...",
            ".KkkKWKWKkkK..",
            "..KkkkkkkkK.BB",
            ".KkBBBBBBkK.bB",
            ".KkBBBBBBkK.bB",
            ".KkkBBBBkkK.bB",
            "..KKBBBBKK..bB",
            "..KkK....KkK..",
            ".KkK......KkK.",
            ".KKK......KKK."
        };
        public static string[] BokoC = {      // porrete levantado (preparando o golpe)
            "..h.......hbB.",
            "..KKKKKKKKKbB.",
            ".KkkkkkkkkKbB.",
            ".KkWKkkWKkkKbB",
            ".KkkkkkkkkKbB.",
            ".KkkKWKWKkkKbB",
            "..KkkkkkkkK.bB",
            ".KkBBBBBBkK.bB",
            ".KkBBBBBBkK...",
            ".KkkBBBBkkK...",
            "..KKBBBBKK....",
            "...KkK..KkK...",
            "...KkK..KkK...",
            "..KKKK..KKKK.."
        };
        // Itens de evolucao (gorro e blusa) na mesma arte pixel do corpo do heroi: H/h/g = gorro (base/brilho/sombra), Q/h/g = blusa
        public static string[] CapItem = {
            "....KKKKKKKK....",
            "...KHHhhHHHHK...",
            "..KHHhHHHHHHgK..",
            ".KHHHHHHHHHHHgK.",
            "KHHHHHHHHHHHHHgK",
            "KHHHHHHHHHHHHggK",
            "KggggggggggggggK",
            ".KKKKKKKKKKKKKK."
        };
        public static string[] ShirtItem = {
            "...KKKKKKKKKK...",
            "..KQQQKKKKQQQK..",
            ".KQQQQQhhQQQQQK.",
            "KQQQQQQQQQQQQQQK",
            "KQQQQQQQQQQQQQQK",
            "KgQQQKQQQQKQQQgK",
            ".KKKKKQQQQKKKKK.",
            "....KQQQQQQK....",
            "....KQQQQQQK....",
            "....KgQQQQgK....",
            "....KBBBBBBK....",
            "....KKKKKKKK...."
        };
        public static string[] BatUp = {
            "KK..........KK",
            "KPK...KK...KPK",
            "KPPK.KPPPK.KPK",
            "KPPPKPEPEPKPPK",
            ".KPPPPPPPPPPK.",
            "..KKPPPPPPKK..",
            "....KKPPKK....",
            "......KK......"
        };
        public static string[] BatDown = {
            "......KK......",
            "....KKPPKK....",
            "..KKPPPPPPKK..",
            ".KPPPPEPEPPPPK",
            "KPK.KPPPPK.KPK",
            "KK...KKKK...KK",
            "..............",
            ".............."
        };
        public static string[] SlimeTall = {
            "....KKKK....",
            "..KKUUUUKK..",
            ".KULLUUUUUK.",
            ".KULUUUUUUK.",
            "KUUWKUUWKUUK",
            "KUUUUUUUUUUK",
            "KUUUUUUUUUUK",
            ".KKUUUUUUKK.",
            "...KKKKKK..."
        };
        public static string[] SlimeFlat = {
            "............",
            "............",
            "............",
            "...KKKKKK...",
            ".KKUULUUUKK.",
            "KUULUUUUUUUK",
            "KUWKUUUWKUUK",
            "KUUUUUUUUUUK",
            ".KKKKKKKKKK."
        };

        // ---------- Itens ----------
        public static string[] Heart = {
            ".KK.KK.",
            "KRRKRRK",
            "KRWRRRK",
            "KRRRRRK",
            ".KRRRK.",
            "..KRK..",
            "...K..."
        };
        public static string[] Rupee = {
            "..K..",
            ".KGK.",
            "KGMGK",
            "KGMGK",
            "KGGGK",
            "KGGGK",
            ".KGK.",
            "..K.."
        };
        public static string[] Rock = {
            ".KK.",
            "KAAK",
            "KAAK",
            ".KK."
        };
    }

    class EvoAnim { public int Stage; public float T; public bool Wait = true; }       // Wait: jogo pausado, item grande, esperando "USAR ITEM"

    // Botao "USAR ITEM" da evolucao: janela pequena que recebe clique (sem roubar o foco, fica atras dos aplicativos como o resto do jogo)
    class EvoForm : Form
    {
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x80; return cp; }     // NOACTIVATE | TOOLWINDOW
        }
        public EvoForm(float sc, Rectangle wa, float centerX, float topY, Action onUse)
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = false;
            int w = (int)(210 * sc), h = (int)(46 * sc);
            Bounds = new Rectangle(wa.X + (int)centerX - w / 2, wa.Y + (int)topY, w, h);
            BackColor = Color.FromArgb(28, 22, 36);
            Button b = new Button();
            b.Text = "USAR ITEM"; b.Dock = DockStyle.Fill; b.FlatStyle = FlatStyle.Flat; b.TabStop = false;
            b.FlatAppearance.BorderSize = 3; b.FlatAppearance.BorderColor = Color.FromArgb(28, 22, 36);
            b.BackColor = Color.FromArgb(245, 205, 70); b.ForeColor = Color.FromArgb(40, 28, 10);
            b.Font = new Font("Consolas", 15 * sc, FontStyle.Bold, GraphicsUnit.Pixel);
            b.Cursor = Cursors.Hand;
            b.Click += delegate { onUse(); };
            Controls.Add(b);
            Shown += delegate { ZOrder.Bottom(Handle); };
        }
    }

    class Enemy
    {
        public int Type;          // 0 octorok, 1 morcego, 2 slime
        public float X, Y, VX, VY, KX, KY, T, Timer, Flash;
        public int HP, MaxHP, Face = 1, Mode;
        public float TX, TY, RT; public bool Moving; public float Scale = 1f, SmokeT, GrowT, SparkT, ShockT = 8f;      // dragao: destino, tempo para trocar de destino, esta andando
    }
    class Rock { public float X, Y, VX, VY, Life; public bool Fire, Homing; }
    class Arrow { public float X, Y, VX, VY; public bool Power; }
    class Tree { public float X, Y; public bool[] Has = new bool[5]; public float[] Regrow = new float[5]; }
    class FlyApple { public float X0, Y0, T; }
    class CookApple { public float X0, Y0, X1, Y1, T, Dur; public int Idx, Dir; }
    class Fire { }
    class Shock { public float X, Y, T, R, R0; public bool Hit; }      // onda de choque do dragao
    class Smoke { public float X, Y, T, Sz; public float Life = 3.2f, Grow = 5f; }
    class Item { public int Kind; public float X, Y, T; public bool Reward; public float VX, VY; }   // 0 coracao, 1 verde, 2 azul, 3 vermelha
    class Puff { public int Kind; public float X, Y, T; }  // 0 morte, 1 faisca, 2 spawn

        // Aviso que aparece por cima de qualquer aplicativo, sem roubar o foco nem bloquear cliques
    class Toast : Form
    {
        string title, sub;
        float sc;
        int life, lifeMs;
        System.Windows.Forms.Timer tm;

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000 | 0x80 | 0x20;     // NOACTIVATE | TOOLWINDOW | TRANSPARENT (clique passa direto); NAO e TOPMOST: fica atras dos aplicativos
                return cp;
            }
        }

        public Toast(string title, string sub, float scale, int lifeMs = 4800, bool tall = false)
        {
            this.title = title; this.sub = sub; this.sc = Math.Max(1f, scale); this.lifeMs = lifeMs;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            Shown += delegate { ZOrder.Bottom(Handle); };
            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = (int)(440 * sc), h = (int)((tall ? 170 : 96) * sc);
            Bounds = new Rectangle(wa.X + (wa.Width - w) / 2, wa.Y + (int)(28 * sc), w, h);
            BackColor = Color.FromArgb(34, 30, 48);
            int r = (int)(22 * sc);
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, r, r, 180, 90); gp.AddArc(w - r, 0, r, r, 270, 90);
            gp.AddArc(w - r, h - r, r, r, 0, 90); gp.AddArc(0, h - r, r, r, 90, 90);
            gp.CloseAllFigures();
            Region = new Region(gp);
            Opacity = 0;
            tm = new System.Windows.Forms.Timer();
            tm.Interval = 30;
            tm.Tick += delegate
            {
                life += 30;
                double o = 0.97;
                if (life < 250) o = 0.97 * life / 250.0;
                else if (life > lifeMs - 500) o = 0.97 * Math.Max(0, 1 - (life - (lifeMs - 500)) / 500.0);
                Opacity = o;
                if (life >= lifeMs) { tm.Stop(); Close(); }
            };
            tm.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float h = Height;
            using (SolidBrush ac = new SolidBrush(Color.FromArgb(247, 168, 60)))
                g.FillRectangle(ac, 0, 0, 8 * sc, h);
            // maca
            float cx = 52 * sc, cy = h / 2 + 3 * sc, r = 20 * sc;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(226, 70, 74))) g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 255, 255, 255))) g.FillEllipse(b, cx - r * 0.6f, cy - r * 0.7f, r * 0.45f, r * 0.7f);
            using (Pen p = new Pen(Color.FromArgb(150, 98, 62), 3 * sc)) g.DrawLine(p, cx, cy - r + 2 * sc, cx + 2 * sc, cy - r - 8 * sc);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(94, 180, 100))) g.FillEllipse(b, cx + 2 * sc, cy - r - 12 * sc, 14 * sc, 8 * sc);
            using (Font f1 = new Font("Segoe UI", 21 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font f2 = new Font("Segoe UI", 15 * sc, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush w1 = new SolidBrush(Color.White))
            using (SolidBrush w2 = new SolidBrush(Color.FromArgb(200, 200, 215)))
            {
                g.DrawString(title, f1, w1, 90 * sc, 20 * sc);
                g.DrawString(sub, f2, w2, new RectangleF(91 * sc, 54 * sc, Width - 110 * sc, Height - 60 * sc));
            }
        }
    }

    // Camada das sombras: janela com transparencia real (alfa por pixel), logo abaixo da janela do heroi.
    // A janela do heroi usa cor-chave (so liga/desliga), entao as sombras semitransparentes ficam nesta camada.
    class ShadowLayer : Form
    {
        [StructLayout(LayoutKind.Sequential)] struct BLENDFUNCTION { public byte Op, Flags, Alpha, Format; }
        [StructLayout(LayoutKind.Sequential)] struct SIZE { public int cx, cy; }
        [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
        [StructLayout(LayoutKind.Sequential)] struct BMIH { public int Size, Width, Height; public short Planes, BitCount; public int Compression, SizeImage, XPels, YPels, ClrUsed, ClrImportant; }
        [DllImport("user32.dll")] static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, IntPtr pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int flags);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc, ref BMIH bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);

        int bw, bh; IntPtr memDC, dib, oldObj, bits; Bitmap bmp; Graphics gfx; SolidBrush brush = new SolidBrush(Color.FromArgb(125, 20, 16, 34));

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x80000 | 0x20 | 0x80 | 0x08000000; return cp; }     // LAYERED | TRANSPARENT | TOOLWINDOW | NOACTIVATE
        }
        public ShadowLayer(Rectangle area)
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; Bounds = area; TopMost = false;
            bw = area.Width; bh = area.Height;
            IntPtr sdc = GetDC(IntPtr.Zero);
            memDC = CreateCompatibleDC(sdc);
            BMIH h = new BMIH(); h.Size = Marshal.SizeOf(typeof(BMIH)); h.Width = bw; h.Height = -bh; h.Planes = 1; h.BitCount = 32;
            dib = CreateDIBSection(sdc, ref h, 0, out bits, IntPtr.Zero, 0);
            ReleaseDC(IntPtr.Zero, sdc);
            oldObj = SelectObject(memDC, dib);
            bmp = new Bitmap(bw, bh, bw * 4, PixelFormat.Format32bppPArgb, bits);
            gfx = Graphics.FromImage(bmp); gfx.SmoothingMode = SmoothingMode.None; gfx.PixelOffsetMode = PixelOffsetMode.Half;
        }
        public void Render(List<float[]> list)
        {
            if (!IsHandleCreated || gfx == null) return;
            gfx.Clear(Color.Transparent);
            foreach (float[] r in list) gfx.FillEllipse(brush, r[0], r[1], r[2], r[3]);
            SIZE sz = new SIZE(); sz.cx = bw; sz.cy = bh;
            POINT src = new POINT();
            BLENDFUNCTION bf = new BLENDFUNCTION(); bf.Op = 0; bf.Flags = 0; bf.Alpha = 255; bf.Format = 1;      // AC_SRC_OVER, AC_SRC_ALPHA
            UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, ref sz, memDC, ref src, 0, ref bf, 2);        // ULW_ALPHA
        }
        protected override void Dispose(bool disposing)
        {
            if (gfx != null) { gfx.Dispose(); gfx = null; }
            if (bmp != null) { bmp.Dispose(); bmp = null; }
            if (memDC != IntPtr.Zero) { SelectObject(memDC, oldObj); DeleteObject(dib); DeleteDC(memDC); memDC = IntPtr.Zero; }
            base.Dispose(disposing);
        }
    }

        // Janela invisivel que fica sobre a area de trabalho (e sob os aplicativos) no modo jogo:
    // engole os cliques, entao a area de trabalho nao seleciona icones nem desenha o retangulo de selecao.
    class ClickShield : Form
    {
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; }     // TOOLWINDOW: fora do Alt+Tab
        }
        public ClickShield()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen.WorkingArea;
            BackColor = Color.Black;
            Opacity = 0.01;                                   // praticamente invisivel, mas recebe o mouse
            TopMost = false;
            Activated += delegate { PushBottom(); };          // ao receber o foco, volta para baixo dos aplicativos
        }
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
        // liga/desliga o "clique atravessa": enquanto Ctrl esta apertado o mouse volta a funcionar na area de trabalho
        public void PassThrough(bool on)
        {
            if (!IsHandleCreated) return;
            int ex = GetWindowLong(Handle, -20);
            bool has = (ex & 0x20) != 0;
            if (on != has) SetWindowLong(Handle, -20, on ? (ex | 0x20) : (ex & ~0x20));
        }
        public void PushTop()
        {
            if (IsHandleCreated) SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0200);
        }
        public void PushBottom()
        {
            if (IsHandleCreated) SetWindowPos(Handle, new IntPtr(1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0200);
        }
    }
        // "FIM DE JOGO" + creditos, num painel por cima de tudo (sem roubar o foco, cliques passam direto)
    class EndShow : Form
    {
        // Edite aqui os creditos. Linhas que comecam com # sao titulos.
        static readonly string[] Lines = {
            "#CRÉDITOS", "",
            "#Ideia", "Alexandre", "",
            "#Criação", "Bia Krepel", "",
            "#Programação", "Claude", "",
            "#Herói", "Link, o aventureiro da área de trabalho", "",
            "#Inimigos", "Octoroks, morcegos, slimes e bokoblins", "",
            "#Chefão", "O Dragão da Fogueira", "",
            "#Cenário", "Macieiras, fogueira e a sua área de trabalho", "",
            "", "Obrigado por jogar!" };
        const int TitleMs = 4500, CreditsMs = 26000;

        float sc;
        int ms;
        public bool Done;
        System.Windows.Forms.Timer tm;

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x80 | 0x20; return cp; }      // sem TOPMOST: atras dos aplicativos
        }

        public EndShow(float scale)
        {
            sc = Math.Max(1f, scale);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            Shown += delegate { ZOrder.Bottom(Handle); };
            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = (int)(560 * sc), h = Math.Min((int)(560 * sc), wa.Height - 40);
            Bounds = new Rectangle(wa.X + (wa.Width - w) / 2, wa.Y + (wa.Height - h) / 2, w, h);
            BackColor = Color.FromArgb(18, 16, 30);
            int r = (int)(28 * sc);
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, r, r, 180, 90); gp.AddArc(w - r, 0, r, r, 270, 90);
            gp.AddArc(w - r, h - r, r, r, 0, 90); gp.AddArc(0, h - r, r, r, 90, 90);
            gp.CloseAllFigures();
            Region = new Region(gp);
            Opacity = 0;
            tm = new System.Windows.Forms.Timer();
            tm.Interval = 30;
            tm.Tick += delegate
            {
                ms += 30;
                int total = TitleMs + CreditsMs;
                double o = 0.96;
                if (ms < 700) o = 0.96 * ms / 700.0;
                else if (ms > total - 900) o = 0.96 * Math.Max(0, (total - ms) / 900.0);
                Opacity = o;
                Invalidate();
                if (ms >= total) { Done = true; tm.Stop(); Close(); }
            };
            tm.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float w = Width, h = Height;
            using (Pen border = new Pen(Color.FromArgb(247, 168, 60), 3 * sc))
                g.DrawRectangle(border, 0, 0, w - 1, h - 1);
            StringFormat center = new StringFormat(); center.Alignment = StringAlignment.Center;
            using (Font big = new Font("Segoe UI", 64 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font mid = new Font("Segoe UI", 24 * sc, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font head = new Font("Segoe UI", 30 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font line = new Font("Segoe UI", 24 * sc, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            using (SolidBrush white = new SolidBrush(Color.FromArgb(240, 240, 248)))
            using (SolidBrush shade = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                if (ms < TitleMs)
                {
                    float y = h / 2 - 70 * sc;
                    g.DrawString("FIM DE JOGO", big, shade, new RectangleF(3 * sc, y + 3 * sc, w, 100 * sc), center);
                    g.DrawString("FIM DE JOGO", big, gold, new RectangleF(0, y, w, 100 * sc), center);
                    g.DrawString("Você derrotou o Dragão!", mid, white, new RectangleF(0, y + 100 * sc, w, 40 * sc), center);
                    return;
                }
                float t = (ms - TitleMs) / (float)CreditsMs;
                float lh = 40 * sc, content = Lines.Length * lh;
                float y0 = h + (-content - h) * t;
                for (int i = 0; i < Lines.Length; i++)
                {
                    float y = y0 + i * lh;
                    if (y < -lh || y > h) continue;
                    string s = Lines[i];
                    if (s.StartsWith("#")) g.DrawString(s.Substring(1), head, gold, new RectangleF(0, y, w, lh), center);
                    else g.DrawString(s, line, white, new RectangleF(0, y, w, lh), center);
                }
            }
        }
    }

        // Tela "VOCE MORREU" com o botao "Tentar de novo". Fica por cima de tudo e, ao contrario dos avisos, recebe cliques.
    class DeathForm : Form
    {
        float sc;
        Rectangle btn;
        bool hover;
        int ms;
        Action onRetry;
        System.Windows.Forms.Timer tm;

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x08000000 | 0x80; return cp; }   // NOACTIVATE | TOOLWINDOW (sem TOPMOST: atras dos aplicativos)
        }

        public DeathForm(float scale, Action onRetry)
        {
            this.onRetry = onRetry;
            sc = Math.Max(1f, scale);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            Shown += delegate { ZOrder.Bottom(Handle); };
            StartPosition = FormStartPosition.Manual;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = (int)(540 * sc), h = (int)(300 * sc);
            Bounds = new Rectangle(wa.X + (wa.Width - w) / 2, wa.Y + (wa.Height - h) / 2, w, h);
            BackColor = Color.FromArgb(24, 10, 16);
            int r = (int)(28 * sc);
            GraphicsPath gp = new GraphicsPath();
            gp.AddArc(0, 0, r, r, 180, 90); gp.AddArc(w - r, 0, r, r, 270, 90);
            gp.AddArc(w - r, h - r, r, r, 0, 90); gp.AddArc(0, h - r, r, r, 90, 90);
            gp.CloseAllFigures();
            Region = new Region(gp);
            btn = new Rectangle((int)((w - 280 * sc) / 2), (int)(h - 108 * sc), (int)(280 * sc), (int)(66 * sc));
            Opacity = 0;
            tm = new System.Windows.Forms.Timer();
            tm.Interval = 30;
            tm.Tick += delegate { ms += 30; Opacity = Math.Min(0.97, 0.97 * ms / 300.0); if (ms >= 300) tm.Stop(); };
            tm.Start();
        }

        public Rectangle ButtonRect { get { return btn; } }
        public bool Ready { get { return ms >= 300; } }
        public void Retry() { Action cb = onRetry; onRetry = null; try { Close(); } catch { } if (cb != null) cb(); }      // 'Tentar de novo' pelo teclado (Enter)

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool h = btn.Contains(e.Location);
            if (h != hover) { hover = h; Cursor = h ? Cursors.Hand : Cursors.Default; Invalidate(); }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && btn.Contains(e.Location))
            {
                Action cb = onRetry; onRetry = null;
                Close();
                if (cb != null) cb();
            }
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            float w = Width, h = Height;
            using (Pen border = new Pen(Color.FromArgb(214, 50, 60), 3 * sc)) g.DrawRectangle(border, 0, 0, w - 1, h - 1);
            StringFormat c = new StringFormat(); c.Alignment = StringAlignment.Center; c.LineAlignment = StringAlignment.Center;
            using (Font big = new Font("Segoe UI", 58 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font bf = new Font("Segoe UI", 26 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush red = new SolidBrush(Color.FromArgb(232, 60, 70)))
            using (SolidBrush shade = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            using (SolidBrush white = new SolidBrush(Color.White))
            using (SolidBrush bg = new SolidBrush(hover ? Color.FromArgb(232, 80, 88) : Color.FromArgb(196, 52, 62)))
            {
                g.DrawString("VOCÊ MORREU", big, shade, new RectangleF(3 * sc, 3 * sc, w, h * 0.55f), c);
                g.DrawString("VOCÊ MORREU", big, red, new RectangleF(0, 0, w, h * 0.55f), c);
                GraphicsPath bp = new GraphicsPath();
                int rr = (int)(18 * sc);
                bp.AddArc(btn.X, btn.Y, rr, rr, 180, 90); bp.AddArc(btn.Right - rr, btn.Y, rr, rr, 270, 90);
                bp.AddArc(btn.Right - rr, btn.Bottom - rr, rr, rr, 0, 90); bp.AddArc(btn.X, btn.Bottom - rr, rr, rr, 90, 90);
                bp.CloseAllFigures();
                g.FillPath(bg, bp); bp.Dispose();
                g.DrawString("Tentar de novo", bf, white, new RectangleF(btn.X, btn.Y, btn.Width, btn.Height), c);
                using (Font sm = new Font("Segoe UI", 17 * sc, FontStyle.Regular, GraphicsUnit.Pixel)) g.DrawString("ou aperte Enter", sm, white, new RectangleF(btn.X, btn.Bottom + 2 * sc, btn.Width, 24 * sc), c);
            }
        }
    }

    class PetForm : Form
    {
        const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
        static readonly Color Key = Color.FromArgb(255, 0, 255);

        public int S;
        Random rnd = new Random();
        System.Windows.Forms.Timer timer;
        Stopwatch clock = Stopwatch.StartNew();
        double lastT;
        float time;
        bool paused;
        NotifyIcon tray;

        Sprite[] heroWalk = new Sprite[2];
        Sprite heroDead;
        Sprite[] octo = new Sprite[2], bat = new Sprite[2], slime = new Sprite[2], boko = new Sprite[3];
        Sprite heartFull, heartEmpty, heartHalf, rockSpr;
        Sprite[] rupeeSpr = new Sprite[6];
        Font font;

        List<Enemy> enemies = new List<Enemy>();
        List<Rock> rocks = new List<Rock>();
        List<Item> items = new List<Item>();
        List<Puff> puffs = new List<Puff>();
        float spawnTimer = 2f;

        // heroi
        int MaxHP = 5;
        float hx, hy, kvx, kvy, walkT, atkT, atkCool, inv, deadT, wx, wy, wait, speechT;
        int hface = 1, rupees;
        float hp = 5;                        // coracoes (aceita meio coracao: encostar em monstro tira 0,5)
        bool walking, attacking, hitDone, dead, swingPractice;
        string speech = "";
        int calmState;                    // 0 normal, 1 aguardando limpar a tela, 2 descansando
        float calmT, nextCalm = 120f + 180f * (float)new Random().NextDouble();
        Sprite heroFront, heroFrontWave, heroSleepSpr, treeSpr, appleSpr, appleHud;
        List<Tree> trees = new List<Tree>();
        List<Arrow> arrows = new List<Arrow>();
        List<FlyApple> flyApples = new List<FlyApple>();
        int apples;
        bool bowing;
        float bowT, bowCool, aimX = 1, aimY;
        float calmDur, phaseT, pickT;
        int phase, picksLeft;
        bool harvest, testLong, forceLong, lastWasLongest;
        Sprite heroSideSleep;
        Tree curTree;

        public PetForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            StartPosition = FormStartPosition.Manual;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Bounds = wa;
            BackColor = Key;
            TransparencyKey = Key;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            baseS = Math.Max(3, (int)Math.Round(Screen.PrimaryScreen.Bounds.Height / 360.0));
            EvoInit();
            ChestInit();
            LoadEquip();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--bau") >= 0) { chestState = 1; kingGear = false; }      // teste rapido: o bau ja aparece
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--evolucao") >= 0) { evoKills = EvoThreshold[0] - 1; evoStage = 0; evoNext = 1; }      // teste rapido: o proximo monstro da a 1a evolucao
            sizeMode = LoadSize();
            S = SizeToS(sizeMode);
            BuildSprites();
            font = new Font("Consolas", S * 5, FontStyle.Bold, GraphicsUnit.Pixel);

            hx = wa.Width / 2f; hy = wa.Height / 2f;
            wx = hx; wy = hy; wait = 1f;

            // macieiras nas bordas (longe do centro e da area de icones/janelas)
            float Wd = wa.Width, Hd = wa.Height;
            trees.Add(NewTree(Wd * (0.90f + 0.04f * (float)rnd.NextDouble()), Hd * (0.45f + 0.08f * (float)rnd.NextDouble())));
            trees.Add(NewTree(Wd * (0.86f + 0.05f * (float)rnd.NextDouble()), Hd * (0.82f + 0.05f * (float)rnd.NextDouble())));
            trees.Add(NewTree(Wd * (0.10f + 0.12f * (float)rnd.NextDouble()), Hd * 0.90f));

            BuildTray();
            record = LoadRecord(); nextPower = Math.Max(25, record + 1);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--poder") >= 0) { rupees = 30; powerPending = true; }      // teste rapido do poder
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--teste") >= 0) { nextCalm = 3f; testLong = true; }   // teste rapido

            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lanche") >= 0) apples = 5;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jogar") >= 0) SetPlayMode(true);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--dragao") >= 0) StartDragonTest();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--chefao") >= 0) StartBossTest();   // teste rapido do lanche
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 30;
            timer.Tick += delegate { Tick(); };
            timer.Start();
            lastT = clock.Elapsed.TotalSeconds;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnPaintBackground(PaintEventArgs e) { }

        // ---- tamanho do heroi e do cenario: 0 pequeno, 1 normal, 2 grande (a escala S muda e tudo acompanha) ----
        int baseS, sizeMode = 1;
        ToolStripMenuItem[] sizeItems = new ToolStripMenuItem[3];
        int SizeToS(int mode)
        {
            if (mode == 0) return Math.Max(2, (int)Math.Round(baseS * 0.67));
            if (mode == 2) return (int)Math.Round(baseS * 1.34);
            return baseS;
        }
        int LoadSize()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return 1;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object v = k.GetValue("Tamanho"); if (v != null) { int m = Convert.ToInt32(v); if (m >= 0 && m <= 2) return m; } } } } catch { }
            return 1;
        }
        void SetSizeMode(int mode)
        {
            sizeMode = mode;
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") == null)
                try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("Tamanho", mode); } catch { }
            S = SizeToS(mode);
            BuildSprites();
            if (font != null) font.Dispose();
            font = new Font("Consolas", S * 5, FontStyle.Bold, GraphicsUnit.Pixel);
            if (helpFont != null) { helpFont.Dispose(); helpFont = null; }
            if (pauseFont != null) { pauseFont.Dispose(); pauseFont = null; }
            for (int i = 0; i < 3; i++) if (sizeItems[i] != null) sizeItems[i].Checked = (i == mode);
            hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx)); hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
        }

        // ================= EQUIPAMENTO (menu de pausa: Espaco) =================
        // indices: arco 0 madeira/1 metal | espada 0 comum/1 Espada Mestre | escudo 0 azul/1 vermelho/2 do rei | gorro 0 verde/1 roxo/2 coroa | roupa 0 verde/1 blusa azul/2 roupa de rei
        int eqBow, eqSword, eqShield, eqHat, eqTunic;
        bool OwnBow(int i) { return i == 0 || (i == 1 && evoStage >= 1); }
        bool OwnSword(int i) { return i == 0 || (i == 1 && evoStage >= 4); }
        bool OwnShield(int i) { return i == 0 || (i == 1 && evoStage >= 4) || (i == 2 && kingGear); }
        bool OwnHat(int i) { return i == 0 || (i == 1 && evoStage >= 2) || (i == 2 && kingGear); }
        bool OwnTunic(int i) { return i == 0 || (i == 1 && evoStage >= 3) || (i == 2 && kingGear); }
        void BestEquip()
        {
            eqBow = evoStage >= 1 ? 1 : 0; eqSword = evoStage >= 4 ? 1 : 0;
            eqShield = kingGear ? 2 : (evoStage >= 4 ? 1 : 0); eqHat = kingGear ? 2 : (evoStage >= 2 ? 1 : 0); eqTunic = kingGear ? 2 : (evoStage >= 3 ? 1 : 0);
        }
        void LoadEquip()
        {
            BestEquip();
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop"))
                {
                    object v = k == null ? null : k.GetValue("Equip");
                    if (v != null)
                    {
                        string[] p = v.ToString().Split(',');
                        if (p.Length == 5) { eqBow = int.Parse(p[0]); eqSword = int.Parse(p[1]); eqShield = int.Parse(p[2]); eqHat = int.Parse(p[3]); eqTunic = int.Parse(p[4]); }
                    }
                }
            }
            catch { BestEquip(); }
            ValidateEquip();
        }
        void SaveEquip()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("Equip", eqBow + "," + eqSword + "," + eqShield + "," + eqHat + "," + eqTunic); } catch { }
        }
        void ValidateEquip()      // nunca fica com algo que o heroi nao tem
        {
            if (!OwnBow(eqBow)) eqBow = 0; if (!OwnSword(eqSword)) eqSword = 0; if (!OwnShield(eqShield)) eqShield = 0; if (!OwnHat(eqHat)) eqHat = 0; if (!OwnTunic(eqTunic)) eqTunic = 0;
        }
        void AutoEquip(int stage)      // ao ganhar um item, ele e equipado
        {
            if (stage == 1) eqBow = 1; else if (stage == 2) eqHat = 1; else if (stage == 3) eqTunic = 1; else if (stage == 4) { eqSword = 1; eqShield = 1; } else if (stage == 5) { eqHat = 2; eqTunic = 2; eqShield = 2; }
            ValidateEquip(); SaveEquip();
        }
        public void EquipItem(int cat, int id)      // cat: 0 arco, 1 espada, 2 escudo, 3 gorro, 4 roupa
        {
            if (cat == 0 && OwnBow(id)) eqBow = id; else if (cat == 1 && OwnSword(id)) eqSword = id; else if (cat == 2 && OwnShield(id)) eqShield = id;
            else if (cat == 3 && OwnHat(id)) eqHat = id; else if (cat == 4 && OwnTunic(id)) eqTunic = id; else return;
            SaveEquip(); BuildSprites();
        }

        // ================= MENU DE PAUSA (Espaco): abas Armas / Roupas / Comida e o heroi ampliado =================
        MenuForm menuForm;
        bool MenuOpen { get { return menuForm != null && !menuForm.IsDisposed; } }
        bool previewMode;
        void OpenMenu()
        {
            if (MenuOpen || EvoWaiting || deathWait) return;
            try { menuForm = new MenuForm(this, S / 3f); menuForm.Show(); }
            catch { menuForm = null; }
        }
        void CloseMenu() { if (MenuOpen) { try { menuForm.Close(); } catch { } } menuForm = null; }
        public bool Owns(int cat, int id)
        {
            switch (cat) { case 0: return OwnBow(id); case 1: return OwnSword(id); case 2: return OwnShield(id); case 3: return OwnHat(id); default: return OwnTunic(id); }
        }
        public int Equipped(int cat)
        {
            switch (cat) { case 0: return eqBow; case 1: return eqSword; case 2: return eqShield; case 3: return eqHat; default: return eqTunic; }
        }
        public int AppleCount { get { return apples; } }
        public float HeartsNow { get { return hp; } }
        public int HeartsMax { get { return MaxHP; } }

        // comer uma maca crua pelo inventario: mostra o heroi comendo na tela ampliada e recupera meio coracao
        float menuEatT;
        public bool MenuEating { get { return menuEatT > 0; } }
        public string MenuEat()
        {
            if (menuEatT > 0) return null;
            if (apples <= 0) return "Sem maçãs";
            if (hp >= MaxHP) return "Corações cheios";
            menuEatT = 0.001f; return null;
        }
        public void MenuTick(float dt)
        {
            if (menuEatT <= 0) return;
            menuEatT += dt;
            if (menuEatT >= 0.9f) { menuEatT = 0; if (apples > 0) { apples--; hp = Math.Min(MaxHP, hp + 0.5f); } }
        }
        bool eatRaw;          // a maca na boca e crua (inventario / fome do automatico): meio coracao

        // desenha o heroi equipado, ampliado, dentro do menu (para ver como ele fica ao trocar de roupa/arma)
        bool previewBare;      // aba Roupas: o heroi nao segura espada, arco nem escudo
        public void DrawHeroPreview(Graphics g, float cx, float groundY, float k, bool bow, bool bare = false)
        {
            previewBare = bare;
            float sax = aimX, say = aimY, sbt = bowT;
            float shx = hx, shy = hy, sp = powerT, si = inv, spk = playPickT, sed = playEatT; bool ser = eatRaw; int shf = hface, sc = calmState, sck = cookState; bool sw = walking, sa = attacking, sb = bowing, sd = dead;
            previewMode = true;
            try
            {
                hx = 0; hy = 0; hface = 1; walking = false; attacking = false; bowing = bow; aimX = 1; aimY = 0; bowT = 0.4f; dead = false; powerT = 0; inv = 0; playPickT = 0; playEatT = menuEatT; eatRaw = menuEatT > 0; calmState = 0; cookState = 0;
                GraphicsState st = g.Save();
                g.TranslateTransform(cx, groundY); g.ScaleTransform(k, k);
                g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.SmoothingMode = SmoothingMode.None;
                DrawHero(g);
                g.Restore(st);
            }
            finally { aimX = sax; aimY = say; bowT = sbt; hx = shx; hy = shy; hface = shf; walking = sw; attacking = sa; bowing = sb; dead = sd; powerT = sp; inv = si; playPickT = spk; playEatT = sed; eatRaw = ser; calmState = sc; cookState = sck; previewMode = false; previewBare = false; }
        }

        // icones dos itens (formas desenhadas por codigo)
        public void DrawIcon(Graphics g, int cat, int id, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            if (cat == 0)
            {
                if (id == 1) { DrawEvoItem(g, 1, cx, cy, u * 0.85f); return; }
                PointF[] pts = new PointF[13];
                for (int i = 0; i < 13; i++) { float tt = (i - 6) / 6f; pts[i] = new PointF(cx + (5f * tt * tt - 2.5f) * u * 0.85f, cy + tt * 12 * u * 0.85f); }
                using (Pen o = new Pen(dark, u * 3.2f)) { o.StartCap = LineCap.Round; o.EndCap = LineCap.Round; g.DrawLines(o, pts); }
                using (Pen m = new Pen(Color.FromArgb(135, 85, 45), u * 1.8f)) { m.StartCap = LineCap.Round; m.EndCap = LineCap.Round; g.DrawLines(m, pts); }
                using (Pen s = new Pen(Color.FromArgb(235, 235, 235), Math.Max(1f, u * 0.4f))) g.DrawLine(s, pts[0], pts[12]);
            }
            else if (cat == 1) IconSword(g, cx, cy, u, id == 1);
            else if (cat == 2) IconShield(g, cx, cy, u, id);
            else if (cat == 3)
            {
                if (id == 0) DrawItemSpr(g, menuSpr[0], cx, cy, u * 0.85f);
                else if (id == 1) DrawEvoItem(g, 2, cx, cy, u * 0.85f);
                else DrawCrownItem(g, cx, cy, u * 0.9f);
            }
            else
            {
                if (id == 0) DrawItemSpr(g, menuSpr[1], cx, cy, u * 0.85f);
                else if (id == 1) DrawEvoItem(g, 3, cx, cy, u * 0.85f);
                else DrawItemSpr(g, menuSpr[2], cx, cy, u * 0.85f);
            }
        }
        void DrawItemSpr(Graphics g, Sprite sp, float cx, float cy, float u)
        {
            if (sp == null) return;
            float k = u / S * 1.7f, w = sp.W * k, h = sp.H * k; g.DrawImage(sp.R, cx - w / 2f, cy - h / 2f, w, h);
        }
        void IconSword(Graphics g, float cx, float cy, float u, bool master)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            Color blade = master ? Color.FromArgb(205, 232, 255) : Color.FromArgb(225, 235, 245), guard = master ? Color.FromArgb(60, 95, 205) : Color.FromArgb(215, 160, 40), grip = master ? Color.FromArgb(80, 55, 150) : Color.FromArgb(135, 85, 45);
            GraphicsState st = g.Save(); g.TranslateTransform(cx, cy); g.RotateTransform(45f);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f)))
            using (SolidBrush bl = new SolidBrush(blade)) using (SolidBrush gu = new SolidBrush(guard)) using (SolidBrush gp = new SolidBrush(grip)) using (SolidBrush wh = new SolidBrush(Color.White))
            {
                PointF[] b = { new PointF(-2 * u, 3 * u), new PointF(-2 * u, -11 * u), new PointF(0, -15 * u), new PointF(2 * u, -11 * u), new PointF(2 * u, 3 * u) };
                g.FillPolygon(bl, b); g.DrawPolygon(ol, b);
                g.FillRectangle(wh, -0.4f * u, -12 * u, 0.8f * u, 14 * u);
                PointF[] gd = { new PointF(-6 * u, 3 * u), new PointF(6 * u, 3 * u), new PointF(7 * u, 6 * u), new PointF(-7 * u, 6 * u) };
                g.FillPolygon(gu, gd); g.DrawPolygon(ol, gd);
                g.FillRectangle(gp, -1.5f * u, 6 * u, 3 * u, 7 * u); g.DrawRectangle(ol, -1.5f * u, 6 * u, 3 * u, 7 * u);
                g.FillEllipse(gu, -2 * u, 12.5f * u, 4 * u, 3.5f * u);
            }
            g.Restore(st);
        }
        void IconShield(Graphics g, float cx, float cy, float u, int type)
        {
            if (type == 2) { DrawShieldItem(g, cx, cy, u * 0.9f); return; }
            Color dark = Color.FromArgb(28, 22, 36), col = type == 1 ? Color.FromArgb(205, 50, 50) : Color.FromArgb(55, 95, 205);
            using (SolidBrush kb = new SolidBrush(dark)) using (SolidBrush b = new SolidBrush(col)) using (SolidBrush ye = new SolidBrush(Color.FromArgb(245, 205, 70)))
            {      // mesmo formato do escudo do jogo: retangulo 6x8 com borda escura e quadradinho amarelo 2x3
                float k = u * 2.2f, x = cx - 3 * k, y = cy - 4 * k;
                g.FillRectangle(kb, x - k, y - k, 8 * k, 10 * k); g.FillRectangle(b, x, y, 6 * k, 8 * k); g.FillRectangle(ye, x + 2 * k, y + 2 * k, 2 * k, 3 * k);
            }
        }
        void IconCap(Graphics g, float cx, float cy, float u, Color c1, Color c2)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f))) using (SolidBrush dk = new SolidBrush(dark)) using (SolidBrush p1 = new SolidBrush(c1)) using (SolidBrush p2 = new SolidBrush(c2))
            {
                PointF[] tail = { new PointF(cx + 4 * u, cy - 9 * u), new PointF(cx + 13 * u, cy + 3 * u), new PointF(cx + 11 * u, cy + 9 * u), new PointF(cx + 3 * u, cy + 1 * u) };
                g.FillEllipse(dk, cx - 10 * u, cy - 11 * u, 20 * u, 17 * u); g.FillEllipse(p1, cx - 9 * u, cy - 10 * u, 18 * u, 15 * u);
                g.FillPolygon(p2, tail); g.DrawPolygon(ol, tail);
                g.FillRectangle(dk, cx - 11 * u, cy + 2 * u, 22 * u, 5 * u); g.FillRectangle(p2, cx - 10 * u, cy + 3 * u, 20 * u, 3 * u);
            }
        }
        void IconShirt(Graphics g, float cx, float cy, float u, Color c1, Color c2)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f))) using (SolidBrush dk = new SolidBrush(dark)) using (SolidBrush b1 = new SolidBrush(c1)) using (SolidBrush b2 = new SolidBrush(c2))
            {
                PointF[] shirt = {
                    new PointF(cx - 4 * u, cy - 10 * u), new PointF(cx + 4 * u, cy - 10 * u), new PointF(cx + 12 * u, cy - 5 * u), new PointF(cx + 9 * u, cy + 0.5f * u),
                    new PointF(cx + 6 * u, cy - 2 * u), new PointF(cx + 6 * u, cy + 10 * u), new PointF(cx - 6 * u, cy + 10 * u), new PointF(cx - 6 * u, cy - 2 * u),
                    new PointF(cx - 9 * u, cy + 0.5f * u), new PointF(cx - 12 * u, cy - 5 * u) };
                g.FillPolygon(b1, shirt); g.DrawPolygon(ol, shirt);
                g.FillRectangle(b2, cx - 6 * u, cy + 8 * u, 12 * u, 2 * u);
                g.FillEllipse(dk, cx - 3 * u, cy - 11.5f * u, 6 * u, 4 * u); g.FillEllipse(b2, cx - 2.2f * u, cy - 11f * u, 4.4f * u, 3 * u);
            }
        }
        public void IconApple(Graphics g, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (SolidBrush dk = new SolidBrush(dark)) using (SolidBrush r = new SolidBrush(Color.FromArgb(215, 55, 55))) using (SolidBrush gr = new SolidBrush(Color.FromArgb(58, 165, 62))) using (SolidBrush sh = new SolidBrush(Color.FromArgb(255, 180, 170)))
            {
                g.FillEllipse(dk, cx - 11 * u, cy - 9 * u, 22 * u, 21 * u); g.FillEllipse(r, cx - 10 * u, cy - 8 * u, 20 * u, 19 * u);
                g.FillEllipse(sh, cx - 6 * u, cy - 5 * u, 4 * u, 3 * u);
                g.FillRectangle(dk, cx - 0.8f * u, cy - 13 * u, 1.6f * u, 5 * u);
                g.FillEllipse(gr, cx + u, cy - 13 * u, 7 * u, 4 * u);
            }
        }

        class MenuForm : Form
        {
            struct Hit { public Rectangle R; public int Kind, A, B; }      // Kind 0 aba | 1 item | 2 fechar
            static readonly string[][] Names = {
                new string[] { "Arco de madeira", "Arco de metal" }, new string[] { "Espada comum", "Espada Mestre" }, new string[] { "Escudo azul", "Escudo vermelho", "Escudo do rei" },
                new string[] { "Gorro verde", "Gorro roxo", "Coroa de ouro" }, new string[] { "Túnica verde", "Blusa azul", "Roupa de rei" } };
            static readonly string[][] Descs = {
                new string[] { "O arco de sempre.", "Flechas com o dobro de dano." }, new string[] { "A espada de sempre.", "Dobro de dano." }, new string[] { "Escudo comum.", "Escudo vermelho do herói.", "Escudo circular do tesouro do rei." },
                new string[] { "O gorro de sempre.", "Gorro roxo da evolução.", "Coroa de ouro do tesouro do rei." }, new string[] { "A túnica de sempre.", "Blusa azul da evolução.", "Manto vermelho, calça marrom e sapatos roxos." } };
            static readonly string[] TabNames = { "Armas", "Roupas", "Comida" };
            PetForm p; float sc; int tab = -1, hover = -1; bool showBow;      // showBow: o heroi do exemplo segura o arco (ao clicar num arco) ou espada e escudo (ao clicar neles)
            List<Hit> hits = new List<Hit>(); string msg;
            System.Windows.Forms.Timer eatTimer;

            protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; } }      // TOOLWINDOW; fica por cima (e ativa) para receber os cliques

            public MenuForm(PetForm owner, float scale)
            {
                p = owner; sc = Math.Max(1f, scale);
                FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = true;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                int w = Math.Min((int)(1040 * sc), wa.Width - 20), h = Math.Min((int)(620 * sc), wa.Height - 20);
                Bounds = new Rectangle(wa.X + (wa.Width - w) / 2, wa.Y + (wa.Height - h) / 2, w, h);
                BackColor = Color.FromArgb(18, 22, 30);
                Shown += delegate { Activate(); };
                eatTimer = new System.Windows.Forms.Timer(); eatTimer.Interval = 33;
                eatTimer.Tick += delegate { if (p.MenuEating) { p.MenuTick(0.033f); Invalidate(); } };
                eatTimer.Start();
                FormClosed += delegate { eatTimer.Stop(); eatTimer.Dispose(); };
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int hv = -1;
                for (int i = 0; i < hits.Count; i++) if (hits[i].R.Contains(e.Location)) hv = i;
                if (hv != hover) { hover = hv; Cursor = hv >= 0 ? Cursors.Hand : Cursors.Default; Invalidate(); }
                base.OnMouseMove(e);
            }
            protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    for (int i = 0; i < hits.Count; i++)
                        if (hits[i].R.Contains(e.Location))
                        {
                            Hit h = hits[i];
                            if (h.Kind == 0) { tab = (tab == h.A) ? -1 : h.A; msg = null; }
                            else if (h.Kind == 1) { p.EquipItem(h.A, h.B); if (h.A == 0) showBow = true; else if (h.A == 1 || h.A == 2) showBow = false; }
                            else if (h.Kind == 2) { p.CloseMenu(); return; }
                            else if (h.Kind == 3) { msg = p.MenuEat(); showBow = false; }
                            Invalidate(); break;
                        }
                base.OnMouseClick(e);
            }

            void Round(Graphics g, Rectangle r, int rad, Brush fill, Pen pen)
            {
                GraphicsPath gp = new GraphicsPath(); int d = rad * 2;
                gp.AddArc(r.X, r.Y, d, d, 180, 90); gp.AddArc(r.Right - d, r.Y, d, d, 270, 90); gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90); gp.CloseAllFigures();
                if (fill != null) g.FillPath(fill, gp); if (pen != null) g.DrawPath(pen, gp); gp.Dispose();
            }

            void Section(Graphics g, string title, int cat, ref int y, Rectangle area, Font hf, Font nf)
            {
                g.DrawString(title, hf, Brushes.Gold, area.X, y); y += (int)(26 * sc);
                int cs = (int)(112 * sc), gap = (int)(12 * sc), x = area.X;
                int n = Names[cat].Length;
                for (int id = 0; id < n; id++)
                {
                    if (!p.Owns(cat, id)) continue;
                    Rectangle r = new Rectangle(x, y, cs, cs);
                    bool eq = p.Equipped(cat) == id; int idx = hits.Count; bool hv = hover == idx;
                    hits.Add(new Hit { R = r, Kind = 1, A = cat, B = id });
                    using (SolidBrush bg = new SolidBrush(hv ? Color.FromArgb(58, 66, 86) : Color.FromArgb(34, 40, 54)))
                    using (Pen pen = new Pen(eq ? Color.FromArgb(247, 190, 70) : Color.FromArgb(80, 90, 112), eq ? 3 * sc : 1.5f * sc))
                        Round(g, r, (int)(10 * sc), bg, pen);
                    p.DrawIcon(g, cat, id, r.X + cs / 2f, r.Y + cs * 0.42f, cs / 34f);
                    StringFormat c = new StringFormat(); c.Alignment = StringAlignment.Center;
                    g.DrawString(eq ? "EQUIPADO" : Names[cat][id], nf, eq ? Brushes.Gold : Brushes.White, new RectangleF(r.X, r.Bottom - 22 * sc, cs, 22 * sc), c);
                    x += cs + gap;
                }
                y += cs + (int)(14 * sc);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics; hits.Clear();
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                float W = Width, H = Height; int pad = (int)(20 * sc);
                using (Pen border = new Pen(Color.FromArgb(247, 190, 70), 3 * sc)) g.DrawRectangle(border, 1, 1, W - 3, H - 3);
                using (Font tf = new Font("Segoe UI", 24 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Font hf = new Font("Segoe UI", 17 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Font nf = new Font("Segoe UI", 13 * sc, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Font sf = new Font("Segoe UI", 15 * sc, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    g.DrawString("HERÓI", tf, Brushes.Gold, pad, 22 * sc);
                    // heroi ampliado (mostra o que esta equipado)
                    Rectangle pv = new Rectangle(pad, (int)(80 * sc), (int)(360 * sc), (int)(H - 80 * sc - pad));
                    using (LinearGradientBrush lg = new LinearGradientBrush(pv, Color.FromArgb(46, 56, 78), Color.FromArgb(22, 28, 40), 90f))
                    using (Pen pp = new Pen(Color.FromArgb(80, 90, 112), 1.5f * sc)) Round(g, pv, (int)(12 * sc), lg, pp);
                    float k = Math.Max(2f, (float)Math.Floor((pv.Height - 150 * sc) / (30f * p.S)));
                    p.DrawHeroPreview(g, pv.X + pv.Width / 2f, pv.Y + pv.Height * 0.62f, k, showBow, tab == 1);
                    g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    float ty = pv.Bottom - 118 * sc;
                    string[] cats = { "Arco", "Espada", "Escudo", "Gorro", "Roupa" };
                    for (int i = 0; i < 5; i++) { g.DrawString(cats[i] + ": " + Names[i][p.Equipped(i)], sf, Brushes.White, pv.X + 16 * sc, ty); ty += 21 * sc; }
                    // abas (fechadas ate serem escolhidas)
                    int x0 = (int)(420 * sc), th = (int)(50 * sc), gapT = (int)(10 * sc), tw = (int)((W - x0 - pad - 60 * sc - 2 * gapT) / 3);
                    for (int i = 0; i < 3; i++)
                    {
                        Rectangle r = new Rectangle(x0 + i * (tw + gapT), pad, tw, th); int idx = hits.Count; bool hv = hover == idx; bool sel = tab == i;
                        hits.Add(new Hit { R = r, Kind = 0, A = i });
                        using (SolidBrush bg = new SolidBrush(sel ? Color.FromArgb(247, 190, 70) : (hv ? Color.FromArgb(58, 66, 86) : Color.FromArgb(34, 40, 54))))
                        using (Pen pen = new Pen(Color.FromArgb(247, 190, 70), 2 * sc)) Round(g, r, (int)(10 * sc), bg, pen);
                        StringFormat c = new StringFormat(); c.Alignment = StringAlignment.Center; c.LineAlignment = StringAlignment.Center;
                        g.DrawString(TabNames[i], hf, sel ? Brushes.Black : Brushes.White, r, c);
                    }
                    Rectangle cl = new Rectangle((int)(W - pad - 44 * sc), pad, (int)(44 * sc), th); int ci = hits.Count; hits.Add(new Hit { R = cl, Kind = 2 });
                    using (SolidBrush bg = new SolidBrush(hover == ci ? Color.FromArgb(200, 60, 70) : Color.FromArgb(120, 40, 50))) Round(g, cl, (int)(10 * sc), bg, null);
                    StringFormat cc = new StringFormat(); cc.Alignment = StringAlignment.Center; cc.LineAlignment = StringAlignment.Center;
                    g.DrawString("X", hf, Brushes.White, cl, cc);
                    // conteudo da aba escolhida
                    Rectangle area = new Rectangle(x0, (int)(90 * sc), (int)(W - x0 - pad), (int)(H - 90 * sc - pad));
                    if (tab < 0)
                        g.DrawString("Escolha uma aba: Armas, Roupas ou Comida.\nClique no que quiser usar; o herói muda ao lado.\nEspaço ou Esc fecham o menu.", sf, Brushes.LightGray, area.X, area.Y + 20 * sc);
                    else
                    {
                        int y = area.Y;
                        if (tab == 0) { Section(g, "ARCOS", 0, ref y, area, hf, nf); Section(g, "ESPADAS", 1, ref y, area, hf, nf); Section(g, "ESCUDOS", 2, ref y, area, hf, nf); }
                        else if (tab == 1) { Section(g, "GORROS", 3, ref y, area, hf, nf); Section(g, "ROUPAS", 4, ref y, area, hf, nf); }
                        else
                        {
                            g.DrawString("COMIDA", hf, Brushes.Gold, area.X, y); y += (int)(26 * sc);
                            int cs = (int)(112 * sc); Rectangle r = new Rectangle(area.X, y, cs, cs);
                            int ai = hits.Count; hits.Add(new Hit { R = r, Kind = 3 });
                            using (SolidBrush bg = new SolidBrush(hover == ai ? Color.FromArgb(58, 66, 86) : Color.FromArgb(34, 40, 54))) using (Pen pen = new Pen(Color.FromArgb(80, 90, 112), 1.5f * sc)) Round(g, r, (int)(10 * sc), bg, pen);
                            p.IconApple(g, r.X + cs / 2f, r.Y + cs * 0.42f, cs / 34f);
                            StringFormat c = new StringFormat(); c.Alignment = StringAlignment.Center;
                            g.DrawString("Maçãs x " + p.AppleCount, nf, Brushes.White, new RectangleF(r.X, r.Bottom - 22 * sc, cs, 22 * sc), c);
                            g.DrawString("Clique na maçã para o herói comer: maçã crua recupera meio coração.\nCom 5 ou mais ele faz lanche na fogueira (assadas recuperam mais).", sf, Brushes.LightGray, area.X, r.Bottom + 16 * sc);
                            g.DrawString("Corações: " + p.HeartsNow.ToString("0.#") + " / " + p.HeartsMax, sf, Brushes.Gold, area.X, r.Bottom + 68 * sc);
                            if (!string.IsNullOrEmpty(msg)) g.DrawString(msg, sf, Brushes.OrangeRed, area.X, r.Bottom + 94 * sc);
                        }
                    }
                    // descricao do item sob o mouse
                    if (hover >= 0 && hover < hits.Count && hits[hover].Kind == 1)
                    {
                        Hit h = hits[hover];
                        g.DrawString(Names[h.A][h.B] + " — " + Descs[h.A][h.B], sf, Brushes.White, area.X, H - pad - 26 * sc);
                    }
                }
            }
        }
        void BuildSprites()
        {
            ValidateEquip();
            Art.Crown = eqHat == 2; Art.Robe = eqTunic == 2;
            Dictionary<char, Color> hov = new Dictionary<char, Color>();
            if (eqHat == 1) hov['H'] = Color.FromArgb(150, 80, 205);       // gorro roxo
            if (eqTunic == 1) hov['Q'] = Color.FromArgb(70, 125, 235);       // blusa azul
            if (eqTunic == 2) { hov['Q'] = Color.FromArgb(190, 35, 60); hov['J'] = Color.FromArgb(150, 100, 55); hov['Z'] = Color.FromArgb(130, 60, 190); }      // bau: manto vermelho, calca marrom, sapatos roxos
            heroWalk[0] = Art.Make(Art.HeroRecolor(Art.HeroA), S, hov);
            heroWalk[1] = Art.Make(Art.HeroRecolor(Art.HeroB), S, hov);
            Bitmap d = (Bitmap)heroWalk[0].R.Clone();
            d.RotateFlip(RotateFlipType.Rotate90FlipNone);
            heroDead = new Sprite(); heroDead.R = d; heroDead.L = d; heroDead.W = d.Width; heroDead.H = d.Height;
            Sprite fs = Art.Make(Art.HeroRecolor(Art.HeroFrontSleep), S, hov);
            Bitmap lie = (Bitmap)fs.R.Clone();
            lie.RotateFlip(RotateFlipType.Rotate270FlipNone);          // cabeca para a esquerda, rosto para cima
            heroSleepSpr = new Sprite(); heroSleepSpr.R = lie; heroSleepSpr.L = lie; heroSleepSpr.W = lie.Width; heroSleepSpr.H = lie.Height;
            Bitmap side = (Bitmap)heroWalk[0].R.Clone();
            side.RotateFlip(RotateFlipType.Rotate270FlipNone);         // de costas, de perfil: cabeca a esquerda, nariz para cima
            heroSideSleep = new Sprite(); heroSideSleep.R = side; heroSideSleep.L = side; heroSideSleep.W = side.Width; heroSideSleep.H = side.Height;
            treeSpr = Art.Make(Art.TreeRows(), S, null);
            appleSpr = Art.Make(Art.AppleSm, S, null);
            appleHud = Art.Make(Art.AppleBig, S, null);
            int ds = (int)Math.Round(S * 1.7); DK = ds / (float)S;          // dragao ~1,7x maior
            dragonSpr[0] = Art.Make(Art.DragonRows(false), ds, null); dragonSpr[1] = Art.Make(Art.DragonRows(true), ds, null);
            Dictionary<char, Color> redOv = Art.Ov('P', Color.FromArgb(210, 45, 50)); redOv['p'] = Color.FromArgb(135, 25, 38); redOv['Y'] = Color.FromArgb(245, 170, 70);      // abaixo de 50% da vida o dragao fica vermelho
            dragonSprRed[0] = Art.Make(Art.DragonRows(false), ds, redOv); dragonSprRed[1] = Art.Make(Art.DragonRows(true), ds, redOv);
            Dictionary<char, Color> glowOv = Art.Ov('P', Color.FromArgb(255, 125, 105)); glowOv['p'] = Color.FromArgb(235, 80, 70); glowOv['Y'] = Color.FromArgb(255, 235, 140); glowOv['W'] = Color.FromArgb(255, 255, 230);      // versao 'brilhante' (mais clara)
            dragonSprGlow[0] = Art.Make(Art.DragonRows(false), ds, glowOv); dragonSprGlow[1] = Art.Make(Art.DragonRows(true), ds, glowOv);
            fireSpr[0] = Art.Make(Art.FireballRows(0), S + 1, null); fireSpr[1] = Art.Make(Art.FireballRows(1), S + 1, null);
            Color[] bk = { Color.FromArgb(215, 55, 55), Color.FromArgb(205, 92, 48), Color.FromArgb(176, 86, 42), Color.FromArgb(142, 74, 36) };
            for (int i = 0; i < 4; i++) { Dictionary<char, Color> ob = Art.Ov('R', bk[i]); ob['G'] = i == 0 ? Color.FromArgb(58, 165, 62) : Color.FromArgb(104, 110, 40); appleBake[i] = Art.Make(Art.AppleSm, S, ob); }
            heroFront = Art.Make(Art.HeroRecolor(Art.HeroFront), S, hov);
            Dictionary<char, Color> capOv = Art.Ov('H', Color.FromArgb(150, 80, 205)); capOv['h'] = Color.FromArgb(205, 150, 240); capOv['g'] = Color.FromArgb(96, 50, 140);
            Dictionary<char, Color> shOv = Art.Ov('Q', Color.FromArgb(70, 125, 235)); shOv['h'] = Color.FromArgb(135, 180, 255); shOv['g'] = Color.FromArgb(45, 85, 190);
            evoItemSpr[0] = Art.Make(Art.CapItem, S, capOv); evoItemSpr[1] = Art.Make(Art.ShirtItem, S, shOv);
            Dictionary<char, Color> gcOv = Art.Ov('H', Color.FromArgb(58, 165, 62)); gcOv['h'] = Color.FromArgb(130, 210, 120); gcOv['g'] = Color.FromArgb(30, 110, 45);
            Dictionary<char, Color> gsOv = Art.Ov('Q', Color.FromArgb(58, 165, 62)); gsOv['h'] = Color.FromArgb(130, 210, 120); gsOv['g'] = Color.FromArgb(30, 110, 45);
            Dictionary<char, Color> rbOv = Art.Ov('Q', Color.FromArgb(190, 35, 60)); rbOv['h'] = Color.FromArgb(235, 100, 120); rbOv['g'] = Color.FromArgb(130, 20, 40);
            menuSpr[0] = Art.Make(Art.CapItem, S, gcOv); menuSpr[1] = Art.Make(Art.ShirtItem, S, gsOv); menuSpr[2] = Art.Make(Art.ShirtItem, S, rbOv);
            heroFrontWave = Art.Make(Art.HeroRecolor(Art.HeroFrontWave), S, hov);
            octo[0] = Art.Make(Art.OctoA, S, null); octo[1] = Art.Make(Art.OctoB, S, null);
            bat[0] = Art.Make(Art.BatUp, S, null); bat[1] = Art.Make(Art.BatDown, S, null);
            slime[0] = Art.Make(Art.SlimeTall, S, null); slime[1] = Art.Make(Art.SlimeFlat, S, null);
            boko[0] = Art.Make(Art.BokoA, S, null); boko[1] = Art.Make(Art.BokoB, S, null); boko[2] = Art.Make(Art.BokoC, S, null);
            heartFull = Art.Make(Art.Heart, S, null);
            Dictionary<char, Color> gray = Art.Ov('R', Color.FromArgb(70, 70, 82));
            gray['W'] = Color.FromArgb(105, 105, 118);
            heartEmpty = Art.Make(Art.Heart, S, gray);
            Bitmap halfBmp = (Bitmap)heartEmpty.R.Clone();      // meio coracao: metade esquerda cheia sobre o coracao vazio
            using (Graphics hg = Graphics.FromImage(halfBmp)) hg.DrawImage(heartFull.R, new Rectangle(0, 0, heartFull.W / 2, heartFull.H), new Rectangle(0, 0, heartFull.W / 2, heartFull.H), GraphicsUnit.Pixel);
            heartHalf = new Sprite(); heartHalf.R = halfBmp; heartHalf.L = halfBmp; heartHalf.W = halfBmp.Width; heartHalf.H = halfBmp.Height;
            rockSpr = Art.Make(Art.Rock, S, null);
            rupeeSpr[0] = Art.Make(Art.Rupee, S, null);
            rupeeSpr[1] = Art.Make(Art.Rupee, S, Art.Ov('G', Color.FromArgb(70, 125, 235)));
            rupeeSpr[2] = Art.Make(Art.Rupee, S, Art.Ov('G', Color.FromArgb(230, 60, 60)));
            rupeeSpr[3] = Art.Make(Art.Rupee, S, Art.Ov('G', Color.FromArgb(240, 200, 60)));      // 10
            rupeeSpr[4] = Art.Make(Art.Rupee, S, Art.Ov('G', Color.FromArgb(150, 80, 205)));      // 50
            rupeeSpr[5] = Art.Make(Art.Rupee, S * 2, Art.Ov('G', Color.FromArgb(255, 205, 40)));      // 300 (recompensa do dragao): dourada e grande
        }

        void BuildTray()
        {
            tray = new NotifyIcon();
            using (Bitmap ib = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(ib))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImage(heroWalk[0].R, 0, 0, 32, 32);
                }
                tray.Icon = Icon.FromHandle(ib.GetHicon());
            }
            tray.Text = "Heroi do Desktop";
            ContextMenuStrip menu = new ContextMenuStrip();
            mAutoItem = new ToolStripMenuItem("Deixar rodando (automático)"); mAutoItem.Checked = true;
            mPlayItem = new ToolStripMenuItem("Jogar (você controla)");
            mAutoItem.Click += delegate { SetPlayMode(false); };
            mPlayItem.Click += delegate { SetPlayMode(true); };
            menu.Items.Add(mAutoItem); menu.Items.Add(mPlayItem); menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Invocar inimigo", null, delegate { Spawn(rnd.Next(3)); });
            menu.Items.Add("Curar heroi", null, delegate { hp = MaxHP; });
            menu.Items.Add("Dar 5 maçãs (lanche)", null, delegate { apples = Math.Max(apples, 5); });
            menu.Items.Add("Soneca agora", null, delegate { if (calmState == 0) nextCalm = 0; });
            menu.Items.Add("Colher macas e dormir (2 min)", null, delegate { if (calmState == 0) { forceLong = true; nextCalm = 0; } });
            menu.Items.Add("Teste: ir para a luta do dragão", null, delegate { StartDragonTest(); });
            ToolStripMenuItem pause = new ToolStripMenuItem("Pausar"); pauseItem = pause;
            pause.Click += delegate { paused = !paused; pause.Text = paused ? "Continuar" : "Pausar"; };
            menu.Items.Add(pause);
            ToolStripMenuItem startup = new ToolStripMenuItem("Iniciar com o Windows");
            startup.Checked = IsStartup();
            startup.Click += delegate { SetStartup(!startup.Checked); startup.Checked = IsStartup(); };
            menu.Items.Add(startup);
            ToolStripMenuItem sizeMenu = new ToolStripMenuItem("Tamanho do herói e do cenário");
            string[] sizeNames = { "Pequeno", "Normal", "Grande" };
            for (int i = 0; i < 3; i++)
            {
                int mi = i;
                ToolStripMenuItem it = new ToolStripMenuItem(sizeNames[i]); it.Checked = (i == sizeMode);
                it.Click += delegate { SetSizeMode(mi); };
                sizeItems[i] = it; sizeMenu.DropDownItems.Add(it);
            }
            menu.Items.Add(sizeMenu);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Inventário (Espaço)", null, delegate { OpenMenu(); });
            menu.Items.Add("Sobre", null, delegate
            {
                MessageBox.Show("Herói do Desktop\nVersão 1.0.0\n\nUm herói pixel-art que vive na sua área de trabalho: luta, colhe maçãs, assa o próprio lanche e enfrenta um dragão.\n\nIdeia: Alexandre\nCriação: Bia Krepel\nProgramação: Claude", "Sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
            menu.Items.Add("Sair", null, delegate { tray.Visible = false; Application.Exit(); });
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        bool IsStartup()
        {
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue("HeroiPet") != null; }
            catch { return false; }
        }
        void SetStartup(bool on)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (on) k.SetValue("HeroiPet", "\"" + Application.ExecutablePath + "\"");
                    else k.DeleteValue("HeroiPet", false);
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tray.Visible = false;
            SaveRecord();
            SaveKills(); SaveChest();
            if (evoForm != null) { try { evoForm.Close(); } catch { } evoForm = null; }
            CloseMenu();
            if (shadowLayer != null) { shadowLayer.Close(); shadowLayer = null; }
            base.OnFormClosing(e);
        }

        // ============================ LOGICA ============================
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        int zTick;
        void Tick()
        {
            if ((zTick++ % 6) == 0 && IsHandleCreated)
                {
                // Normal: fica no fundo (abaixo dos aplicativos). Com "Mostrar area de trabalho" (Win+D) a area de trabalho sobe e cobriria o heroi:
                // nesse caso ele sobe junto, para ficar por cima dela.
                bool front = DesktopShown();
                SetWindowPos(Handle, front ? new IntPtr(-1) : new IntPtr(1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0200);      // HWND_TOPMOST (so com a area de trabalho na frente) ou HWND_BOTTOM
                if (shadowLayer == null) { try { shadowLayer = new ShadowLayer(Bounds); shadowLayer.Show(); } catch { shadowLayer = null; } }
                if (shadowLayer != null && shadowLayer.IsHandleCreated) SetWindowPos(shadowLayer.Handle, Handle, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 | 0x0200);   // logo abaixo do heroi
                if (shield != null) { if (front) shield.PushTop(); else shield.PushBottom(); }
            }
            if (deathForm != null && !deathForm.IsDisposed && deathForm.Ready && IsDown(0x0D)) deathForm.Retry();      // Enter = Tentar de novo (se a janela estiver escondida atras de um aplicativo)
            if (shield != null) shield.PassThrough(IsDown(0x11));        // Ctrl apertado: cliques passam para a area de trabalho
            double now = clock.Elapsed.TotalSeconds;
            float dt = (float)Math.Min(0.05, now - lastT);
            lastT = now;
            CheckSpacePause();
            if (!paused && !EvoWaiting && !MenuOpen) Step(dt);      // com o menu aberto o jogo fica pausado          // com o item grande na tela o jogo fica pausado ate apertar "USAR ITEM"
            Invalidate();
        }

        float W { get { return ClientSize.Width; } }
        float H { get { return ClientSize.Height; } }

        void Step(float dt)
        {
            time += dt;
            if (deathWait)                           // tela de morte aberta: o jogo fica parado
            {
                for (int i = puffs.Count - 1; i >= 0; i--) { puffs[i].T += dt; if (puffs[i].T > 0.45f) puffs.RemoveAt(i); }
                if (speechT > 0) speechT -= dt;
                return;
            }
            // ---- ciclo de descanso: a cada 2-5 min, 1 min sem inimigos ----
            if (calmState == 0)
            {
                if (cookState == 0 && !playMode) nextCalm -= dt;
                if (nextCalm <= 0 && !dead && !playMode && powerT <= 0) calmState = 1;          // para de gerar inimigos
            }
            else if (calmState == 1)
            {
                if (enemies.Count == 0 && rocks.Count == 0 && items.Count == 0 && !dead && !attacking)
                {
                    calmState = 2; calmT = 0; speechT = 0;
                    // pausa de 0,5 a 2 min; 40% de chance de ser a longa
                    // e a maior possivel (2 min), e so nessa ele colhe macas
                    bool longest = forceLong || testLong || rnd.NextDouble() < 0.40;
                    lastWasLongest = longest;
                    forceLong = false;
                    calmDur = longest ? 120f : 30f + (float)rnd.NextDouble() * 80f;
                    curTree = null; harvest = false;
                    if (longest) { curTree = NearestTreeWithApples(); harvest = curTree != null; }   // pausa mais longa: colhe macas
                    phase = harvest ? 0 : 2; phaseT = 0; pickT = 0;
                }
            }
            else
            {
                calmT += dt;
                if (enemies.Count > 0 || dead) EndCalm(true);        // invocado/atacado: acorda
                else if (calmT >= calmDur) EndCalm(false);
            }
            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                Arrow a = arrows[i];
                a.X += a.VX * dt; a.Y += a.VY * dt;
                bool gone = a.X < -30 || a.Y < -30 || a.X > W + 30 || a.Y > H + 30;
                for (int k = enemies.Count - 1; k >= 0 && !gone; k--)
                {
                    Enemy en = enemies[k];
                    float dx = en.X - a.X, dy = (en.Y - (en.Type == 1 ? 9 * S : (en.Type == 3 ? 12 * S * DK * en.Scale : 4 * S))) - a.Y;
                    float hitR = en.Type == 3 ? 14 * S * DK * en.Scale : 7 * S;
                    if (dx * dx + dy * dy < hitR * hitR) { DamageEnemy(en, (en.Type == 3 ? 50 : 1) * ((!a.Power && eqBow == 1) ? 2 : 1)); gone = true; }      // arco prateado (evolucao 1): o dobro de dano (flecha de poder nao muda)
                }
                if (gone) arrows.RemoveAt(i);
            }
            for (int i = flyApples.Count - 1; i >= 0; i--)
            {
                flyApples[i].T += dt;
                if (flyApples[i].T >= 0.6f)
                {
                    apples++;
                    puffs.Add(new Puff { Kind = 1, X = hx + hface * 6 * S, Y = hy - 20 * S });
                    flyApples.RemoveAt(i);
                }
            }
            foreach (Tree tr in trees)
                for (int i = 0; i < 5; i++)
                    if (!tr.Has[i]) { tr.Regrow[i] -= dt; if (tr.Regrow[i] <= 0) tr.Has[i] = true; }
            spawnTimer -= dt;
            if (spawnTimer <= 0)
            {
                spawnTimer = (3f + (float)rnd.NextDouble() * 4f) / SpawnRateMul();
                if (enemies.Count < EnemyCap() && !dead && calmState == 0 && cookState == 0 && bossPhase == 0) Spawn(SpawnType());
            }
            UpdatePower(dt);
            UpdateEvo(dt);
            UpdateChest(dt);
            UpdateHero(dt);
            if (playMode) CheckRupeeLevel();
            UpdateBoss(dt);
            // 5 macas: avisa na tela e, quando estiver livre, vai assar um lanche
            if (apples >= 5 && !toastShown && bossPhase == 0) { toastShown = true; ShowToast(); }
            if (apples < 5) toastShown = false;
            if (cookState == 0 && calmState == 0 && bossPhase == 0 && powerT <= 0 && !lootHold && apples >= 5 && enemies.Count == 0 && rocks.Count == 0 && !dead && !attacking && !bowing)
            {
                cookState = 1; cookT = 0; fireK = 0; fireX = W / 2; fireY = H / 2;
                for (int i = 0; i < 5; i++) onFire[i] = false;
            }
            UpdateCookApples(dt);
            UpdateEnemies(dt);
            UpdateRocks(dt);
            UpdateShocks(dt);
            UpdateItems(dt);
            for (int i = puffs.Count - 1; i >= 0; i--)
            {
                puffs[i].T += dt;
                if (puffs[i].T > 0.45f) puffs.RemoveAt(i);
            }
            if (speechT > 0) speechT -= dt;
        }

        // ================= EVOLUCAO: monstros derrotados no automatico dao equipamentos ao heroi =================
        // 1000 monstros: arco de metal | +500: gorro roxo | +500: blusa azul | +1000: Espada Mestre e escudo vermelho
        static readonly int[] EvoThreshold = { 1000, 1500, 2000, 3000 };
        static readonly int[] EvoNeeded = { 1000, 500, 500, 1000 };         // monstros que foram precisos para cada item
        static readonly string[] EvoTitle = { "ARCO DE METAL", "GORRO ROXO", "BLUSA AZUL", "ESPADA MESTRE E ESCUDO VERMELHO" };
        int evoKills;                        // total de monstros derrotados no modo automatico (salvo)
        int evoStage;                        // equipamentos ja adquiridos (0 a 4)
        int evoNext;                         // proximo estagio a anunciar (1 a 4; 5 = acabou)
        Queue<int> evoQueue = new Queue<int>();
        EvoAnim evo;
        Sprite[] menuSpr = new Sprite[3];      // gorro verde, blusa verde e roupa de rei em pixel art (para o menu)
        Sprite[] evoItemSpr = new Sprite[2];      // gorro e blusa em pixel art (iguais ao corpo)
        Font evoFont;
        const float EvoFly = 1.5f;

        Color ShieldColor() { return eqShield == 1 ? Color.FromArgb(205, 50, 50) : Color.FromArgb(55, 95, 205); }
        Color BladeColor() { return eqSword == 1 ? Color.FromArgb(205, 232, 255) : Color.FromArgb(225, 235, 245); }
        Color GuardColor() { return eqSword == 1 ? Color.FromArgb(60, 95, 205) : Color.FromArgb(215, 160, 40); }
        Color GripColor() { return eqSword == 1 ? Color.FromArgb(80, 55, 150) : Color.FromArgb(135, 85, 45); }
        Color TunicColor() { return eqTunic == 2 ? Color.FromArgb(190, 35, 60) : (eqTunic == 1 ? Color.FromArgb(70, 125, 235) : Color.FromArgb(58, 165, 62)); }

        int LoadKills()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return 0;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object v = k.GetValue("Monstros"); if (v != null) return Math.Max(0, Convert.ToInt32(v)); } } } catch { }
            return 0;
        }
        void SaveKills()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("Monstros", evoKills); } catch { }
        }

        void EvoInit()
        {
            evoKills = LoadKills();
            evoStage = 0; while (evoStage < 4 && evoKills >= EvoThreshold[evoStage]) evoStage++;       // ja conquistados em outras sessoes: sem animacao
            evoNext = evoStage + 1;
        }

        // chamado a cada monstro derrotado no modo automatico
        void CountKill()
        {
            evoKills++;
            if (evoKills % 5 == 0) SaveKills();
            if (evoNext <= 4 && evoKills >= EvoThreshold[evoNext - 1]) { evoQueue.Enqueue(evoNext); evoNext++; SaveKills(); }
        }

        void UpdateEvo(float dt)
        {
            if (evo == null)
            {
                if (evoQueue.Count == 0) return;      // o anuncio aparece nos dois modos (automatico e jogavel)
                evo = new EvoAnim(); evo.Stage = evoQueue.Dequeue(); evo.T = 0; evo.Wait = true;      // o jogo pausa ate apertar "USAR ITEM"
                try
                {
                    evoForm = new EvoForm(S / 3f, Screen.PrimaryScreen.WorkingArea, W / 2f, H / 2f + 58 * S, UseEvoItem);
                    evoForm.Show();
                }
                catch { evoForm = null; UseEvoItem(); }
                return;
            }
            if (evo.Wait) return;
            evo.T += dt;
            if (evo.T >= EvoFly)
            {
                if (evo.Stage == 5) { kingGear = true; SaveChest(); } else evoStage = Math.Max(evoStage, evo.Stage);
                AutoEquip(evo.Stage);      // o item novo ja vem equipado (pode trocar no menu: Espaco)
                BuildSprites();
                for (int k = 0; k < 10; k++) puffs.Add(new Puff { Kind = k % 2, X = hx + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = hy - (float)rnd.NextDouble() * 18 * S });
                Say("Evoluí!");
                evo = null;
            }
        }

        EvoForm evoForm;
        bool EvoWaiting { get { return evo != null && evo.Wait; } }
        // botao "USAR ITEM": o jogo volta, o item diminui e voa ate o heroi
        void UseEvoItem()
        {
            if (evo != null) { evo.Wait = false; evo.T = 0; }
            if (evoForm != null) { try { evoForm.Close(); } catch { } evoForm = null; }
            CloseMenu();
        }

        // o item aparece grande no meio da tela com o numero de monstros, diminui e voa ate o heroi
        void DrawEvo(Graphics g)
        {
            if (evo == null) return;
            float t = evo.T, bigU = S * 3.4f, normU = S * 0.55f;
            float cx = W / 2f, cy = H / 2f - 10 * S, u, x, y;
            if (evo.Wait)
            {
                u = bigU * (1f + 0.04f * (float)Math.Sin(clock.Elapsed.TotalSeconds * 6)); x = cx; y = cy;      // pulsa enquanto espera
            }
            else
            {
                float q = t / EvoFly; q = q * q * (3 - 2 * q);
                u = bigU + (normU - bigU) * q;
                float tx = hx, ty = hy - 10 * S;
                x = cx + (tx - cx) * q; y = cy + (ty - cy) * q;
            }
            DrawEvoItem(g, evo.Stage, x, y, u);
            if (evo.Wait || t < 0.35f)      // textos so enquanto o item esta grande
            {
                if (evoFont == null) evoFont = new Font("Consolas", S * 9, FontStyle.Bold, GraphicsUnit.Pixel);
                string l1 = evo.Stage == 5 ? "TESOURO DO REI!" : EvoTitle[evo.Stage - 1] + "!", l2 = evo.Stage == 5 ? "COROA, ROUPA DE REI, CALÇA, SAPATOS ROXOS E ESCUDO" : EvoNeeded[evo.Stage - 1] + " MONSTROS DERROTADOS";
                SizeF s1 = g.MeasureString(l1, evoFont), s2 = g.MeasureString(l2, evoFont);
                float ty2 = cy + 17 * bigU / 1.6f;
                DrawTextF(g, l1, evoFont, W / 2f - s1.Width / 2, ty2, Color.FromArgb(255, 220, 90));
                DrawTextF(g, l2, evoFont, W / 2f - s2.Width / 2, ty2 + s1.Height + S, Color.White);
            }
        }

        // desenha o equipamento (formas em unidades u; a cor-chave nao permite transparencia, entao so cores solidas)
        void DrawEvoItem(Graphics g, int stage, float cx, float cy, float u)
        {
            if (stage == 2 || stage == 3)      // gorro / blusa: mesma arte pixel do corpo do heroi, ampliada
            {
                Sprite sp = evoItemSpr[stage - 2]; float k = u / S * 1.7f, w = sp.W * k, h = sp.H * k;
                g.DrawImage(sp.R, cx - w / 2f, cy - h / 2f, w, h);
                return;
            }
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f)))
            using (SolidBrush dk = new SolidBrush(dark))
            {
                ol.LineJoin = LineJoin.Round;
                if (stage == 1)           // arco de metal
                {
                    PointF[] pts = new PointF[13];
                    for (int i = 0; i < 13; i++) { float tt = (i - 6) / 6f; pts[i] = new PointF(cx + (5f * tt * tt - 2.5f) * u, cy + tt * 12 * u); }
                    using (Pen o2 = new Pen(dark, u * 3.6f)) { o2.StartCap = LineCap.Round; o2.EndCap = LineCap.Round; g.DrawLines(o2, pts); }
                    using (Pen m = new Pen(Color.FromArgb(185, 192, 205), u * 2f)) { m.StartCap = LineCap.Round; m.EndCap = LineCap.Round; g.DrawLines(m, pts); }
                    using (Pen h = new Pen(Color.FromArgb(240, 245, 255), Math.Max(1f, u * 0.6f))) g.DrawLines(h, pts);
                    using (Pen st = new Pen(Color.FromArgb(235, 235, 235), Math.Max(1f, u * 0.5f))) g.DrawLine(st, pts[0], pts[12]);
                    using (SolidBrush gr = new SolidBrush(Color.FromArgb(60, 70, 110))) g.FillRectangle(gr, cx + 2.4f * u - 1.5f * u, cy - 2.5f * u, 3 * u, 5 * u);
                }
                else if (stage == 2)      // gorro roxo
                {
                    using (SolidBrush p1 = new SolidBrush(Color.FromArgb(150, 80, 205)))
                    using (SolidBrush p2 = new SolidBrush(Color.FromArgb(96, 50, 140)))
                    {
                        PointF[] tail = { new PointF(cx + 4 * u, cy - 9 * u), new PointF(cx + 13 * u, cy + 3 * u), new PointF(cx + 11 * u, cy + 9 * u), new PointF(cx + 3 * u, cy + 1 * u) };
                        g.FillEllipse(dk, cx - 10 * u, cy - 11 * u, 20 * u, 17 * u);
                        g.FillEllipse(p1, cx - 9 * u, cy - 10 * u, 18 * u, 15 * u);
                        g.FillPolygon(p2, tail); g.DrawPolygon(ol, tail);
                        g.FillRectangle(dk, cx - 11 * u, cy + 2 * u, 22 * u, 5 * u);
                        g.FillRectangle(p2, cx - 10 * u, cy + 3 * u, 20 * u, 3 * u);
                        using (SolidBrush hl = new SolidBrush(Color.FromArgb(200, 150, 240))) g.FillEllipse(hl, cx - 6 * u, cy - 8 * u, 5 * u, 3 * u);
                    }
                }
                else if (stage == 3)      // blusa azul
                {
                    using (SolidBrush b1 = new SolidBrush(Color.FromArgb(70, 125, 235)))
                    using (SolidBrush b2 = new SolidBrush(Color.FromArgb(45, 85, 190)))
                    {
                        PointF[] shirt = {
                            new PointF(cx - 4 * u, cy - 10 * u), new PointF(cx + 4 * u, cy - 10 * u), new PointF(cx + 12 * u, cy - 5 * u), new PointF(cx + 9 * u, cy + 0.5f * u),
                            new PointF(cx + 6 * u, cy - 2 * u), new PointF(cx + 6 * u, cy + 10 * u), new PointF(cx - 6 * u, cy + 10 * u), new PointF(cx - 6 * u, cy - 2 * u),
                            new PointF(cx - 9 * u, cy + 0.5f * u), new PointF(cx - 12 * u, cy - 5 * u) };
                        g.FillPolygon(b1, shirt); g.DrawPolygon(ol, shirt);
                        g.FillRectangle(b2, cx - 6 * u, cy + 8 * u, 12 * u, 2 * u);
                        g.FillEllipse(dk, cx - 3 * u, cy - 11.5f * u, 6 * u, 4 * u);
                        g.FillEllipse(b2, cx - 2.2f * u, cy - 11f * u, 4.4f * u, 3 * u);
                    }
                }
                else if (stage == 5)      // tesouro do bau: coroa, manto de rei, calca e sapatos
                {
                    float uu = Math.Min(u * 0.8f, W / 125f);
                    DrawCrownItem(g, cx - 46 * uu, cy - 1 * uu, uu);
                    DrawRobeItem(g, cx - 22 * uu, cy, uu);
                    DrawPantsItem(g, cx - 1 * uu, cy, uu);
                    DrawShoesItem(g, cx + 21 * uu, cy + 2 * uu, uu);
                    DrawShieldItem(g, cx + 44 * uu, cy, uu);
                }
                else                      // espada mestre + escudo vermelho
                {
                    float sx = cx - 8 * u;
                    using (SolidBrush bl = new SolidBrush(Color.FromArgb(205, 232, 255)))
                    using (SolidBrush wh = new SolidBrush(Color.FromArgb(255, 255, 255)))
                    using (SolidBrush gu = new SolidBrush(Color.FromArgb(60, 95, 205)))
                    using (SolidBrush go = new SolidBrush(Color.FromArgb(215, 160, 40)))
                    using (SolidBrush gp = new SolidBrush(Color.FromArgb(80, 55, 150)))
                    {
                        PointF[] blade = { new PointF(sx - 2 * u, cy + 3 * u), new PointF(sx - 2 * u, cy - 15 * u), new PointF(sx, cy - 19 * u), new PointF(sx + 2 * u, cy - 15 * u), new PointF(sx + 2 * u, cy + 3 * u) };
                        g.FillPolygon(bl, blade); g.DrawPolygon(ol, blade);
                        g.FillRectangle(wh, sx - 0.4f * u, cy - 16 * u, 0.8f * u, 18 * u);
                        PointF[] guard = { new PointF(sx - 6 * u, cy + 3 * u), new PointF(sx + 6 * u, cy + 3 * u), new PointF(sx + 7 * u, cy + 6 * u), new PointF(sx - 7 * u, cy + 6 * u) };
                        g.FillPolygon(gu, guard); g.DrawPolygon(ol, guard);
                        g.FillRectangle(go, sx - 1.5f * u, cy + 3.5f * u, 3 * u, 2 * u);
                        g.FillRectangle(dk, sx - 1.5f * u, cy + 6 * u, 3 * u, 7 * u); g.FillRectangle(gp, sx - 0.8f * u, cy + 6 * u, 1.6f * u, 7 * u);
                        g.FillEllipse(go, sx - 2 * u, cy + 12.5f * u, 4 * u, 3.5f * u);
                    }
                    float hx2 = cx + 9 * u;
                    using (SolidBrush rd = new SolidBrush(Color.FromArgb(205, 50, 50)))
                    using (SolidBrush rd2 = new SolidBrush(Color.FromArgb(150, 30, 35)))
                    using (SolidBrush ye = new SolidBrush(Color.FromArgb(245, 205, 70)))
                    {
                        PointF[] sh = { new PointF(hx2 - 7 * u, cy - 8 * u), new PointF(hx2 + 7 * u, cy - 8 * u), new PointF(hx2 + 7 * u, cy + 3 * u), new PointF(hx2, cy + 12 * u), new PointF(hx2 - 7 * u, cy + 3 * u) };
                        g.FillPolygon(rd, sh); g.DrawPolygon(ol, sh);
                        g.FillRectangle(rd2, hx2 - 7 * u, cy - 8 * u, 2 * u, 11 * u);
                        g.FillRectangle(dk, hx2 - 3.4f * u, cy - 5.4f * u, 6.8f * u, 6.8f * u); g.FillRectangle(ye, hx2 - 2.6f * u, cy - 4.6f * u, 5.2f * u, 5.2f * u);
                    }
                }
            }
        }

        // ================= BAU DO REI (so no modo automatico) =================
        // Depois de 5000 monstros vermelhos (octoroks) e bokoblins derrotados (morcegos e gelatinas NAO contam) aparece um bau no cenario.
        // Ao apertar 4, o heroi vai ate ele e abre: ganha coroa de ouro, roupa de rei, calca marrom e sapatos roxos.
        const int ChestKillsNeeded = 5000;
        int chestKills, chestState;          // chestState: 0 contando | 1 bau no cenario | 2 ja aberto
        bool kingGear, chestGo, prev4;
        float chestOpenT;
        float ChestX { get { return W * 0.62f; } }
        float ChestY { get { return H * 0.34f; } }

        void ChestInit()
        {
            chestKills = 0; chestState = 0;
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") == null)
                try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object a = k.GetValue("MonstrosBau"), b = k.GetValue("Bau"); if (a != null) chestKills = Math.Max(0, Convert.ToInt32(a)); if (b != null) chestState = Math.Max(0, Math.Min(2, Convert.ToInt32(b))); } } } catch { }
            if (chestState == 0 && evoStage < 4) chestKills = 0;      // contagem so vale depois do conjunto das evolucoes
            if (chestState == 0 && chestKills >= ChestKillsNeeded) chestState = 1;
            kingGear = chestState == 2;
        }
        void SaveChest()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) { k.SetValue("MonstrosBau", chestKills); k.SetValue("Bau", chestState); } } catch { }
        }
        void CountChestKill()
        {
            if (chestState != 0 || evoStage < 4) return;      // a contagem do bau so comeca depois do conjunto das evolucoes (arco, gorro, blusa, espada e escudo) estar completo
            chestKills++;
            if (chestKills % 10 == 0) SaveChest();
            if (chestKills >= ChestKillsNeeded) { chestState = 1; SaveChest(); Say("Um baú apareceu!"); }
        }
        void UpdateChest(float dt)
        {
            if (dead || bossPhase != 0) chestGo = false;      // o bau vale nos dois modos; fica parado durante a luta com o dragao
            if (chestState == 1 && chestOpenT <= 0 && bossPhase == 0)
            {
                bool k4 = IsDown(0x34) || IsDown(0x64);      // tecla 4 (fileira de cima ou teclado numerico)
                if (k4 && !prev4 && !chestGo) { chestGo = true; if (calmState != 0) EndCalm(false); Say("Vou abrir!"); }
                prev4 = k4;
            }
            if (chestOpenT > 0)
            {
                chestOpenT += dt;
                if (chestOpenT >= 1.4f) { chestOpenT = 0; chestState = 2; chestGo = false; SaveChest(); evoQueue.Enqueue(5); }      // o tesouro aparece grande (como as evolucoes)
            }
        }
        void UpdateChestHero(float dt)
        {
            float tx = ChestX, ty = ChestY + 10 * S;
            if (chestOpenT <= 0)
            {
                MoveHero(tx, ty, 56 * S, dt);
                if (walking) walkT += dt;
                float d2 = (tx - hx) * (tx - hx) + (ty - hy) * (ty - hy);
                if (d2 < (3 * S) * (3 * S)) { chestOpenT = 0.001f; hface = ChestX >= hx ? 1 : -1; }
            }
            hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx)); hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
        }

        void DrawChest(Graphics g)
        {
            if (chestState != 1 || bossPhase != 0) return;
            float x = ChestX, y = ChestY;
            float open = chestOpenT > 0 ? Math.Min(1f, chestOpenT / 0.5f) : 0f;
            Shadow(g, x, y, 22);
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush wood = new SolidBrush(Color.FromArgb(150, 95, 45)))
            using (SolidBrush wood2 = new SolidBrush(Color.FromArgb(115, 70, 32)))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 205, 40)))
            using (SolidBrush gold2 = new SolidBrush(Color.FromArgb(200, 140, 20)))
            {
                g.FillRectangle(k, x - 10 * S, y - 10 * S, 20 * S, 11 * S);                 // corpo
                g.FillRectangle(wood, x - 9 * S, y - 9 * S, 18 * S, 9 * S);
                g.FillRectangle(wood2, x - 9 * S, y - 4 * S, 18 * S, S);
                g.FillRectangle(gold, x - 7 * S, y - 9 * S, 2 * S, 9 * S); g.FillRectangle(gold, x + 5 * S, y - 9 * S, 2 * S, 9 * S);
                if (open <= 0)
                {
                    g.FillRectangle(k, x - 10 * S, y - 17 * S, 20 * S, 8 * S);             // tampa fechada
                    g.FillRectangle(wood, x - 9 * S, y - 16 * S, 18 * S, 6 * S);
                    g.FillRectangle(gold, x - 7 * S, y - 16 * S, 2 * S, 6 * S); g.FillRectangle(gold, x + 5 * S, y - 16 * S, 2 * S, 6 * S);
                    g.FillRectangle(k, x - 2 * S, y - 12 * S, 4 * S, 5 * S); g.FillRectangle(gold, x - S, y - 11 * S, 2 * S, 3 * S);
                }
                else
                {
                    float lift = open * 9 * S;
                    g.FillRectangle(k, x - 10 * S, y - 17 * S - lift, 20 * S, 3 * S); g.FillRectangle(wood, x - 9 * S, y - 16 * S - lift, 18 * S, 2 * S);   // tampa levantada
                    g.FillRectangle(gold2, x - 8 * S, y - 10 * S, 16 * S, 2 * S);
                    for (int i = 0; i < 12; i++)
                    {
                        double a = time * 3 + i; float h = ((i * 7) % 5) * 3 * S + (float)((time * 40 + i * 9) % 20) * S * 0.6f;
                        g.FillRectangle(gold, x + (float)Math.Cos(a) * 8 * S, y - 12 * S - h * open, 2 * S, 2 * S);
                    }
                }
            }
        }
        void DrawChestPrompt(Graphics g)
        {
            if (chestState != 1 || bossPhase != 0 || chestOpenT > 0 || chestGo) return;
            string t = "APERTE 4 PARA ABRIR";
            SizeF sz = g.MeasureString(t, font);
            float pad = 2 * S, x = ChestX - sz.Width / 2, y = ChestY - 17 * S - sz.Height - 4 * S - (float)Math.Abs(Math.Sin(time * 3)) * 2 * S;
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(46, 40, 62)))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            {
                g.FillRectangle(k, x - pad - S, y - pad - S, sz.Width + 2 * pad + 2 * S, sz.Height + 2 * pad + 2 * S);
                g.FillRectangle(bg, x - pad, y - pad, sz.Width + 2 * pad, sz.Height + 2 * pad);
                g.FillRectangle(gold, x - pad, y - pad, sz.Width + 2 * pad, S);
            }
            DrawText(g, t, x, y, Color.FromArgb(255, 220, 90));
        }

        // itens do tesouro (usados no anuncio grande)
        // escudo do heroi: normal (retangular) ou, com o tesouro do bau, circular roxo escuro com um quadrado azul (um pouco menor, cobre menos o personagem)
        void DrawShieldBody(Graphics g, float sx, float sy)
        {
            if (eqShield == 2)
            {
                float cx = sx + 3 * S, cy = sy + 4 * S, ro = 3.6f * S, ri = 2.8f * S;
                using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
                using (SolidBrush pu = new SolidBrush(Color.FromArgb(70, 30, 110)))
                using (SolidBrush bl = new SolidBrush(Color.FromArgb(70, 125, 235)))
                {
                    g.FillEllipse(kb, cx - ro, cy - ro, 2 * ro, 2 * ro);
                    g.FillEllipse(pu, cx - ri, cy - ri, 2 * ri, 2 * ri);
                    g.FillRectangle(kb, cx - 1.7f * S, cy - 1.7f * S, 3.4f * S, 3.4f * S);
                    g.FillRectangle(bl, cx - 1.3f * S, cy - 1.3f * S, 2.6f * S, 2.6f * S);
                }
                return;
            }
            using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush bb = new SolidBrush(ShieldColor()))
            using (SolidBrush yb = new SolidBrush(Color.FromArgb(245, 205, 70)))
            {
                g.FillRectangle(kb, sx - S, sy - S, 8 * S, 10 * S);
                g.FillRectangle(bb, sx, sy, 6 * S, 8 * S);
                g.FillRectangle(yb, sx + 2 * S, sy + 2 * S, 2 * S, 3 * S);
            }
        }

        void DrawShieldItem(Graphics g, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (SolidBrush kb = new SolidBrush(dark))
            using (SolidBrush pu = new SolidBrush(Color.FromArgb(70, 30, 110)))
            using (SolidBrush rim = new SolidBrush(Color.FromArgb(110, 60, 160)))
            using (SolidBrush bl = new SolidBrush(Color.FromArgb(70, 125, 235)))
            {
                g.FillEllipse(kb, cx - 8.5f * u, cy - 8.5f * u, 17 * u, 17 * u);
                g.FillEllipse(rim, cx - 7.5f * u, cy - 7.5f * u, 15 * u, 15 * u);
                g.FillEllipse(pu, cx - 6.4f * u, cy - 6.4f * u, 12.8f * u, 12.8f * u);
                g.FillRectangle(kb, cx - 3.8f * u, cy - 3.8f * u, 7.6f * u, 7.6f * u);
                g.FillRectangle(bl, cx - 3 * u, cy - 3 * u, 6 * u, 6 * u);
            }
        }
        void DrawCrownItem(Graphics g, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f)))
            using (SolidBrush go = new SolidBrush(Color.FromArgb(255, 205, 40)))
            using (SolidBrush go2 = new SolidBrush(Color.FromArgb(215, 150, 20)))
            using (SolidBrush red = new SolidBrush(Color.FromArgb(215, 40, 60)))
            using (SolidBrush blu = new SolidBrush(Color.FromArgb(70, 125, 235)))
            {
                ol.LineJoin = LineJoin.Round;
                PointF[] cr = { new PointF(cx - 9 * u, cy + 4 * u), new PointF(cx - 9 * u, cy - 5 * u), new PointF(cx - 4.5f * u, cy - 1 * u), new PointF(cx, cy - 7 * u), new PointF(cx + 4.5f * u, cy - 1 * u), new PointF(cx + 9 * u, cy - 5 * u), new PointF(cx + 9 * u, cy + 4 * u) };
                g.FillPolygon(go, cr); g.DrawPolygon(ol, cr);
                g.FillRectangle(go2, cx - 9 * u, cy + 1.5f * u, 18 * u, 2.5f * u);
                g.FillEllipse(red, cx - 1.5f * u, cy - 2 * u, 3 * u, 3 * u); g.FillEllipse(blu, cx - 6.5f * u, cy - 0.5f * u, 2 * u, 2 * u); g.FillEllipse(blu, cx + 4.5f * u, cy - 0.5f * u, 2 * u, 2 * u);
                foreach (float px in new float[] { -9f, 0f, 9f }) g.FillEllipse(go, cx + (px - 1) * u, cy + (px == 0 ? -8f : -6f) * u, 2 * u, 2 * u);
            }
        }
        void DrawRobeItem(Graphics g, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f)))
            using (SolidBrush rd = new SolidBrush(Color.FromArgb(190, 35, 60)))
            using (SolidBrush rd2 = new SolidBrush(Color.FromArgb(140, 20, 45)))
            using (SolidBrush fur = new SolidBrush(Color.FromArgb(245, 245, 250)))
            using (SolidBrush go = new SolidBrush(Color.FromArgb(255, 205, 40)))
            using (SolidBrush dk = new SolidBrush(dark))
            {
                ol.LineJoin = LineJoin.Round;
                PointF[] robe = { new PointF(cx - 5 * u, cy - 10 * u), new PointF(cx + 5 * u, cy - 10 * u), new PointF(cx + 12 * u, cy - 5 * u), new PointF(cx + 9 * u, cy + 0.5f * u), new PointF(cx + 7 * u, cy - 2 * u), new PointF(cx + 8 * u, cy + 12 * u), new PointF(cx - 8 * u, cy + 12 * u), new PointF(cx - 7 * u, cy - 2 * u), new PointF(cx - 9 * u, cy + 0.5f * u), new PointF(cx - 12 * u, cy - 5 * u) };
                g.FillPolygon(rd, robe); g.DrawPolygon(ol, robe);
                g.FillRectangle(go, cx - 7.5f * u, cy + 3 * u, 15 * u, 2 * u);                 // cinto dourado
                g.FillRectangle(go, cx - 8 * u, cy + 10 * u, 16 * u, 2 * u);                   // barra dourada
                g.FillEllipse(dk, cx - 6 * u, cy - 12 * u, 12 * u, 5 * u); g.FillEllipse(fur, cx - 5.2f * u, cy - 11.6f * u, 10.4f * u, 4.2f * u);      // gola de pele
                for (int i = 0; i < 3; i++) g.FillRectangle(dk, cx + (-3 + i * 3) * u, cy - 10.2f * u, u, u);
            }
        }
        void DrawPantsItem(Graphics g, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f)))
            using (SolidBrush br = new SolidBrush(Color.FromArgb(150, 100, 55)))
            using (SolidBrush br2 = new SolidBrush(Color.FromArgb(110, 70, 35)))
            {
                ol.LineJoin = LineJoin.Round;
                PointF[] p = { new PointF(cx - 7 * u, cy - 9 * u), new PointF(cx + 7 * u, cy - 9 * u), new PointF(cx + 8 * u, cy + 11 * u), new PointF(cx + 1 * u, cy + 11 * u), new PointF(cx, cy - 2 * u), new PointF(cx - 1 * u, cy + 11 * u), new PointF(cx - 8 * u, cy + 11 * u) };
                g.FillPolygon(br, p); g.DrawPolygon(ol, p);
                g.FillRectangle(br2, cx - 7 * u, cy - 9 * u, 14 * u, 2.5f * u);
            }
        }
        void DrawShoesItem(Graphics g, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            using (Pen ol = new Pen(dark, Math.Max(1f, u * 0.7f)))
            using (SolidBrush pu = new SolidBrush(Color.FromArgb(130, 60, 190)))
            using (SolidBrush pu2 = new SolidBrush(Color.FromArgb(90, 40, 140)))
            {
                ol.LineJoin = LineJoin.Round;
                foreach (float off in new float[] { -6f, 6f })
                {
                    float bx = cx + off * u;
                    PointF[] b = { new PointF(bx - 4 * u, cy - 8 * u), new PointF(bx + 2 * u, cy - 8 * u), new PointF(bx + 2 * u, cy), new PointF(bx + 6 * u, cy + 2 * u), new PointF(bx + 6 * u, cy + 6 * u), new PointF(bx - 4 * u, cy + 6 * u) };
                    g.FillPolygon(pu, b); g.DrawPolygon(ol, b);
                    g.FillRectangle(pu2, bx - 4 * u, cy + 4 * u, 10 * u, 2 * u);
                }
            }
        }
        // ================= MODO PODER: ao bater o recorde de rupias do automatico =================
        int record;                         // maximo de rupias ja conseguido no modo automatico (salvo no registro)
        int nextPower;                      // rupias necessarias para disparar o proximo poder
        float powerT, powerAng, powerShootT, powerSpinT;      // powerT > 0: poder ativo (15 s)
        Sprite[] heroPow = new Sprite[2];

        // O recorde nunca diminui: fica no registro E num arquivo de reserva (%APPDATA%\HeroiDoDesktop\recorde.txt); vale o maior dos dois.
        // Com a variavel de ambiente HEROI_TESTE definida (testes automaticos) nada e lido nem gravado.
        static string RecordFile { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"HeroiDoDesktop\recorde.txt"); } }
        int LoadRecord()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return 0;
            int best = 0;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object v = k.GetValue("Recorde"); if (v != null) best = Math.Max(best, Convert.ToInt32(v)); } } } catch { }
            try { if (System.IO.File.Exists(RecordFile)) { int v; if (int.TryParse(System.IO.File.ReadAllText(RecordFile).Trim(), out v)) best = Math.Max(best, v); } } catch { }
            return best;
        }
        // Regra pedida: o recorde zera quando passa de 80 (grava 0 no registro e no arquivo)
        void ResetRecord()
        {
            record = 0; nextPower = 25;
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("Recorde", 0); } catch { }
            try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RecordFile)); System.IO.File.WriteAllText(RecordFile, "0"); } catch { }
        }
        void SaveRecord()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            int best = Math.Max(record, LoadRecord());          // nunca grava um valor menor que o ja guardado
            record = best;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("Recorde", best); } catch { }
            try { System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(RecordFile)); System.IO.File.WriteAllText(RecordFile, best.ToString()); } catch { }
        }

        // copia do sprite em azul brilhante (contorno escuro continua escuro)
        Sprite TintBlue(Sprite s)
        {
            Sprite t = new Sprite(); t.W = s.W; t.H = s.H;
            Bitmap[] src = { s.R, s.L }; Bitmap[] dst = new Bitmap[2];
            for (int i = 0; i < 2; i++)
            {
                Bitmap b = new Bitmap(src[i].Width, src[i].Height, PixelFormat.Format32bppArgb);
                for (int y = 0; y < b.Height; y++)
                    for (int x = 0; x < b.Width; x++)
                    {
                        Color c = src[i].GetPixel(x, y);
                        if (c.A == 0) continue;
                        int lum = (c.R + c.G + c.B) / 3;
                        b.SetPixel(x, y, lum < 60 ? Color.FromArgb(10, 20, 80) : Color.FromArgb(70 + lum / 5, 170 + lum / 6, 255));
                    }
                dst[i] = b;
            }
            t.R = dst[0]; t.L = dst[1];
            return t;
        }

        const int SlimesForPower = 20;       // gelatinas que o heroi precisa derrotar para lancar o poder
        int slimeKills;                      // gelatinas (slimes) derrotadas desde o ultimo poder (zera ao chegar a 20)
        bool powerPending;                   // 20 gelatinas feitas: o poder comeca assim que o heroi estiver livre
        bool lootHold;                     // do inicio do poder ate pegar tudo que caiu: itens nao somem e o lanche espera

        void UpdatePower(float dt)
        {
            if (lootHold && (playMode || (powerT <= 0 && items.Count == 0))) { lootHold = false; nextPower = Math.Max(nextPower, record + 1); }      // o que foi pego depois do poder nao dispara outro poder na hora
            if (playMode) { powerPending = false; slimeKills = 0; if (powerT > 0) { powerT = 0; enemies.Clear(); rocks.Clear(); arrows.Clear(); inv = 0; } return; }      // entrou no modo jogo: poder acaba em silencio, sem recompensa
            if (rupees > record) { record = rupees; SaveRecord(); }
            if (record > 80) ResetRecord();
            if (powerT <= 0)
            {
                if (powerPending && !lootHold && !dead && cookState == 0 && bossPhase == 0 && !paused) { powerPending = false; StartPower(); }
                return;
            }
            powerT -= dt;
            if (powerT <= 0) EndPower();
        }

        void StartPower()
        {
            if (calmState != 0) EndCalm(false);
            powerT = 15f; powerAng = 0; powerShootT = 0; powerSpinT = 0; lootHold = true;
            attacking = false; bowing = false; inv = 0;
            spawnTimer = 0.8f;
            for (int k = 0; k < 9; k++) Spawn(SpawnType());      // 18x mais monstros: ja comeca com uma leva
            Say("PODER!");
            for (int k = 0; k < 8; k++) puffs.Add(new Puff { Kind = k % 2, X = hx + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = hy - (float)rnd.NextDouble() * 14 * S });
        }

        // fim do poder: os monstros que sobraram explodem, ele colhe macas (e depois assa e come, pelo ciclo normal do lanche)
        void EndPower()
        {
            powerT = 0;
            if (rupees > record) record = rupees;
            SaveRecord();
            if (record > 80) ResetRecord();
            rupees = 0; nextPower = Math.Max(25, record + 1);          // as rupias zeram; o proximo poder exige bater o recorde de novo
            foreach (Enemy e in enemies)
                for (int k = 0; k < 4; k++)
                    puffs.Add(new Puff { Kind = k % 2, X = e.X + ((float)rnd.NextDouble() - 0.5f) * 10 * S, Y = e.Y - (float)rnd.NextDouble() * 10 * S });
            enemies.Clear(); rocks.Clear(); arrows.Clear();
            spawnTimer = 16f; inv = 0f; attacking = false; bowing = false;      // inv = 0: durante o lanche o heroi nao pode ficar piscando
            int need = 3;                                                        // durante o poder so da para pegar 3 macas: o lanche so acontece quando juntar 5 ou mais
            int got = 0;
            foreach (Tree t in trees)
                for (int i = 0; i < 5 && got < need; i++)
                    if (t.Has[i])
                    {
                        t.Has[i] = false; t.Regrow[i] = 60f + (float)rnd.NextDouble() * 40f;
                        flyApples.Add(new FlyApple { X0 = AppleX(t, i), Y0 = AppleY(t, i) });
                        got++;
                    }
            Say("Uff!");
        }

        void UpdatePowerHero(float dt)
        {
            powerAng += dt * 1.0f; powerSpinT += dt;
            float cx = W / 2f, cy = H / 2f, rx = W * 0.36f, ry = H * 0.34f;
            hx = cx + rx * (float)Math.Cos(powerAng);
            hy = cy + ry * (float)Math.Sin(powerAng * 1.3f);
            hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx)); hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
            hface = ((int)(powerSpinT * 12)) % 2 == 0 ? 1 : -1;       // gira
            walking = true; walkT += dt;
            powerShootT -= dt;
            while (powerShootT <= 0)            // pode sair mais de um projetil por quadro, para manter 5x mais que antes
            {
                powerShootT += 0.05f;                                    // 20 projeteis azuis por segundo (antes 5/s)
                Enemy tg = null; float bd = float.MaxValue;
                foreach (Enemy e in enemies) { float d2 = (e.X - hx) * (e.X - hx) + (e.Y - hy) * (e.Y - hy); if (d2 < bd) { bd = d2; tg = e; } }
                double ang = tg != null ? Math.Atan2((tg.Y - (tg.Type == 1 ? 9 : 4) * S) - (hy - 8 * S), tg.X - hx) + (rnd.NextDouble() - 0.5) * 0.3 : powerSpinT * 9;
                float sp = 260 * S;
                arrows.Add(new Arrow { X = hx, Y = hy - 8 * S, VX = (float)Math.Cos(ang) * sp, VY = (float)Math.Sin(ang) * sp, Power = true });
            }
        }

        void DrawPowerHero(Graphics g)
        {
            Shadow(g, hx, hy, 12);
            bool right = hface > 0;
            int fr = ((int)(powerSpinT * 10)) % 2;
            // aura: faiscas girando em volta
            Color[] cc = { Color.FromArgb(255, 255, 255), Color.FromArgb(255, 235, 120), Color.FromArgb(255, 190, 60) };      // faiscas douradas: o heroi nao fica azul
            for (int k = 0; k < 10; k++)
            {
                double a = powerSpinT * 7 + k * Math.PI * 2 / 10;
                float rr = (12 + 3 * (float)Math.Sin(powerSpinT * 15 + k)) * S;
                using (SolidBrush b = new SolidBrush(cc[k % 3]))
                    g.FillRectangle(b, hx + (float)Math.Cos(a) * rr - S, hy - 8 * S + (float)Math.Sin(a) * rr * 0.8f - S, 2 * S, 2 * S);
            }
            Spr(g, heroWalk[fr], hx, hy - (fr == 1 ? S : 0), right);       // cores normais do heroi
        }

        void DrawPowerArrow(Graphics g, Arrow a)
        {
            float l = (float)Math.Sqrt(a.VX * a.VX + a.VY * a.VY); if (l < 1) return;
            float ux = a.VX / l, uy = a.VY / l;
            Color[] cc = { Color.FromArgb(255, 255, 255), Color.FromArgb(120, 230, 255), Color.FromArgb(50, 150, 255), Color.FromArgb(30, 90, 220) };
            for (int k = 0; k < 4; k++)
                using (SolidBrush b = new SolidBrush(cc[k]))
                    g.FillRectangle(b, a.X - ux * k * 2.2f * S - S, a.Y - uy * k * 2.2f * S - S, 2 * S, 2 * S);
        }

                // ================= LANCHE: fogueirinha + macas assadas =================
        int cookState, throwIdx, lastEat;      // 0 nada | 1 vai ao meio | 2 acende | 3 joga | 4 assa (10 s) | 5 pega | 6 come | 7 apaga
        float cookT, bakeT, fireK, fireX, fireY;
        bool[] onFire = new bool[5];
        List<CookApple> cookApples = new List<CookApple>();
        Fire fire = new Fire();
        Sprite[] appleBake = new Sprite[4];

        // pilha: 3 maçãs embaixo (0,1,2) e 2 em cima (3,4)
        float SlotX(int i) { return i < 3 ? fireX + (i - 1) * 6.2f * S : fireX + (i - 3.5f) * 6.2f * S; }
        float SlotY(int i) { return fireY - 3.5f * S - (i < 3 ? 0 : 5.2f * S); }
        int BakeIdx() { return cookState >= 5 ? 3 : Math.Min(3, (int)(bakeT / 10f * 4f)); }

        void ShowToast()
        {
            try
            {
                Toast t = new Toast("Link juntou 5 maçãs!", "Ele vai assar e comer um lanche.", S / 3f);
                t.Show();
            }
            catch { }
        }

        void AbortCook()
        {
            if (cookState >= 3 && cookState <= 5) apples += 5;       // devolve as macas (ele tenta de novo depois)
            cookState = 0; fireK = 0; cookApples.Clear();
            for (int i = 0; i < 5; i++) onFire[i] = false;
        }

        void LaunchCookApple(float x0, float y0, float x1, float y1, int idx, int dir)
        {
            CookApple a = new CookApple();
            a.X0 = x0; a.Y0 = y0; a.X1 = x1; a.Y1 = y1; a.Idx = idx; a.Dir = dir; a.Dur = 0.6f;
            cookApples.Add(a);
        }

        void UpdateCookApples(float dt)
        {
            for (int i = cookApples.Count - 1; i >= 0; i--)
            {
                cookApples[i].T += dt;
                if (cookApples[i].T >= cookApples[i].Dur)
                {
                    if (cookApples[i].Dir == 1) { onFire[cookApples[i].Idx] = true; reserved[cookApples[i].Idx] = false; }
                    cookApples.RemoveAt(i);
                }
            }
        }

        void UpdateCook(float dt)
        {
            cookT += dt;
            if (enemies.Count > 0) { AbortCook(); return; }
            float handX = hx + hface * 6 * S, handY = hy - 10 * S;
            if (cookState == 1)
            {
                MoveHero(fireX - 14 * S, fireY + 3 * S, 44 * S, dt);
                if (walking) walkT += dt;
                else { cookState = 2; cookT = 0; hface = 1; Say("Hora do lanche!"); }
            }
            else if (cookState == 2)
            {
                fireK = Math.Min(1f, cookT / 1.6f);
                if (cookT >= 1.9f) { cookState = 3; cookT = 0; throwIdx = 0; apples = Math.Max(0, apples - 5); }
            }
            else if (cookState == 3)
            {
                while (throwIdx < 5 && cookT >= throwIdx * 0.4f)
                {
                    LaunchCookApple(handX, handY, SlotX(throwIdx), SlotY(throwIdx), throwIdx, 1);
                    throwIdx++;
                }
                if (throwIdx >= 5 && cookApples.Count == 0) { cookState = 4; cookT = 0; bakeT = 0; }
            }
            else if (cookState == 4)
            {
                bakeT += dt;
                if (bakeT >= 5f && bakeT < 5.1f && speechT <= 0) Say("Que cheiro bom...");
                if (bakeT >= 10f) { cookState = 5; cookT = 0; throwIdx = 0; Say("Prontas!"); }
            }
            else if (cookState == 5)
            {
                while (throwIdx < 5 && cookT >= throwIdx * 0.35f)
                {
                    int kk = 4 - throwIdx;          // pega primeiro as de cima
                    onFire[kk] = false;
                    LaunchCookApple(SlotX(kk), SlotY(kk), handX, handY, kk, -1);
                    throwIdx++;
                }
                if (throwIdx >= 5 && cookApples.Count == 0) { cookState = 6; cookT = 0; lastEat = -1; }
            }
            else if (cookState == 6)
            {
                int idx = (int)(cookT / 1.2f);
                if (idx != lastEat)
                {
                    if (lastEat >= 0 && hp < MaxHP) hp = Math.Min(MaxHP, hp + 1f);           // terminou de comer uma maca: recupera 1 coracao
                    lastEat = idx;
                    if (idx < 5) Say("Nham!");
                }
                if (cookT >= 6f) { cookState = 7; cookT = 0; Say("Que delícia!"); }
            }
            else if (cookState == 7)
            {
                fireK = Math.Max(0f, 1f - cookT / 1.5f);
                if (cookT >= 1.5f) { cookState = 0; fireK = 0; }
            }
            hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx));
            hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
        }

                // ================= MODO JOGO =================
        // Mouse apertado: anda ate o cursor | 1: espada | 2: arco (mira no cursor) | 3: colhe maca
        // A janela deixa os cliques passarem, entao o mouse e o teclado sao lidos direto do Windows.
        bool playMode, pickedFlag, prev3;
        ClickShield shield;
        ToolStripMenuItem mAutoItem, mPlayItem;
        int lastStep;
        float playPickT;
        Tree playTree;
        static Func<int, bool> KeyHook;          // (so para testes automaticos)
        static Func<Point> CursorHook;

        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
        bool IsDown(int vk)
        {
            if (KeyHook != null) return KeyHook(vk);
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        // valores: 1 verde, 5 azul, 20 vermelha, 10 amarela, 50 roxa. No modo jogo so caem as de 5 a 50, para nao demorar.
        static int RupeeValue(int kind) { switch (kind) { case 1: return 1; case 2: return 5; case 3: return 20; case 4: return 10; case 6: return 300; default: return 50; } }
        int RupeeKind(int r)
        {
            if (!playMode)
            {
                if (record >= 50) return r < 30 ? 1 : (r < 60 ? 2 : (r < 80 ? 4 : (r < 95 ? 3 : 5)));      // recorde >= 50: 30% de 1, 30% de 5, 20% de 10, 15% de 20, 5% de 50
                return r < 60 ? 1 : (r < 90 ? 2 : 3);
            }
            return r < 40 ? 2 : (r < 70 ? 4 : (r < 90 ? 3 : 5));       // 40% de 5, 30% de 10, 20% de 20, 10% de 50
        }
        int RupeeStep() { return Math.Min(6, rupees / 25); }       // cada 25 rupias = +10% de monstros, ate +60%

        // Quantidade de monstros: comeca em 2/3 do maximo da animacao (4) e cresce 10% (do inicial) a cada 25 rupias
        int EnemyCap()
        {
            if (!playMode) return powerT > 0 ? 72 : 4;       // poder: 18x mais monstros
            float cap = 4f * 2f / 3f * (1f + 0.1f * RupeeStep());
            int b = (int)Math.Floor(cap);
            return b + (rnd.NextDouble() < cap - b ? 1 : 0);
        }
        float SpawnRateMul() { return playMode ? 1f + 0.1f * RupeeStep() : (powerT > 0 ? 18f : 1f); }

        // depois de +30%, cada novo aumento de monstros da um coracao (vida maxima +1)
        void CheckRupeeLevel()
        {
            int step = RupeeStep();
            if (step <= lastStep) return;
            for (int s = lastStep + 1; s <= step; s++)
                if (s >= 4 && MaxHP < 8)
                {
                    MaxHP++; hp = Math.Min(MaxHP, hp + 1);
                    puffs.Add(new Puff { Kind = 1, X = hx, Y = hy - 10 * S });
                    Say("+1 coração!");
                }
            lastStep = step;
        }

        void SetMaxHp(int max)
        {
            int old = MaxHP;
            MaxHP = max;
            if (max > old) hp = Math.Min(MaxHP, hp + (max - old));          // ganhou coracoes: vem cheios
            else hp = Math.Min(hp, MaxHP);
        }

        // Ao entrar no modo jogo: o heroi vai para o meio da tela e os monstros do modo automatico somem
        void EnterPlay()
        {
            foreach (Enemy e in enemies) puffs.Add(new Puff { Kind = 0, X = e.X, Y = e.Y - 4 * S });
            enemies.Clear(); rocks.Clear(); arrows.Clear();
            if (cookState != 0) AbortCook();
            if (dead) { dead = false; hp = MaxHP; }
            hx = W / 2; hy = H / 2; kvx = kvy = 0; inv = 1f; walking = false;
            puffs.Add(new Puff { Kind = 2, X = hx, Y = hy });
            rupees = 0; apples = 0; flyApples.Clear(); toastShown = false;     // o que foi juntado no automatico some
            spawnTimer = 2.5f;
        }

        void SetPlayMode(bool on)
        {
            bool was = playMode;
            playMode = on;
            if (mAutoItem != null) { mAutoItem.Checked = !on; mPlayItem.Checked = on; }
            attacking = false; bowing = false; bowT = 0; playPickT = 0;
            if (!on && paused) { paused = false; if (pauseItem != null) pauseItem.Text = "Pausar"; }
            CloseDeath();
            ResetBoss();
            if (on)
            {
                if (!was) EnterPlay(); if (calmState != 0) EndCalm(false);          // sem acenar nem dormir no modo jogo
                if (shield == null) { shield = new ClickShield(); shield.Show(); }      // cliques nao chegam mais na area de trabalho
                lastStep = RupeeStep();
                SetMaxHp(5 + Math.Max(0, Math.Min(3, lastStep - 3)));      // os coracoes extras do nivel voltam no modo jogo
                try { new Toast("Modo jogo ligado", "Mouse: andar | 1 espada | 2 arco | 3 colher", S / 3f).Show(); } catch { }
            }
            else
            {
                if (shield != null) { shield.Close(); shield = null; }
                SetMaxHp(5);                                                // no automatico os coracoes extras sao perdidos
                rupees = 0; lastStep = 0;                                    // e as rupias tambem voltam a zero
                nextCalm = 120f + (float)rnd.NextDouble() * 180f;
            }
        }

        void PlayerControl(float dt)
        {
            bowCool -= dt;
            Point cp = CursorHook != null ? CursorHook() : Cursor.Position;
            float cx = cp.X - Left, cy = cp.Y - Top;
            bool mouse = IsDown(0x01) && !IsDown(0x11);     // com Ctrl apertado o mouse e do Windows, o Link nao anda
            bool k1 = IsDown(0x31) || IsDown(0x61), k2 = IsDown(0x32) || IsDown(0x62), k3 = IsDown(0x33) || IsDown(0x63);
            if (playEatT > 0)                        // comendo uma maca assada: recupera 2 coracoes
            {
                playEatT += dt;
                if (playEatT >= 0.9f) { playEatT = 0; hp = Math.Min(MaxHP, hp + 2); atePhase1 = true; Say("Nham!"); }
                prev3 = k3;
                return;
            }

            if (playPickT > 0)                       // esta colhendo uma maca
            {
                playPickT += dt; pickT = playPickT;
                if (!pickedFlag && playPickT >= 0.4f && playTree != null)
                {
                    int idx = FirstApple(playTree);
                    if (idx >= 0)
                    {
                        playTree.Has[idx] = false; playTree.Regrow[idx] = 60f + (float)rnd.NextDouble() * 40f;
                        flyApples.Add(new FlyApple { X0 = AppleX(playTree, idx), Y0 = AppleY(playTree, idx) });
                    }
                    pickedFlag = true;
                }
                if (playPickT >= 0.8f) playPickT = 0;
                prev3 = k3;
                return;
            }
            if (k3 && !prev3 && bossPhase == 1 && FireInteract()) { prev3 = true; return; }
            if (k3 && !prev3)
            {
                Tree best = null; float bd = 18 * S;
                foreach (Tree t in trees)
                {
                    if (FirstApple(t) < 0) continue;
                    float d = (float)Math.Sqrt((t.X - hx) * (t.X - hx) + (t.Y - hy) * (t.Y - hy));
                    if (d < bd) { bd = d; best = t; }
                }
                prev3 = true;
                if (best != null)
                {
                    playTree = best; playPickT = 0.001f; pickedFlag = false; pickT = 0;
                    hface = best.X >= hx ? 1 : -1;
                    return;
                }
                Say("Sem maçãs aqui");
            }
            prev3 = k3;

            if (k1 && atkCool <= 0)                  // 1: espada, virado para o cursor
            {
                if (Math.Abs(cx - hx) > 2) hface = cx >= hx ? 1 : -1;
                StartAttack(false);
                return;
            }
            if (k2)                                  // 2: arco apontado para o cursor
            {
                bowing = true;
                float sx = hx + hface * 7 * S, sy = hy - 8 * S;
                float ax = cx - sx, ay = cy - sy, al = (float)Math.Sqrt(ax * ax + ay * ay);
                if (al < 1) { ax = hface; ay = 0; al = 1; }
                aimX = ax / al; aimY = ay / al;
                if (Math.Abs(aimX) > 0.15f) hface = aimX > 0 ? 1 : -1;
                bowT += dt;
                if (bowT >= 0.4f && bowCool <= 0)
                {
                    arrows.Add(new Arrow { X = sx, Y = sy, VX = aimX * 210 * S, VY = aimY * 210 * S });
                    bowT = 0; bowCool = 0.35f;
                }
            }
            else bowT = 0;

            if (mouse)                               // mouse apertado: segue o cursor
            {
                float d = (float)Math.Sqrt((cx - hx) * (cx - hx) + (cy - hy) * (cy - hy));
                if (d > 3 * S)
                {
                    int keep = hface;
                    MoveHero(cx, cy, 56 * S, dt);
                    if (bowing) hface = keep;
                }
            }
        }

        int hurtCount, killCount;      // estatisticas (usadas nos testes)
                // ================= CHEFAO: DRAGAO (so no modo jogo) =================
        // No nivel maximo (150 rupias) e depois de derrotar 5 monstros: os que sobram explodem, aparece uma fogueira,
        // o jogador colhe macas (tecla 3), poe na fogueira e come (tecla 3) ate encher a vida. Ai a fogueira explode e sai o dragao.
        int bossPhase, killsAtMax;      // 0 normal | 1 fogueira de cura | 2 heroi se afasta | 3 explosao e fumaca | 4 luta | 5 vitoria
        float bossT, playEatT, leaveX, leaveY, bossDeadX, bossDeadY;
        int bossDeadFace = 1; bool rewardSpawned; float msgT;      // fim do dragao: fantasma normal, explosao com 1000 rupias, mensagem
        bool atePhase1, endStarted, bossSpawned;
        float[] bakeTs = new float[5];
        bool[] reserved = new bool[5];
        Enemy boss;
        List<Smoke> smokes = new List<Smoke>();
        EndShow endShow;
        Sprite[] dragonSpr = new Sprite[2], dragonSprRed = new Sprite[2], dragonSprGlow = new Sprite[2], fireSpr = new Sprite[2];
        float DK = 1.7f;                  // fator de tamanho do dragao em relacao ao tamanho normal

        bool DragonAlive() { return boss != null && enemies.Contains(boss); }

        // Atalho de teste: liga o modo jogo no nivel maximo e pula direto para o aviso de colher macas (vida baixa para testar a cura)
        // PERIODO DE TESTE: coloca o jogador logo antes da luta com o dragao (modo jogo, nivel maximo, vida cheia, fogueira ja explodindo)
        void StartDragonTest()
        {
            if (!playMode) SetPlayMode(true); else ResetBoss();
            enemies.Clear(); rocks.Clear(); arrows.Clear(); items.Clear(); puffs.Clear();
            rupees = 150; lastStep = RupeeStep(); SetMaxHp(8); hp = MaxHP; dead = false; deathWait = false; inv = 2f;
            apples = 0; powerT = 0; evo = null; hface = 1;
            fireX = W / 2; fireY = H / 2;
            hx = W * 0.15f; hy = H * 0.6f;
            bossPhase = 3; bossT = 0; bossSpawned = false;           // o dragao nasce em ~1 s, saindo da fumaça da fogueira
            FireExplode();
            Say("Hora da luta!");
        }

        void StartBossTest()
        {
            rupees = 150;
            if (!playMode) SetPlayMode(true);
            else { ResetBoss(); SetMaxHp(8); lastStep = 6; }
            enemies.Clear(); rocks.Clear();
            hp = 3; apples = Math.Max(apples, 3);              // 3 macas para ja poder assar
            killsAtMax = 5;                         // o proximo passo ja dispara a fase da fogueira
        }

        void ResetBoss()
        {
            bossPhase = 0; killsAtMax = 0; bossT = 0; atePhase1 = false; playEatT = 0; endStarted = false; bossSpawned = false;
            if (boss != null) { enemies.Remove(boss); boss = null; }
            shocks.Clear(); smokes.Clear(); rewardSpawned = false; msgT = 0; items.RemoveAll(delegate (Item it) { return it.Reward; });
            rocks.RemoveAll(delegate (Rock r) { return r.Fire; });
            for (int i = 0; i < 5; i++) { reserved[i] = false; bakeTs[i] = 0; }
            if (cookState == 0) { fireK = 0; for (int i = 0; i < 5; i++) onFire[i] = false; }
            if (endShow != null && !endShow.IsDisposed) endShow.Close();
            endShow = null;
        }

        void StartFinalPhase()
        {
            bossPhase = 1; bossT = 0; atePhase1 = false; playEatT = 0;
            foreach (Enemy e in enemies)             // os monstros que sobraram explodem
            {
                puffs.Add(new Puff { Kind = 0, X = e.X, Y = e.Y - 4 * S });
                puffs.Add(new Puff { Kind = 1, X = e.X - 3 * S, Y = e.Y - 8 * S });
                puffs.Add(new Puff { Kind = 1, X = e.X + 3 * S, Y = e.Y });
            }
            enemies.Clear(); rocks.Clear();
            fireX = W / 2; fireY = H / 2; fireK = 0;
            for (int i = 0; i < 5; i++) { onFire[i] = false; reserved[i] = false; bakeTs[i] = 0; }
            cookApples.Clear();
            try
            {
                new Toast("Nível máximo!", "Colha maçãs com a tecla 3, coloque-as na fogueira e coma, também com a tecla 3. Isso regenera sua vida.", S / 3f, 11000, true).Show();
            }
            catch { }
        }

        void FireExplode()
        {
            for (int k = 0; k < 10; k++)
            {
                puffs.Add(new Puff { Kind = k % 2, X = fireX + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = fireY - (float)rnd.NextDouble() * 12 * S });
            }
            for (int k = 0; k < 18; k++)
                smokes.Add(new Smoke { X = fireX + ((float)rnd.NextDouble() - 0.5f) * 14 * S, Y = fireY - (float)rnd.NextDouble() * 6 * S, T = -(float)rnd.NextDouble() * 0.7f, Sz = 3 + (float)rnd.NextDouble() * 3 });
            fireK = 0;
            for (int i = 0; i < 5; i++) { onFire[i] = false; reserved[i] = false; }
            cookApples.Clear();
        }

        void SpawnDragon()
        {
            bossSpawned = true;
            boss = new Enemy();
            boss.Type = 3; boss.X = fireX; boss.Y = fireY + 8 * S;
            boss.HP = boss.MaxHP = 5000;
            boss.Timer = 1.5f; boss.Face = hx >= fireX ? 1 : -1;
            enemies.Add(boss);
            for (int k = 0; k < 6; k++) puffs.Add(new Puff { Kind = 2, X = fireX + ((float)rnd.NextDouble() - 0.5f) * 16 * S, Y = fireY - (float)rnd.NextDouble() * 10 * S });
        }

        void BossDefeated(Enemy e)
        {
            shocks.Clear(); bossPhase = 5; bossT = 0; endStarted = false; rewardSpawned = false; msgT = 0;
            bossDeadX = e.X; bossDeadY = e.Y; bossDeadFace = e.Face;
            rocks.Clear();
            for (int k = 0; k < 8; k++)
                puffs.Add(new Puff { Kind = k % 2, X = e.X + ((float)rnd.NextDouble() - 0.5f) * 30 * S, Y = e.Y - (float)rnd.NextDouble() * 24 * S });
        }

        // explosao do dragao: 1000 rupias em valores diferentes (2x300 + 4x50 + 5x20 + 8x10 + 3x5 + 5x1 = 1000)
        void SpawnDragonReward()
        {
            int[] kinds = { 6, 6, 5, 5, 5, 5, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 2, 2, 2, 1, 1, 1, 1, 1 };
            for (int i = 0; i < kinds.Length; i++)
            {
                double a = rnd.NextDouble() * Math.PI * 2; float sp = (35 + (float)rnd.NextDouble() * 70) * S;
                items.Add(new Item { Kind = kinds[i], X = bossDeadX, Y = bossDeadY - 8 * S, Reward = true, VX = (float)Math.Cos(a) * sp, VY = (float)Math.Sin(a) * sp * 0.7f });
            }
            for (int k = 0; k < 22; k++)
                puffs.Add(new Puff { Kind = k % 2, X = bossDeadX + ((float)rnd.NextDouble() - 0.5f) * 40 * S, Y = bossDeadY - (float)rnd.NextDouble() * 30 * S });
            for (int k = 0; k < 14; k++)
                smokes.Add(new Smoke { X = bossDeadX + ((float)rnd.NextDouble() - 0.5f) * 36 * S, Y = bossDeadY - (float)rnd.NextDouble() * 12 * S, T = -(float)rnd.NextDouble() * 0.4f, Sz = 4 + (float)rnd.NextDouble() * 4, Life = 2.6f, Grow = 7f });
            Say("1000 rupias!");
        }

        // o dragao derrotado volta ao normal (roxo, tamanho normal) e pisca antes de explodir
        void DrawDragonGhost(Graphics g)
        {
            if (bossPhase != 5 || rewardSpawned) return;
            if (((int)(bossT * 12)) % 2 == 0) return;
            Sprite sp = dragonSpr[0];
            Shadow(g, bossDeadX, bossDeadY, 34 * DK);
            g.DrawImage(bossDeadFace > 0 ? sp.R : sp.L, (int)(bossDeadX - sp.W / 2f), (int)(bossDeadY - sp.H), sp.W, sp.H);
        }

        Font msgFont;
        void DrawBossMsg(Graphics g)
        {
            if (msgT <= 0) return;
            if (msgFont == null) msgFont = new Font("Consolas", S * 8, FontStyle.Bold, GraphicsUnit.Pixel);
            string a = "ESPERE POR MAIS ATUALIZAÇÕES";
            SizeF sz = g.MeasureString(a, msgFont);
            float pad = 6 * S, x = W / 2f - sz.Width / 2, y = H / 2f - sz.Height / 2;
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(46, 40, 62)))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            {
                g.FillRectangle(k, x - pad - S, y - pad - S, sz.Width + 2 * pad + 2 * S, sz.Height + 2 * pad + 2 * S);
                g.FillRectangle(bg, x - pad, y - pad, sz.Width + 2 * pad, sz.Height + 2 * pad);
                g.FillRectangle(gold, x - pad, y - pad, sz.Width + 2 * pad, S);
            }
            DrawTextF(g, a, msgFont, x, y, Color.FromArgb(255, 220, 90));
        }

        void BossLeave(float dt)                // o heroi se afasta da fogueira sozinho
        {
            MoveHero(leaveX, leaveY, 52 * S, dt);
            if (walking) walkT += dt;
            else if (bossT > 0.3f)
            {
                bossPhase = 3; bossT = 0; bossSpawned = false;
                hface = fireX >= hx ? 1 : -1;
                FireExplode();
            }
            hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx));
            hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
        }

        void UpdateBoss(float dt)
        {
            if (!playMode) return;
            if (bossPhase == 0) { if (killsAtMax >= 5) StartFinalPhase(); return; }
            bossT += dt;
            for (int i = 0; i < 5; i++) if (onFire[i]) bakeTs[i] += dt;
            for (int i = smokes.Count - 1; i >= 0; i--) { smokes[i].T += dt; if (smokes[i].T > smokes[i].Life) smokes.RemoveAt(i); }

            if (bossPhase == 1)
            {
                fireK = Math.Min(1f, bossT / 1.5f);
                // vida cheia depois de ter comido pelo menos uma maca: o heroi se afasta e a fogueira explode
                if (atePhase1 && playEatT <= 0 && hp >= MaxHP && cookApples.Count == 0 && !dead)
                {
                    bossPhase = 2; bossT = 0;
                    float side = hx >= fireX ? 1 : -1;
                    leaveX = Math.Max(10 * S, Math.Min(W - 10 * S, fireX + side * 95 * S));
                    leaveY = fireY + 10 * S;
                    Say("Hora de sair daqui!");
                }
            }
            else if (bossPhase == 3)
            {
                if (!bossSpawned && bossT >= 0.9f) SpawnDragon();
                if (bossT >= 3.2f) bossPhase = 4;
            }
            else if (bossPhase == 5)
            {
                // 0-1,3 s: o dragao volta ao normal (sem vermelho, tamanho normal) e pisca; depois EXPLODE e solta 1000 rupias
                if (bossT >= 1.3f && !rewardSpawned) { rewardSpawned = true; SpawnDragonReward(); }
                if (rewardSpawned && bossT < 3.3f && ((int)(bossT * 10)) != ((int)((bossT - dt) * 10)))
                    puffs.Add(new Puff { Kind = rnd.Next(2), X = bossDeadX + ((float)rnd.NextDouble() - 0.5f) * 44 * S, Y = bossDeadY - (float)rnd.NextDouble() * 34 * S });
                // depois de vencer o dragao: "Espere por mais atualizacoes" e entao os creditos
                if (bossT >= 3.6f && msgT <= 0 && !endStarted) msgT = 0.001f;
                if (msgT > 0 && !endStarted)
                {
                    msgT += dt;
                    if (msgT >= 4.5f)
                    {
                        endStarted = true; msgT = 0;
                        try { endShow = new EndShow(S / 3f); endShow.Show(); } catch { endShow = null; }
                    }
                }
                if (endStarted && (endShow == null || endShow.IsDisposed || endShow.Done))
                    SetPlayMode(false);                 // acabou: volta ao modo automatico
            }
        }

        // tecla 3 perto da fogueira: poe uma maca crua, ou come uma assada
        bool FireInteract()
        {
            float d = (float)Math.Sqrt((fireX - hx) * (fireX - hx) + (fireY - hy) * (fireY - hy));
            if (d > 24 * S) return false;
            int free = -1;
            for (int i = 0; i < 5; i++) if (!onFire[i] && !reserved[i]) { free = i; break; }
            if (apples > 0 && free >= 0)
            {
                apples--; reserved[free] = true; bakeTs[free] = 0;
                hface = fireX >= hx ? 1 : -1;
                LaunchCookApple(hx + hface * 6 * S, hy - 10 * S, SlotX(free), SlotY(free), free, 1);
                Say("Na fogueira!");
                return true;
            }
            for (int i = 4; i >= 0; i--)
                if (onFire[i] && bakeTs[i] >= 10f)
                {
                    onFire[i] = false; playEatT = 0.001f;
                    hface = fireX >= hx ? 1 : -1;
                    return true;
                }
            bool baking = false;
            for (int i = 0; i < 5; i++) if (onFire[i] || reserved[i]) baking = true;
            Say(baking ? "Ainda assando..." : "Sem maçãs");
            return true;
        }

        void DrawSmokes(Graphics g)
        {
            foreach (Smoke s in smokes)
            {
                if (s.T < 0) continue;
                float k = s.T / s.Life;
                float sz = S * (s.Sz + k * s.Grow);
                int shade = (int)(210 - 90 * k);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(shade, shade, shade + 10)))
                    g.FillRectangle(b, s.X - sz / 2, s.Y - s.T * 11 * S - sz / 2, sz, sz);
            }
        }

        void DrawDragon(Graphics g, Enemy e)
        {
            Shadow(g, e.X, e.Y, 34 * DK * e.Scale);
            foreach (Shock sk in shocks)      // aviso da onda de choque: a sombra do dragao pisca 3 vezes (vermelha)
                if (sk.T < ShockWarn && ((int)(sk.T / 0.25f)) % 2 == 0)
                    using (SolidBrush rb = new SolidBrush(Color.FromArgb(235, 40, 50)))
                        g.FillEllipse(rb, e.X - 17 * S * DK * e.Scale, e.Y - 2 * S, 34 * S * DK * e.Scale, 5 * S);
            float k = (e.T < 1.5f ? 0.15f + 0.85f * (e.T / 1.5f) : 1f) * e.Scale;
            bool furiaD = e.HP * 2 <= e.MaxHP;
            bool brilha = furiaD && e.GrowT < 3f && ((int)(time * 8)) % 2 == 0;      // brilha enquanto cresce
            Sprite sp = (brilha ? dragonSprGlow : (furiaD ? dragonSprRed : dragonSpr))[e.Mode == 1 ? 1 : 0];
            int w = (int)(sp.W * k), h = (int)(sp.H * k);
            float bob = e.Moving ? Math.Abs((float)Math.Sin(e.T * 7)) * 2 * S : 0;      // pula um pouco ao andar
            g.DrawImage(e.Face > 0 ? sp.R : sp.L, (int)(e.X - w / 2f), (int)(e.Y - h - bob), w, h);
        }

                // Comandos do modo jogo, no canto superior direito
        Font helpFont;
        void DrawControlsHelp(Graphics g)
        {
            if (helpFont == null) helpFont = new Font("Consolas", Math.Max(9, S * 4), FontStyle.Bold, GraphicsUnit.Pixel);
            List<string> lineList = new List<string> {
                "COMANDOS",
                "Mouse apertado: andar",
                "1: espada",
                "2: arco",
                "3: colher / assar / comer maçã" };
            if (chestState == 1) lineList.Add("4: abrir o baú");
            lineList.Add("Espaço: pausar");
            string[] lines = lineList.ToArray();
            float lh = helpFont.Height + S, wmax = 0;
            foreach (string s in lines) wmax = Math.Max(wmax, g.MeasureString(s, helpFont).Width);
            float pad = 3 * S, bw = wmax + pad * 2 + 6 * S, bh = lines.Length * lh + pad * 2;
            float x = W - bw - 4 * S, y = 4 * S;
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(46, 40, 62)))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            {
                g.FillRectangle(k, x - S, y - S, bw + 2 * S, bh + 2 * S);
                g.FillRectangle(bg, x, y, bw, bh);
                g.FillRectangle(gold, x, y, bw, S);
            }
            for (int i = 0; i < lines.Length; i++)
                DrawTextF(g, lines[i], helpFont, x + pad, y + pad + i * lh, i == 0 ? Color.FromArgb(247, 190, 70) : Color.White);
        }

        void DrawTextF(Graphics g, string s, Font fnt, float x, float y, Color c)
        {
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
                for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                        if (ox != 0 || oy != 0) g.DrawString(s, fnt, k, x + ox, y + oy);
            using (SolidBrush b = new SolidBrush(c)) g.DrawString(s, fnt, b, x, y);
        }
        void DrawBossBar(Graphics g)
        {
            if (!DragonAlive()) return;
            float bw = 60 * S, bh = 4 * S, x = W / 2 - bw / 2, y = 15 * S;
            float frac = Math.Max(0f, boss.HP / (float)boss.MaxHP);
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(70, 20, 30)))
            using (SolidBrush fg = new SolidBrush(Color.FromArgb(225, 55, 60)))
            using (SolidBrush hl = new SolidBrush(Color.FromArgb(255, 130, 120)))
            {
                g.FillRectangle(k, x - S, y - S, bw + 2 * S, bh + 2 * S);
                g.FillRectangle(bg, x, y, bw, bh);
                g.FillRectangle(fg, x, y, bw * frac, bh);
                g.FillRectangle(hl, x, y, bw * frac, S);
            }
            string name = "Dragão";
            SizeF sz = g.MeasureString(name, font);
            DrawText(g, name, x + bw / 2 - sz.Width / 2, y + bh + S, Color.White);
        }

        void DrawPlayEat(Graphics g, float f, float bob)
        {
            float sc2 = 2.3f - 1.7f * Math.Min(1f, playEatT / 0.9f);
            if (eatRaw)      // maca crua (verde/vermelha)
            {
                int w = (int)(appleSpr.W * sc2 * 0.8f), h = (int)(appleSpr.H * sc2 * 0.8f);
                g.DrawImage(appleSpr.R, (int)(hx + f * 5 * S - w / 2f), (int)(hy - bob - 9 * S - h / 2f), w, h);
            }
            else DrawBakeApple(g, 3, hx + f * 5 * S, hy - bob - 9 * S, sc2);
        }

        // ---- modo automatico: com so 1 coracao e sem monstros num raio de 4 Links, vai a uma arvore e come uma maca ----
        int autoEatPh; Tree autoEatTree;
        void AutoEatAbortIfBusy() { if (autoEatPh == 2) AutoEatAbort(); }
        bool EnemyWithin(float r) { foreach (Enemy e in enemies) if (ED(e) < r) return true; return false; }
        void AutoEatAbort() { autoEatPh = 0; autoEatTree = null; playPickT = 0; playEatT = 0; eatRaw = false; }
        bool AutoEatApple(float dt)
        {
            float R = 4 * 16 * S;        // 4 tamanhos do Link
            if (autoEatPh == 0)
            {
                if (hp > 1 || hp <= 0 || dead || EnemyWithin(R)) return false;
                Tree t = NearestTreeWithApples(); if (t == null) return false;
                autoEatTree = t; autoEatPh = 1; Say("Preciso comer...");
            }
            if (autoEatPh < 3 && EnemyWithin(R)) { AutoEatAbort(); return false; }
            if (autoEatPh == 1)
            {
                if (autoEatTree == null || FirstApple(autoEatTree) < 0) { autoEatTree = NearestTreeWithApples(); if (autoEatTree == null) { AutoEatAbort(); return false; } }
                float d = (float)Math.Sqrt((autoEatTree.X - hx) * (autoEatTree.X - hx) + (autoEatTree.Y - hy) * (autoEatTree.Y - hy));
                if (d > 12 * S) { MoveHero(autoEatTree.X, autoEatTree.Y, 56 * S, dt); return true; }
                hface = autoEatTree.X >= hx ? 1 : -1; playTree = autoEatTree; playPickT = 0.001f; pickedFlag = false; pickT = 0; autoEatPh = 2; return true;
            }
            if (autoEatPh == 2)
            {
                playPickT += dt; pickT = playPickT;
                if (!pickedFlag && playPickT >= 0.4f)
                {
                    int idx = FirstApple(autoEatTree);
                    if (idx < 0) { AutoEatAbort(); return false; }
                    autoEatTree.Has[idx] = false; autoEatTree.Regrow[idx] = 60f + (float)rnd.NextDouble() * 40f; pickedFlag = true;
                }
                if (playPickT >= 0.8f) { playPickT = 0; playEatT = 0.001f; eatRaw = true; autoEatPh = 3; }
                return true;
            }
            playEatT += dt;                // autoEatPh == 3: comendo
            if (playEatT >= 0.9f) { playEatT = 0; eatRaw = false; hp = Math.Min(MaxHP, hp + 0.5f); Say("Nham!"); autoEatPh = 0; autoEatTree = null; }
            return true;
        }

                // ================= MORTE NO MODO JOGO =================
        // Aparece "VOCE MORREU" com o botao "Tentar de novo". Ao clicar, o heroi volta com 10 rupias a menos (ou 0 se tinha 10 ou menos).
        bool deathWait;
        DeathForm deathForm;

        // monstros vermelhos (octorok): 10% menos frequentes no modo jogo; o resto vira morcego e slime
        int SpawnType()
        {
            if (!playMode) return rnd.Next(100) < 15 ? 4 : rnd.Next(3);
            double r = rnd.NextDouble();
            return r < 0.25 ? 0 : (r < 0.55 ? 1 : (r < 0.85 ? 2 : 4));     // 4 = bokoblin (15%)
        }

        void ShowDeath()
        {
            deathWait = true;
            try { deathForm = new DeathForm(S / 3f, DoRetry); deathForm.Show(); } catch { deathForm = null; deathWait = false; }
        }

        void CloseDeath()
        {
            if (deathForm != null && !deathForm.IsDisposed) deathForm.Close();
            deathForm = null; deathWait = false;
        }

        void DoRetry()
        {
            deathWait = false; deathForm = null;
            rupees = rupees > 10 ? rupees - 10 : 0;
            int step = RupeeStep();
            lastStep = step;
            SetMaxHp(5 + Math.Max(0, Math.Min(3, step - 3)));          // menos rupias = menos corações de nivel e menos monstros
            dead = false; deadT = 0; hp = MaxHP; inv = 2f; kvx = kvy = 0;
            hx = bossPhase >= 3 ? W * 0.15f : W / 2; hy = H / 2;
            puffs.Add(new Puff { Kind = 2, X = hx, Y = hy });
            if (DragonAlive()) { shocks.Clear(); boss.ShockT = 8f; boss.HP = boss.MaxHP; rocks.RemoveAll(delegate (Rock r) { return r.Fire; }); }     // o dragao recupera toda a vida quando o heroi morre
            Say("Voltei!");
        }

        // Espaco pausa e continua o jogo (so vale com a area de trabalho em foco, para nao pausar enquanto voce digita em outro programa)
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        static Func<bool> FgHook;                 // (so para testes automaticos)
        bool prevSpace;
        ToolStripMenuItem pauseItem;
        bool ShieldFocused()
        {
            if (FgHook != null) return FgHook();
            return shield != null && !shield.IsDisposed && GetForegroundWindow() == shield.Handle;
        }
        // Espaco abre/fecha o MENU DE PAUSA (armas, roupas, comida). Vale com a area de trabalho em foco (no modo jogo, com a janela de protecao)
        bool SpaceAllowed()
        {
            if (FgHook != null) return FgHook();
            if (playMode) return ShieldFocused();
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64); GetClassName(GetForegroundWindow(), sb, 64);
            string cn = sb.ToString(); return cn == "Progman" || cn == "WorkerW" || cn == "Shell_TrayWnd" || cn == "Shell_SecondaryTrayWnd" || cn == "" || cn == "NotifyIconOverflowWindow";
        }
        void CheckSpacePause()
        {
            bool d = IsDown(0x20);
            if (d && !prevSpace) { if (MenuOpen) CloseMenu(); else if (SpaceAllowed()) OpenMenu(); }
            if (MenuOpen && IsDown(0x1B)) CloseMenu();      // Esc tambem fecha
            prevSpace = d;
        }
        Font pauseFont;
        void DrawPauseBanner(Graphics g)
        {
            if (pauseFont == null) pauseFont = new Font("Consolas", 9 * S, FontStyle.Bold, GraphicsUnit.Pixel);
            string a = "PAUSADO", b = "Espaço para continuar";
            SizeF sa = g.MeasureString(a, pauseFont), sb = g.MeasureString(b, helpFont ?? font);
            float w = Math.Max(sa.Width, sb.Width) + 16 * S, h = sa.Height + sb.Height + 10 * S, x = W / 2 - w / 2, y = H * 0.28f - h / 2;      // acima do meio, para nao cobrir o heroi
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(46, 40, 62)))
            using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            {
                g.FillRectangle(k, x - S, y - S, w + 2 * S, h + 2 * S);
                g.FillRectangle(bg, x, y, w, h);
                g.FillRectangle(gold, x, y, w, S);
            }
            DrawTextF(g, a, pauseFont, W / 2 - sa.Width / 2, y + 4 * S, Color.FromArgb(247, 190, 70));
            DrawTextF(g, b, helpFont ?? font, W / 2 - sb.Width / 2, y + 4 * S + sa.Height + S, Color.White);
        }
        // ---- "Mostrar area de trabalho": detecta quando a area de trabalho esta na frente de tudo ----
        [DllImport("user32.dll")] static extern IntPtr GetTopWindow(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr h, uint cmd);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", CharSet = CharSet.Auto)] static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int val, int size);
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int Left, Top, Right, Bottom; }
        uint myPid;
        bool DesktopShown()
        {
            if (myPid == 0) myPid = (uint)Process.GetCurrentProcess().Id;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(64);
            for (IntPtr h = GetTopWindow(IntPtr.Zero); h != IntPtr.Zero; h = GetWindow(h, 2))
            {
                if (!IsWindowVisible(h) || IsIconic(h)) continue;
                if ((GetWindowLong(h, -20) & 0x8) != 0) continue;                  // sempre-por-cima (barra de tarefas etc.)
                uint pid; GetWindowThreadProcessId(h, out pid); if (pid == myPid) continue;
                int cloaked; if (DwmGetWindowAttribute(h, 14, out cloaked, 4) == 0 && cloaked != 0) continue;   // janelas "escondidas" do Windows
                sb.Length = 0; GetClassName(h, sb, 64); string c = sb.ToString();
                if (c == "Progman" || c == "WorkerW") return true;                // a area de trabalho esta na frente
                RECT r; if (GetWindowRect(h, out r) && (r.Right - r.Left < 40 || r.Bottom - r.Top < 40)) continue;   // janelas auxiliares minusculas
                return false;                                                      // um aplicativo esta na frente
            }
            return true;
        }
        bool toastShown;
        void Say(string s) { speech = s; speechT = 0.8f; }

        Tree NewTree(float x, float y)
        {
            Tree t = new Tree(); t.X = x; t.Y = y;
            for (int i = 0; i < 5; i++) t.Has[i] = true;
            return t;
        }
        float AppleX(Tree t, int i) { return t.X - 10 * S + (Art.AppleSlots[i, 0] + 0.5f) * S; }
        float AppleY(Tree t, int i) { return t.Y - 24 * S + (Art.AppleSlots[i, 1] + 0.5f) * S; }
        int FirstApple(Tree t)
        {
            for (int i = 0; i < 5; i++) if (t.Has[i]) return i;
            return -1;
        }
        Tree NearestTreeWithApples()
        {
            Tree best = null; float bd = float.MaxValue;
            foreach (Tree t in trees)
            {
                if (FirstApple(t) < 0) continue;
                float d = (t.X - hx) * (t.X - hx) + (t.Y - hy) * (t.Y - hy);
                if (d < bd) { bd = d; best = t; }
            }
            return best;
        }

        void EndCalm(bool startled)
        {
            calmState = 0; calmT = 0;
            nextCalm = 120f + (float)rnd.NextDouble() * 180f;
            spawnTimer = startled ? 1f : 3f;
            wait = 1f;
            Say(startled ? "!" : "Hm?");
        }

        void Spawn(int type)
        {
            Enemy e = new Enemy();
            e.Type = type;
            int edge = rnd.Next(4);
            float m = 6 * S;
            if (edge == 0) { e.X = m; e.Y = m * 3 + (float)rnd.NextDouble() * (H - m * 6); }
            else if (edge == 1) { e.X = W - m; e.Y = m * 3 + (float)rnd.NextDouble() * (H - m * 6); }
            else if (edge == 2) { e.X = m * 3 + (float)rnd.NextDouble() * (W - m * 6); e.Y = 16 * S; }
            else { e.X = m * 3 + (float)rnd.NextDouble() * (W - m * 6); e.Y = H - m; }
            e.HP = type == 1 ? 1 : (type == 4 ? 3 : 2);
            e.Timer = 0.8f + (float)rnd.NextDouble();
            puffs.Add(new Puff { Kind = 2, X = e.X, Y = e.Y });
            enemies.Add(e);
        }

        Enemy NearestBat()
        {
            Enemy best = null; float bd = float.MaxValue;
            foreach (Enemy e in enemies)
            {
                if (e.Type != 1) continue;
                float dx = e.X - hx, dy = e.Y - hy, d = dx * dx + dy * dy;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        Enemy Nearest()
        {
            Enemy best = null; float bd = float.MaxValue;
            foreach (Enemy e in enemies)
            {
                if (e.Type == 1) continue;
                float dx = e.X - hx, dy = e.Y - hy, d = dx * dx + dy * dy;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

                // ================= PILOTO AUTOMATICO CUIDADOSO =================
        // Mantem distancia, atira de longe, recua quando algo chega perto e so usa a espada quando e seguro.
        // Ajuste do "cuidado" do piloto automatico (distancias em unidades de pixel-art). Medido em simulacao:
        // o heroi toma cerca de metade dos danos de antes, derrotando o mesmo numero de inimigos.
        const float TuneSafe = 22f, TuneLow = 30f, TuneDodge = 13f, TuneBat = 12f, TuneItem = 22f, TuneAtk = 13.5f;
        float ED(Enemy e) { return (float)Math.Sqrt((e.X - hx) * (e.X - hx) + (e.Y - hy) * (e.Y - hy)); }

        bool ItemSafe(Item it)
        {
            foreach (Enemy e in enemies)
                if ((e.X - it.X) * (e.X - it.X) + (e.Y - it.Y) * (e.Y - it.Y) < (24 * S) * (24 * S)) return false;
            return true;
        }

        void AimAndShoot(Enemy e, float dt)
        {
            bowing = true;
            float d = ED(e), lead = d / (210f * S);
            float tx = e.X + e.VX * lead, ty = e.Y - (e.Type == 1 ? 9 : 4) * S + e.VY * lead;
            float sx = hx + hface * 7 * S, sy = hy - 8 * S;
            float ax = tx - sx, ay = ty - sy, al = (float)Math.Sqrt(ax * ax + ay * ay);
            if (al < 1) { ax = hface; ay = 0; al = 1; }
            aimX = ax / al; aimY = ay / al;
            if (Math.Abs(aimX) > 0.15f) hface = aimX > 0 ? 1 : -1;
            bowT += dt;
            if (bowT >= 0.4f && bowCool <= 0)
            {
                arrows.Add(new Arrow { X = sx, Y = sy, VX = aimX * 210 * S, VY = aimY * 210 * S });
                bowT = 0; bowCool = 0.35f;
                if (rnd.Next(100) < 20) Say("Toma!");
            }
        }

        // afasta-se de todos os inimigos proximos; nas bordas da tela desliza ao longo da parede
        void Retreat(float dt)
        {
            float rx = 0, ry = 0, R = 34 * S;
            foreach (Enemy e in enemies)
            {
                float d = ED(e);
                if (d > R || d < 0.5f) continue;
                float w = (R - d) / R;
                rx += (hx - e.X) / d * w; ry += (hy - e.Y) / d * w;
            }
            float m = (float)Math.Sqrt(rx * rx + ry * ry);
            if (m < 0.01f) return;
            rx /= m; ry /= m;
            if (hx < 10 * S && rx < 0) { rx = 0; ry = ry >= 0 ? 1 : -1; }
            if (hx > W - 10 * S && rx > 0) { rx = 0; ry = ry >= 0 ? 1 : -1; }
            if (hy < 28 * S && ry < 0) { ry = 0; rx = rx >= 0 ? 1 : -1; }
            if (hy > H - 4 * S && ry > 0) { ry = 0; rx = rx >= 0 ? 1 : -1; }
            MoveHero(hx + rx * 100, hy + ry * 100, 64 * S, dt);
        }

        void AutoControl(float dt)
        {
            bowCool -= dt;
            Enemy near = null; float nd = float.MaxValue; int crowd = 0;
            foreach (Enemy e in enemies)
            {
                float d = ED(e);
                if (d < nd) { nd = d; near = e; }
                if (d < 30 * S) crowd++;
            }
            bool lowHp = hp <= 2;
            float safe = (lowHp ? TuneLow : TuneSafe) * S;

            if (TryDodge(dt)) { AutoEatAbortIfBusy(); bowT = 0; return; }
            if (AutoEatApple(dt)) { bowT = 0; return; }

            // octoroks e slimes: luta de espada (aproxima e golpeia), mas so se nao houver item para pegar primeiro
            if (near != null && near.Type != 1 && nd < 110 * S)
            {
                Item it0 = NearestItem();
                if (it0 != null && nd >= TuneItem * S) { MoveHero(it0.X, it0.Y, 56 * S, dt); bowT = 0; return; }
                if (Math.Abs(near.X - hx) > 2) hface = near.X >= hx ? 1 : -1;
                if (nd < TuneAtk * S && atkCool <= 0) StartAttack(false);
                else if (nd > (TuneAtk - 3f) * S) MoveHero(near.X, near.Y, 52 * S, dt);
                bowT = 0;
                return;
            }

            if (near != null && nd < (near.Type == 1 && !lowHp ? TuneBat * S : safe))
            {
                bool cornered = hx < 10 * S || hx > W - 10 * S || hy < 28 * S || hy > H - 4 * S;
                bool melee = atkCool <= 0 && nd < 15 * S && ((crowd <= 1 && !lowHp) || (cornered && nd < 13 * S));
                if (melee)
                {
                    hface = near.X >= hx ? 1 : -1;
                    StartAttack(false);
                    return;
                }
                Retreat(dt);
                AimAndShoot(near, dt);
                return;
            }

            Item it = NearestItem();
            if (near != null && nd < 140 * S)
            {
                if (it != null && nd > 45 * S && ItemSafe(it)) { MoveHero(it.X, it.Y, 56 * S, dt); bowT = 0; return; }
                AimAndShoot(near, dt);
                if (nd > 120 * S) MoveHero(near.X, near.Y, 36 * S, dt);
                return;
            }

            bowT = 0;
            if (it != null && ItemSafe(it)) { MoveHero(it.X, it.Y, 56 * S, dt); return; }
            if (wait > 0)
            {
                wait -= dt;
                if (wait <= 0)
                {
                    wx = 10 * S + (float)rnd.NextDouble() * (W - 20 * S);
                    wy = 28 * S + (float)rnd.NextDouble() * (H - 34 * S);
                }
            }
            else
            {
                MoveHero(wx, wy, 24 * S, dt);
                if (!walking)
                {
                    wait = 1.5f + (float)rnd.NextDouble() * 3f;
                    if (rnd.Next(100) < 25) StartAttack(true);
                    else if (rnd.Next(100) < 30) hface = -hface;
                }
            }
        }

        void StartAttack(bool practice)
        {
            attacking = true; atkT = 0; hitDone = false; swingPractice = practice;
            if (rnd.Next(100) < 45) Say(new string[] { "Hyaa!", "Hah!", "Yah!", "Toma!" }[rnd.Next(4)]);
        }

        void MoveHero(float tx, float ty, float speed, float dt)
        {
            float dx = tx - hx, dy = ty - hy, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 1) { walking = false; return; }
            float step = Math.Min(d, speed * dt);
            hx += dx / d * step; hy += dy / d * step;
            if (Math.Abs(dx) > 2) hface = dx > 0 ? 1 : -1;
            walking = true;
        }

        void UpdateHero(float dt)
        {
            walking = false;
            bowing = false;
            if (powerT > 0) { UpdatePowerHero(dt); return; }
            if (chestGo && chestState == 1 && !dead && cookState == 0) { UpdateChestHero(dt); return; }
            if (cookState != 0 && !dead) { UpdateCook(dt); return; }
            if (bossPhase == 2 && !dead) { BossLeave(dt); return; }
            if (bossPhase == 3 && !dead) { hface = fireX >= hx ? 1 : -1; return; }
            if (calmState == 2 && !dead)
            {
                phaseT += dt;
                if (phase == 0)                       // vai ate a macieira
                {
                    if (curTree == null) { phase = 2; phaseT = 0; }
                    else
                    {
                        MoveHero(curTree.X + (hx < curTree.X ? -1 : 1) * 9 * S, curTree.Y + 2 * S, 38 * S, dt);
                        if (walking) walkT += dt;
                        else { phase = 1; phaseT = 0; pickT = 0; picksLeft = 3; hface = hx < curTree.X ? 1 : -1; }
                    }
                }
                else if (phase == 1)                  // estica o braco e colhe 3 macas
                {
                    pickT += dt;
                    if (pickT >= 1.5f)
                    {
                        pickT = 0;
                        int idx = FirstApple(curTree);
                        if (idx >= 0 && picksLeft > 0)
                        {
                            curTree.Has[idx] = false; curTree.Regrow[idx] = 60f + (float)rnd.NextDouble() * 40f;
                            flyApples.Add(new FlyApple { X0 = AppleX(curTree, idx), Y0 = AppleY(curTree, idx) });
                            picksLeft--;
                        }
                        else picksLeft = 0;
                    }
                    if (picksLeft <= 0 && pickT >= 0.9f) { phase = 2; phaseT = 0; }
                }
                else if (phase == 2 && phaseT >= 1.5f) { phase = harvest ? 4 : 3; phaseT = 0; }       // abaixa espada e escudo
                else if (phase == 3)                  // cumprimenta (so nas pausas curtas)
                {
                    if (phaseT > 0.1f && phaseT < 0.2f && speechT <= 0) Say("Oi!");
                    if (phaseT > 1.9f && phaseT < 2.0f && speechT <= 0) Say("Oi!");
                    if (phaseT >= 3.5f) { phase = 4; phaseT = 0; }
                }
                else if (phase == 4 && phaseT >= 1.2f) { phase = 5; phaseT = 0; }                   // deita no chao
                hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx));
                hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
                return;
            }
            if (dead)
            {
                if (playMode) { if (!deathWait) ShowDeath(); return; }     // modo jogo: espera o clique em "Tentar de novo"
                deadT -= dt;
                if (deadT <= 0)
                {
                    dead = false; hp = MaxHP; inv = 2f; hx = bossPhase >= 3 ? W * 0.15f : W / 2; hy = H / 2;
                    puffs.Add(new Puff { Kind = 2, X = hx, Y = hy });
                    Say("Voltei!");
                }
                return;
            }
            inv -= dt; atkCool -= dt;
            hx += kvx * dt; hy += kvy * dt;
            float damp = Math.Min(1f, 9f * dt);
            kvx -= kvx * damp; kvy -= kvy * damp;

            if (attacking)
            {
                atkT += dt;
                if (!hitDone && atkT >= 0.11f) { hitDone = true; DoSwing(); }
                if (atkT >= 0.32f) { attacking = false; atkCool = 0.22f; }
            }
            else if (playMode) { PlayerControl(dt); }
            else
            {
                AutoControl(dt);
            }
            if (walking) walkT += dt;
            hx = Math.Max(6 * S, Math.Min(W - 6 * S, hx));
            hy = Math.Max(17 * S, Math.Min(H - 1 * S, hy));
        }

        // Desvia de pedras que vao passar perto: anda de lado, perpendicular a trajetoria
        bool TryDodge(float dt)
        {
            foreach (Rock r in rocks)
            {
                float v2 = r.VX * r.VX + r.VY * r.VY;
                if (v2 < 1) continue;
                float rx = hx - r.X, ry = (hy - 6 * S) - r.Y;
                float t = (rx * r.VX + ry * r.VY) / v2;           // tempo ate o ponto mais proximo
                if (t < 0 || t > 1.1f) continue;
                float px = rx - r.VX * t, py = ry - r.VY * t;      // desvio lateral
                float pd = (float)Math.Sqrt(px * px + py * py);
                if (pd > TuneDodge * S) continue;
                float sp = (float)Math.Sqrt(v2);
                float nx, ny;
                if (pd < 0.5f) { nx = -r.VY / sp; ny = r.VX / sp; }
                else { nx = px / pd; ny = py / pd; }
                int keep = hface;
                MoveHero(hx + nx * 100, hy + ny * 100, 100 * S, dt);
                hface = keep;
                return true;
            }
            return false;
        }

        Item NearestItem()
        {
            Item best = null; float bd = float.MaxValue;
            foreach (Item it in items)
            {
                float dx = it.X - hx, dy = it.Y - hy, d = dx * dx + dy * dy;
                if (d < bd) { bd = d; best = it; }
            }
            return best;
        }

        void DoSwing()
        {
            float cx = hx + hface * 8 * S, R = 18 * S;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                float dx = e.X - cx, dy = e.Y - hy;
                { float rad = e.Type == 3 ? R + 14 * S * DK * e.Scale : R; if (dx * dx + dy * dy < rad * rad) DamageEnemy(e, (e.Type == 3 ? 150 : 1) * (eqSword == 1 ? 2 : 1)); }      // espada azulada (evolucao 4): o dobro de dano
            }
            for (int i = rocks.Count - 1; i >= 0; i--)
            {
                Rock r = rocks[i];
                float dx = r.X - cx, dy = r.Y - (hy - 6 * S);
                if (dx * dx + dy * dy < R * R)
                {
                    puffs.Add(new Puff { Kind = 1, X = r.X, Y = r.Y });
                    rocks.RemoveAt(i);
                }
            }
        }

        void DamageEnemy(Enemy e) { DamageEnemy(e, 1); }

        void DamageEnemy(Enemy e, int dmg)
        {
            if (e.Type == 3 && e.T < 1.5f) return;          // o dragao ainda esta saindo da fumaca
            e.HP -= dmg; e.Flash = e.Type == 3 ? 0.12f : 0.25f;
            float dx = e.X - hx, dy = e.Y - hy, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 1) { dx = hface; dy = 0; d = 1; }
            if (e.Type != 3) { e.KX = dx / d * 110 * S; e.KY = dy / d * 110 * S; }
            puffs.Add(new Puff { Kind = 1, X = e.X, Y = e.Y - 5 * S });
            if (e.HP <= 0)
            {
                puffs.Add(new Puff { Kind = 0, X = e.X, Y = e.Y - 4 * S });
                enemies.Remove(e); killCount++;
                if (e.Type == 3) { BossDefeated(e); return; }
                if (!playMode) CountKill();                                     // evolucao: conta todo monstro derrotado no automatico
                if (!playMode && (e.Type == 0 || e.Type == 4)) CountChestKill();      // bau do rei: so octoroks (vermelhos) e bokoblins contam (morcegos e gelatinas nao)
                if (e.Type == 2 && !playMode && powerT <= 0 && !lootHold)      // gelatinas derrotadas: ao chegar a 20 o contador zera e o poder e lancado
                {
                    slimeKills++;
                    if (slimeKills >= SlimesForPower) { slimeKills = 0; powerPending = true; }
                }
                if (playMode && bossPhase == 0 && RupeeStep() >= 6) killsAtMax++;       // mortes no nivel maximo
                int roll = rnd.Next(100);
                if (roll < 20) items.Add(new Item { Kind = 0, X = e.X, Y = e.Y });
                else if (roll < 65)
                {
                    int r2 = rnd.Next(100);
                    items.Add(new Item { Kind = RupeeKind(r2), X = e.X, Y = e.Y });
                }
            }
        }

        // dano: 1 coracao por padrao; encostar em monstro tira meio (0,5); bola de fogo tira 1 e ignora o escudo e o tempo de invulnerabilidade
        void HurtHero(float fromX, float fromY, float dmg = 1f, bool ignoreInv = false)
        {
            if ((inv > 0 && !ignoreInv) || dead || powerT > 0) return;
            hp -= dmg; hurtCount++; inv = 1.2f; attacking = false; atkCool = 0.3f;
            float dx = hx - fromX, dy = hy - fromY, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 1) { dx = -hface; dy = 0; d = 1; }
            kvx = dx / d * 120 * S; kvy = dy / d * 120 * S;
            if (hp <= 0)
            {
                hp = 0; dead = true; deadT = 3f;
                puffs.Add(new Puff { Kind = 0, X = hx, Y = hy - 6 * S });
            }
            else Say("Ai!");
        }

        void UpdateEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                e.T += dt;
                if (e.Flash > 0) e.Flash -= dt;
                float dx = hx - e.X, dy = hy - e.Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < 1) d = 1;
                float ux = dx / d, uy = dy / d;
                if (Math.Abs(dx) > 2) e.Face = dx > 0 ? 1 : -1;

                if (e.Type == 3) // dragao: parado, cospe bolas de fogo no heroi (mesma frequencia dos monstros vermelhos)
                {
                    if (e.T > 1.5f)
                    {
                        e.Timer -= dt;
                        e.RT -= dt; e.Moving = false;
                        if (e.HP * 100 <= e.MaxHP * 15)      // ultimos 15% de vida: a cada 8 s, onda de choque (tira 2 coracoes)
                        {
                            e.ShockT -= dt;
                            if (e.ShockT <= 0) { e.ShockT = 8f; shocks.Add(new Shock { X = e.X, Y = e.Y - 2 * S, R = 1.5f * dragonSpr[0].W * e.Scale, R0 = 17 * S * DK * e.Scale }); }      // area: diametro = 3x o tamanho do dragao
                        }
                        else e.ShockT = 8f;
                        if (e.Mode == 0)
                        {
                            bool furia = e.HP * 2 <= e.MaxHP;                          // metade da vida: vermelho, +10% maior, mais rapido, solta fumaca, mais fogo
                            if (furia)      // cresce LENTAMENTE (3 s) ate 1,2x (10% + mais 10%) e brilha durante o crescimento
                            {
                                if (e.GrowT < 3f) e.GrowT = Math.Min(3f, e.GrowT + dt);
                                float gq = e.GrowT / 3f; gq = gq * gq * (3 - 2 * gq);
                                e.Scale = 1f + 0.2f * gq;
                                if (e.GrowT < 3f)
                                {
                                    e.SparkT -= dt;
                                    if (e.SparkT <= 0) { e.SparkT = 0.12f; puffs.Add(new Puff { Kind = 1, X = e.X + ((float)rnd.NextDouble() - 0.5f) * 40 * S * e.Scale, Y = e.Y - (float)rnd.NextDouble() * 38 * S * e.Scale }); }
                                }
                            }
                            else { e.Scale = 1f; e.GrowT = 0f; }
                            float halfW = 24 * DK * e.Scale * S + 4 * S, topH = 40 * DK * e.Scale * S + 6 * S;
                            if (e.RT <= 0 || ((e.TX - e.X) * (e.TX - e.X) + (e.TY - e.Y) * (e.TY - e.Y)) < (3 * S) * (3 * S))
                            {
                                // novo destino: um ponto em volta do heroi, a uns 70-110 de distancia
                                double ang = rnd.NextDouble() * Math.PI * 2; float dist = (70 + (float)rnd.NextDouble() * 40) * S;
                                e.TX = Math.Max(halfW, Math.Min(W - halfW, hx + (float)Math.Cos(ang) * dist));
                                e.TY = Math.Max(topH, Math.Min(H - 3 * S, hy + (float)Math.Sin(ang) * dist * 0.7f));
                                e.RT = 2.5f + (float)rnd.NextDouble() * 2f;
                            }
                            float mdx = e.TX - e.X, mdy = e.TY - e.Y, md = (float)Math.Sqrt(mdx * mdx + mdy * mdy);
                            float hdx = hx - e.X, hdy = hy - e.Y;
                            bool tooClose = hdx * hdx + hdy * hdy < (22 * S * DK * e.Scale) * (22 * S * DK * e.Scale);      // nao anda por cima do heroi
                            if (tooClose && md > 1 && (mdx * hdx + mdy * hdy) > 0) e.RT = 0;      // perto demais: nao anda para o heroi, escolhe outro destino
                            else if (md > 1) { float vel = (furia ? 18f : 14f) * S; e.X += mdx / md * vel * dt; e.Y += mdy / md * vel * dt; e.Moving = true; }      // furia: um pouco mais rapido, ainda bem mais lento que o heroi (56)
                            if (furia)
                            {
                                e.SmokeT -= dt;
                                if (e.SmokeT <= 0)      // de vez em quando sai fumaca do nariz
                                {
                                    e.SmokeT = 2.5f + (float)rnd.NextDouble() * 3f;
                                    Sprite dsp = dragonSprRed[0]; float sgn = e.Face > 0 ? 1f : -1f;
                                    float nx = e.X + sgn * ((43f / 48f) - 0.5f) * dsp.W * e.Scale, ny = e.Y - dsp.H * e.Scale + (13f / 40f) * dsp.H * e.Scale;
                                    for (int sk = 0; sk < 4; sk++) smokes.Add(new Smoke { X = nx + ((float)rnd.NextDouble() - 0.5f) * 2 * S, Y = ny, T = -sk * 0.18f, Sz = 1.6f + (float)rnd.NextDouble() * 1.4f, Life = 1.7f, Grow = 3f });
                                }
                            }
                        }
                        if (e.Mode == 0) { if (e.Timer <= 0) { e.Mode = 1; e.Timer = 0.6f; } }
                        else if (e.Timer <= 0)
                        {
                            bool homing = e.HP * 10 <= e.MaxHP;                       // 10% da vida: bolas teleguiadas que explodem em 3 s
                            bool rage = e.HP * 2 <= e.MaxHP;                         // abaixo de 50% da vida: furia (3 bolas em leque, mais rapido)
                            float mx = e.X + e.Face * 17 * S * DK * e.Scale, my = e.Y - 15 * S * DK * e.Scale;
                            float a0 = (float)Math.Atan2((hy - 6 * S) - my, hx - mx);
                            int nb = rage ? 3 : 1;
                            for (int bi = 0; bi < nb; bi++)
                            {
                                float ang = a0 + (bi - (nb - 1) / 2f) * 0.24f;
                                Rock r = new Rock(); r.Fire = true; r.Homing = homing; r.X = mx; r.Y = my;
                                float spd = homing ? 52 * S : 80 * S;
                                r.VX = (float)Math.Cos(ang) * spd; r.VY = (float)Math.Sin(ang) * spd;
                                rocks.Add(r);
                            }
                            e.Mode = 0; e.Timer = (1.5f + (float)rnd.NextDouble() * 1.5f) * (rage ? 1f / 1.2f : 1f);      // furia: bolas de fogo 20% mais frequentes
                        }
                    }
                }
                else if (powerT > 0) // poder do heroi: todos vao direto nele, com o dobro da velocidade
                {
                    float sp = (e.Type == 0 ? 16 : (e.Type == 1 ? 30 : (e.Type == 2 ? 28 : 26))) * 2 * S;
                    e.Mode = 0; e.VX = ux * sp; e.VY = uy * sp;
                    if (d > 3 * S) { e.X += e.VX * dt; e.Y += e.VY * dt; }
                }
                else if (e.Type == 0) // octorok: anda, para, mira e cospe pedra
                {
                    e.Timer -= dt;
                    if (e.Mode == 0)
                    {
                        e.X += e.VX * dt; e.Y += e.VY * dt;
                        if (e.Timer <= 0) { e.Mode = 1; e.Timer = 0.6f; e.VX = e.VY = 0; }
                        else if (e.VX == 0 && e.VY == 0)
                        {
                            float sp = 16 * S;
                            if (rnd.Next(100) < 65)
                            {
                                if (Math.Abs(dx) > Math.Abs(dy)) e.VX = Math.Sign(dx) * sp; else e.VY = Math.Sign(dy) * sp;
                            }
                            else
                            {
                                int dir = rnd.Next(4);
                                e.VX = dir == 0 ? sp : (dir == 1 ? -sp : 0);
                                e.VY = dir == 2 ? sp : (dir == 3 ? -sp : 0);
                            }
                        }
                    }
                    else if (e.Timer <= 0)
                    {
                        Rock r = new Rock();
                        r.X = e.X + e.Face * 5 * S; r.Y = e.Y - 4 * S;
                        r.VX = ux * 80 * S; r.VY = uy * 80 * S;
                        rocks.Add(r);
                        e.Mode = 0; e.Timer = 1.5f + (float)rnd.NextDouble() * 1.5f; e.VX = e.VY = 0;
                    }
                }
                else if (e.Type == 1) // morcego: voa ondulando em direcao ao heroi
                {
                    float wob = (float)Math.Sin(e.T * 5f) * 26 * S;
                    e.VX = ux * 30 * S - uy * wob * 0.6f;
                    e.VY = uy * 30 * S + ux * wob * 0.6f;
                    e.X += e.VX * dt;
                    e.Y += e.VY * dt;
                }
                else if (e.Type == 4) // bokoblin: persegue, levanta o porrete e bate
                {
                    e.Timer -= dt;
                    if (e.Mode == 0)
                    {
                        if (d > 12 * S) { e.X += ux * 26 * S * dt; e.Y += uy * 26 * S * dt; }
                        if (d < 16 * S && e.Timer <= 0) { e.Mode = 1; e.Timer = 0.55f; }
                    }
                    else if (e.Mode == 1)
                    {
                        if (e.Timer <= 0)
                        {
                            if (d < 20 * S) HurtHero(e.X, e.Y);
                            puffs.Add(new Puff { Kind = 1, X = e.X + e.Face * 12 * S, Y = e.Y - 3 * S });
                            e.Mode = 2; e.Timer = 0.8f;
                        }
                    }
                    else if (e.Timer <= 0) { e.Mode = 0; e.Timer = 0.2f; }
                }
                else // slime: pula em ciclos
                {
                    e.Timer -= dt;
                    if (e.Mode == 0 && e.Timer <= 0) { e.Mode = 1; e.Timer = 0.45f; }
                    else if (e.Mode == 1)
                    {
                        e.X += ux * 55 * S * dt; e.Y += uy * 55 * S * dt;
                        if (e.Timer <= 0) { e.Mode = 0; e.Timer = 0.7f + (float)rnd.NextDouble() * 0.5f; }
                    }
                }
                e.X += e.KX * dt; e.Y += e.KY * dt;
                float dm = Math.Min(1f, 8f * dt);
                e.KX -= e.KX * dm; e.KY -= e.KY * dm;
                e.X = Math.Max(4 * S, Math.Min(W - 4 * S, e.X));
                e.Y = Math.Max(12 * S, Math.Min(H, e.Y));

                if (!dead && inv <= 0)
                {
                    float cx = hx - e.X, cy = hy - e.Y;
                    {
                        bool toca;
                        if (e.Type == 3)      // dragao: so as patas/corpo (elipse no chao); o alcance da espada passa disso, entao da para bater sem ser tocado
                        {
                            float rx = 11 * S * DK * e.Scale, ry = 5.5f * S * DK * e.Scale;
                            toca = e.T > 1.5f && (cx * cx) / (rx * rx) + (cy * cy) / (ry * ry) < 1f;
                        }
                        else toca = cx * cx + cy * cy < (9 * S) * (9 * S);
                        if (toca) HurtHero(e.X, e.Y, 0.5f);      // encostar em monstro: meio coracao
                    }      // encostar em monstro: meio coracao
                }
            }
        }

        // ---------- onda de choque do dragao ----------
        List<Shock> shocks = new List<Shock>();
        const float ShockWarn = 1.5f, ShockExpand = 2.0f;       // a sombra do dragao pisca 3 vezes (1,5 s) e dela sai a onda (2 s)
        void UpdateShocks(float dt)
        {
            for (int i = shocks.Count - 1; i >= 0; i--)
            {
                Shock s = shocks[i]; s.T += dt;
                if (s.T >= ShockWarn && s.T < ShockWarn + ShockExpand && !s.Hit && !dead)
                {
                    float r = s.R0 + (s.R - s.R0) * (s.T - ShockWarn) / ShockExpand;
                    float dx = hx - s.X, dy = (hy - s.Y) / 0.7f, n = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (Math.Abs(n - r) < 7 * S) { s.Hit = true; HurtHero(s.X, s.Y, 2f, true); }      // encostou na onda: 2 coracoes
                }
                if (s.T > ShockWarn + ShockExpand + 0.25f) shocks.RemoveAt(i);
            }
        }
        void DrawShocks(Graphics g)
        {
            foreach (Shock s in shocks)
            {
                if (s.T < ShockWarn) continue;      // o aviso e a sombra do dragao piscando (ver DrawDragon)
                float r = s.R0 + (s.R - s.R0) * Math.Min(1f, (s.T - ShockWarn) / ShockExpand);
                using (Pen o = new Pen(Color.FromArgb(225, 70, 40), 8 * S))
                using (Pen m = new Pen(Color.FromArgb(255, 170, 60), 5 * S))
                using (Pen w = new Pen(Color.FromArgb(255, 245, 190), 2 * S))
                {
                    g.DrawEllipse(o, s.X - r, s.Y - r * 0.7f, 2 * r, 1.4f * r);
                    g.DrawEllipse(m, s.X - r, s.Y - r * 0.7f, 2 * r, 1.4f * r);
                    g.DrawEllipse(w, s.X - r, s.Y - r * 0.7f, 2 * r, 1.4f * r);
                }
            }
        }
        void UpdateRocks(float dt)
        {
            for (int i = rocks.Count - 1; i >= 0; i--)
            {
                Rock r = rocks[i];
                if (r.Homing)
                {
                    r.Life += dt;
                    float ca = (float)Math.Atan2(r.VY, r.VX), ta = (float)Math.Atan2((hy - 6 * S) - r.Y, hx - r.X);
                    float da = ta - ca;
                    while (da > Math.PI) da -= 2 * (float)Math.PI;
                    while (da < -Math.PI) da += 2 * (float)Math.PI;
                    float maxTurn = 1.8f * dt;
                    da = Math.Max(-maxTurn, Math.Min(maxTurn, da));
                    ca += da;
                    r.VX = (float)Math.Cos(ca) * 52 * S; r.VY = (float)Math.Sin(ca) * 52 * S;
                    if (r.Life >= 3f)                    // explode depois de 3 s
                    {
                        for (int k = 0; k < 7; k++)
                            puffs.Add(new Puff { Kind = k % 2, X = r.X + ((float)rnd.NextDouble() - 0.5f) * 14 * S, Y = r.Y + ((float)rnd.NextDouble() - 0.5f) * 14 * S });
                        float ex = hx - r.X, ey = (hy - 6 * S) - r.Y;
                        if (!dead && ex * ex + ey * ey < (16 * S) * (16 * S)) HurtHero(r.X, r.Y, 1f, true);
                        rocks.RemoveAt(i); continue;
                    }
                }
                r.X += r.VX * dt; r.Y += r.VY * dt;
                if (r.X < -20 || r.Y < -20 || r.X > W + 20 || r.Y > H + 20) { rocks.RemoveAt(i); continue; }
                if (dead) continue;
                float dx = r.X - hx, dy = r.Y - (hy - 6 * S);
                float rr7 = r.Fire ? 9 * S : 7 * S;
                if (dx * dx + dy * dy < rr7 * rr7)
                {
                    float sp = (float)Math.Sqrt(r.VX * r.VX + r.VY * r.VY);
                    bool blocked = !r.Fire && sp > 0 && (r.VX * hface) / sp < -0.35f;   // pedra vem de frente: escudo! (bola de fogo nao e bloqueada)
                    if (blocked) puffs.Add(new Puff { Kind = 1, X = hx + hface * 3 * S, Y = hy - 7 * S });
                    else HurtHero(r.X - r.VX, r.Y - r.VY, 1f, r.Fire);      // cada bola de fogo tira 1 coracao, mesmo durante a invulnerabilidade
                    rocks.RemoveAt(i);
                }
            }
        }

        void UpdateItems(float dt)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                Item it = items[i];
                if (it.Reward)      // recompensa do dragao: espalha para fora e nao some ate ser pega
                {
                    it.X = Math.Max(4 * S, Math.Min(W - 4 * S, it.X + it.VX * dt)); it.Y = Math.Max(14 * S, Math.Min(H - 2 * S, it.Y + it.VY * dt));
                    float dm = Math.Min(1f, 3f * dt); it.VX -= it.VX * dm; it.VY -= it.VY * dm;
                    it.T = 0;
                }
                else it.T += dt;                                 // os itens somem normalmente (15 s), inclusive depois do poder
                if (it.T > 15f) { items.RemoveAt(i); continue; }
                if (dead) continue;
                float dx = it.X - hx, dy = it.Y - hy;
                if (dx * dx + dy * dy < (8 * S) * (8 * S))
                {
                    if (it.Kind == 0) { if (hp < MaxHP) hp = Math.Min(MaxHP, hp + 1f); }
                    else rupees += RupeeValue(it.Kind);
                    puffs.Add(new Puff { Kind = 1, X = it.X, Y = it.Y - 4 * S });
                    items.RemoveAt(i);
                }
            }
        }

        // ============================ DESENHO ============================
        void Spr(Graphics g, Sprite sp, float cx, float feetY, bool right)
        {
            g.DrawImage(right ? sp.R : sp.L, (int)(cx - sp.W / 2f), (int)(feetY - sp.H), sp.W, sp.H);
        }

        List<float[]> shadowList = new List<float[]>();
        ShadowLayer shadowLayer;
        void Shadow(Graphics g, float x, float y, float wTiles)
        {
            if (previewMode) return;
            // as sombras vao para a camada com transparencia real (ShadowLayer), desenhada depois
            shadowList.Add(new float[] { x - wTiles * S / 2f, y - S, wTiles * S, 3 * S });
        }

        void DrawText(Graphics g, string s, float x, float y, Color c)
        {
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
                for (int ox = -1; ox <= 1; ox++)
                    for (int oy = -1; oy <= 1; oy++)
                        if (ox != 0 || oy != 0) g.DrawString(s, font, k, x + ox, y + oy);
            using (SolidBrush b = new SolidBrush(c)) g.DrawString(s, font, b, x, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            shadowList.Clear();
            PaintWorld(e);
            if (shadowLayer != null) shadowLayer.Render(shadowList);
        }

        void PaintWorld(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Key);
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            // itens
            foreach (Item it in items)
            {
                if (it.T > 12f && ((int)(it.T * 8)) % 2 == 0) continue;
                float bob = (float)Math.Abs(Math.Sin(it.T * 4)) * 2 * S;
                Shadow(g, it.X, it.Y, 6);
                Sprite sp = it.Kind == 0 ? heartFull : rupeeSpr[it.Kind - 1];
                Spr(g, sp, it.X, it.Y - 2 * S - bob, true);
            }

            DrawDragonGhost(g);
            DrawShocks(g);
            DrawChest(g);
            // entidades ordenadas por Y (heroi incluso)
            List<object> ents = new List<object>();
            foreach (Enemy en in enemies) ents.Add(en);
            foreach (Tree tr in trees) ents.Add(tr);
            if (cookState != 0 || bossPhase == 1) ents.Add(fire);
            ents.Add(this);
            ents.Sort(delegate (object a, object b) { return YOf(a).CompareTo(YOf(b)); });
            foreach (object o in ents)
            {
                if (o == this) DrawHero(g);
                else if (o is Tree) DrawTree(g, (Tree)o);
                else if (o is Fire) DrawFire(g);
                else DrawEnemy(g, (Enemy)o);
            }

            foreach (Rock r in rocks)
                {
                    if (r.Fire && r.Homing)
                    {
                        Sprite fs = fireSpr[((int)(time * 12)) % 2];
                        float k = 1.4f + (r.Life > 2f ? 0.18f * (float)Math.Sin(r.Life * 34) : 0f);      // pisca mais forte perto de explodir
                        int fw = (int)(fs.W * k), fh = (int)(fs.H * k);
                        g.DrawImage(fs.R, (int)(r.X - fw / 2f), (int)(r.Y - fh / 2f), fw, fh);
                    }
                    else if (r.Fire) Spr(g, fireSpr[((int)(time * 12)) % 2], r.X, r.Y + fireSpr[0].H / 2f, true);
                    else Spr(g, rockSpr, r.X, r.Y + 2 * S, true);
                }

            foreach (Arrow a in arrows)
            {
                if (a.Power) DrawPowerArrow(g, a); else DrawArrow(g, a.X, a.Y, a.VX, a.VY, 1f);
            }

            foreach (FlyApple fa in flyApples)
            {
                float u = Math.Min(1f, fa.T / 0.6f);
                float tx = hx + hface * 6 * S, ty = hy - 20 * S;
                float ax = fa.X0 + (tx - fa.X0) * u, ay = fa.Y0 + (ty - fa.Y0) * u - (float)Math.Sin(u * Math.PI) * 6 * S;
                Spr(g, appleSpr, ax, ay + appleSpr.H / 2f, true);
            }

            DrawCookApples(g);
            DrawSmokes(g);
            foreach (Puff p in puffs) DrawPuff(g, p);

            DrawChestPrompt(g);
            DrawHud(g);
            DrawBossMsg(g);
            DrawEvo(g);

            if (speechT > 0 && !dead)
            {
                SizeF sz = g.MeasureString(speech, font);
                DrawText(g, speech, hx - sz.Width / 2, hy - 16 * S - sz.Height - 2 * S, Color.White);
            }
        }

        float YOf(object o)
        {
            if (o == this) return hy;
            if (o is Tree) return ((Tree)o).Y;
            if (o is Fire) return fireY;
            return ((Enemy)o).Y;
        }

        void DrawTree(Graphics g, Tree t)
        {
            Shadow(g, t.X, t.Y, 16);
            Spr(g, treeSpr, t.X, t.Y, true);
            for (int i = 0; i < 5; i++)
                if (t.Has[i]) Spr(g, appleSpr, AppleX(t, i), AppleY(t, i) + appleSpr.H / 2f, true);
        }

        void DrawArrow(Graphics g, float x, float y, float vx, float vy, float scale)
        {
            float sp = (float)Math.Sqrt(vx * vx + vy * vy);
            if (sp < 1) return;
            float dx = vx / sp, dy = vy / sp, px = -dy, py = dx;
            float len = 10 * S * scale;
            using (Pen sh = new Pen(Color.FromArgb(135, 85, 45), S))
                g.DrawLine(sh, x - dx * len, y - dy * len, x, y);
            using (Pen fe = new Pen(Color.FromArgb(235, 235, 235), S))
            {
                g.DrawLine(fe, x - dx * len, y - dy * len, x - dx * (len - 2 * S) + px * 2 * S, y - dy * (len - 2 * S) + py * 2 * S);
                g.DrawLine(fe, x - dx * len, y - dy * len, x - dx * (len - 2 * S) - px * 2 * S, y - dy * (len - 2 * S) - py * 2 * S);
            }
            PointF[] head = {
                new PointF(x + dx * 3 * S, y + dy * 3 * S),
                new PointF(x + px * 1.5f * S, y + py * 1.5f * S),
                new PointF(x - px * 1.5f * S, y - py * 1.5f * S) };
            using (SolidBrush hb = new SolidBrush(Color.FromArgb(225, 235, 245)))
                g.FillPolygon(hb, head);
        }

        void DrawBow(Graphics g, float f, float bob)
        {
            float cx = hx + f * 7 * S, cy = hy - bob - 8 * S;
            float dx = aimX, dy = aimY, px = -dy, py = dx;
            float pull = Math.Min(1f, bowT / 0.4f);
            PointF[] pts = new PointF[9];
            for (int i = 0; i < 9; i++)
            {
                float u = (i - 4) / 4f, bend = 3 * S * (1 - u * u);
                pts[i] = new PointF(cx + px * u * 7 * S + dx * bend, cy + py * u * 7 * S + dy * bend);
            }
            if (eqBow == 1)      // arco de metal (evolucao 1): prata com contorno escuro
            {
                using (Pen bo = new Pen(Color.FromArgb(28, 22, 36), S + 3)) g.DrawLines(bo, pts);
                using (Pen bm = new Pen(Color.FromArgb(185, 192, 205), S + 1)) g.DrawLines(bm, pts);
                using (Pen bh = new Pen(Color.FromArgb(240, 245, 255), 1)) g.DrawLines(bh, pts);
            }
            else
                using (Pen bw = new Pen(Color.FromArgb(135, 85, 45), S + 1))
                    g.DrawLines(bw, pts);
            float nx = cx - dx * pull * 5 * S, ny = cy - dy * pull * 5 * S;
            using (Pen st = new Pen(Color.FromArgb(235, 235, 235), 1))
            {
                g.DrawLine(st, cx + px * 7 * S, cy + py * 7 * S, nx, ny);
                g.DrawLine(st, nx, ny, cx - px * 7 * S, cy - py * 7 * S);
            }
            if (pull > 0.05f) DrawArrow(g, nx + dx * 10 * S, ny + dy * 10 * S, dx, dy, 1f);
        }

        // braco esticado para colher a maca
        void DrawReach(Graphics g, float f, float bob)
        {
            if ((calmState != 2 || phase != 1) && playPickT <= 0) return;
            float reach = (float)Math.Sin(Math.Min(1f, pickT / 1.0f) * Math.PI * 0.5);
            float x = hx + f * 4 * S, y0 = hy - bob - 10 * S, y1 = y0 - (5 + 5 * reach) * S;
            using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush gb = new SolidBrush(Color.FromArgb(58, 165, 62)))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(250, 208, 165)))
            {
                g.FillRectangle(kb, x - 2 * S, y1, 4 * S, y0 - y1);
                g.FillRectangle(gb, x - S, y1 + 3 * S, 2 * S, y0 - y1 - 3 * S);
                g.FillRectangle(kb, x - 2 * S, y1 - 3 * S, 4 * S, 5 * S);
                g.FillRectangle(sb, x - S, y1 - 2 * S, 2 * S, 3 * S);
            }
        }

        void DrawHero(Graphics g)
        {
            if (dead)
            {
                Shadow(g, hx, hy, 14);
                Spr(g, heroDead, hx, hy, true);
                if (((int)(time * 3)) % 2 == 0) DrawText(g, "x_x", hx - 8 * S, hy - 12 * S, Color.White);
                return;
            }
            if (powerT > 0) { DrawPowerHero(g); return; }
            if (calmState == 2 && phase >= 2) { DrawCalm(g); return; }
            if (inv > 0 && cookState == 0 && ((int)(time * 14)) % 2 == 0) return;
            Shadow(g, hx, hy, 12);
            bool right = hface > 0;
            int fr = walking ? ((int)(walkT * 9)) % 2 : 0;
            float bob = (walking && fr == 1) ? S : 0;
            Spr(g, heroWalk[fr], hx, hy - bob, right);
            if (previewBare)      // sem armas: os dois bracos iguais soltos ao lado do corpo (o da frente e o de tras)
            {
                float ay = hy - bob - 7 * S;
                Color dk = TunicColor();      // manga da cor da camisa
                using (SolidBrush tb = new SolidBrush(dk))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(250, 208, 165)))
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float ax = hx + side * 4 * S;
                        g.FillRectangle(tb, ax - S, ay, 2 * S, 2 * S);
                        g.FillRectangle(sb, ax - S, ay + 2 * S, 2 * S, 2 * S);
                    }
                return;
            }

            // escudo
            float f = hface;
            float sx = hx - f * 3 * S - 3 * S, sy = hy - bob - 11 * S;
            DrawShieldBody(g, sx, sy);


            if (cookState == 6) DrawEatApple(g, f, bob); else if (playEatT > 0) DrawPlayEat(g, f, bob);
            if (calmState == 2 || cookState != 0 || playPickT > 0 || playEatT > 0) { DrawReach(g, f, bob); return; }   // espada guardada
            if (bowing) { DrawBow(g, f, bob); return; }

            // espada
            float handX = hx + f * 5 * S, handY = hy - bob - 4 * S;
            float ang;
            float prog = attacking ? Math.Min(1f, atkT / 0.32f) : 0;
            if (attacking) ang = -110f + 170f * (float)(1 - Math.Pow(1 - prog, 2));
            else ang = walking ? -70f : -80f;
            double rad = ang * Math.PI / 180.0;
            float dx = f * (float)Math.Cos(rad), dy = (float)Math.Sin(rad);
            float px = -dy, py = dx;
            float len = 14 * S, w = 1.2f * S;

            if (attacking && prog > 0.2f && prog < 0.95f)
            {
                using (SolidBrush ab = new SolidBrush(Color.FromArgb(235, 245, 255)))
                    for (float a = -110f; a < ang; a += 10f)
                    {
                        double r2 = a * Math.PI / 180.0;
                        float ax = handX + f * (float)Math.Cos(r2) * len * 1.05f;
                        float ay = handY + (float)Math.Sin(r2) * len * 1.05f;
                        g.FillRectangle(ab, ax - S, ay - S, 2 * S, 2 * S);
                    }
            }
            PointF[] blade = {
                new PointF(handX + px * w, handY + py * w),
                new PointF(handX - px * w, handY - py * w),
                new PointF(handX + dx * len - px * w, handY + dy * len - py * w),
                new PointF(handX + dx * (len + 3 * S), handY + dy * (len + 3 * S)),
                new PointF(handX + dx * len + px * w, handY + dy * len + py * w)
            };
            using (SolidBrush bl = new SolidBrush(BladeColor()))
            using (Pen ol = new Pen(Color.FromArgb(28, 22, 36), Math.Max(1f, S)))      // contorno na mesma espessura do resto do jogo (1 pixel da arte)
            {
                g.FillPolygon(bl, blade);
                g.DrawPolygon(ol, blade);
            }
            using (Pen gd = new Pen(GuardColor(), 2 * S))
                g.DrawLine(gd, handX + px * 3 * S, handY + py * 3 * S, handX - px * 3 * S, handY - py * 3 * S);
            using (Pen hd = new Pen(GripColor(), S + 1))
                g.DrawLine(hd, handX, handY, handX - dx * 3 * S, handY - dy * 3 * S);
        }

        // Descanso (fases 2-5): abaixa espada e escudo, [acena], agacha e dorme de costas, de perfil (pose do Snoopy)
        void DrawCalm(Graphics g)
        {
            if (phase == 5)
            {
                Shadow(g, hx, hy + S, 17);
                Spr(g, heroSideSleep, hx, hy + S, true);       // bracos retos, junto ao corpo, encostados no chao
                float zt = time * 0.8f;
                for (int i = 0; i < 3; i++)
                {
                    float ph = (zt + i * 0.33f) % 1f;
                    DrawText(g, i == 1 ? "Z" : "z", hx - 9 * S + ph * 8 * S + i * S, hy - 15 * S - ph * 14 * S, Color.White);
                }
                return;
            }
            if (phase == 4)
            {
                Shadow(g, hx, hy, 12);
                Spr(g, heroFront, hx, hy + S * 2, true);       // agachando
                return;
            }
            Shadow(g, hx, hy, 12);
            bool waving = phase == 3;
            Spr(g, waving ? heroFrontWave : heroFront, hx, hy, true);
            float q = phase == 2 ? Math.Min(1f, phaseT / 1.5f) : 1f;
            q = q * q * (3 - 2 * q);

            // escudo: do peito para o chao, ao lado
            float sx = hx - 7 * S + (-5 * S) * q, sy = hy - 12 * S + 6 * S * q;
            DrawShieldBody(g, sx, sy);


            if (!waving)
            {
                // espada na mao direita: da posicao de combate para a ponta no chao
                float handX = hx + 6 * S, handY = hy - 5 * S;
                double rad = (-80f + 175f * q) * Math.PI / 180.0;
                float dx = (float)Math.Cos(rad), dy = (float)Math.Sin(rad), px = -dy, py = dx;
                float len = 12 * S, w = 1.2f * S;
                PointF[] blade = {
                    new PointF(handX + px * w, handY + py * w), new PointF(handX - px * w, handY - py * w),
                    new PointF(handX + dx * len - px * w, handY + dy * len - py * w),
                    new PointF(handX + dx * (len + 3 * S), handY + dy * (len + 3 * S)),
                    new PointF(handX + dx * len + px * w, handY + dy * len + py * w) };
                using (SolidBrush bl = new SolidBrush(BladeColor()))
                using (Pen ol = new Pen(Color.FromArgb(28, 22, 36), Math.Max(1f, S)))
                using (Pen gd = new Pen(GuardColor(), 2 * S))
                {
                    g.FillPolygon(bl, blade); g.DrawPolygon(ol, blade);
                    g.DrawLine(gd, handX + px * 3 * S, handY + py * 3 * S, handX - px * 3 * S, handY - py * 3 * S);
                }
            }
            else
            {
                // espada guardada nas costas (so o cabo aparece sobre o ombro) e a mao direita acena
                float sw = (float)Math.Sin(time * 9) * S;
                using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
                using (SolidBrush gb = new SolidBrush(TunicColor()))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(250, 208, 165)))
                using (SolidBrush gold = new SolidBrush(Color.FromArgb(215, 160, 40)))
                using (SolidBrush br = new SolidBrush(Color.FromArgb(135, 85, 45)))
                {
                    g.FillRectangle(br, hx - 7 * S, hy - 11 * S, 2 * S, 3 * S);
                    g.FillRectangle(gold, hx - 8 * S, hy - 9 * S, 4 * S, S);
                    g.FillRectangle(kb, hx + 4 * S, hy - 12 * S, 4 * S, 6 * S);       // manga, saindo do ombro
                    g.FillRectangle(gb, hx + 5 * S, hy - 11 * S, 2 * S, 4 * S);
                    g.FillRectangle(kb, hx + 4 * S + sw, hy - 16 * S, 4 * S, 5 * S);  // mao
                    g.FillRectangle(sb, hx + 5 * S + sw, hy - 15 * S, 2 * S, 3 * S);
                }
            }
        }

                // ---------- fogueira e macas assadas ----------
        void DrawBakeApple(Graphics g, int idx, float cx, float cy, float sc)
        {
            Sprite sp = appleBake[idx];
            int w = (int)(sp.W * sc), h = (int)(sp.H * sc);
            g.DrawImage(sp.R, (int)(cx - w / 2f), (int)(cy - h / 2f), w, h);
        }

        void DrawFire(Graphics g)
        {
            if (fireK <= 0) return;
            float x = fireX, y = fireY;
            Shadow(g, x, y + S, 16);
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush st = new SolidBrush(Color.FromArgb(150, 150, 162)))
            using (SolidBrush br = new SolidBrush(Color.FromArgb(135, 85, 45)))
            using (SolidBrush dk = new SolidBrush(Color.FromArgb(95, 60, 30)))
            {
                g.FillRectangle(k, x - 9 * S, y - 3 * S, 18 * S, 4 * S);        // toras
                g.FillRectangle(br, x - 8 * S, y - 2 * S, 16 * S, 2 * S);
                g.FillRectangle(dk, x - 8 * S, y - S, 16 * S, S);
                for (int i = 0; i < 7; i++)                                       // pedrinhas ao redor
                {
                    float sx = x - 10.5f * S + i * 3.5f * S;
                    g.FillRectangle(k, sx - S, y + S, 3 * S, 2 * S);
                    g.FillRectangle(st, sx, y + S + S / 2, S + S / 2, S);
                }
            }
            Color[] cs = { Color.FromArgb(222, 62, 44), Color.FromArgb(252, 152, 42), Color.FromArgb(255, 232, 96) };
            int[] hs = { 13, 10, 6 }, ws = { 11, 7, 3 };
            for (int L = 0; L < 3; L++)
            {
                int h = (int)(hs[L] * fireK * (1f + 0.16f * (float)Math.Sin(time * 13 + L * 2)));
                using (SolidBrush b = new SolidBrush(cs[L]))
                    for (int r = 0; r < h; r++)
                    {
                        float fr = r / (float)Math.Max(1, h);
                        int w = Math.Max(1, (int)Math.Round(ws[L] * (1f - fr * fr * 0.92f)));
                        int off = (int)Math.Round(Math.Sin(time * 9 + r * 0.7 + L) * 0.7 * fr * 2);
                        g.FillRectangle(b, x - w * S / 2f + off * S, y - 3 * S - (r + 1) * S, w * S, S);
                    }
            }
            using (SolidBrush sp = new SolidBrush(Color.FromArgb(255, 220, 90)))
                for (int i = 0; i < 4; i++)                                       // faiscas
                {
                    float ph = (time * 0.9f + i * 0.27f) % 1f;
                    if (ph > 0.75f) continue;
                    g.FillRectangle(sp, x + (float)Math.Sin(ph * 7 + i * 2) * 5 * S, y - 12 * S * fireK - ph * 14 * S, S, S);
                }
            for (int i = 0; i < 5; i++)                                           // macas na brasa
            {
                if (!onFire[i]) continue;
                DrawBakeApple(g, bossPhase == 1 ? Math.Min(3, (int)(bakeTs[i] / 10f * 4f)) : BakeIdx(), SlotX(i), SlotY(i) - S, 1.5f);
                if ((cookState == 4 && bakeT > 3f) || (bossPhase == 1 && bakeTs[i] > 3f))
                    using (SolidBrush sm = new SolidBrush(Color.FromArgb(235, 235, 242)))
                        for (int k2 = 0; k2 < 2; k2++)
                        {
                            float ph = (time * 0.8f + i * 0.31f + k2 * 0.5f) % 1f;
                            if (ph > 0.85f) continue;
                            g.FillRectangle(sm, SlotX(i) + (float)Math.Sin(ph * 6 + i) * S, SlotY(i) - 6 * S - ph * 13 * S, S, S);
                        }
            }
            // ponta da chama na frente da pilha (as macas ficam "lambidas" pelo fogo)
            for (int L = 1; L < 3; L++)
            {
                int h = (int)(hs[L] * fireK * (1f + 0.16f * (float)Math.Sin(time * 13 + L * 2)));
                using (SolidBrush b = new SolidBrush(cs[L]))
                    for (int r = 7; r < h; r++)
                    {
                        float fr = r / (float)Math.Max(1, h);
                        int w = Math.Max(1, (int)Math.Round(ws[L] * (1f - fr * fr * 0.92f)));
                        int off = (int)Math.Round(Math.Sin(time * 9 + r * 0.7 + L) * 0.7 * fr * 2);
                        g.FillRectangle(b, x - w * S / 2f + off * S, y - 3 * S - (r + 1) * S, w * S, S);
                    }
            }
        }

        void DrawCookApples(Graphics g)
        {
            foreach (CookApple a in cookApples)
            {
                float u = Math.Min(1f, a.T / a.Dur);
                float x = a.X0 + (a.X1 - a.X0) * u, y = a.Y0 + (a.Y1 - a.Y0) * u - (float)Math.Sin(u * Math.PI) * 10 * S;
                DrawBakeApple(g, a.Dir == 1 ? 0 : 3, x, y, 1.5f);
            }
        }

        // maca assada na boca, diminuindo a cada mordida
        void DrawEatApple(Graphics g, float f, float bob)
        {
            int idx = (int)(cookT / 1.2f);
            if (idx > 4) return;
            float p = (cookT % 1.2f) / 1.2f;
            DrawBakeApple(g, 3, hx + f * 5 * S, hy - bob - 9 * S, 2.3f - 1.7f * p);
        }

        void DrawEnemy(Graphics g, Enemy e)
        {
            if (e.Flash > 0 && ((int)(e.Flash * 30)) % 2 == 0) return;
            if (e.Type == 3) { DrawDragon(g, e); return; }
            bool right = e.Face > 0;
            if (e.Type == 0)
            {
                Shadow(g, e.X, e.Y, 12);
                int fr = (e.Mode == 0 && (e.VX != 0 || e.VY != 0)) ? ((int)(e.T * 6)) % 2 : 0;
                Spr(g, octo[fr], e.X, e.Y, right);
            }
            else if (e.Type == 1)
            {
                Shadow(g, e.X, e.Y, 9);
                float lift = 9 * S + (float)Math.Sin(e.T * 6) * 2 * S;
                Spr(g, bat[((int)(e.T * 9)) % 2], e.X, e.Y - lift, right);
            }
            else if (e.Type == 4)
            {
                Shadow(g, e.X, e.Y, 12);
                int bf = e.Mode == 1 ? 2 : (e.Mode == 0 && ((hx - e.X) * (hx - e.X) + (hy - e.Y) * (hy - e.Y)) > (12 * S) * (12 * S) ?((int)(e.T * 6)) % 2 : 0);
                Spr(g, boko[bf], e.X, e.Y, right);
            }
            else
            {
                Shadow(g, e.X, e.Y, 11);
                float hop = e.Mode == 1 ? (float)Math.Sin((1 - e.Timer / 0.45f) * Math.PI) * 5 * S : 0;
                Spr(g, slime[e.Mode == 1 ? 0 : 1], e.X, e.Y - hop, right);
            }
        }

        void DrawPuff(Graphics g, Puff p)
        {
            float t = p.T / 0.45f;
            Color c = p.Kind == 0 ? Color.White : (p.Kind == 1 ? Color.FromArgb(255, 235, 70) : Color.FromArgb(170, 215, 255));
            int n = p.Kind == 1 ? 6 : 8;
            float rad = (p.Kind == 2 ? (1 - t) : t) * (p.Kind == 1 ? 8 : 12) * S;
            float sz = Math.Max(1, (1 - t) * 3 * S);
            using (SolidBrush b = new SolidBrush(c))
                for (int i = 0; i < n; i++)
                {
                    double a = i * Math.PI * 2 / n + (p.Kind == 2 ? t * 3 : 0);
                    g.FillRectangle(b, p.X + (float)Math.Cos(a) * rad - sz / 2, p.Y + (float)Math.Sin(a) * rad - sz / 2, sz, sz);
                }
        }

        void DrawHud(Graphics g)
        {
            int hw = heartFull.W + S;
            float total = MaxHP * hw + 34 * S + 22 * S + (playMode ? 30 * S : 0);
            float x0 = W / 2 - total / 2, y0 = 4 * S;
            for (int i = 0; i < MaxHP; i++)
                g.DrawImage((i + 1 <= hp ? heartFull : (i + 0.5f <= hp ? heartHalf : heartEmpty)).R, (int)(x0 + i * hw), (int)y0);
            float rx = x0 + MaxHP * hw + 4 * S;
            g.DrawImage(rupeeSpr[0].R, (int)rx, (int)y0 - S);
            DrawText(g, "x" + rupees, rx + rupeeSpr[0].W + 2 * S, y0, Color.White);
            float axx = rx + rupeeSpr[0].W + 2 * S + 18 * S;
            g.DrawImage(appleHud.R, (int)axx, (int)y0 - S);          // maca do tamanho da rupia
            DrawText(g, "x" + apples, axx + appleHud.W + 2 * S, y0, Color.White);
            if (playMode) DrawText(g, "NV " + (RupeeStep() + 1), axx + appleHud.W + 2 * S + 16 * S, y0, Color.FromArgb(255, 220, 90));
            DrawBossBar(g);
            if (!playMode)
            {
                string gs = powerT > 0 ? "PODER! " + (int)Math.Ceiling(powerT) + "s" : "GELATINAS " + slimeKills + "/" + SlimesForPower;
                SizeF gz = g.MeasureString(gs, font);
                DrawText(g, gs, W - gz.Width - 6 * S, 4 * S, powerT > 0 ? Color.FromArgb(120, 230, 255) : Color.FromArgb(150, 200, 255));
            }
            if (playMode) DrawControlsHelp(g);
            if (paused) DrawPauseBanner(g);
        }
    }
}


