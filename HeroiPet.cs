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
                Guarda.Restore();      // se o registro veio com menos progresso que a copia de seguranca, volta a copia (antes do jogo ler tudo)
                Application.EnableVisualStyles();
                Application.Run(new PetForm());
            }
        }
    }

    // Copia de seguranca do progresso: todo o registro do jogo (HKCU\Software\HeroiDoDesktop) e guardado tambem em
    // %APPDATA%\HeroiDoDesktop\progresso.txt (mais progresso.bak, a copia anterior). Ao abrir, se o registro tiver MENOS monstros que a copia
    // (registro zerado/antigo, desligamento brusco do Windows), a copia volta para o registro. A copia nunca e trocada por algo com menos monstros.
    static class Guarda
    {
        const string Sub = @"Software\HeroiDoDesktop";
        static bool Teste { get { return Environment.GetEnvironmentVariable("HEROI_TESTE") != null; } }
        static string Dir { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeroiDoDesktop"); } }
        static string Arq { get { return System.IO.Path.Combine(Dir, "progresso.txt"); } }
        static Dictionary<string, string> Ler(string f)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try { foreach (string ln in System.IO.File.ReadAllLines(f)) { int i = ln.IndexOf('='); if (i > 0) d[ln.Substring(0, i)] = ln.Substring(i + 1); } } catch { }
            return d;
        }
        static int Monstros(Dictionary<string, string> d)
        {
            string v; int n;
            if (d.TryGetValue("Monstros", out v) && v.StartsWith("d:") && int.TryParse(v.Substring(2), out n)) return n;
            return -1;
        }
        static int NoRegistro()
        {
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Sub)) { if (k != null) { object v = k.GetValue("Monstros"); if (v != null) return Convert.ToInt32(v); } } } catch { }
            return 0;
        }
        public static void Restore()
        {
            if (Teste) return;
            try
            {
                Dictionary<string, string> d = Ler(Arq);
                int guardado = Monstros(d);
                if (guardado < 0 || NoRegistro() >= guardado) return;
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(Sub))
                {
                    foreach (KeyValuePair<string, string> kv in d)
                    {
                        if (kv.Key == "Recorde") continue;      // o recorde tem a propria copia (recorde.txt)
                        int n;
                        if (kv.Value.StartsWith("d:") && int.TryParse(kv.Value.Substring(2), out n)) k.SetValue(kv.Key, n, RegistryValueKind.DWord);
                        else if (kv.Value.StartsWith("s:")) k.SetValue(kv.Key, kv.Value.Substring(2), RegistryValueKind.String);
                    }
                    k.Flush();
                }
            }
            catch { }
        }
        public static void Backup()
        {
            if (Teste) return;
            try
            {
                int atual = NoRegistro();
                if (atual <= 0 || atual < Monstros(Ler(Arq))) return;      // nunca troca a copia por uma com menos progresso
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(Sub, true))
                {
                    if (k == null) return;
                    foreach (string nome in k.GetValueNames())
                    {
                        object v = k.GetValue(nome);
                        if (v == null || nome.Length == 0) continue;
                        sb.Append(nome).Append('=').Append(v is int ? "d:" : "s:").Append(v.ToString().Replace("\r", "").Replace("\n", "")).Append('\n');
                    }
                    k.Flush();      // forca o Windows a gravar o registro no disco agora
                }
                string novo = sb.ToString();
                if (novo == ultimo) return;      // nada mudou desde a ultima copia: sem escrita em disco
                System.IO.Directory.CreateDirectory(Dir);
                string tmp = Arq + ".novo";
                System.IO.File.WriteAllText(tmp, novo);
                if (System.IO.File.Exists(Arq)) System.IO.File.Copy(Arq, System.IO.Path.Combine(Dir, "progresso.bak"), true);
                System.IO.File.Copy(tmp, Arq, true);
                System.IO.File.Delete(tmp);
                ultimo = novo;
            }
            catch { }
        }
        static string ultimo = "";
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
        public static bool Crown, Robe, Legs, MushHat, MushShirt, AppleHat, Tiara, CuteShirt;      // Tiara: conjunto fada (tiara em vez de gorro); CuteShirt: blusa 1 quadradinho maior com centro lilas      // AppleHat: gorro do conjunto maca (cabinho de maca em cima)
        static string Ctr(string t, int w) { int pad = Math.Max(0, (w - 14) / 2); return (new string('.', pad) + t).PadRight(w, '.').Substring(0, w); }      // centraliza um desenho de 14 de largura      // MushHat/MushShirt: bolinhas brancas do conjunto cogumelo      // Legs: conjunto com calca e sapatos coloridos (J/Z)     // equipado: coroa de ouro / roupa de rei (com calca marrom e sapatos roxos)
        static string CuteTunic(string s, int i)      // blusa um pouco maior (1 quadradinho para tras) e, nas linhas da blusa, o meio fica lilas ('z')
        {
            char[] a = s.ToCharArray(); int f = 0; while (f < a.Length && a[f] == '.') f++;
            if (f > 0 && f + 1 < a.Length) { a[f - 1] = 'K'; a[f] = a[f + 1] == 'K' ? 'Q' : a[f + 1]; if (i == 13) a[f] = 'B'; }      // aumenta a blusa para tras (o contorno vai 1 quadradinho para fora)
            if (false) { int q0 = -1, q1 = -1; for (int c = 0; c < a.Length; c++) if (a[c] == 'Q') { if (q0 < 0) q0 = c; q1 = c; } if (q0 >= 0) { int m = (q0 + q1) / 2; if (a[m] == 'Q') a[m] = 'z'; if (m + 1 < a.Length && a[m + 1] == 'Q') a[m + 1] = 'z'; } }
            return new string(a);
        }
        static string HatDetail(string s, int i)      // i = brilho (claro); so troca pixels do gorro (H). Sem sombra escura.
        {
            char[] a = s.ToCharArray();
            if (i == 1) { foreach (int c in new int[] { 5, 6 }) if (c < a.Length && a[c] == 'H') a[c] = 'i'; }      // so o brilho claro; sem sombra escura
            else if (i == 2) { if (4 < a.Length && a[4] == 'H') a[4] = 'i'; }
            return new string(a);
        }
        static string Spots(string s, int[] cols, char target)      // troca alguns pixels do gorro/blusa por branco (bolinhas de cogumelo)
        {
            char[] a = s.ToCharArray();
            foreach (int c in cols) if (c < a.Length && a[c] == target) a[c] = 'W';
            return new string(a);
        }
        public static string[] HeroRecolor(string[] rows)
        {
            List<string> r = new List<string>();
            if (Crown && !Tiara) { r.Add("....C.CC.C...."); r.Add("....CCCCCC...."); r.Add("....CRCCRC...."); }      // coroa sobre o gorro
            if (Tiara) { int tw = rows[0].Length; r.Add(Ctr(".....FPPF.....", tw)); r.Add(Ctr("...F.FFFF.F...", tw)); }      // a 3a fileira da tiara substitui o contorno de cima da cabeca (i == 0): assim ela encosta no cabelo      // tiara PRATEADA (F/A) com joia lilas;  com joia lilas (sem gorro: o cabelo aparece)
            if (AppleHat) { int aw = rows[0].Length; r.Add(Ctr("........KNNK..", aw)); r.Add(Ctr(".....KBBKNNK..", aw)); r.Add(Ctr(".....KBBK.....", aw)); }      // cabinho de maca: talo marrom e folha verde
            for (int i = 0; i < rows.Length; i++)
            {
                if (Tiara && i == 0) { r.Add(Ctr("...AFFFFFFA...", rows[0].Length)); continue; }
                string s = rows[i].Replace('Y', 'V').Replace('G', i < 9 ? (Crown ? 'V' : 'H') : 'Q');      // cabelo castanho ('V') para contrastar com o rosto; com a coroa: sem gorro (cabelo aparece) e a coroa fica sobre a cabeca
                if (Crown && (i == 4 || i == 5) && s.StartsWith("KV")) s = ".K" + s.Substring(2);      // sem gorro: tira a pontinha do canto esquerdo da cabeca
                if (MushHat && i >= 1 && i <= 3) s = Spots(s, i == 1 ? new int[] { 6 } : (i == 2 ? new int[] { 4, 8 } : new int[] { 3, 6, 10 }), 'H');
                if (!Crown && i >= 1 && i <= 3) s = HatDetail(s, i);      // brilho e sombra do gorro, como no icone do inventario
                if (CuteShirt && i >= 10 && i <= 13 && (rows == HeroA || rows == HeroB)) s = CuteTunic(s, i);      // so de lado: de frente a blusa fica simetrica      // blusa do conjunto fada: 1 quadradinho maior atras e centro lilas
                if (Legs && i == 13) s = s.Replace('B', 'b');      // faixa marrom da blusa vira tom mais escuro da blusa (amarela na noite); cor via hov[b]
                if (MushShirt && i >= 10 && i <= 12) s = Spots(s, i == 10 ? new int[] { 5, 9 } : (i == 11 ? new int[] { 4, 7, 10 } : new int[] { 5, 9 }), 'Q');
                if (Legs && !Robe) { if (i == 14) s = s.Replace('W', 'J'); else if (i == 15) s = s.Replace('B', 'Z'); }
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

        public static Func<Color, Color> Tint;      // fase 2: monstros em tons sombrios (aplicado a cada pixel ao montar o sprite)
        // Feiticeiro Sombrio (vilao da fase 2): capuz roxo, olhos amarelos, barra dourada (R roxo, r sombra, E olhos, G ouro)
        public static string[] MageRows = {
            "......KKKKKK......",
            "....KKRRRRRRKK....",
            "...KRRRRRRRRRRK...",
            "..KRRRRRRRRRRRRK..",
            "..KRRRKKKKKKRRRK..",
            "..KRRKKKKKKKKRRK..",
            "..KRRKEEKKEEKRRK..",
            "..KRRKKKKKKKKRRK..",
            "..KRRRKKKKKKRRRK..",
            "...KRRRRKKRRRRK...",
            "..KRRRRRRRRRRRRK..",
            ".KRRRGRRRRRRGRRRK.",
            ".KRrRRGRRRRGRRrRK.",
            ".KRrRRRGGGGRRRrRK.",
            ".KRrRRRRRRRRRRrRK.",
            ".KRrrRRRRRRRRrrRK.",
            ".KRrrRRRRRRRRrrRK.",
            "KRRrrRRRRRRRRrrRRK",
            "KRRrrRRRRRRRRrrRRK",
            "KRRRRRRRRRRRRRRRRK",
            "KGGGGGGGGGGGGGGGGK",
            "KKKKKKKKKKKKKKKKKK"
        };
        // Feiticeiro do tamanho do Link (14 x 16): capuz roxo, olhos amarelos, barra dourada (mesmas letras do Feiticeiro grande)
        public static string[] WizRows = {
            "....KKKKKK....",
            "...KRRRRRRK...",
            "..KRRRRRRRRK..",
            "..KRRKKKKRRK..",
            "..KRKEKKEKRK..",
            "..KRRKKKKRRK..",
            "...KRRKKRRK...",
            "..KRRRRRRRRK..",
            ".KRRGRRRRGRRK.",
            ".KRrRGGGGRrRK.",
            ".KRrRRRRRRrRK.",
            ".KRrRRRRRRrRK.",
            ".KRRRRRRRRRRK.",
            ".KRRRRRRRRRRK.",
            ".KGGGGGGGGGGK.",
            ".KKKKKKKKKKKK."
        };
        // pedras do lugar escuro: 3 modelos desenhados a mao (0 bloco anguloso com musgo, 1 rocha alta facetada, 2 rocha larga com pedrinha);
        // K contorno, c luz, a base, d sombra, m musgo
        static readonly string[][] StoneArt = {
            new string[] {
                "......KKKK......",
                "....KKccccKK....",
                "...KmmmccaaaK...",
                "..KcccccaaaaadK.",
                ".KcccccaaaaaaadK",
                "KccccaaaKaaaaddK",
                "KcccaaaaKaaaaddK",
                "KccaaaaaaKaaddKK",
                "KKaaaaaaaaaadddK",
                ".KKaaaaaaaaadddK",
                "..KKddddddddddK.",
                "....KKKKKKKKKK.." },
            new string[] {
                "....KKKKKK....",
                "...KccccccK...",
                "..KcccccaaaK..",
                "..KccccaaaaaK.",
                ".KcccccaaaaaaK",
                ".KccccaaaaaadK",
                ".KcccaaKaaaadK",
                "KccaaaaaKaaadK",
                "KcaaaaaaaKaddK",
                "KaaaaaaaaaaddK",
                "KKaaaaaaaadddK",
                ".KKaaaaaadddK.",
                "..KKddddddKK..",
                "....KKKKKK...." },
            new string[] {
                "......KKKK..........",
                "....KKccccKK...KKK..",
                "...KcccccaaaKKKccaK.",
                "..KccccaaaaaaKcaaadK",
                ".KcccaaaaaaaKaaaaddK",
                ".KccaaaaaaaaadKaaadK",
                "KcaaaaaaaaaaddKaaddK",
                "KaaaaaaaaaaadddKddKK",
                "KKaaaaaaaaaddddKKKK.",
                ".KKddddddddddddKK...",
                "..KKKKKKKKKKKKKK...." } };
        public static string[] StoneRows(int v) { return StoneArt[v % 3]; }
        // cajado do Feiticeiro (11 x 33): esfera roxa com brilho amarelo presa por garras douradas, haste roxa torcida com aneis dourados (mesma paleta do Feiticeiro)
        public static string[] StaffRows = {
            "...KKKKK...",
            "..KoooooK..",
            ".KooqqoooK.",
            ".KoqEEoooK.",
            ".KooEEoooK.",
            ".KoooooooK.",
            "..KoooooK..",
            "G.KKoooKK.G",
            "GG.KKKKK.GG",
            ".GGKGGGKGG.",
            "..GGGrGGG..",
            "...KGGGK...",
            "....KRK....",
            "....KrK....",
            "....KRK....",
            "...KRrK....",
            "...KrRK....",
            "....KRK....",
            "...KGGGK...",
            "....KrK....",
            "....KRK....",
            "....KrK....",
            "...KRrK....",
            "...KrRK....",
            "....KRK....",
            "...KGGGK...",
            "....KRK....",
            "....KrK....",
            "....KRK....",
            "....KrK....",
            "....KGK....",
            "....KGK....",
            ".....K....."
        };
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
                        if (Tint != null) col = Tint(col);
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
            "...KGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KGGYYYYYYYSK.",
            ".KGYYYYYYYYSSK",
            ".KYYYYYYYYKSSK",
            "..KYYYYYYSSSK.",
            "..KYYYYYSSSK..",
            "..KYYKGGGGKK..",
            "...KGGGGGGGK..",
            "...KGGGGGGGK..",
            "...KGGGGGGSK..",
            "...KBBBBBBBK..",
            "...KWWK.KWWK..",
            "...KBBK.KBBK.."
        };
        public static string[] HeroB = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "...KGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KGGYYYYYYYSK.",
            ".KGYYYYYYYYSSK",
            ".KYYYYYYYYKSSK",
            "..KYYYYYYSSSK.",
            "..KYYYYYSSSK..",
            "..KYYKGGGGKK..",
            "...KGGGGGGGK..",
            "...KGGGGGGGK..",
            "...KGGGGGGSK..",
            "...KBBBBBBBK..",
            "...KWWKKWWWK..",
            "...KBBKKBBBK.."
        };

        // ---------- Heroi dormindo: o desenho de frente (olhos fechados, maos na barriga)
        // e girado 90 graus, entao fica de barriga para cima, com exatamente o mesmo tamanho de em pe
        public static string[] HeroFrontSleep = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KYYYYYYYYYYK.",
            ".KYYYYYSSSYYK.",
            ".KYSKKSSKKSYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KGGGGSSGGGGK.",
            "..KBBBBBBBBK..",
            "..KWWWKKWWWK..",
            "..KBBBKKBBBK.."
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

        // heroi de frente para o inventario: mesma cabeca e franja do sprite frontal atual, mas com o corpo mais estreito
        public static string[] HeroFrontInv = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KYYYYYYYYYYK.",
            ".KYYYYYSSSYYK.",
            ".KYYYYSSKSSYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "...KGGGGGGK...",
            "..KSGGGGGGSK..",
            "..KSGGGGGGSK..",
            "...KBBBBBBK...",
            "...KWWKKWWK...",
            "...KBBKKBBK..."
        };
        // ---------- Heroi de frente ----------
        public static string[] HeroFront = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KYYYYYYYYYYK.",
            ".KYYYYYSSSYYK.",
            ".KYYYYSSKSSYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "..KGGGGGGGGK..",
            ".KSGGGGGGGGSK.",
            ".KSGGGGGGGGSK.",
            "..KBBBBBBBBK..",
            "..KWWWKKWWWK..",
            "..KBBBKKBBBK.."
        };
        // igual, mas sem a mao esquerda (o braco levantado e desenhado a parte)
        public static string[] HeroFrontWave = {
            "....KKKKKK....",
            "...KGGGGGGK...",
            "..KGGGGGGGGK..",
            ".KGGGGGGGGGGK.",
            ".KYYYYYYYYYYK.",
            ".KYYYYYSSSYYK.",
            ".KYYYYSSKSSYK.",
            "..KYSSSSSSYK..",
            "...KSSSSSSK...",
            "...KKGGGGKK...",
            "..KGGGGGGGGK..",
            ".KSGGGGGGGGGK.",
            ".KSGGGGGGGGGK.",
            "..KBBBBBBBBK..",
            "..KWWWKKWWWK..",
            "..KBBBKKBBBK.."
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
        public static string[] NoClub(string[] rows)      // tira as colunas do porrete (as 2 ultimas)
        {
            string[] r = new string[rows.Length];
            for (int i = 0; i < rows.Length; i++) { string s = rows[i]; char[] c = s.ToCharArray(); for (int k = 12; k < c.Length; k++) if (c[k] == 'B' || c[k] == 'b') c[k] = '.'; r[i] = new string(c); }
            return r;
        }
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
        // conjunto cogumelo: gorro e blusa vermelhos com bolinhas brancas (W) e o Cogumelo-maca (arma)
        public static string[] CapItemMush = {
            "....KKKKKKKK....",
            "...KHWWHHHHHK...",
            "..KHHhHHWWHHgK..",
            ".KHWWHHHHHHWWgK.",
            "KHHHHWWHHHHHHHgK",
            "KHHHHHHHHHHHHggK",
            "KggggggggggggggK",
            ".KKKKKKKKKKKKKK."
        };
        public static string[] ShirtItemMush = {
            "...KKKKKKKKKK...",
            "..KQQQKKKKQQQK..",
            ".KQWQQQhhQQQWQK.",
            ".KQQQWQQQQQQWQK.",
            ".KgQKQQQQQQKQgK.",
            ".KKKKQQQQQQKKKK.",
            "....KQQWQQQK....",
            "....KQQQQWQK....",
            "....KgQQQQgK....",
            "....KBBBBBBK....",
            "....KKKKKKKK...."
        };
        public static string[] MushItem = {
            "....KKKKKKKK....",
            "..KKRRRRRRRRKK..",
            ".KRRRRRWRRRRRRK.",
            "KRRWRRRRRRRWRRRK",
            "KRRRRRWRRRRRRRRK",
            "KRWRRRRRRRWRRRRK",
            "KRRRRRRWRRRRRWRK",
            "KRRRWRRRRRRRRRRK",
            "KKKKKKKKKKKKKKKK",
            ".....KTTTTK.....",
            ".....KTtTTK.....",
            ".....KTTTTK.....",
            ".....KTtTTK.....",
            ".....KTTTTK.....",
            "......KKKK......",
            "................"
        };
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
        public static string[] TiaraItem = {      // icone da tiara prateada (conjunto de fada)
            "......KKKK......",
            ".....KFPPFK.....",
            "..KK.KFPPFK.KK..",
            ".KFFK.KFFK.KFFK.",
            ".KFFFKFFFFKFFFK.",
            ".KAFFFFFFFFFFAK.",
            "..KAAAAAAAAAAK..",
            "...KKKKKKKKKK..."
        };
        public static string[] ShirtItem = {
            "...KKKKKKKKKK...",
            "..KQQQKKKKQQQK..",
            ".KQQQQQhhQQQQQK.",
            ".KQQQQQQQQQQQQK.",
            ".KgQKQQQQQQKQgK.",
            ".KKKKQQQQQQKKKK.",
            "....KQQQQQQK....",
            "....KQQQQQQK....",
            "....KgQQQQgK....",
            "....KBBBBBBK....",
            "....KKKKKKKK...."
};
        // roupa de rei em pixel art: blusa vermelha (Q/g) com faixa dourada (C) no pescoco e na barra
        public static string[] RobeItem = {
            "...KKKKKKKKKK...",
            "..KQQCCCCCCQQK..",
            ".KQQQQCCCCQQQQK.",
            ".KQQQQQCCQQQQQK.",
            ".KgQKQQQQQQKQgK.",
            ".KKKKQQQQQQKKKK.",
            "....KQQQQQQK....",
            "....KQQQQQQK....",
            "....KgQQQQgK....",
            "....KCCCCCCK....",
            "....KKKKKKKK...."
};
        // coroa de ouro em pixel art (C ouro, c ouro escuro, R rubi, U safira)
        public static string[] CrownItem = {
            "......KKKK......",
            "..KK..KCCK..KK..",
            ".KCCK.KCCK.KCCK.",
            ".KCCKKCCCCKKCCK.",
            ".KCCCCCCCCCCCCK.",
            ".KCCCCCRRCCCCCK.",
            ".KCUCCCRRCCCUCK.",
            ".KccccccccccccK.",
            ".KKKKKKKKKKKKKK."
        };
        // marreta de pedra com runa de raio (G pedra, g sombra, h brilho, c/C runa, B/b cabo)
        public static string[] HammerItem = {
            "..KKKKKKKKKKKK..",
            ".KhhhhhhhhhhhhK.",
            ".KhGGGGGGGGGGgK.",
            ".KGGGGGccGGGGgK.",
            ".KGGGGcCCcGGGgK.",
            ".KGGGGGccGGGGgK.",
            ".KgGGGGGGGGGggK.",
            ".KKKKKKKKKKKKKK.",
            ".....KBBBBK.....",
            ".....KBbBBK.....",
            ".....KBbBBK.....",
            ".....KBBBBK.....",
            ".....KBbBBK.....",
            ".....KBBBBK.....",
            ".....KBBBBK.....",
            "......KKKK......"
        };
        // espada de chamas: lamina em chamas (F vermelho-alaranjado nas linguas de fogo, O laranja, Y miolo amarelo), guarda vermelha com centro dourado, cabo escuro (B)
        public static string[] FlameItem = {
            ".......KK.......",
            "......KYYK......",
            ".....KOYYOK.....",
            "....KFOYYOFK....",
            ".....KOYYOK.....",
            "....KFOYYOFK....",
            ".....KOYYOK.....",
            "...KFFOYYOFFK...",
            "....KFOYYOFK....",
            ".....KOYYOK.....",
            "......KOOK......",
            "..KKKKKOOKKKKK..",
            ".KRRRRYYYYRRRRK.",
            ".....KBBBBK.....",
            ".....KBbBBK.....",
            "......KKKK......"
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
        // ---------- FASE 2: os mesmos monstros, mais detalhados (octorok VERDE, bokoblin mais ESCURO com armadura) ----------
        public static string[] Octo2A = { ".K.KKKKKK.K.", "..KGGhhGGK..", ".KGGGhGGGGK.", "KGGWWGGWWGGK", "KGGWKGGWKGGK", "KGgGGGGGGgGK", "KGGGKKKKGGGK", ".KGgGGGGgGK.", "..KKGGGGKK..", ".KG.K..K.GK." };
        public static string[] Octo2B = { ".K.KKKKKK.K.", "..KGGhhGGK..", ".KGGGhGGGGK.", "KGGWWGGWWGGK", "KGGWKGGWKGGK", "KGgGGGGGGgGK", "KGGGKKKKGGGK", ".KGgGGGGgGK.", "..KKGGGGKK..", "..KG.KK.GK.." };
        public static string[] Bat2Up = { "KK..........KK", "KPK...KK...KPK", "KPpK.KPPK.KpPK", "KPpPKPEPEPKPpK", ".KPpPPPPPPpPK.", "..KKPWPPWPKK..", "....KKPPKK....", "......KK......" };
        public static string[] Bat2Down = { "......KK......", "....KKPPKK....", "..KKPPPPPPKK..", ".KPpPEPEPPpPK.", "KPK.KPWWPK.KPK", "KK...KKKK...KK", "..............", ".............." };
        public static string[] Slime2Tall = { "....KKKK....", "..KKUUUUKK..", ".KULLUUUUUK.", ".KULUUUUUUK.", "KUUWKUUWKUUK", "KUuUUUUUUuUK", "KUUUUUUUUUUK", ".KKUUUUUUKK.", "...KKKKKK..." };
        public static string[] Slime2Flat = { "............", "............", "............", "...KKKKKK...", ".KKUULUUUKK.", "KUULUUUUUuUK", "KUWKUUUWKUUK", "KUUUUUUUUuUK", ".KKKKKKKKKK." };
        public static string[] Boko2A = { "..h.......h...", "..KKKKKKKKK...", ".KkkkkkkkkK...", ".KkWKkkWKkkK..", ".KkkkkkkkkK...", ".KkkKWKWKkkK..", "..KkkkkkkkK.BB", ".KAABBBBAAK.bB", ".KAABBBBAAK.bB", ".KkABBBBAkK.bB", "..KKBBBBKK..bB", "...KAK..KAK...", "...KAK..KAK...", "..KKKK..KKKK.." };
        public static string[] Boko2B = { "..h.......h...", "..KKKKKKKKK...", ".KkkkkkkkkK...", ".KkWKkkWKkkK..", ".KkkkkkkkkK...", ".KkkKWKWKkkK..", "..KkkkkkkkK.BB", ".KAABBBBAAK.bB", ".KAABBBBAAK.bB", ".KkABBBBAkK.bB", "..KKBBBBKK..bB", "..KAK....KAK..", ".KAK......KAK.", ".KKK......KKK." };
        public static string[] Boko2C = { "..h.......hbB.", "..KKKKKKKKKbB.", ".KkkkkkkkkKbB.", ".KkWKkkWKkkKbB", ".KkkkkkkkkKbB.", ".KkkKWKWKkkKbB", "..KkkkkkkkK.bB", ".KAABBBBAAK.bB", ".KAABBBBAAK...", ".KkABBBBAkK...", "..KKBBBBKK....", "...KAK..KAK...", "...KAK..KAK...", "..KKKK..KKKK.." };
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
        public int HP, MaxHP, Face = 1, Mode; public bool Wave, Fairy;      // Fairy: marcado para a fadinha derrotar (20% dos monstros)
      //      // Wave: faz parte da onda final antes do dragao
        public bool NoAtk;      // monstrinho que a estrela vai atingir: nao ataca (nem encosta) antes de virar chefao
        public float SlamCd, SlamT, SlamX, SlamY, SlamX0, SlamY0, Lift; public int SlamPh;      // salto esmagador da Gelatina de Fogo Gigante (fase 3)
        public float TX, TY, RT; public bool Moving; public float Scale = 1f, SmokeT, GrowT, SparkT, ShockT = 8f;      // dragao: destino, tempo para trocar de destino, esta andando
    }
    class Rock { public float X, Y, VX, VY, Life; public bool Fire, Homing, Arrow, Orb, Ember; }      // Ember: bolinha de fogo da gelatina de fogo (fase 3): igual a pedra de octorok (explode em 3 s)      // Orb: esfera sombria do Feiticeiro      // Arrow: flecha dos bokoblins a cavalo
    class Arrow { public float X, Y, VX, VY; public bool Power, Crystal, Glow; }
    class Tree { public float X, Y; public bool Stone; public int Variant; public bool[] Has = new bool[5]; public float[] Regrow = new float[5]; }      // Stone: pedra da Fase 2 (no lugar da macieira)
    class AllyMark { }
    class Spark { public float X, Y, VX, VY, T, Life; public int Col; }      // estrelinha roxa da explosao do Feiticeiro
    class Acid { public float SX, SY, TX, TY, T, Dur; }      // bola de acido do Octorok Gigante: sai da boca e cai onde o heroi estava
    class Puddle { public float X, Y; public int W, H, Type; public Bitmap Bmp; }      // poca de agua (a figura ja vem pronta em Bmp)
    class Drop { public float X, Y, VX, VY, T; }      // gota de agua que espirra      // marcador do Feiticeiro aliado na lista de desenho (ordenada por Y)
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
        // camada de escuridao: filtro cinza + degrade nas bordas (overlay ja pronto) e/ou preto de "escurecer a tela" (fade 0..255)
        public void RenderTint(Bitmap overlay, int fade)
        {
            if (!IsHandleCreated || gfx == null) return;
            gfx.Clear(Color.Transparent);
            if (overlay != null) gfx.DrawImageUnscaled(overlay, 0, 0);
            if (fade > 0) using (SolidBrush fb = new SolidBrush(Color.FromArgb(Math.Min(255, fade), 0, 0, 0))) gfx.FillRectangle(fb, 0, 0, bw, bh);
            SIZE sz = new SIZE(); sz.cx = bw; sz.cy = bh;
            POINT src = new POINT();
            BLENDFUNCTION bf = new BLENDFUNCTION(); bf.Op = 0; bf.Flags = 0; bf.Alpha = 255; bf.Format = 1;
            UpdateLayeredWindow(Handle, IntPtr.Zero, IntPtr.Zero, ref sz, memDC, ref src, 0, ref bf, 2);
        }
        public void Render(List<float[]> list)
        {
            if (!IsHandleCreated || gfx == null) return;
            gfx.Clear(Color.Transparent);
            foreach (float[] r in list)
            {
                if (r.Length == 7)      // brilho do cajado: gradiente transparente (x, y, raio, opacidade 0-255)
                {
                    using (GraphicsPath gp = new GraphicsPath())
                    {
                        gp.AddEllipse(r[0] - r[2], r[1] - r[2], 2 * r[2], 2 * r[2]);
                        using (PathGradientBrush pg = new PathGradientBrush(gp)) { pg.CenterColor = Color.FromArgb((int)r[3], 215, 170, 255); pg.SurroundColors = new Color[] { Color.FromArgb(0, 170, 100, 255) }; gfx.FillPath(pg, gp); }
                    }
                }
                else if (r.Length == 6)      // brilhinho da fadinha: quadradinho pequeno e meio transparente (r[3] = opacidade, r[4] = 1 se a fadinha e rosa)
                {
                    Color sc = Color.FromArgb((int)r[3], 90, 175, 255);      // brilhinhos azuis
                    using (SolidBrush sb = new SolidBrush(sc)) gfx.FillRectangle(sb, r[0], r[1], r[2], r[2]);
                }
                else if (r.Length == 8)      // Link-fada: r[4]==1 -> brilho redondo amarelo (raio r[2]); senao quadradinho amarelo (lado r[2]); r[3] = opacidade
                {
                    if (r[4] == 1f)
                    {
                        using (GraphicsPath gp = new GraphicsPath())
                        {
                            gp.AddEllipse(r[0] - r[2], r[1] - r[2], 2 * r[2], 2 * r[2]);
                            using (PathGradientBrush pg = new PathGradientBrush(gp)) { pg.CenterColor = Color.FromArgb((int)r[3], 255, 235, 90); pg.SurroundColors = new Color[] { Color.FromArgb(0, 255, 210, 40) }; gfx.FillPath(pg, gp); }
                        }
                    }
                    else if (r[4] == 2f) using (SolidBrush bb2 = new SolidBrush(Color.FromArgb((int)r[3], 110, 175, 255)))      // estrelinha azul (cruz) meio transparente
                    {
                        float u3 = r[2] / 3f; gfx.FillRectangle(bb2, r[0] - r[2] / 2f, r[1] - u3 / 2f, r[2], u3); gfx.FillRectangle(bb2, r[0] - u3 / 2f, r[1] - r[2] / 2f, u3, r[2]);
                    }
                    else using (SolidBrush yb = new SolidBrush(Color.FromArgb((int)r[3], 255, 226, 70))) gfx.FillRectangle(yb, r[0] - r[2] / 2f, r[1] - r[2] / 2f, r[2], r[2]);
                }
                else if (r.Length > 4)      // onda de choque do dragao: anel amarelo transparente
                {
                    using (Pen o = new Pen(Color.FromArgb(95, 255, 215, 50), 8 * r[4])) gfx.DrawEllipse(o, r[0], r[1], r[2], r[3]);
                    using (Pen m = new Pen(Color.FromArgb(110, 255, 240, 130), 3 * r[4])) gfx.DrawEllipse(m, r[0], r[1], r[2], r[3]);
                }
                else gfx.FillEllipse(brush, r[0], r[1], r[2], r[3]);
            }
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
        Sprite[] octo = new Sprite[2], bat = new Sprite[2], slime = new Sprite[2], boko = new Sprite[4];
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
        Sprite heroFrontInv, heroFront, heroFrontWave, heroSleepSpr, treeSpr, appleSpr, appleHud;
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
            FairyInit(); ChestInit();      // a fadinha vem ANTES do conjunto de rei
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
            LoadWallets();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--poder") >= 0) { rupees = 30; powerPending = true; }      // teste rapido do poder
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--teste") >= 0) { nextCalm = 3f; testLong = true; }   // teste rapido

            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--lanche") >= 0) apples = 5;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jogar") >= 0) SetPlayMode(true);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--fase2") >= 0) StartPhase2Test();      // atalho: direto na fase 2
#if ATALHOS
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--cena2") >= 0) StartScene2Test();      // atalho: 2a conversa do Feiticeiro
#endif
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--fase3") >= 0) StartPhase3Test();      // atalho: direto na fase 3
#if ATALHOS
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--cena") >= 0) StartSceneTest();      // atalho: cena do Feiticeiro e depois a fase 2
#endif
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
        bool offBow, offSword, offShield;      // arma desequipada (clicar de novo no menu): o Link nao a segura
        public bool FairySet { get { return eqHat == 10 && eqTunic == 10; } }      // tiara + blusa de fada: nao da para equipar armas
        bool OwnBow(int i) { return i == 0 || (i == 1 && evoStage >= 1) || (i == 2 && WeaponsOwned >= 3) || (i == 3 && SetsOwned >= 9); }      // 3 = arco sombrio (conjunto sombra)
        bool OwnSword(int i) { return i == 0 || (i == 1 && evoStage >= 4) || (i == 2 && WeaponsOwned >= 1) || (i == 3 && WeaponsOwned >= 2) || (i == 4 && SetsOwned >= 6) || (i == 5 && SetsOwned >= 9); }      // (6 = conjunto cogumelo)
        // ================= 3 ARMAS NOVAS: marreta (2 maos), espada de chamas e arco de cristal =================
        // Liberadas UMA DEPOIS DA OUTRA, 500 monstros cada (marreta com 500, espada com 1000, arco com 1500), por um contador proprio que comeca em zero ao instalar a versao.
        static readonly int[] WpnThr = { 550, 1100, 1650 };      // 10% mais dificil que 500/1000/1500
        static readonly string[] WpnName = { "Marreta trovão", "Espada de chamas", "Arco de cristal" };
        int wpnKills, wpnKnown; bool wpnTest;
        int WeaponsOwned { get { if (wpnTest) return 3; int n = 0; while (n < 3 && wpnKills >= WpnThr[n]) n++; return n; } }
        int LoadWpnKills()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return 0;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object v = k.GetValue("MonstrosArmas"); if (v != null) return Math.Max(0, Convert.ToInt32(v)); } } } catch { }
            SaveWpnKills(0); return 0;
        }
        void SaveWpnKills(int v)
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("MonstrosArmas", v); } catch { }
        }
        void UnlockWeapon(int i)      // arma nova: ja vem equipada, com aviso
        {
            if (i == 0) eqSword = 2; else if (i == 1) eqSword = 3; else eqBow = 2;
            ValidateEquip(); SaveEquip(); newItemUntil = time + 5f; Say("Arma nova: " + WpnName[i] + "!");
            for (int k = 0; k < 10; k++) puffs.Add(new Puff { Kind = k % 2, X = hx + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = hy - (float)rnd.NextDouble() * 18 * S });
        }
        float SwHit { get { return FairyOn ? 0.12f : eqSword == 2 ? 0.30f : (eqSword == 3 ? 0.07f : (eqSword == 4 ? 0.14f : 0.11f)); } }      // marreta: golpe lento e pesado; espada de chamas: rapida
        float SwEnd { get { return FairyOn ? 0.36f : eqSword == 2 ? 0.55f : (eqSword == 3 ? 0.20f : (eqSword == 4 ? 0.36f : 0.32f)); } }
        float SwCool { get { return FairyOn ? 0.08f : eqSword == 2 ? 0.40f : (eqSword == 3 ? 0.08f : (eqSword == 4 ? 0.24f : 0.22f)); } }
        int SwordDamage() { return FairyOn ? 450 : eqSword == 1 ? 300 : (eqSword == 2 ? 600 : (eqSword == 3 ? 450 : (eqSword == 4 ? 700 : (eqSword == 5 ? 800 : 150)))); }
        float crystalQ = -1f;      // arco de cristal: a 2a flecha sai logo depois da 1a, no mesmo rumo (uma seguida da outra)
        void FireArrows(float sx, float sy)
        {
            if (FairyOn) { arrows.Add(new Arrow { X = sx, Y = sy, VX = aimX * 190 * S, VY = aimY * 190 * S, Glow = true }); return; }      // Link-fada: bolinha amarela brilhante no lugar da flecha
            if (eqBow == 2) { arrows.Add(new Arrow { X = sx, Y = sy, VX = aimX * 210 * S, VY = aimY * 210 * S, Crystal = true }); crystalQ = 0.09f; }
            else arrows.Add(new Arrow { X = sx, Y = sy, VX = aimX * 210 * S, VY = aimY * 210 * S });
        }
        void UpdateCrystalQueue(float dt)
        {
            if (crystalQ < 0) return;
            crystalQ -= dt;
            if (crystalQ > 0) return;
            crystalQ = -1f;
            if (dead || eqBow != 2) return;
            arrows.Add(new Arrow { X = hx + hface * 7 * S, Y = hy - 8 * S, VX = aimX * 210 * S, VY = aimY * 210 * S, Crystal = true });      // 2a flecha, mesma mira
        }        // arma especial na mao (marreta ou espada de chamas); devolve false para as espadas comuns
        bool DrawSpecialSword(Graphics g, float handX, float handY, float dx, float dy)      // armas especiais ficam 20% menores em proporcao ao Link (escala em volta da mao)
        {
            if (eqSword < 2 || eqSword == 5) return false;
            GraphicsState wst = g.Save(); g.TranslateTransform(handX, handY); g.ScaleTransform(0.8f, 0.8f); g.TranslateTransform(-handX, -handY);
            bool wr = DrawSpecialSwordRaw(g, handX, handY, dx, dy);
            g.Restore(wst); return wr;
        }
        bool DrawSpecialSwordRaw(Graphics g, float handX, float handY, float dx, float dy)
        {
            if (eqSword < 2 || eqSword == 5) return false;
            float px = -dy, py = dx; Color dark = Color.FromArgb(28, 22, 36);
            if (eqSword == 2) { DrawPixelHammer(g, handX, handY, dx, dy, S); return true; }      // marreta (forma simples)
            if (eqSword == 4)      // Cogumelo-maca em pixels: talo cremoso, chapeu vermelho e bolinhas redondas
            {
                SmoothingMode oldSm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
                Color cream = Color.FromArgb(238, 224, 188), red = Color.FromArgb(214, 48, 52);
                float[][] spots = { new float[] { 0f, 2f }, new float[] { 3.2f, 1.2f }, new float[] { -3.2f, 1.2f }, new float[] { 1.7f, 3.9f }, new float[] { -1.7f, 3.9f } };
                using (SolidBrush br = new SolidBrush(red))
                for (int ci = -18; ci <= 18; ci++) for (int cj = -18; cj <= 18; cj++)
                {
                    float vx = (ci + 0.5f) * S, vy = (cj + 0.5f) * S, u = (vx * dx + vy * dy) / S, w = (vx * px + vy * py) / S;
                    float ex = w / 5.2f, ey = (u - 4.5f) / 4.8f, exo = w / 6.2f, eyo = (u - 4.5f) / 5.8f;
                    bool cap = u >= 4.5f && ex * ex + ey * ey <= 1f, capO = u >= 3.5f && exo * exo + eyo * eyo <= 1f && !(u < 4.5f && Math.Abs(w) > 6f);
                    bool stem = u >= -2.5f && u < 5f && Math.Abs(w) <= 1.1f, stemO = u >= -3.5f && u < 5f && Math.Abs(w) <= 2.1f;
                    bool dot = false; foreach (float[] s2 in spots) { float qa = w - s2[0], qb = (u - 4.5f) - s2[1]; if (qa * qa + qb * qb <= 1.1f) dot = true; }
                    if (cap && dot) br.Color = Color.White; else if (cap) br.Color = red; else if (capO && u < 4.5f) br.Color = dark; else if (stem) br.Color = cream; else if (capO || stemO) br.Color = dark; else continue;
                    g.FillRectangle(br, handX + ci * S - 0.3f, handY + cj * S - 0.3f, S + 0.6f, S + 0.6f);
                }
                g.SmoothingMode = oldSm;
                return true;
            }
            // ESPADA DE CHAMAS: lamina de aco em brasa (do vermelho escuro no cabo ao amarelo na ponta), guarda de aco com gemas de fogo; SEM labaredas nem fagulhas saindo dela (cobriam o Link)
            DrawFlameSword(g, handX, handY, dx, dy, S);
            return true;
        }
        void DrawFlameSword(Graphics g, float handX, float handY, float dx, float dy, float sc)
        {
            float px = -dy, py = dx; SmoothingMode oldSm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            Color dark = Color.FromArgb(34, 14, 20), outl = Color.FromArgb(90, 20, 18);
            const float L0 = 1.8f, L1 = 17f;      // inicio e fim da lamina (em quadradinhos)
            float tm = time;
            using (SolidBrush br = new SolidBrush(dark))
                for (int ci = -23; ci <= 23; ci++) for (int cj = -23; cj <= 23; cj++)
                {
                    float vx = (ci + 0.5f) * sc, vy = (cj + 0.5f) * sc, u = (vx * dx + vy * dy) / sc, w = (vx * px + vy * py) / sc, aw = Math.Abs(w);
                    Color? c = null;
                    if (u >= -4.2f && u < 0f)      // cabo: couro escuro com aneis em brasa
                    {
                        if (aw <= 0.9f) c = (((int)Math.Floor(u + 8f)) % 2 == 0) ? Color.FromArgb(70, 34, 30) : Color.FromArgb(120, 46, 32);
                        else if (aw <= 1.9f) c = dark;
                    }
                    else if (u >= 0f && u < 1.8f)      // guarda de aco com gemas de fogo nas pontas
                    {
                        if (aw <= 4.4f) c = (aw >= 3.0f) ? Color.FromArgb(255, 150, 40) : Color.FromArgb(104, 100, 122);
                        else if (aw <= 5.4f) c = dark;
                    }
                    else if (u >= L0 && u <= L1)
                    {
                        float t = (u - L0) / (L1 - L0), hw = 1.9f - 1.4f * t;
                        if (u > L1 - 2.6f) hw = Math.Max(0.35f, hw - (u - (L1 - 2.6f)) * 0.22f);      // ponta afilada
                        if (aw <= hw)
                        {
                            Color b0 = t < 0.5f ? Lerp(Color.FromArgb(170, 28, 24), Color.FromArgb(255, 130, 28), t * 2f) : Lerp(Color.FromArgb(255, 130, 28), Color.FromArgb(255, 232, 110), (t - 0.5f) * 2f);
                            c = aw <= 0.5f ? Lerp(b0, Color.FromArgb(255, 250, 205), 0.7f) : b0;      // fio central mais claro
                        }
                        else if (aw <= hw + 1.1f) c = outl;
                    }
                    if (c == null) continue;
                    br.Color = c.Value; g.FillRectangle(br, handX + ci * sc - 0.3f, handY + cj * sc - 0.3f, sc + 0.6f, sc + 0.6f);
                }
            g.SmoothingMode = oldSm;
        }
        static Color Lerp(Color a, Color b, float t) { t = Math.Max(0f, Math.Min(1f, t)); return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
        bool OwnShield(int i) { return i == 0 || (i == 1 && evoStage >= 4) || (i == 2 && kingGear) || (i == 3 && SetsOwned >= 7) || (i == 4 && SetsOwned >= 9); }      // 3 = escudo maca cortada (conjunto maca)
        bool OwnHat(int i) { return i == 0 || (i == 1 && evoStage >= 2) || (i == 2 && kingGear) || (i >= 3 && i - 3 < SetsOwned); }
        bool OwnTunic(int i) { return i == 0 || (i == 1 && evoStage >= 3) || (i == 2 && kingGear) || (i >= 3 && i - 3 < SetsOwned); }

        // ================= 5 CONJUNTOS DE ROUPA (liberados pelo total de monstros derrotados no automatico) =================
        // Cada conjunto = gorro + blusa + calca + sapatos de uma cor. Liberam sozinhos quando evoKills chega ao limite (ver SetThr). Aparecem em Roupas > Gorros / Roupas.
        class OutSet { public string Name; public Color Hat, Tunic, Pants, Shoes; public OutSet(string n, Color h, Color t, Color p, Color s) { Name = n; Hat = h; Tunic = t; Pants = p; Shoes = s; } }
        static readonly OutSet[] Sets = {
            new OutSet("areia", Color.FromArgb(226, 186, 96), Color.FromArgb(205, 118, 52), Color.FromArgb(150, 100, 55), Color.FromArgb(96, 58, 30)),
            new OutSet("gelo", Color.FromArgb(205, 236, 255), Color.FromArgb(120, 190, 235), Color.FromArgb(80, 125, 190), Color.FromArgb(235, 245, 255)),
            new OutSet("fogo", Color.FromArgb(225, 72, 30), Color.FromArgb(185, 38, 28), Color.FromArgb(70, 38, 36), Color.FromArgb(250, 175, 45)),
            new OutSet("noite", Color.FromArgb(82, 66, 180), Color.FromArgb(58, 50, 140), Color.FromArgb(34, 30, 80), Color.FromArgb(205, 175, 70)),
            new OutSet("mar", Color.FromArgb(30, 160, 170), Color.FromArgb(28, 125, 150), Color.FromArgb(20, 80, 110), Color.FromArgb(240, 230, 200)),
            new OutSet("cogumelo", Color.FromArgb(214, 48, 52), Color.FromArgb(214, 48, 52), Color.FromArgb(214, 196, 156), Color.FromArgb(172, 140, 98)),
            new OutSet("maça", Color.FromArgb(200, 34, 44), Color.FromArgb(200, 34, 44), Color.FromArgb(244, 228, 168), Color.FromArgb(70, 150, 60)),
            new OutSet("fada", Color.FromArgb(255, 170, 205), Color.FromArgb(255, 190, 215), Color.FromArgb(255, 160, 196), Color.FromArgb(226, 54, 66)),      // SURPRESA: rosa de algodao-doce com bolinhas brancas, calca clarinha e sapatinhos lilas (bonitinho e fofo)
            new OutSet("sombra", Color.FromArgb(34, 34, 50), Color.FromArgb(38, 38, 56), Color.FromArgb(22, 22, 32), Color.FromArgb(78, 22, 30)) };      // imita o Link Sombrio: gorro e blusa quase pretos, calca escura, sapatos vinho, cinto prateado e rosto palido      // maca: gorro e blusa vermelho-maca, calca cor de polpa, sapatos verde-folha, mais o escudo de maca cortada
        // cogumelo: gorro e blusa vermelhos com bolinhas brancas, calca cor do talo, mais o Cogumelo-maca (arma)
        static readonly int[] SetThr = { 4950, 6600, 8800, 11550, 15400, 17600, 20900, 36000, 52000 };      // os 2 ultimos (fada e sombra) sao DIFICEIS: +15000 e +31000 monstros depois do conjunto maca;      // (10% mais dificil que 4500/6000/8000/10500/14000/16000/19000) monstros derrotados depois de instalar esta versao (setKills) para liberar cada conjunto
        bool setsTest; int setsKnown;
        int setKills;      // monstros derrotados DEPOIS de instalar esta versao (contador proprio: nao herda os monstros que ja foram mortos em versoes antigas)
        int LoadSetKills()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return 0;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object v = k.GetValue("MonstrosConjuntos"); if (v != null) return Math.Max(0, Convert.ToInt32(v)); } } } catch { }
            SaveSetKills(0);      // primeira vez com esta versao: a contagem dos conjuntos comeca agora (zero)
            return 0;
        }
        void SaveSetKills(int v)
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("MonstrosConjuntos", v); } catch { }
        }
        int SetsOwned { get { if (setsTest) return Sets.Length; int n = 0; while (n < Sets.Length && setKills >= SetThr[n]) n++; return n; } }
        static Color SetBelt(int si) { if (si == 7) return Color.FromArgb(176, 216, 255); return si == 3 ? Color.FromArgb(240, 200, 40) : (si == 8 ? Color.FromArgb(196, 198, 214) : Shade(Sets[si].Tunic, -0.45f)); }      // noite: amarelo; sombra: prateado
        static Color Shade(Color c, float t) { return t >= 0 ? Color.FromArgb((int)(c.R + (255 - c.R) * t), (int)(c.G + (255 - c.G) * t), (int)(c.B + (255 - c.B) * t)) : Color.FromArgb((int)(c.R * (1 + t)), (int)(c.G * (1 + t)), (int)(c.B * (1 + t))); }
        void UnlockSet(int i)      // conjunto novo: ja vem vestido, com aviso
        {
            eqHat = 3 + i; eqTunic = 3 + i; if (i == 5) eqSword = 4; if (i == 6) eqShield = 3; if (i == 8) { eqSword = 5; eqShield = 4; eqBow = 3; } ValidateEquip(); SaveEquip(); BuildSprites(); newItemUntil = time + 5f;
            Say("Conjunto novo: " + Sets[i].Name + "!");
            for (int k = 0; k < 10; k++) puffs.Add(new Puff { Kind = k % 2, X = hx + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = hy - (float)rnd.NextDouble() * 18 * S });
        }
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
                        object vo = k.GetValue("EquipOff"); if (vo != null) { int ob = Convert.ToInt32(vo); offBow = (ob & 1) != 0; offSword = (ob & 2) != 0; offShield = (ob & 4) != 0; }
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
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) { k.SetValue("Equip", eqBow + "," + eqSword + "," + eqShield + "," + eqHat + "," + eqTunic); k.SetValue("EquipOff", (offBow ? 1 : 0) | (offSword ? 2 : 0) | (offShield ? 4 : 0)); } } catch { }
        }
        void ValidateEquip()      // nunca fica com algo que o heroi nao tem
        {
            if (!OwnBow(eqBow)) eqBow = 0; if (!OwnSword(eqSword)) eqSword = 0; if (!OwnShield(eqShield)) eqShield = 0; if (!OwnHat(eqHat)) eqHat = 0; if (!OwnTunic(eqTunic)) eqTunic = 0;
        }
        float newItemUntil = -1f;
        void DrawNewItemNotice(Graphics g)
        {
            if (time >= newItemUntil) return;
            string t = "NOVO ITEM"; SizeF z = g.MeasureString(t, font);
            float w = z.Width + 10 * S, h = z.Height + 4 * S, x = W - w - 6 * S, y = H - h - 8 * S;      // canto inferior direito
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36))) using (SolidBrush bg = new SolidBrush(Color.FromArgb(46, 40, 62))) using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            {
                g.FillRectangle(k, x - S, y - S, w + 2 * S, h + 2 * S); g.FillRectangle(bg, x, y, w, h); g.FillRectangle(gold, x, y, S, h);
            }
            DrawText(g, t, x + 6 * S, y + 2 * S, Color.FromArgb(255, 220, 90));
        }
        void AutoEquip(int stage)      // ao ganhar um item, ele e equipado
        {
            offBow = offSword = offShield = false;
            if (stage == 1) eqBow = 1; else if (stage == 2) eqHat = 1; else if (stage == 3) eqTunic = 1; else if (stage == 4) { eqSword = 1; eqShield = 1; } else if (stage == 5) { eqHat = 2; eqTunic = 2; eqShield = 2; }
            ValidateEquip(); SaveEquip();
            newItemUntil = time + 5f;      // aviso pequeno "NOVO ITEM" no canto inferior direito por 5 s
        }
        public void EquipItem(int cat, int id)      // cat: 0 arco, 1 espada, 2 escudo, 3 gorro, 4 roupa
        {
            if (cat == 5) { if (Owns(5, id)) { if (eqFairy == id) offFairy = !offFairy; else { eqFairy = id; offFairy = false; } SaveFairy(); } return; }      // fadinha: so troca a aparencia
            if (cat <= 2 && FairySet) return;      // conjunto de fada: sem armas
            if (cat == 0 && OwnBow(id)) { if (eqBow == id) offBow = !offBow; else { eqBow = id; offBow = false; } } else if (cat == 1 && OwnSword(id)) { if (eqSword == id) offSword = !offSword; else { eqSword = id; offSword = false; } } else if (cat == 2 && OwnShield(id)) { if (eqShield == id) offShield = !offShield; else { eqShield = id; offShield = false; } }
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
            switch (cat) { case 0: return OwnBow(id); case 1: return OwnSword(id); case 2: return OwnShield(id); case 3: return OwnHat(id); case 4: return OwnTunic(id); default: return (id == 0 && fairyState == 1) || (id >= 1 && id <= 3 && fairySkin >= id) || (id == 4 && fairyState == 1 && SetsOwned >= 9); }
        }
        public int Equipped(int cat)
        {
            switch (cat) { case 0: return FairySet || offBow ? -1 : eqBow; case 1: return FairySet || offSword ? -1 : eqSword; case 2: return FairySet || offShield ? -1 : eqShield; case 3: return eqHat; case 4: return eqTunic; default: return offFairy ? -1 : eqFairy; }
        }
        public int AppleCount { get { return apples; } }
        public float HeartsNow { get { return hp; } }
        public int HeartsMax { get { return MaxHP; } }

        // comer uma maca crua pelo inventario: mostra o heroi comendo na tela ampliada e recupera meio coracao
        float menuEatT;
        float menuPickT;
        public bool MenuEating { get { return menuEatT > 0 || menuPickT > 0; } }
        // "Colher maçã" no inventario: o menu fecha, o heroi vai ate a macieira mais proxima e colhe DUAS macas; enquanto isso nao nascem monstros.
        public string MenuPick()
        {
            if (menuEatT > 0 || menuPickT > 0) return null;
            if (dead || cookState != 0 || powerT > 0 || bossPhase != 0 || chestGo || errandPh != 0) return "Agora não dá para colher";
            Tree t = NearestTreeWithApples();
            if (t == null) return "Nenhuma árvore tem maçãs agora";
            errandTree = t; errandLeft = 2; errandPh = 1; pickedFlag = false;
            if (calmState != 0) EndCalm(false);
            CloseMenu(); return null;
        }
        int errandPh, errandLeft; Tree errandTree;      // errandPh: 0 nada | 1 indo ate a arvore | 2 colhendo
        void ErrandAbort() { errandPh = 0; errandTree = null; playPickT = 0; }
        void ErrandControl(float dt)
        {
            if (dead) { ErrandAbort(); return; }
            if (errandPh == 1)
            {
                if (errandTree == null || FirstApple(errandTree) < 0) { errandTree = NearestTreeWithApples(); if (errandTree == null) { ErrandAbort(); return; } }
                float d = (float)Math.Sqrt((errandTree.X - hx) * (errandTree.X - hx) + (errandTree.Y - hy) * (errandTree.Y - hy));
                if (d > 12 * S) { MoveHero(errandTree.X, errandTree.Y, 56 * S, dt); return; }
                hface = errandTree.X >= hx ? 1 : -1; playTree = errandTree; playPickT = 0.001f; pickedFlag = false; pickT = 0; errandPh = 2; return;
            }
            playPickT += dt; pickT = playPickT;
            if (!pickedFlag && playPickT >= 0.4f)
            {
                int idx = FirstApple(errandTree);
                if (idx < 0) { ErrandAbort(); return; }
                errandTree.Has[idx] = false; errandTree.Regrow[idx] = 60f + (float)rnd.NextDouble() * 40f;
                flyApples.Add(new FlyApple { X0 = AppleX(errandTree, idx), Y0 = AppleY(errandTree, idx) });      // a maca voa para o inventario
                pickedFlag = true;
            }
            if (playPickT >= 0.8f) { playPickT = 0; errandLeft--; if (errandLeft <= 0) { ErrandAbort(); Say("Peguei!"); } else errandPh = 1; }
        }
        public string MenuEat()
        {
            if (menuEatT > 0 || menuPickT > 0) return null;
            if (apples <= 0) return "Sem maçãs";
            if (hp >= MaxHP) return "Corações cheios";
            menuEatT = 0.001f; return null;
        }
        public void MenuTick(float dt)
        {
            if (menuPickT > 0)
            {
                menuPickT += dt;
                if (menuPickT >= 0.8f)
                {
                    menuPickT = 0; Tree t = NearestTreeWithApples();
                    if (t != null) { int idx = FirstApple(t); if (idx >= 0) { t.Has[idx] = false; t.Regrow[idx] = 60f + (float)rnd.NextDouble() * 40f; apples++; } }
                }
                return;
            }
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
            float sax = aimX, say = aimY, sbt = bowT, spt = pickT;
            float shx = hx, shy = hy, sp = powerT, si = inv, spk = playPickT, sed = playEatT; bool ser = eatRaw; int shf = hface, sc = calmState, sck = cookState; bool sw = walking, sa = attacking, sb = bowing, sd = dead;
            previewMode = true;
            try
            {
                hx = 0; hy = 0; hface = 1; walking = false; attacking = false; bowing = bow; aimX = 1; aimY = 0; bowT = 0.4f; dead = false; powerT = 0; inv = 0; playPickT = menuPickT; pickT = menuPickT; playEatT = menuEatT; eatRaw = menuEatT > 0; calmState = 0; cookState = 0;
                GraphicsState st = g.Save();
                g.TranslateTransform(cx, groundY); g.ScaleTransform(k, k);
                g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half; g.SmoothingMode = SmoothingMode.None;
                DrawHeroFrontPreview(g, bow, bare);      // no inventario o heroi fica DE FRENTE
                if (fairyState == 1 && !offFairy) DrawFairyShape(g, 9 * S, -25 * S, S, eqFairy, false, ((int)(time * 16)) % 2 == 0);      // a fadinha aparece ao lado da cabeca, com a aparencia escolhida
                g.Restore(st);
            }
            finally { pickT = spt; aimX = sax; aimY = say; bowT = sbt; hx = shx; hy = shy; hface = shf; walking = sw; attacking = sa; bowing = sb; dead = sd; powerT = sp; inv = si; playPickT = spk; playEatT = sed; eatRaw = ser; calmState = sc; cookState = sck; previewMode = false; previewBare = false; }
        }

        // heroi de frente para o inventario: sprite frontal (com as maos dos dois lados), escudo no peito e espada ou arco na mao direita (sem armas na aba Roupas)
        void DrawHeroFrontPreview(Graphics g, bool bow, bool bare)
        {
            if (eqTunic == 10 && eqHat == 10) DrawFairyWings(g, hx, hy - 12 * S, 1, true);      // conjunto fada: asinhas atras
            Spr(g, heroFrontInv, hx, hy, true);      // mesma aparencia atual de frente (cabeca e franja da pausa), com o corpo mais estreito
            if (!bare && !FairySet)
            {
                if (eqSword != 2 && !offShield) DrawShieldBody(g, hx - 9 * S, hy - 9 * S);      // escudo no braco esquerdo, encostado no corpo (sem cobrir o rosto)
                if (bow) { if (!offBow) DrawFrontBow(g, hx + 7 * S, hy - 7 * S); } else if (!offSword) DrawSwordAt(g, hx + 5 * S, hy - 6 * S, -80f);
            }
            if (menuEatT > 0)      // comendo a maca crua, de frente
            {
                float sc2 = 2.3f - 1.7f * Math.Min(1f, menuEatT / 0.9f); int w = (int)(appleSpr.W * sc2 * 0.8f), h = (int)(appleSpr.H * sc2 * 0.8f);
                g.DrawImage(appleSpr.R, (int)(hx - w / 2f), (int)(hy - 8 * S - h / 2f), w, h);
            }
        }
        // MARRETA TROVAO (estilo do Cogumelo-maca: formas grossas, cores vivas, contorno forte): cabo grosso de madeira e cabeca grande de pedra em 2 tons com um raio azul bem visivel
        void DrawPixelHammer(Graphics g, float handX, float handY, float dx, float dy, float sc)
        {
            float px = -dy, py = dx; SmoothingMode oldSm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            Color dark = Color.FromArgb(30, 24, 44);
            using (SolidBrush br = new SolidBrush(Color.Black))
                for (int ci = -19; ci <= 19; ci++) for (int cj = -19; cj <= 19; cj++)
                {
                    float vx = (ci + 0.5f) * sc, vy = (cj + 0.5f) * sc, u = (vx * dx + vy * dy) / sc, w = (vx * px + vy * py) / sc, aw = Math.Abs(w); Color? c = null;
                    if (u >= -3f && u < 8.5f) { if (aw <= 1.15f) c = w > 0f ? Color.FromArgb(190, 122, 60) : Color.FromArgb(140, 84, 40); else if (aw <= 2.15f) c = dark; }      // cabo grosso
                    else if (u >= 8.5f && u <= 15f)
                    {
                        bool inside = aw <= 6.4f && u >= 9.5f && u <= 14f;
                        if (inside) { c = w > 0f ? Color.FromArgb(212, 218, 236) : Color.FromArgb(150, 160, 188); if (aw >= 5.2f || u >= 13f) c = Color.FromArgb(112, 122, 152); }      // cabeca de pedra: topo claro, base escura, borda mais escura
                        else if (aw <= 7.4f) c = dark;
                        if (inside && Math.Abs(u - 11.7f) <= 1.5f && aw <= 2.2f) c = (Math.Abs(u - 11.7f) <= 0.6f && aw <= 0.9f) ? Color.White : Color.FromArgb(70, 215, 240);      // raio azul grande no centro
                    }
                    if (c == null) continue;
                    br.Color = c.Value; g.FillRectangle(br, handX + ci * sc - 0.3f, handY + cj * sc - 0.3f, sc + 0.6f, sc + 0.6f);
                }
            g.SmoothingMode = oldSm;
        }
        // ESPADA COMUM / MESTRE PIXELADA (pixel art de verdade): feita com quadradinhos MENORES que os do resto do jogo (metade do tamanho: 2 px quando S=3-4), entao a lamina fica fina.
        // A espada so aponta nas 8 direcoes de 45 graus (o angulo e "travado" na mais proxima) e o eixo passa pelo CENTRO dos quadradinhos: nunca deforma.
        // Medidas em quadradinhos pequenos: lamina de 3 (centro claro) com contorno escuro que afina em triangulo, guarda de 9, cabo de 1 e pomo.
        void DrawPixelatedSword(Graphics g, float handX, float handY, float dx, float dy, float sc, float lenCells)
        {
            double ang = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * (Math.PI / 4); dx = (float)Math.Cos(ang); dy = (float)Math.Sin(ang);
            float px = -dy, py = dx; SmoothingMode oldSm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            float cell = Math.Max(1f, (float)Math.Floor(sc / 2f + 0.5f)); handX = (float)Math.Round(handX); handY = (float)Math.Round(handY);      // quadradinho pequeno, em pixels inteiros
            Color outl = Color.FromArgb(28, 22, 36), blade = BladeColor(), guard = GuardColor(), grip = GripColor(), lite = Lerp(blade, Color.White, 0.6f);
            float tipU = (float)Math.Round(lenCells * sc / cell);
            int R = (int)Math.Ceiling(tipU + 4);
            using (SolidBrush br = new SolidBrush(Color.Black))
                for (int ci = -R; ci <= R; ci++) for (int cj = -R; cj <= R; cj++)
                {
                    float vx = ci, vy = cj, u = vx * dx + vy * dy, w = vx * px + vy * py, aw = Math.Abs(w); Color? c = null;
                    if (u >= -6.5f && u < -5.5f) { if (aw <= 0.4f) c = guard; else if (aw <= 1.2f) c = outl; }      // pomo (1 linha)
                    else if (u >= -5.5f && u < -1.5f) { if (aw <= 0.4f) c = grip; else if (aw <= 1.2f) c = outl; }      // cabo curto de 1 quadradinho de largura
                    else if (u >= -1.5f && u <= 1.5f && aw <= 5.2f)
                    {
                        if (aw <= 1.5f && u > 0.5f) c = (aw <= 0.4f) ? lite : blade;      // a lamina nasce da guarda
                        else c = (Math.Abs(u) <= 0.5f && aw <= 4.2f) ? guard : outl;      // guarda: barra de 9 com contorno em cima e embaixo
                    }
                    else if (u > 1.5f && u <= tipU + 1.5f)
                    {
                        float inW = 1.5f; if (u > tipU - 6f) inW = Math.Max(0.4f, 1.5f - (u - (tipU - 6f)) * 0.22f);      // lamina de 3 quadradinhos que afina em triangulo ate a ponta
                        if (u <= tipU + 0.5f && aw <= inW) c = (aw <= 0.4f) ? lite : blade;
                        else if (u <= tipU + 0.5f && aw <= inW + 1f) c = outl;
                        else if (u > tipU + 0.5f && aw <= 0.4f) c = outl;      // ultima ponta escura
                    }
                    if (c == null) continue;
                    br.Color = c.Value; g.FillRectangle(br, handX + ci * cell - cell / 2f, handY + cj * cell - cell / 2f, cell, cell);
                }
            g.SmoothingMode = oldSm;
        }
        void DrawSwordAt(Graphics g, float handX, float handY, float deg)
        {
            double rad = deg * Math.PI / 180.0;
            float dx = (float)Math.Cos(rad), dy = (float)Math.Sin(rad), px = -dy, py = dx, len = 9.5f * S, w = 1.0f * S;
            if (DrawSpecialSword(g, handX, handY, dx, dy)) return;
            DrawPixelatedSword(g, handX, handY, dx, dy, S, 9.5f);
        }
        // ARCO em pixel art, igual no jogo, no menu de pausa, no inventario e no item grande: o arco e uma curva de quadradinhos (miolo de 1 unidade, contorno escuro de 1 unidade)
        // dx,dy = para onde aponta (a barriga do arco vai para la); half = meia altura; bend = quanto a barriga avanca; eq: 0 madeira | 1 metal | 2 cristal
        void PixelBow(Graphics g, float cx, float cy, float dx, float dy, float half, float bend, float unit, int eq)
        {
            Color dk = Color.FromArgb(28, 22, 36);
            Color main = eq == 3 ? Color.FromArgb(78, 74, 108) : eq == 2 ? Color.FromArgb(60, 200, 210) : (eq == 1 ? Color.FromArgb(185, 192, 205) : Color.FromArgb(135, 85, 45));
            Color hi = eq == 3 ? Color.FromArgb(160, 156, 200) : eq == 2 ? Color.FromArgb(215, 255, 255) : (eq == 1 ? Color.FromArgb(240, 245, 255) : Color.FromArgb(185, 125, 70));
            SmoothingMode sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            float px = -dy, py = dx; int n = Math.Max(10, (int)(half * 2 / unit * 1.6f));
            PointF[] pts = new PointF[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n * 2f - 1f, b = bend * (1f - u * u);
                float x = cx + px * u * half + dx * b, y = cy + py * u * half + dy * b;
                pts[i] = new PointF((float)Math.Floor(x / unit) * unit, (float)Math.Floor(y / unit) * unit);
            }
            using (SolidBrush bd = new SolidBrush(dk)) using (SolidBrush bm = new SolidBrush(main)) using (SolidBrush bh = new SolidBrush(hi))
            {
                Color shd = eq == 3 ? Color.FromArgb(46, 42, 74) : eq == 2 ? Color.FromArgb(34, 150, 170) : (eq == 1 ? Color.FromArgb(128, 138, 166) : Color.FromArgb(150, 86, 36));
                using (SolidBrush bs2 = new SolidBrush(shd))
                {
                    foreach (PointF p in pts) g.FillRectangle(bd, p.X - unit, p.Y - unit, 4 * unit, 4 * unit);      // membros GROSSOS (2 quadradinhos) com contorno forte
                    foreach (PointF p in pts) g.FillRectangle(bm, p.X, p.Y, 2 * unit, 2 * unit);
                    foreach (PointF p in pts) g.FillRectangle(bs2, p.X + unit, p.Y + unit, unit, unit);      // lado de baixo mais escuro
                    for (int i = 1; i < n; i += 3) g.FillRectangle(bh, pts[i].X, pts[i].Y, unit, unit);      // brilho
                }
                Color gc = eq == 3 ? Color.FromArgb(196, 40, 56) : eq == 2 ? Color.FromArgb(255, 170, 40) : (eq == 1 ? Color.FromArgb(222, 176, 64) : Color.FromArgb(104, 64, 36));      // empunhadura (5 quadradinhos no meio) numa cor diferente
                using (SolidBrush gb = new SolidBrush(gc)) for (int k = -2; k <= 2; k++) { int idx = Math.Max(0, Math.Min(n, (n >> 1) + k * 2)); g.FillRectangle(gb, pts[idx].X, pts[idx].Y, unit, unit); }
                if (eq == 2)      // joia laranja no meio e pontas de cristal
                {
                    using (SolidBrush gem = new SolidBrush(Color.FromArgb(255, 170, 40))) g.FillRectangle(gem, pts[n / 2].X - unit / 2f, pts[n / 2].Y - unit / 2f, 2 * unit, 2 * unit);
                    using (SolidBrush tip = new SolidBrush(Color.FromArgb(120, 235, 245))) { g.FillRectangle(tip, pts[0].X, pts[0].Y, unit, unit); g.FillRectangle(tip, pts[n].X, pts[n].Y, unit, unit); }
                }
            }
            g.SmoothingMode = sm;
        }
        void DrawFrontBow(Graphics g, float x, float y)      // arco vertical na mao direita (de frente), com a barriga para a direita
        {
            PixelBow(g, x, y, 1f, 0f, 5.6f * S, 2.4f * S, S, eqBow);
            using (Pen st = new Pen(Color.FromArgb(235, 235, 235), Math.Max(1f, S * 0.5f))) g.DrawLine(st, x, y - 5.6f * S, x, y + 5.6f * S);
        }
        void BowIcon(Graphics g, int eq, float cx, float cy, float u)      // arco para icones e item grande (a barriga para a direita, como no jogo)
        {
            float half = 11 * u, unit = Math.Max(1f, u * 0.8f);
            PixelBow(g, cx - 2 * u, cy, 1f, 0f, half, 5 * u, unit, eq);
            using (Pen st = new Pen(Color.FromArgb(235, 235, 235), Math.Max(1f, u * 0.4f))) g.DrawLine(st, cx - 2 * u, cy - half, cx - 2 * u, cy + half);
        }
        // icones dos itens (formas desenhadas por codigo)
        public void DrawIcon(Graphics g, int cat, int id, float cx, float cy, float u)
        {
            Color dark = Color.FromArgb(28, 22, 36);
            if (cat == 0)
            {
                if (id == 1) { DrawEvoItem(g, 1, cx, cy, u * 0.85f); return; }
                BowIcon(g, id, cx, cy, u * 0.85f);
            }
            else if (cat == 1) { if (id >= 2) DrawItemSpr(g, wpnSpr[id - 2], cx, cy, u * 0.85f); else IconSword(g, cx, cy, u, id == 1); }
            else if (cat == 2) IconShield(g, cx, cy, u, id);
            else if (cat == 3)
            {
                if (id == 0) DrawItemSpr(g, menuSpr[0], cx, cy, u * 0.85f);
                else if (id == 1) DrawEvoItem(g, 2, cx, cy, u * 0.85f);
                else if (id == 2) DrawCrownItem(g, cx, cy, u * 0.9f);
                else DrawItemSpr(g, setCap[id - 3], cx, cy, u * 0.85f);
            }
            else if (cat == 5) DrawFairyShape(g, cx, cy, u * 2.4f, id, false, true);
            else
            {
                if (id == 0) DrawItemSpr(g, menuSpr[1], cx, cy, u * 0.85f);
                else if (id == 1) DrawEvoItem(g, 3, cx, cy, u * 0.85f);
                else if (id == 2) DrawItemSpr(g, menuSpr[2], cx, cy, u * 0.85f);
                else DrawItemSpr(g, setShirt[id - 3], cx, cy, u * 0.85f);
            }
        }
        void DrawItemSpr(Graphics g, Sprite sp, float cx, float cy, float u)
        {
            if (sp == null) return;
            float k = u / S * 1.7f, w = sp.W * k, h = sp.H * k;
            InterpolationMode im = g.InterpolationMode; PixelOffsetMode po = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;      // pixel art: sem borrar
            g.DrawImage(sp.R, cx - w / 2f, cy - h / 2f, w, h);
            g.InterpolationMode = im; g.PixelOffsetMode = po;
        }
        void IconCrystalBow(Graphics g, float cx, float cy, float u)      // arco de cristal: turquesa, com joia laranja e duas flechas de cristal
        {
            Color dark = Color.FromArgb(28, 22, 36);
            PointF[] pts = new PointF[13];
            for (int i = 0; i < 13; i++) { float tt = (i - 6) / 6f; pts[i] = new PointF(cx + (5f * tt * tt - 2.5f) * u * 0.85f, cy + tt * 12 * u * 0.85f); }
            using (Pen o = new Pen(dark, u * 3.2f)) { o.StartCap = LineCap.Round; o.EndCap = LineCap.Round; g.DrawLines(o, pts); }
            using (Pen m = new Pen(Color.FromArgb(60, 200, 210), u * 1.8f)) { m.StartCap = LineCap.Round; m.EndCap = LineCap.Round; g.DrawLines(m, pts); }
            using (Pen s = new Pen(Color.FromArgb(235, 235, 235), Math.Max(1f, u * 0.4f))) g.DrawLine(s, pts[0], pts[12]);
            using (SolidBrush gem = new SolidBrush(Color.FromArgb(255, 170, 40))) g.FillRectangle(gem, pts[6].X - u, pts[6].Y - u, 2 * u, 2 * u);
            using (SolidBrush tip = new SolidBrush(Color.FromArgb(120, 235, 245))) { g.FillRectangle(tip, pts[0].X - u * 0.6f, pts[0].Y - u * 0.6f, 1.2f * u, 1.2f * u); g.FillRectangle(tip, pts[12].X - u * 0.6f, pts[12].Y - u * 0.6f, 1.2f * u, 1.2f * u); }
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
            if (type == 3) { float k3 = u * 2.2f; DrawApplePix(g, cx - 4 * k3, cy - 5 * k3, k3); return; }
            if (type == 4) { float k4 = u * 2.2f; DrawDarkShieldPix(g, cx - 4 * k4, cy - 5 * k4, k4); return; }      // escudo sombrio
            Color dark = Color.FromArgb(28, 22, 36), col = type == 4 ? Color.FromArgb(52, 46, 84) : (type == 1 ? Color.FromArgb(205, 50, 50) : Color.FromArgb(55, 95, 205));
            using (SolidBrush kb = new SolidBrush(dark)) using (SolidBrush b = new SolidBrush(col)) using (SolidBrush ye = new SolidBrush(type == 4 ? Color.FromArgb(204, 206, 224) : Color.FromArgb(245, 205, 70)))
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
            // a mesma maca em pixel art do indicador do topo da tela, ampliada
            float sz = 24 * u; InterpolationMode im = g.InterpolationMode; PixelOffsetMode po = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor; g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(appleHud.R, cx - sz / 2f, cy - sz / 2f, sz, sz);
            g.InterpolationMode = im; g.PixelOffsetMode = po;
        }

        class MenuForm : Form
        {
            struct Hit { public Rectangle R; public int Kind, A, B; }      // Kind 0 aba | 1 item | 2 fechar
            static readonly string[][] Names = {
                new string[] { "Arco de madeira", "Arco de metal", "Arco de cristal", "Arco sombrio" }, new string[] { "Espada comum", "Espada Mestre", "Marreta trovão", "Espada de chamas", "Espada cogumelo", "Espada sombria" }, new string[] { "Escudo azul", "Escudo vermelho", "Escudo do rei", "Escudo maçã", "Escudo sombrio" },
                new string[] { "Gorro verde", "Gorro roxo", "Coroa de ouro", "Gorro areia", "Gorro gelo", "Gorro fogo", "Gorro noite", "Gorro mar", "Gorro cogumelo", "Gorro maçã", "Tiara", "Gorro sombra" }, new string[] { "Túnica verde", "Blusa azul", "Roupa de rei", "Blusa areia", "Blusa gelo", "Blusa fogo", "Blusa noite", "Blusa mar", "Blusa cogumelo", "Blusa maçã", "Blusa de fada", "Blusa sombra" }, new string[] { "Fadinha", "Fadinha rosa", "Fadinha folha", "Fadinha estrela", "Fadinha sombria" } };
            static readonly string[][] Descs = {
                new string[] { "O arco de sempre.", "Flechas com o dobro de dano.", "Atira 2 flechas de cristal, uma logo depois da outra.", "Arco escuro do Link Sombrio (vem com o conjunto sombra)." }, new string[] { "A espada de sempre: 150 de dano.", "Espada Mestre: o dobro de dano, 300 de dano.", "Usa as DUAS mãos (sem escudo): golpe lento, área grande, 600 de dano.", "Lâmina de fogo: golpes muito rápidos, 450 de dano.", "Um cogumelo gigante de maça: acerta em área e solta esporos, 700 de dano (vem com o conjunto cogumelo).", "Lâmina escura do Link Sombrio: 800 de dano (vem com o conjunto sombra)." }, new string[] { "Escudo comum.", "Escudo vermelho do herói.", "Escudo circular do tesouro do rei.", "Escudo de maçã cortada (vem com o conjunto maçã).", "Escudo escuro com emblema prateado, do Link Sombrio (vem com o conjunto sombra)." },
                new string[] { "O gorro de sempre.", "Gorro roxo da evolução.", "Coroa de ouro do tesouro do rei.", "Conjunto areia: gorro, blusa, calça e sapatos.", "Conjunto gelo: gorro, blusa, calça e sapatos.", "Conjunto fogo: gorro, blusa, calça e sapatos.", "Conjunto noite: gorro, blusa, calça e sapatos.", "Conjunto mar: gorro, blusa, calça e sapatos.", "Conjunto cogumelo: gorro com bolinhas, blusa de cogumelo e calça cor do talo.", "Conjunto maçã: gorro, blusa, calça e sapatos, mais o escudo de maçã cortada.", "Tiara prateada do conjunto de fada: com a blusa de fada o Link voa e não usa armas.", "Conjunto sombra: gorro, blusa, calça e sapatos escuros, no estilo do Link Sombrio." }, new string[] { "A túnica de sempre.", "Blusa azul da evolução.", "Manto vermelho, calça marrom e sapatos roxos.", "Blusa, calça e sapatos do conjunto areia.", "Blusa, calça e sapatos do conjunto gelo.", "Blusa, calça e sapatos do conjunto fogo.", "Blusa, calça e sapatos do conjunto noite.", "Blusa, calça e sapatos do conjunto mar.", "Blusa de cogumelo com bolinhas e calça cor do talo (conjunto cogumelo).", "Blusa, calça e sapatos do conjunto maçã.", "Blusa de fada, calça e sapatos do conjunto de fada (com a tiara o Link voa e não usa armas).", "Blusa, calça e sapatos do conjunto sombra." }, new string[] { "A fadinha ajudante.", "Corpo rosa e asas azul escuro.", "Corpo verde e asas douradas.", "Corpo violeta e asas turquesa.", "Corpo roxo escuro e asas sombrias (vem com o conjunto sombra)." } };
            static readonly string[] TabNames = { "Armas", "Roupas", "Comida" };
            PetForm p; float sc; int tab = -1, hover = -1; bool showBow;      // showBow: o heroi do exemplo segura o arco (ao clicar num arco) ou espada e escudo (ao clicar neles)
            List<Hit> hits = new List<Hit>(); string msg;
            int scrollY, contentH; Rectangle clipArea;      // rolagem (roda do mouse) quando ha muitos itens
            System.Windows.Forms.Timer eatTimer;

            protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x80; return cp; } }      // TOOLWINDOW; fica por cima (e ativa) para receber os cliques

            public MenuForm(PetForm owner, float scale)
            {
                p = owner; sc = Math.Max(1f, scale);
                FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = false;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                int w = Math.Min((int)(1040 * sc), wa.Width - 20), h = Math.Min((int)(620 * sc), wa.Height - 20);
                Bounds = new Rectangle(wa.X + (wa.Width - w) / 2, wa.Y + (wa.Height - h) / 2, w, h);
                BackColor = Color.FromArgb(18, 22, 30);
                Shown += delegate { Activate(); p.PushLayers(); };
                Activated += delegate { p.PushLayers(); };      // clicar nele o traria para a frente dos aplicativos: volta para o fundo na hora
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
            protected override void OnMouseWheel(MouseEventArgs e)
            {
                scrollY = Math.Max(0, Math.Min(Math.Max(0, contentH - clipArea.Height), scrollY - e.Delta / 2)); Invalidate();
                base.OnMouseWheel(e);
            }
            protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
            protected override void OnMouseClick(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    for (int i = 0; i < hits.Count; i++)
                        if (hits[i].R.Contains(e.Location))
                        {
                            Hit h = hits[i];
                            if (h.Kind == 0) { tab = (tab == h.A) ? -1 : h.A; msg = null; scrollY = 0; }
                            else if (h.Kind == 1) { p.EquipItem(h.A, h.B); if (h.A == 0) showBow = true; else if (h.A == 1 || h.A == 2) showBow = false; }
                            else if (h.Kind == 2) { p.CloseMenu(); return; }
                            else if (h.Kind == 3) { msg = p.MenuEat(); showBow = false; }
                            else if (h.Kind == 4) { msg = p.MenuPick(); showBow = false; }
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
                    if (x + cs > area.Right + 1) { x = area.X; y += cs + (int)(10 * sc); }      // quebra de linha
                    Rectangle r = new Rectangle(x, y, cs, cs);
                    bool eq = p.Equipped(cat) == id; int idx = hits.Count; bool vis = r.IntersectsWith(clipArea); bool hv = vis && hover == idx;
                    if (vis) hits.Add(new Hit { R = r, Kind = 1, A = cat, B = id });
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
                    float k = Math.Max(2f, (float)Math.Floor(pv.Height * 0.8f / (30f * p.S)));      // sem legenda: o heroi (com a fadinha) fica grande e centralizado no quadro
                    p.DrawHeroPreview(g, pv.X + pv.Width / 2f, pv.Y + pv.Height / 2f + 13.5f * p.S * k, k, showBow, tab == 1);      // pes abaixo do centro: o conjunto (fadinha ate os pes, ~27 S) fica no meio
                    g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
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
                        int y = area.Y - scrollY; clipArea = area; if (tab != 2) g.SetClip(area);
                        if (tab == 0) { Section(g, "ARCOS", 0, ref y, area, hf, nf); Section(g, "ESPADAS", 1, ref y, area, hf, nf); Section(g, "ESCUDOS", 2, ref y, area, hf, nf); }
                        else if (tab == 1) { Section(g, "GORROS", 3, ref y, area, hf, nf); Section(g, "ROUPAS", 4, ref y, area, hf, nf); if (p.Owns(5, 0)) Section(g, "FADINHA", 5, ref y, area, hf, nf); }
                        if (tab != 2) { contentH = y - (area.Y - scrollY); g.ResetClip(); if (tab == 0 && p.FairySet) g.DrawString("Com a tiara e a blusa de fada o Link voa e não usa armas.", sf, Brushes.Gold, area.X, H - pad - 26 * sc); }
                        else
                        {
                            g.DrawString("COMIDA", hf, Brushes.Gold, area.X, y); y += (int)(26 * sc);
                            int cs = (int)(112 * sc); Rectangle r = new Rectangle(area.X, y, cs, cs);
                            int ai = hits.Count; hits.Add(new Hit { R = r, Kind = 3 });
                            using (SolidBrush bg = new SolidBrush(hover == ai ? Color.FromArgb(58, 66, 86) : Color.FromArgb(34, 40, 54))) using (Pen pen = new Pen(Color.FromArgb(80, 90, 112), 1.5f * sc)) Round(g, r, (int)(10 * sc), bg, pen);
                            p.IconApple(g, r.X + cs / 2f, r.Y + cs * 0.42f, cs / 34f);
                            StringFormat c = new StringFormat(); c.Alignment = StringAlignment.Center;
                            g.DrawString("Maçãs x " + p.AppleCount, nf, Brushes.White, new RectangleF(r.X, r.Bottom - 22 * sc, cs, 22 * sc), c);
                            Rectangle eb = new Rectangle(area.X, r.Bottom + 8 * sc > 0 ? (int)(r.Bottom + 8 * sc) : r.Bottom, cs, (int)(32 * sc));      // opcao "Comer maçã" logo abaixo do icone
                            int ei = hits.Count; hits.Add(new Hit { R = eb, Kind = 3 });
                            using (SolidBrush bg2 = new SolidBrush(hover == ei ? Color.FromArgb(255, 210, 100) : Color.FromArgb(247, 190, 70))) using (Pen pen2 = new Pen(Color.FromArgb(28, 22, 36), 1.5f * sc)) Round(g, eb, (int)(8 * sc), bg2, pen2);
                            using (SolidBrush dk2 = new SolidBrush(Color.FromArgb(28, 22, 36))) g.DrawString("Comer maçã", nf, dk2, new RectangleF(eb.X, eb.Y + 5 * sc, eb.Width, eb.Height), c);
                            Rectangle pb = new Rectangle(eb.Right + (int)(10 * sc), eb.Y, cs, eb.Height);      // opcao "Colher maçã" ao lado
                            int pi = hits.Count; hits.Add(new Hit { R = pb, Kind = 4 });
                            using (SolidBrush bg3 = new SolidBrush(hover == pi ? Color.FromArgb(150, 225, 130) : Color.FromArgb(105, 200, 95))) using (Pen pen3 = new Pen(Color.FromArgb(28, 22, 36), 1.5f * sc)) Round(g, pb, (int)(8 * sc), bg3, pen3);
                            using (SolidBrush dk3 = new SolidBrush(Color.FromArgb(28, 22, 36))) g.DrawString("Colher maçã", nf, dk3, new RectangleF(pb.X, pb.Y + 5 * sc, pb.Width, pb.Height), c);
                            g.DrawString("Maçã crua recupera meio coração.\nCom 5 ou mais ele faz lanche na fogueira (assadas recuperam mais).", sf, Brushes.LightGray, area.X, eb.Bottom + 12 * sc);
                            g.DrawString("Corações: " + p.HeartsNow.ToString("0.#") + " / " + p.HeartsMax, sf, Brushes.Gold, area.X, eb.Bottom + 64 * sc);
                            if (!string.IsNullOrEmpty(msg)) g.DrawString(msg, sf, Brushes.OrangeRed, area.X, eb.Bottom + 90 * sc);
                        }
                    }
                    // descricao do item sob o mouse
                    if (hover >= 0 && hover < hits.Count && hits[hover].Kind == 1)
                    {
                        Hit h = hits[hover];
                        if (h.A == 3 || h.A == 4 || h.A == 5) { }      // roupas (gorros e blusas) e fadinha: nenhum texto embaixo
                        else if (h.A <= 2) g.DrawString(Descs[h.A][h.B], sf, Brushes.White, new RectangleF(area.X, H - pad - 26 * sc, area.Width, 30 * sc));      // armas, arcos e escudos: so a descricao, sem o nome
                        else { g.DrawString(Names[h.A][h.B], sf, Brushes.Gold, area.X, H - pad - 50 * sc); g.DrawString(Descs[h.A][h.B], sf, Brushes.White, new RectangleF(area.X, H - pad - 26 * sc, area.Width, 30 * sc)); }
                    }
                }
            }
        }
        void BuildSprites()
        {
            ValidateEquip();
            Art.Crown = eqHat == 2 || eqHat == 10; Art.Tiara = eqHat == 10; Art.CuteShirt = eqTunic == 10; Art.Robe = eqTunic == 2; Art.Legs = eqTunic >= 3; Art.MushHat = eqHat == 8; Art.AppleHat = eqHat == 9; Art.MushShirt = eqTunic == 8;
            Dictionary<char, Color> hov = new Dictionary<char, Color>();
            if (eqHat == 1) hov['H'] = Color.FromArgb(150, 80, 205);       // gorro roxo
            if (eqTunic == 1) hov['Q'] = Color.FromArgb(70, 125, 235);       // blusa azul
            if (eqTunic == 10) hov['z'] = Color.FromArgb(206, 176, 244);      // conjunto fada: meio da blusa lilas
            if (eqHat == 11) { hov['S'] = Color.FromArgb(206, 198, 214); hov['V'] = Color.FromArgb(54, 54, 72); }      // conjunto sombra: rosto palido e cabelo escuro como o Link Sombrio
            if (eqHat >= 3) hov['H'] = Sets[eqHat - 3].Hat;                                                  // conjuntos novos: gorro
            if (eqTunic >= 3) { OutSet os = Sets[eqTunic - 3]; hov['Q'] = os.Tunic; hov['J'] = os.Pants; hov['Z'] = os.Shoes; }
            if (eqTunic >= 3) hov['b'] = SetBelt(eqTunic - 3);      // faixa da blusa: tom mais escuro (noite: amarela)      // blusa, calca e sapatos
            for (int si = 0; si < Sets.Length; si++)      // icones do inventario
            {
                Dictionary<char, Color> co = Art.Ov('H', Sets[si].Hat); co['h'] = Shade(Sets[si].Hat, 0.4f); co['g'] = Shade(Sets[si].Hat, -0.4f); setCap[si] = Art.Make(si == 7 ? Art.TiaraItem : (si == 5 ? Art.CapItemMush : Art.CapItem), S, co);
                Dictionary<char, Color> so2 = Art.Ov('Q', Sets[si].Tunic); so2['h'] = Shade(Sets[si].Tunic, 0.4f); so2['g'] = Shade(Sets[si].Tunic, -0.4f); so2['B'] = SetBelt(si); setShirt[si] = Art.Make(si == 5 ? Art.ShirtItemMush : Art.ShirtItem, S, so2);
            }
            if (eqTunic == 2) { hov['Q'] = Color.FromArgb(190, 35, 60); hov['J'] = Color.FromArgb(150, 100, 55); hov['Z'] = Color.FromArgb(130, 60, 190); }      // bau: manto vermelho, calca marrom, sapatos roxos
            { Color hb = hov.ContainsKey('H') ? hov['H'] : Color.FromArgb(58, 165, 62); hov['i'] = Shade(hb, 0.4f); }
            heroWalk[0] = Art.Make(Art.HeroRecolor(Art.HeroA), S, hov);
            heroWalk[1] = Art.Make(Art.HeroRecolor(Art.HeroB), S, hov);
            UpdateTrayIcon();      // o icone da bandeja acompanha a roupa
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
            heroFrontInv = Art.Make(Art.HeroRecolor(Art.HeroFrontInv), S, hov);
            Dictionary<char, Color> capOv = Art.Ov('H', Color.FromArgb(150, 80, 205)); capOv['h'] = Color.FromArgb(205, 150, 240); capOv['g'] = Color.FromArgb(96, 50, 140);
            Dictionary<char, Color> shOv = Art.Ov('Q', Color.FromArgb(70, 125, 235)); shOv['h'] = Color.FromArgb(135, 180, 255); shOv['g'] = Color.FromArgb(45, 85, 190);
            evoItemSpr[0] = Art.Make(Art.CapItem, S, capOv); evoItemSpr[1] = Art.Make(Art.ShirtItem, S, shOv);
            Dictionary<char, Color> gcOv = Art.Ov('H', Color.FromArgb(58, 165, 62)); gcOv['h'] = Color.FromArgb(130, 210, 120); gcOv['g'] = Color.FromArgb(30, 110, 45);
            Dictionary<char, Color> gsOv = Art.Ov('Q', Color.FromArgb(58, 165, 62)); gsOv['h'] = Color.FromArgb(130, 210, 120); gsOv['g'] = Color.FromArgb(30, 110, 45);
            Dictionary<char, Color> rbOv = Art.Ov('Q', Color.FromArgb(190, 35, 60)); rbOv['h'] = Color.FromArgb(235, 100, 120); rbOv['g'] = Color.FromArgb(130, 20, 40);
            menuSpr[0] = Art.Make(Art.CapItem, S, gcOv); menuSpr[1] = Art.Make(Art.ShirtItem, S, gsOv); Dictionary<char, Color> rrOv = Art.Ov('Q', Color.FromArgb(190, 35, 60)); rrOv['g'] = Color.FromArgb(130, 20, 40); rrOv['C'] = Color.FromArgb(255, 205, 40);
            menuSpr[2] = Art.Make(Art.RobeItem, S, rrOv);
            Dictionary<char, Color> crOv = Art.Ov('C', Color.FromArgb(255, 205, 40)); crOv['c'] = Color.FromArgb(215, 150, 20); crOv['R'] = Color.FromArgb(215, 40, 60); crOv['U'] = Color.FromArgb(70, 125, 235);
            menuSpr[3] = Art.Make(Art.CrownItem, S, crOv);
            Dictionary<char, Color> hmOv = Art.Ov('G', Color.FromArgb(150, 155, 172)); hmOv['g'] = Color.FromArgb(95, 100, 118); hmOv['h'] = Color.FromArgb(210, 215, 230); hmOv['c'] = Color.FromArgb(90, 225, 235); hmOv['C'] = Color.FromArgb(235, 255, 255);
            wpnSpr[0] = Art.Make(Art.HammerItem, S, hmOv);
            Dictionary<char, Color> flOv = Art.Ov('O', Color.FromArgb(255, 140, 40)); flOv['Y'] = Color.FromArgb(255, 225, 90); flOv['F'] = Color.FromArgb(232, 78, 30); flOv['R'] = Color.FromArgb(170, 40, 40); flOv['B'] = Color.FromArgb(110, 40, 40); flOv['b'] = Color.FromArgb(75, 25, 30);
            wpnSpr[1] = Art.Make(Art.FlameItem, S, flOv);
            Dictionary<char, Color> mhOv = Art.Ov('R', Color.FromArgb(214, 48, 52)); mhOv['W'] = Color.FromArgb(250, 250, 250); mhOv['T'] = Color.FromArgb(238, 224, 188); mhOv['t'] = Color.FromArgb(200, 175, 130);
            wpnSpr[2] = Art.Make(Art.MushItem, S, mhOv);
            Dictionary<char, Color> dkOv = Art.Ov('O', Color.FromArgb(84, 80, 120)); dkOv['F'] = Color.FromArgb(84, 80, 120); dkOv['Y'] = Color.FromArgb(172, 170, 204); dkOv['R'] = Color.FromArgb(176, 34, 54); dkOv['B'] = Color.FromArgb(56, 44, 90); dkOv['b'] = Color.FromArgb(34, 26, 56);
            wpnSpr[3] = Art.Make(Art.FlameItem, S, dkOv);      // icone da espada sombria (mesma forma, cores escuras)
            heroFrontWave = Art.Make(Art.HeroRecolor(Art.HeroFrontWave), S, hov);
            if (phase2)      // fase 2: os mesmos monstros, mais detalhados (octorok verde, bokoblin mais escuro com armadura)
            {
                Dictionary<char, Color> o2 = Art.Ov('h', Color.FromArgb(150, 225, 120)); Dictionary<char, Color> b2 = Art.Ov('p', Color.FromArgb(88, 45, 130)); b2['P'] = Color.FromArgb(124, 62, 176);
                Dictionary<char, Color> s2 = Art.Ov('u', Color.FromArgb(40, 85, 190));
                Dictionary<char, Color> k2 = Art.Ov('k', Color.FromArgb(128, 56, 66)); k2['B'] = Color.FromArgb(84, 62, 52); k2['b'] = Color.FromArgb(62, 40, 30); k2['h'] = Color.FromArgb(200, 190, 160); k2['A'] = Color.FromArgb(112, 118, 134);
                octo[0] = Art.Make(Art.Octo2A, S, o2); octo[1] = Art.Make(Art.Octo2B, S, o2);
                bat[0] = Art.Make(Art.Bat2Up, S, b2); bat[1] = Art.Make(Art.Bat2Down, S, b2);
                slime[0] = Art.Make(Art.Slime2Tall, S, s2); slime[1] = Art.Make(Art.Slime2Flat, S, s2);
                if (phase3)      // fase 3: nao ha mais a gelatina normal, so a de fogo (atira bolinhas de fogo)
                {
                    Dictionary<char, Color> f3 = Art.Ov('U', Color.FromArgb(238, 96, 34)); f3['L'] = Color.FromArgb(255, 186, 76); f3['u'] = Color.FromArgb(188, 46, 24);
                    slime[0] = Art.Make(Art.Slime2Tall, S, f3); slime[1] = Art.Make(Art.Slime2Flat, S, f3);
                }
                boko[0] = Art.Make(Art.Boko2A, S, k2); boko[1] = Art.Make(Art.Boko2B, S, k2); boko[2] = Art.Make(Art.Boko2C, S, k2);
                boko[3] = Art.Make(Art.NoClub(Art.Boko2A), S, k2);      // sem o porrete (golpe): tambem na cor escura da fase 2
            }
            else
            {
                octo[0] = Art.Make(Art.OctoA, S, null); octo[1] = Art.Make(Art.OctoB, S, null);
                bat[0] = Art.Make(Art.BatUp, S, null); bat[1] = Art.Make(Art.BatDown, S, null);
                slime[0] = Art.Make(Art.SlimeTall, S, null); slime[1] = Art.Make(Art.SlimeFlat, S, null);
                boko[3] = Art.Make(Art.NoClub(Art.BokoA), S, null);      // sem o porrete: o porrete do golpe e desenhado a parte, girando
            boko[0] = Art.Make(Art.BokoA, S, null); boko[1] = Art.Make(Art.BokoB, S, null); boko[2] = Art.Make(Art.BokoC, S, null);
            }
            Art.Tint = null;
            Dictionary<char, Color> mgOv = Art.Ov('R', Color.FromArgb(105, 45, 165)); mgOv['r'] = Color.FromArgb(64, 26, 110); mgOv['E'] = Color.FromArgb(255, 240, 90); mgOv['G'] = Color.FromArgb(255, 205, 40); mgOv['K'] = Color.FromArgb(28, 22, 36);
            mageSpr = Art.Make(Art.MageRows, (int)Math.Round(S * 1.6), mgOv);
            wizSpr = Art.Make(Art.WizRows, S, mgOv);
            Dictionary<char, Color> obOv = Art.Ov('h', Color.FromArgb(150, 225, 120));      // Octorok Gigante: verde, igual ao octorok da fase 2
            octoBoss[0] = Art.Make(Art.Octo2A, S * 4, obOv); octoBoss[1] = Art.Make(Art.Octo2B, S * 4, obOv);      // Octorok Gigante (verde, 4x o tamanho de um octorok)
            if (phase3) { Dictionary<char, Color> fb = Art.Ov('U', Color.FromArgb(238, 96, 34)); fb['L'] = Color.FromArgb(255, 186, 76); fb['u'] = Color.FromArgb(188, 46, 24); slimeBoss[0] = Art.Make(Art.Slime2Tall, S * 5, fb); slimeBoss[1] = Art.Make(Art.Slime2Flat, S * 5, fb); }      // Gelatina de Fogo Gigante (chefao da fase 3)
            if (acidPoolBmp != null) acidPoolBmp.Dispose();
            if (phase3) acidPoolBmp = BuildPuddleColored((int)Math.Round(31.2f * S), (int)Math.Round(15.6f * S), 0, Color.FromArgb(150, 50, 10), Color.FromArgb(240, 110, 30), Color.FromArgb(255, 220, 120)); else
            acidPoolBmp = BuildPuddleColored((int)Math.Round(31.2f * S), (int)Math.Round(15.6f * S), 0, Color.FromArgb(70, 120, 20), Color.FromArgb(150, 220, 40), Color.FromArgb(235, 255, 150));      // Feiticeiro do tamanho do Link (cena e aliado)
            Dictionary<char, Color> stOv = new Dictionary<char, Color>(); stOv['a'] = Color.FromArgb(118, 120, 134); stOv['c'] = Color.FromArgb(168, 172, 186); stOv['d'] = Color.FromArgb(84, 86, 100); stOv['m'] = Color.FromArgb(84, 134, 74);
            for (int sv = 0; sv < 3; sv++) stoneSpr[sv] = Art.Make(Art.StoneRows(sv), Math.Max(S + 1, S * 3 / 2), stOv);      // pedras maiores que 1 quadradinho do heroi (cerca de 1,5x)
            Dictionary<char, Color> sfOv = new Dictionary<char, Color>(); sfOv['o'] = Color.FromArgb(150, 80, 215); sfOv['q'] = Color.FromArgb(225, 185, 255); sfOv['E'] = Color.FromArgb(255, 240, 90); sfOv['R'] = Color.FromArgb(105, 45, 165); sfOv['r'] = Color.FromArgb(64, 26, 110); sfOv['G'] = Color.FromArgb(255, 205, 40); sfOv['K'] = Color.FromArgb(28, 22, 36);
            staffSpr = Art.Make(Art.StaffRows, S, sfOv);
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

        [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
        IntPtr trayHandle = IntPtr.Zero;
        void UpdateTrayIcon()      // o icone da bandeja mostra o heroi com a roupa de agora (refeito a cada troca de roupa)
        {
            if (tray == null || heroWalk[0] == null) return;
            try
            {
                using (Bitmap ib = new Bitmap(32, 32))
                {
                    using (Graphics g = Graphics.FromImage(ib))
                    {
                        g.Clear(Color.Transparent);
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(heroWalk[0].R, 0, 0, 32, 32);
                    }
                    IntPtr h = ib.GetHicon();
                    tray.Icon = Icon.FromHandle(h);
                    if (trayHandle != IntPtr.Zero) DestroyIcon(trayHandle);
                    trayHandle = h;
                }
            }
            catch { }
        }
        void BuildTray()
        {
            tray = new NotifyIcon();
            UpdateTrayIcon();
            tray.Text = "Heroi do Desktop";
            ContextMenuStrip menu = new ContextMenuStrip();
            mAutoItem = new ToolStripMenuItem("Deixar rodando (automático)"); mAutoItem.Checked = true;
            mPlayItem = new ToolStripMenuItem("Jogar (você controla)");
            mAutoItem.Click += delegate { SetPlayMode(false); };
            mPlayItem.Click += delegate { SetPlayMode(true); };
            menu.Items.Add(mAutoItem); menu.Items.Add(mPlayItem); menu.Items.Add(new ToolStripSeparator());
#if ATALHOS      // atalhos de teste das conversas do Feiticeiro: so existem se compilar com /define:ATALHOS (a versao entregue nao tem)
            menu.Items.Add("Atalho: 1a conversa do Feiticeiro (Ctrl+Shift+F1)", null, delegate { StartSceneTest(); });
            menu.Items.Add("Atalho: 2a conversa do Feiticeiro (Ctrl+Shift+F2)", null, delegate { StartScene2Test(); });
#endif
            menu.Items.Add("Invocar inimigo", null, delegate { Spawn(rnd.Next(3)); });
            menu.Items.Add("Curar heroi", null, delegate { hp = MaxHP; });
            menu.Items.Add("Soneca agora", null, delegate { if (calmState == 0) nextCalm = 0; });
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
                MessageBox.Show("Herói do Desktop\nVersão 2.0.0\n\nUm herói pixel-art que vive na sua área de trabalho: luta, colhe maçãs, assa o próprio lanche e enfrenta um dragão.\n\nIdeia: Alexandre\nCriação: Bia Krepel\nProgramação: Claude", "Sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            SaveRecord(); SaveWallets();
            SaveKills(); SaveChest(); SaveFairy(); SaveSetKills(setKills); SaveWpnKills(wpnKills);
            Guarda.Backup();
            if (evoForm != null) { try { evoForm.Close(); } catch { } evoForm = null; }
            CloseMenu();
            if (shadowLayer != null) { shadowLayer.Close(); shadowLayer = null; }
            if (tintLayer != null) { tintLayer.Close(); tintLayer = null; }
            base.OnFormClosing(e);
        }

        // ============================ LOGICA ============================
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        int zTick;
        // Camadas das janelas do jogo: tudo fica no fundo (abaixo dos aplicativos), com o menu de pausa por cima do heroi e dos inimigos.
        // Com "Mostrar area de trabalho" (Win+D) a area de trabalho sobe e cobriria o heroi: nesse caso ele (e o menu) sobem junto, para ficar por cima dela.
        // Ordem no fundo (de cima para baixo): menu > heroi > sombras > protecao de cliques. O ultimo a ser mandado para o fundo fica mais embaixo.
        public void PushLayers()
        {
            if (!IsHandleCreated) return;
            const uint F = 0x0001 | 0x0002 | 0x0010 | 0x0200;      // NOMOVE | NOSIZE | NOACTIVATE | NOOWNERZORDER
            bool front = DesktopShown();
            bool menuOn = MenuOpen && menuForm.IsHandleCreated;
            if (menuOn && !front) SetWindowPos(menuForm.Handle, new IntPtr(1), 0, 0, 0, 0, F);
            if (tintLayer == null && (darkOn || fadeA > 0f)) { try { tintLayer = new ShadowLayer(Bounds); tintLayer.Show(); } catch { tintLayer = null; } }
            bool tintOn = tintLayer != null && tintLayer.IsHandleCreated;
            if (tintOn && !front) SetWindowPos(tintLayer.Handle, new IntPtr(1), 0, 0, 0, 0, F);      // camada de escuridao: logo acima do heroi
            SetWindowPos(Handle, front ? new IntPtr(-1) : (tintOn ? tintLayer.Handle : new IntPtr(1)), 0, 0, 0, 0, F);      // HWND_TOPMOST (so com a area de trabalho na frente) ou HWND_BOTTOM (abaixo da camada de escuridao)
            if (tintOn && front) SetWindowPos(tintLayer.Handle, new IntPtr(-1), 0, 0, 0, 0, F);
            if (shadowLayer == null) { try { shadowLayer = new ShadowLayer(Bounds); shadowLayer.Show(); } catch { shadowLayer = null; } }
            if (shadowLayer != null && shadowLayer.IsHandleCreated) SetWindowPos(shadowLayer.Handle, Handle, 0, 0, 0, 0, F);   // logo abaixo do heroi
            if (shield != null) { if (front) shield.PushTop(); else shield.PushBottom(); }
            if (menuOn && front) SetWindowPos(menuForm.Handle, new IntPtr(-1), 0, 0, 0, 0, F);      // por cima do heroi, so enquanto a area de trabalho esta na frente
        }
        void Tick()
        {
            if ((zTick++ % 6) == 0) PushLayers();
            if (zTick % 700 == 0) { SaveWallets(); Guarda.Backup(); }      // ~a cada 20 s: copia de seguranca do progresso (arquivo) e grava o registro no disco
            if (deathForm != null && !deathForm.IsDisposed && deathForm.Ready && IsDown(0x0D)) deathForm.Retry();      // Enter = Tentar de novo (se a janela estiver escondida atras de um aplicativo)
            if (shield != null) shield.PassThrough(IsDown(0x11));        // Ctrl apertado: cliques passam para a area de trabalho
            double now = clock.Elapsed.TotalSeconds;
            float dt = (float)Math.Min(0.05, now - lastT);
            lastT = now;
            CheckSpacePause();
#if ATALHOS
            CheckSceneHotkeys();
#endif
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
            if (p2MsgT > 0) { p2MsgT += dt; if (p2MsgT >= 5f) p2MsgT = 0; }
            if (scene != 0) { UpdateScene(dt); return; }      // cena do Feiticeiro: o jogo fica parado
            if (staffPending) { staffPending = false; if (interlude && staffPh == 0) StartStaffEvent(); }
            UpdateStaff(dt); UpdateAlly(dt); UpdateDrops(dt); UpdateFairyFx(dt); UpdateStar(dt); UpdateAcid(dt); UpdateSparks(dt);
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
                    float dx = en.X - a.X, dy = (en.Y - (en.Type == 1 ? 9 * S : (en.Type == 5 ? 9 * S : (en.Type == 7 ? 16 * S * en.Scale : (en.Type == 3 ? 12 * S * DK * en.Scale : 4 * S))))) - a.Y;
                    float hitR = en.Type == 3 ? 14 * S * DK * en.Scale : (en.Type == 7 ? 22 * S * en.Scale : (en.Type == 5 ? 9 * S : 7 * S));
                    if (dx * dx + dy * dy < hitR * hitR) { DamageEnemy(en, a.Power ? 9999999 : (a.Glow ? 100 : 50 * (eqBow >= 1 ? 2 : 1))); gone = true; }      // arco prateado (evolucao 1): o dobro de dano (flecha de poder nao muda)
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
                if (enemies.Count < EnemyCap() && !dead && calmState == 0 && cookState == 0 && bossPhase == 0 && errandPh == 0 && staffPh == 0) Spawn(SpawnType());      // sem monstros enquanto colhe a pedido
            }
            UpdatePower(dt);
            UpdateEvo(dt);
            UpdateChest(dt);
            UpdateFairy(dt);
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
            UpdateCrystalQueue(dt);
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
        static readonly int[] EvoThreshold = { 1100, 1650, 2200, 3300 };      // 10% mais dificil que 1000/1500/2000/3000
        static readonly int[] EvoNeeded = { 1000, 500, 500, 1000 };         // monstros que foram precisos para cada item
        static readonly string[] EvoTitle = { "ARCO DE METAL", "GORRO ROXO", "BLUSA AZUL", "ESPADA MESTRE E ESCUDO VERMELHO" };
        int evoKills;                        // total de monstros derrotados no modo automatico (salvo)
        int evoStage;                        // equipamentos ja adquiridos (0 a 4)
        int evoNext;                         // proximo estagio a anunciar (1 a 4; 5 = acabou)
        Queue<int> evoQueue = new Queue<int>();
        EvoAnim evo;
        Sprite[] wpnSpr = new Sprite[4];      // icones: marreta, espada de chamas
        Sprite[] menuSpr = new Sprite[4]; Sprite[] setCap = new Sprite[9], setShirt = new Sprite[9];      // gorro verde, blusa verde e roupa de rei em pixel art (para o menu)
        Sprite[] evoItemSpr = new Sprite[2];      // gorro e blusa em pixel art (iguais ao corpo)
        Font evoFont;
        const float EvoFly = 1.5f;

        Color ShieldColor() { if (eqShield == 4) return Color.FromArgb(52, 46, 84); return eqShield == 1 ? Color.FromArgb(205, 50, 50) : Color.FromArgb(55, 95, 205); }
        Color BladeColor() { if (eqSword == 5) return Color.FromArgb(84, 80, 120); return eqSword == 1 ? Color.FromArgb(205, 232, 255) : Color.FromArgb(225, 235, 245); }
        Color GuardColor() { if (eqSword == 5) return Color.FromArgb(176, 34, 54); return eqSword == 1 ? Color.FromArgb(60, 95, 205) : Color.FromArgb(215, 160, 40); }
        Color GripColor() { if (eqSword == 5) return Color.FromArgb(52, 40, 84); return eqSword == 1 ? Color.FromArgb(80, 55, 150) : Color.FromArgb(135, 85, 45); }
        Color TunicColor() { if (eqTunic >= 3) return Sets[eqTunic - 3].Tunic; return eqTunic == 2 ? Color.FromArgb(190, 35, 60) : (eqTunic == 1 ? Color.FromArgb(70, 125, 235) : Color.FromArgb(58, 165, 62)); }

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
            wpnKills = LoadWpnKills(); wpnKnown = 0; while (wpnKnown < 3 && wpnKills >= WpnThr[wpnKnown]) wpnKnown++;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--armas") >= 0) wpnTest = true;      // teste rapido: as 3 armas liberadas
            setKills = LoadSetKills(); setsKnown = 0; while (setsKnown < Sets.Length && setKills >= SetThr[setsKnown]) setsKnown++;      // ja liberados antes: sem aviso
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--conjuntos") >= 0) setsTest = true;      // teste rapido: os 5 conjuntos liberados
        }

        // chamado a cada monstro derrotado no modo automatico
        void CountKill()
        {
            evoKills++;
            if (evoKills % 5 == 0) SaveKills();
            wpnKills++; if (wpnKills % 5 == 0) SaveWpnKills(wpnKills);
            { int wn = 0; while (wn < 3 && wpnKills >= WpnThr[wn]) wn++; if (wn > wpnKnown && !wpnTest) { wpnKnown = wn; UnlockWeapon(wn - 1); SaveWpnKills(wpnKills); } }      // arma nova (uma depois da outra)
            setKills++; if (setKills % 5 == 0) SaveSetKills(setKills);
            { int so = 0; while (so < Sets.Length && setKills >= SetThr[so]) so++; if (so > setsKnown && !setsTest) { setsKnown = so; UnlockSet(so - 1); SaveSetKills(setKills); } }      // conjunto de roupa novo
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
                if (stage == 1) BowIcon(g, 1, cx, cy, u);           // arco de metal
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
        const int ChestKillsNeeded = 5500;      // (era 5000; +10%)
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
            if (chestState == 0 && (evoStage < 4 || fairyState != 1)) chestKills = 0;      // contagem so vale depois do conjunto das evolucoes
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
            if (chestState != 0 || evoStage < 4 || fairyState != 1) return;      // a contagem do bau so comeca depois do conjunto das evolucoes (arco, gorro, blusa, espada e escudo) estar completo
            chestKills++;
            if (chestKills % 10 == 0) SaveChest();
            if (chestKills >= ChestKillsNeeded) { chestState = 1; SaveChest(); Say("Um baú apareceu!"); }
        }
        // ================= FADINHA (ajudante) =================
        // Depois de conseguir a roupa de rei, mais 50 monstros derrotados (modo automatico) dao uma fadinha. Ela voa perto da cabeca do heroi e derrota 20% dos
        // monstros que aparecem: voa ate o monstro, gira rapido em volta da cabeca dele (um pouco acima) e ele cai. Vale nos dois modos.
        const int FairyKillsNeeded = 55;      // (era 50; +10%)
        const int FairyPinkKillsNeeded = 110;      // 110 monstros depois do bau: fadinha rosa (era 100)
        const int FairySkinKillsNeeded = 550;      // 550 monstros por nivel depois da fadinha: folha 1100, estrela 1650 (era 500)
        bool offFairy;      // fadinha desequipada (clicar de novo no menu): some e nao ataca
        int fairyKills2, fairySkin, eqFairy;      // eqFairy: aparencia escolhida no inventario (0 normal | 1 rosa)
        int fairyKills, fairyState, fairyPh;      // fairyState: 0 contando | 1 tem a fadinha;  fairyPh: 0 segue o heroi | 1 voa ate o alvo | 2 gira em volta
        float fx, fy, fairyT, fairyOrbit, fairyTrail, fairyCool = 30f; Enemy fairyTarget; bool prevF4, fairyReadySaid;      // fairyCool: a cada 30 s a fadinha fica pronta (pisca amarela) e ataca por comando (tecla 4)
        float FairyRate { get { return eqFairy == 3 ? 2f : 1f; } }      // a fadinha de ESTRELA ataca o DOBRO de vezes: 40% dos monstros marcados e pronta a cada 15 s (as outras: 20% e 30 s)
        bool FairyReady { get { return fairyState == 1 && !offFairy && fairyCool <= 0; } }
        void FairyInit()
        {
            fairyKills = 0; fairyState = 0; fairyKills2 = 0; fairySkin = 0;
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") == null)
                try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object a = k.GetValue("MonstrosFada"), b = k.GetValue("Fada"), c2 = k.GetValue("MonstrosFada2"), d2 = k.GetValue("FadaSkin"); if (c2 != null) fairyKills2 = Math.Max(0, Convert.ToInt32(c2)); if (d2 != null) fairySkin = Math.Max(0, Math.Min(3, Convert.ToInt32(d2))); if (a != null) fairyKills = Math.Max(0, Convert.ToInt32(a)); if (b != null) fairyState = Math.Max(0, Math.Min(1, Convert.ToInt32(b))); } } } catch { }
            if (fairyState == 0 && evoStage < 4) fairyKills = 0;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--fada") >= 0) { fairyState = 1; }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--fada2") >= 0) { kingGear = true; fairyState = 1; fairySkin = 1; }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--fada4") >= 0) { kingGear = true; fairyState = 1; fairySkin = 3; }      // teste: as 4 aparencias      // teste rapido: fadinha com as asas azul escuro e rosa      // teste rapido: ja com a fadinha
            eqFairy = fairySkin;
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") == null) try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { object v = k == null ? null : k.GetValue("FadaLook"); object vo = k == null ? null : k.GetValue("FadaOff"); offFairy = vo != null && Convert.ToInt32(vo) != 0; if (v != null) eqFairy = Math.Max(0, Math.Min(4, Convert.ToInt32(v))); } } catch { }
            if (eqFairy > fairySkin && eqFairy != 4) eqFairy = 0;
            fx = W / 2f; fy = H / 2f;
        }
        void SaveFairy()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) { k.SetValue("MonstrosFada", fairyKills); k.SetValue("Fada", fairyState); k.SetValue("MonstrosFada2", fairyKills2); k.SetValue("FadaSkin", fairySkin); k.SetValue("FadaLook", eqFairy); k.SetValue("FadaOff", offFairy ? 1 : 0); } } catch { }
        }
        void CountFairyKill()
        {
            if (fairyState == 1 && kingGear && fairySkin < 3)      // depois do bau (conjunto de rei): 100 / 1000 / 1500 monstros dao as aparencias 1 (rosa), 2 (folha) e 3 (estrela)
            {
                fairyKills2++;
                if (fairyKills2 % 25 == 0) SaveFairy();
                if (fairyKills2 >= (fairySkin == 0 ? FairyPinkKillsNeeded : FairySkinKillsNeeded * (fairySkin + 1))) { fairySkin++; eqFairy = fairySkin; SaveFairy(); Say("Fadinha nova!"); for (int k = 0; k < 10; k++) puffs.Add(new Puff { Kind = 1, X = fx + ((float)rnd.NextDouble() - 0.5f) * 24 * S, Y = fy + ((float)rnd.NextDouble() - 0.5f) * 16 * S }); }
                return;
            }
            if (fairyState != 0 || evoStage < 4) return;      // a fadinha vem depois das 4 evolucoes (e antes do conjunto de rei)
            fairyKills++;
            if (fairyKills % 10 == 0) SaveFairy();
            if (fairyKills >= FairyKillsNeeded)
            {
                fairyState = 1; fairyCool = 30f / FairyRate; SaveFairy(); newItemUntil = time + 5f; fx = hx; fy = hy - 26 * S; Say("Uma fadinha!");
                for (int k = 0; k < 10; k++) puffs.Add(new Puff { Kind = 1, X = fx + ((float)rnd.NextDouble() - 0.5f) * 24 * S, Y = fy + ((float)rnd.NextDouble() - 0.5f) * 16 * S });
            }
        }
        void UpdateFairy(float dt)
        {
            if (fairyState != 1 || offFairy) return;
            fairyT += dt;
            if (fairyCool > 0) { fairyCool -= dt; if (fairyCool <= 0) { fairyCool = 0; fairyReadySaid = false; } }
            if (FairyReady && !fairyReadySaid && !dead) { fairyReadySaid = true; Say("Fadinha pronta: 4"); }
            bool k4 = IsDown(0x34) || IsDown(0x64);      // tecla 4: comanda a fadinha a atacar (so quando ela esta piscando em amarelo)
            if (((k4 && !prevF4) || !playMode) && FairyReady && fairyTarget == null && bossPhase == 0 && !dead)      // no modo automatico ela ataca sozinha assim que fica pronta; no jogo so com a tecla 4
            {
                Enemy best = null; float bd2 = float.MaxValue;      // o monstro mais perto do heroi (menos o dragao)
                foreach (Enemy e in enemies) { if (e.Type == 3 || e.Type == 7) continue; float d = (e.X - hx) * (e.X - hx) + (e.Y - hy) * (e.Y - hy); if (d < bd2) { bd2 = d; best = e; } }
                if (best != null) { fairyTarget = best; fairyPh = 1; fairyOrbit = 0; fairyCool = 30f / FairyRate; }
                else if (playMode) Say("Sem alvo");
            }
            prevF4 = k4;
            if (fairyTarget != null && !enemies.Contains(fairyTarget)) { fairyTarget = null; fairyPh = 0; }
            if (bossPhase != 0 || dead) { fairyTarget = null; fairyPh = 0; }
            if (fairyTarget == null && bossPhase == 0 && !dead)
            {
                float bd = float.MaxValue;
                foreach (Enemy e in enemies)
                {
                    if (e.Type == 3 || e.Type == 7) continue; if (!(eqFairy == 4 && e.Type == 0) && (!e.Fairy || e.Wave)) continue;      // fadinha sombria: tambem ataca TODOS os monstros vermelhos (octoroks)
                    float d = (e.X - fx) * (e.X - fx) + (e.Y - fy) * (e.Y - fy);
                    if (d < bd) { bd = d; fairyTarget = e; }
                }
                if (fairyTarget != null) { fairyPh = 1; fairyOrbit = 0; }
            }
            float oldX = fx, oldY = fy;
            if (fairyTarget == null)
            {
                float homeX = hx + hface * 5 * S + (float)Math.Sin(fairyT * 2.2) * 7 * S, homeY = hy - 26 * S + (float)Math.Sin(fairyT * 3.1) * 2 * S;      // perto da cabeca do heroi
                float k = Math.Min(1f, 6f * dt); fx += (homeX - fx) * k; fy += (homeY - fy) * k;
            }
            else
            {
                Enemy tg = fairyTarget; float headY = tg.Y - (tg.Type == 1 ? 15 : 17) * S;      // um pouco acima da cabeca do monstro
                if (fairyPh == 1)
                {
                    float dx = tg.X - fx, dy = headY - fy, d = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (d < 8 * S) { fairyPh = 2; fairyOrbit = 0; }
                    else { float st = Math.Min(d, 190 * S * dt); fx += dx / d * st; fy += dy / d * st; }
                }
                else
                {
                    fairyOrbit += dt; float a = fairyOrbit * 18f;      // volta rapida em torno da cabeca
                    fx = tg.X + (float)Math.Cos(a) * 8 * S; fy = headY + (float)Math.Sin(a) * 4 * S;
                    if (fairyOrbit >= 0.7f)
                    {
                        for (int k = 0; k < 6; k++) puffs.Add(new Puff { Kind = 1, X = tg.X + ((float)rnd.NextDouble() - 0.5f) * 12 * S, Y = headY + (float)rnd.NextDouble() * 10 * S });
                        Enemy t2 = tg; fairyTarget = null; fairyPh = 0; DamageEnemy(t2, 9999);
                    }
                }
            }
            fx = Math.Max(3 * S, Math.Min(W - 3 * S, fx)); fy = Math.Max(3 * S, Math.Min(H - 3 * S, fy));
            fairyTrail -= dt;
            if (fairyTrail <= 0 && (Math.Abs(fx - oldX) + Math.Abs(fy - oldY)) > 1.5f * S) { fairyTrail = FairyReady ? 0.04f : 0.07f; fairySparks.Add(new float[] { fx + ((float)rnd.NextDouble() - 0.5f) * 3 * S, fy + 1 * S + (float)rnd.NextDouble() * 2 * S, 0f }); }      // pronta: mais brilhinhos      // brilhinhos pequenos que somem
            for (int si = fairySparks.Count - 1; si >= 0; si--) { fairySparks[si][2] += dt; if (fairySparks[si][2] > 0.55f) fairySparks.RemoveAt(si); }
        }
        List<float[]> fairySparks = new List<float[]>();      // brilhinhos da fadinha: x, y, idade
        void DrawFairy(Graphics g)
        {
            if (fairyState != 1 || offFairy) return;
            foreach (float[] sp in fairySparks) { float q = 1f - sp[2] / 0.55f; shadowList.Add(new float[] { sp[0], sp[1] + sp[2] * 6 * S, S * 1f, 150f * q, eqFairy == 1 ? 1f : 0f, 0f }); }      // camada com transparencia real, abaixo do heroi
            bool hot = FairyReady && ((int)(time * 7)) % 2 == 0;      // pronta: pisca e fica mais amarela
            DrawFairyShape(g, fx, fy, S, eqFairy, hot, ((int)(time * 16)) % 2 == 0);
        }
        // desenho da fadinha (look 0 normal | 1 rosa com asas azul escuro); usado no jogo, no inventario e nos icones
        void DrawFairyShape(Graphics g, float x, float y, float u, int look, bool hot, bool flap)
        {
            Color[][] pal = {      // corpo, nucleo, asa, asa clara: 0 normal | 1 rosa | 2 folha | 3 estrela
                new Color[] { Color.FromArgb(255, 245, 160), Color.White, Color.FromArgb(190, 225, 255), Color.FromArgb(235, 245, 255) },
                new Color[] { Color.FromArgb(245, 130, 195), Color.FromArgb(255, 225, 240), Color.FromArgb(40, 50, 150), Color.FromArgb(70, 85, 190) },
                new Color[] { Color.FromArgb(120, 215, 95), Color.FromArgb(235, 255, 200), Color.FromArgb(235, 165, 40), Color.FromArgb(255, 215, 110) },
                new Color[] { Color.FromArgb(185, 115, 240), Color.FromArgb(250, 235, 255), Color.FromArgb(40, 185, 175), Color.FromArgb(125, 230, 220) },
                new Color[] { Color.FromArgb(78, 56, 120), Color.FromArgb(214, 150, 255), Color.FromArgb(34, 28, 54), Color.FromArgb(98, 66, 148) } };
            Color[] pc = pal[Math.Max(0, Math.Min(4, look))];
            using (SolidBrush glow = new SolidBrush(hot ? Color.FromArgb(255, 205, 20) : pc[0]))
            using (SolidBrush wing = new SolidBrush(pc[2]))
            using (SolidBrush wing2 = new SolidBrush(pc[3]))
            using (SolidBrush core = new SolidBrush(hot ? Color.FromArgb(255, 255, 110) : pc[1]))
            {
                g.FillRectangle(glow, x - 2 * u, y - u, 4 * u, 2 * u);      // bolinha menor que as asas
                g.FillRectangle(glow, x - u, y - 2 * u, 2 * u, 4 * u);
                g.FillRectangle(core, x - u, y - u, 2 * u, 2 * u);
                float wy = flap ? -3 * u : -1 * u;
                g.FillRectangle(wing, x - 6 * u, y + wy - u, 3 * u, 2 * u); g.FillRectangle(wing2, x - 5 * u, y + wy, 2 * u, u);
                g.FillRectangle(wing, x + 3 * u, y + wy - u, 3 * u, 2 * u); g.FillRectangle(wing2, x + 3 * u, y + wy, 2 * u, u);
            }
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
        // ESCUDO DE MACA CORTADA: grade de 11 x 14 quadradinhos (quadradinhos 8% maiores que na versao de 12 x 15, e mais simples): casca vermelha com um brilho, polpa creme,
        // 2 sementes, talo e folha. A forma (dois lobos com covinha em cima) e gerada por codigo.
        static string[] appleRows;
        static string[] AppleShieldRows()
        {
            if (appleRows != null) return appleRows;
            const int W = 11, H = 14; bool[,] ins = new bool[H, W]; int[,] dep = new int[H, W];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float px = x + 0.5f, py = y + 0.5f, ax = (px - 3.7f) / 3.6f, ay = (py - 8.6f) / 5.0f, bx = (px - 7.3f) / 3.6f, by = (py - 8.6f) / 5.0f;
                bool body = (ax * ax + ay * ay <= 1f) || (bx * bx + by * by <= 1f), dimple = (px - 5.5f) * (px - 5.5f) + (py - 4.0f) * (py - 4.0f) < 1.4f * 1.4f;
                ins[y, x] = body && !dimple && y >= 3;
            }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) dep[y, x] = ins[y, x] ? 99 : -1;
            for (int pass = 0; pass < 4; pass++)
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    if (!ins[y, x] || dep[y, x] != 99) continue;
                    bool edge = y == 0 || x == 0 || y == H - 1 || x == W - 1; int best = 99;
                    int[] dx4 = { 1, -1, 0, 0 }, dy4 = { 0, 0, 1, -1 };
                    for (int d = 0; d < 4; d++) { int nx = x + dx4[d], ny = y + dy4[d]; if (nx < 0 || ny < 0 || nx >= W || ny >= H || !ins[ny, nx]) edge = true; else if (dep[ny, nx] < 99) best = Math.Min(best, dep[ny, nx] + 1); }
                    if (edge) dep[y, x] = 0; else if (best < 99) dep[y, x] = best;
                }
            char[][] g = new char[H][]; for (int y = 0; y < H; y++) { g[y] = new char[W]; for (int x = 0; x < W; x++) g[y][x] = '.'; }
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                if (!ins[y, x]) continue; int d = dep[y, x]; char c;
                if (d == 0) c = 'K';
                else if (d == 1) c = (x <= 3 && y >= 5 && y <= 7) ? 'h' : 'R';      // casca vermelha com um brilho simples
                else c = 'F';      // polpa creme
                g[y][x] = c;
            }
            g[8][4] = 'B'; g[9][4] = 'B'; g[8][6] = 'B'; g[9][6] = 'B';      // 2 sementes
            // talo (marrom) e folha (verde) em cima
            string[] top = { "....KK.KKK.", "....TTKLLLK", "....TT.KKK." };
            for (int y = 0; y < top.Length && y < 3; y++) for (int x = 0; x < W; x++) if (top[y][x] != '.') g[y][x] = top[y][x];
            string[] rows = new string[H]; for (int y = 0; y < H; y++) rows[y] = new string(g[y]);
            appleRows = rows; return rows;
        }
        // ESCUDO SOMBRIO (conjunto sombra): escudo em "brasao" - topo reto e lados retos que afunilam em TRIANGULO ate uma ponta embaixo. Grade de 11 x 14 gerada por codigo
        // (mesmo tamanho do escudo maca: 8k x 10k): contorno escuro, borda de aco arroxeada, corpo roxo escuro com sombra a direita e um losango prateado no centro.
        static string[] darkShieldRows;
        static string[] DarkShieldRows()
        {
            if (darkShieldRows != null) return darkShieldRows;
            const int W = 11, H = 14; bool[,] ins = new bool[H, W]; int[,] dep = new int[H, W];
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                float half = y <= 6 ? 5.2f : 5.2f * (13.6f - y) / 7f, dxp = Math.Abs(x + 0.5f - 5.5f);
                ins[y, x] = dxp <= half && !(y == 0 && (x == 0 || x == W - 1));
                dep[y, x] = ins[y, x] ? 99 : -1;
            }
            for (int pass = 0; pass < 3; pass++)
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                {
                    if (!ins[y, x] || dep[y, x] != 99) continue;
                    bool edge = false; int best = 99; int[] ddx = { 1, -1, 0, 0, 1, -1, 1, -1 }, ddy = { 0, 0, 1, -1, 1, 1, -1, -1 };
                    for (int d = 0; d < 8; d++) { int nx = x + ddx[d], ny = y + ddy[d]; if (nx < 0 || ny < 0 || nx >= W || ny >= H || !ins[ny, nx]) edge = true; else if (dep[ny, nx] < 99) best = Math.Min(best, dep[ny, nx] + 1); }
                    if (edge) dep[y, x] = 0; else if (best < 99) dep[y, x] = best;
                }
            string[] rows = new string[H];
            for (int y = 0; y < H; y++)
            {
                char[] c = new char[W];
                for (int x = 0; x < W; x++)
                {
                    if (!ins[y, x]) { c[x] = '.'; continue; }
                    int d = dep[y, x];
                    c[x] = d == 0 ? 'K' : (d == 1 ? 'M' : (x >= 7 ? 'd' : 'D'));
                    if (d >= 2 && Math.Abs(x - 5) + Math.Abs(y - 5) <= 2) c[x] = (x == 5 && y == 5) ? 'e' : 'E';      // losango prateado no centro
                }
                rows[y] = new string(c);
            }
            darkShieldRows = rows; return rows;
        }
        void DrawDarkShieldPix(Graphics g, float x0, float y0, float k)
        {
            SmoothingMode sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            string[] rows = DarkShieldRows(); float cell = k * 8f / 11f;
            Color[] cs = { Color.FromArgb(28, 22, 36), Color.FromArgb(138, 132, 178), Color.FromArgb(56, 48, 90), Color.FromArgb(38, 32, 66), Color.FromArgb(204, 206, 224), Color.FromArgb(246, 246, 255) };
            const string keys = "KMDdEe";
            using (SolidBrush b = new SolidBrush(Color.Black))
                for (int r = 0; r < rows.Length; r++) for (int c = 0; c < rows[r].Length; c++)
                {
                    int ci = keys.IndexOf(rows[r][c]); if (ci < 0) continue;
                    float xa = (float)Math.Round(x0 + c * cell), xb = (float)Math.Round(x0 + (c + 1) * cell), ya = (float)Math.Round(y0 + r * cell), yb = (float)Math.Round(y0 + (r + 1) * cell);
                    b.Color = cs[ci]; g.FillRectangle(b, xa, ya, Math.Max(1f, xb - xa), Math.Max(1f, yb - ya));
                }
            g.SmoothingMode = sm;
        }
        void DrawApplePix(Graphics g, float x0, float y0, float k)      // escudo de maca cortada: mesmo tamanho de antes (8k x 10k), agora com quadradinhos menores (2/3)
        {
            SmoothingMode sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            string[] rows = AppleShieldRows(); float cell = k * 8f / 11f;
            Color[] cs = { Color.FromArgb(28, 22, 36), Color.FromArgb(200, 34, 44), Color.FromArgb(150, 22, 34), Color.FromArgb(242, 120, 120), Color.FromArgb(250, 236, 190), Color.FromArgb(226, 206, 148), Color.FromArgb(214, 190, 128), Color.FromArgb(110, 60, 30), Color.FromArgb(70, 38, 20), Color.FromArgb(120, 80, 40), Color.FromArgb(70, 160, 60), Color.FromArgb(136, 214, 100) };
            const string keys = "KRrhFfcBbTLl";
            using (SolidBrush b = new SolidBrush(Color.Black))
                for (int r = 0; r < rows.Length; r++) for (int c = 0; c < rows[r].Length; c++)
                {
                    int ci = keys.IndexOf(rows[r][c]); if (ci < 0) continue;
                    float xa = (float)Math.Round(x0 + c * cell), xb = (float)Math.Round(x0 + (c + 1) * cell), ya = (float)Math.Round(y0 + r * cell), yb = (float)Math.Round(y0 + (r + 1) * cell);
                    b.Color = cs[ci]; g.FillRectangle(b, xa, ya, Math.Max(1f, xb - xa), Math.Max(1f, yb - ya));
                }
            g.SmoothingMode = sm;
        }
        static string[] kingShieldRows;
        static string[] KingShieldRows()      // escudo do rei: circulo de 10x10 quadradinhos (contorno, aro lilas, fundo roxo e quadrado azul no centro)
        {
            if (kingShieldRows != null) return kingShieldRows;
            string[] r = new string[10];
            for (int y = 0; y < 10; y++)
            {
                char[] c = new char[10];
                for (int x = 0; x < 10; x++)
                {
                    double dx = x - 4.5, dy = y - 4.5, d = Math.Sqrt(dx * dx + dy * dy);
                    if (d > 4.95) c[x] = '.'; else if (d > 4.0) c[x] = 'K'; else if (d > 3.1) c[x] = 'M'; else c[x] = 'P';
                    if (Math.Abs(dx) <= 1.6 && Math.Abs(dy) <= 1.6) c[x] = (Math.Abs(dx) <= 0.6 && Math.Abs(dy) <= 0.6) ? 'U' : 'K';
                }
                r[y] = new string(c);
            }
            kingShieldRows = r; return r;
        }
        void DrawKingShieldPix(Graphics g, float cx, float cy, float cell)      // pixelado como os outros escudos; (cx,cy) = centro
        {
            SmoothingMode sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            string[] rows = KingShieldRows(); float x0 = cx - 5 * cell, y0 = cy - 5 * cell;
            Color[] cs = { Color.FromArgb(28, 22, 36), Color.FromArgb(110, 60, 160), Color.FromArgb(70, 30, 110), Color.FromArgb(70, 125, 235) };
            const string keys = "KMPU";
            using (SolidBrush b = new SolidBrush(Color.Black))
                for (int r = 0; r < rows.Length; r++) for (int c = 0; c < rows[r].Length; c++)
                {
                    int ci = keys.IndexOf(rows[r][c]); if (ci < 0) continue;
                    float xa = (float)Math.Round(x0 + c * cell), xb = (float)Math.Round(x0 + (c + 1) * cell), ya = (float)Math.Round(y0 + r * cell), yb = (float)Math.Round(y0 + (r + 1) * cell);
                    b.Color = cs[ci]; g.FillRectangle(b, xa, ya, Math.Max(1f, xb - xa), Math.Max(1f, yb - ya));
                }
            g.SmoothingMode = sm;
        }
        void DrawShieldBody(Graphics g, float sx, float sy)
        {
            if (eqShield == 2) { DrawKingShieldPix(g, sx + 3 * S, sy + 4 * S, 0.72f * S); return; }
            if (eqShield == 3) { DrawApplePix(g, sx - S, sy - S, S); return; }
            if (eqShield == 4) { DrawDarkShieldPix(g, sx - S, sy - S, S); return; }      // escudo sombrio: triangular embaixo
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
            using (SolidBrush yb = new SolidBrush(eqShield == 4 ? Color.FromArgb(204, 206, 224) : Color.FromArgb(245, 205, 70)))      // escudo sombrio: emblema prateado
            {
                g.FillRectangle(kb, sx, sy, 6 * S, 8 * S);      // escudo azul/vermelho menor em proporcao ao Link (antes 8 x 10)
                g.FillRectangle(bb, sx + S, sy + S, 4 * S, 6 * S);
                g.FillRectangle(yb, sx + 2 * S, sy + 2 * S, 2 * S, 2 * S);
            }
        }

        void DrawShieldItem(Graphics g, float cx, float cy, float u)
        {
            DrawKingShieldPix(g, cx, cy, 1.7f * u); return;      // icone pixelado
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
        void DrawCrownItem(Graphics g, float cx, float cy, float u)      // coroa em pixel art (a mesma do menu e do tesouro)
        {
            DrawItemSpr(g, menuSpr[3], cx, cy, u * 1.0f);
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
        int RupeeStep() { return Math.Min(6, rupees / 20); }       // indice do nivel dentro da fase (0..6): cada 20 rupias sobe um nivel (fases 1 e 2)
        float DiffStep() { if (phase3) return 5f + 1.5f * RupeeStep(); return RupeeStep() + (phase2 ? 3 : 0) +      // FASE 3: comeca na dificuldade do 3o nivel da fase 2 (passo 5) e sobe o que ja subia (+10%) MAIS 5% por nivel = +15% (1,5 passo)
             ((!phase2 && RupeeStep() == 1) ? 0.5f : 0f); }      // NIVEL 2 da fase 1: um pouco mais dificil (+5% de monstros e de ritmo)
        //      // dificuldade: cada nivel = +10% de monstros. A fase 2 comeca no NIVEL 8 com a dificuldade do nivel 4 (passo 3) e sobe 1 por nivel ate o 14

        // Quantidade de monstros: comeca em 2/3 do maximo da animacao (4) e cresce 10% (do inicial) a cada 20 rupias
        int EnemyCap()
        {
            if (!playMode) return powerT > 0 ? 72 : 4;       // poder: 18x mais monstros
            float cap = 4f * 2f / 3f * (1f + 0.1f * DiffStep());
            if (interlude) cap *= 2.4f;      // fase intermediaria: muitos morcegos
            int b = (int)Math.Floor(cap);
            return b + (rnd.NextDouble() < cap - b ? 1 : 0);
        }
        float SpawnRateMul() { return playMode ? (1f + 0.1f * DiffStep()) * (interlude ? 2.2f : 1f) : (powerT > 0 ? 18f : 1f); }

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
        int walletAuto;      // saldo do modo automatico: guardado enquanto joga no modo jogavel e salvo no registro (o saldo do jogavel e temporario, como sempre foi)
        void LoadWallets()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\HeroiDoDesktop")) { if (k != null) { object a = k.GetValue("RupiasAuto"); if (a != null) rupees = Math.Max(0, Convert.ToInt32(a)); } } } catch { }
            lastStep = RupeeStep();
        }
        void SaveWallets()
        {
            if (Environment.GetEnvironmentVariable("HEROI_TESTE") != null) return;
            try { using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\HeroiDoDesktop")) k.SetValue("RupiasAuto", playMode ? walletAuto : rupees); } catch { }
        }
        void EnterPlay()
        {
            foreach (Enemy e in enemies) puffs.Add(new Puff { Kind = 0, X = e.X, Y = e.Y - 4 * S });
            enemies.Clear(); rocks.Clear(); arrows.Clear();
            if (cookState != 0) AbortCook();
            if (dead) { dead = false; hp = MaxHP; }
            hx = W / 2; hy = H / 2; kvx = kvy = 0; inv = 1f; walking = false;
            puffs.Add(new Puff { Kind = 2, X = hx, Y = hy });
            walletAuto = rupees; rupees = 0; apples = 0; flyApples.Clear(); toastShown = false;     // as rupias do automatico ficam guardadas (salvas); o jogavel comeca do zero, como antes
            spawnTimer = 2.5f;
        }

        void SetPlayMode(bool on)
        {
            bool was = playMode;
            playMode = on;
            if (!on) RestoreWorld();      // volta as macieiras, tira o escuro, a cena e o aliado
            if (!on && (phase2 || phase3)) { phase2 = false; phase3 = false; p3EndT = 0; BuildSprites(); }      // saiu do jogo: a fase 2 acaba
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
                if (was) rupees = walletAuto;      // volta o saldo do automatico; o do jogavel some, como antes
                lastStep = RupeeStep(); SaveWallets();
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
            { bool a1 = k1, a2 = k2; if (!SwordUse) k1 = false; if (!BowUse) k2 = false; if (a1 && !SwordUse && BowUse) k2 = true; if (a2 && !BowUse && SwordUse) k1 = true; }      // sem uma das armas, a tecla usa a outra
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
                    FireArrows(sx, sy);
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
        // No nivel maximo (120 rupias) e depois de derrotar 5 monstros: os que sobram explodem, aparece uma fogueira,
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
            bossPhase = 0; killsAtMax = 0; bossT = 0; atePhase1 = false; playEatT = 0; endStarted = false; bossSpawned = false; waveSpawned = 0; waveT = 2f;
            if (boss != null) { enemies.Remove(boss); boss = null; }
            acids.Clear(); pools.Clear(); starPh = 0; starTarget = null;
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
            if (!phase2) for (int k = 0; k < 10; k++)      // a fumaca e as faiscas da fogueira so aparecem na fase 1 (dragao); nas fases 2 e 3 a fumaca sai do monstrinho
            {
                puffs.Add(new Puff { Kind = k % 2, X = fireX + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = fireY - (float)rnd.NextDouble() * 12 * S });
            }
            if (!phase2) for (int k = 0; k < 18; k++)
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

        // ================= FASE 2 =================
        // Depois de vencer o Dragao comeca a FASE 2: os mesmos 7 niveis (agora a cada 40 rupias), monstros sombrios com o dobro de vida e,
        // no fim, o vilao: o FEITICEIRO SOMBRIO (8000 de vida), que teleporta, lanca aneis de esferas, tiros mirados e invoca gelatinas.
        bool phase2, phase3, bossDeadMage; Sprite mageSpr; float p3EndT;      // phase3: Fase 3 (usa tudo da Fase 2 com phase2 = true)
        float rewardWaitT, p2MsgT;      // p2MsgT: aviso 'VOCE PASSOU PARA A FASE 2!' (depois da cena)
              // tempo esperando o heroi pegar as rupias do dragao antes da Fase 2

        // ================= CENA DO FEITICEIRO (entre a Fase 1 e a 2) + MUNDO ESCURO DA FASE 2 + CAJADO =================
        // Depois do dragao e das rupias: os monstros somem, a tela amplia em cima do Link, o Feiticeiro (do tamanho do Link, fingindo ser bom) chega e conversa;
        // a tela volta ao normal, escurece, e comeca uma FASE INTERMEDIARIA JOGAVEL num lugar escuro (pedras, pocas, filtro cinza, bordas pretas) com o Feiticeiro seguindo como aliado.
        // Depois de matar muitos morcegos o cajado cai no meio da tela, o Feiticeiro pega e some; a tela escurece e so entao comeca a Fase 2 (como antes).
        int scene, dlgIdx;      // scene: 0 nada | 1 monstros somem + amplia | 2 feiticeiro chega | 3 conversa | 4 volta ao normal | 5 escurece | 6 clareia
        float sceneT, zoomE, fadeA, zfx, zfy;      // zoomE 0..1 (amplia 1x -> 1,8x ate o Link ficar no centro); fadeA 0..1 (preto por cima de tudo)
        bool allyOn, darkOn, tintWasOn, interlude; int lastFa = -1; bool lastDark; float ax, ay; int aface = -1;      // interlude: fase intermediaria jogavel (lugar escuro com morcegos e o Feiticeiro aliado), entre a Fase 1 e a 2
        Sprite wizSpr, staffSpr; Sprite[] stoneSpr = new Sprite[3]; AllyMark allyMark = new AllyMark();
        bool wizHasStaff;      // depois de pegar o cajado, o Feiticeiro passa a segura-lo sempre que aparece
        List<Drop> drops = new List<Drop>(); float dropT; bool wasInPuddle;
        ShadowLayer tintLayer; Bitmap darkOverlay;
        List<Tree> savedTrees; List<Puddle> puddles = new List<Puddle>();
        bool staffPending; int staffPh, batKills; float staffT, staffX, staffGround; Font dlgFont;      // staffPh: 0 nada | 1 cajado cai | 2 feiticeiro vai pegar | 3 sumiu (mensagem)
        const int BatsForStaff = 30;
        static readonly string[][] Dlg = {
            new string[] { "Feiticeiro", "Preciso da sua ajuda, recupere meu cajado, você é forte e valente." },
            new string[] { "Link", "Por que precisa dele?" },
            new string[] { "Feiticeiro", "Seu poder..." },
            new string[] { "Feiticeiro", "Você só pode saber onde ele está." } };
        // 2a cena (entre a Fase 2 e a 3): o Feiticeiro pede a espada. '|' = pausa dentro da fala; speaker "*" = acao (aponta o cajado, sem fala)
        static readonly string[][] Dlg2 = {
            new string[] { "Feiticeiro", "Preciso de ajuda...| Você pode me dar sua...| sua espada?" },
            new string[] { "Link", "Hã?" },
            new string[] { "Feiticeiro", "Preciso de equipamento" },
            new string[] { "Link", "NÃO!" },
            new string[] { "*", "" },
            new string[] { "Feiticeiro", "Meu amigo, não se preocupe, eu...| nunca o machucaria, he he" } };
        static readonly float[] DlgTime2 = { 6.2f, 2.4f, 3.4f, 2.4f, 12f, 6f };      // a ameaca com o cajado (acao) dura 12 s
        bool sceneTest, prevHk1, prevHk2; int sceneKind = 1; float wizPoint, wizGlow;      // sceneTest: atalho de teste: toca so a conversa e volta ao jogo normal      // wizGlow 0..1: a ponta do cajado brilha como se fosse atirar      // sceneKind: 1 primeira conversa (vai para a fase intermediaria) | 2 segunda (vai para a Fase 3); wizPoint 0..1: o Feiticeiro aponta o cajado
        string[][] CurDlg { get { return sceneKind == 2 ? Dlg2 : Dlg; } }
        float[] CurDlgTime { get { return sceneKind == 2 ? DlgTime2 : DlgTime; } }
        static readonly float[] DlgTime = { 6.6f, 4.6f, 2.8f, 5.8f };      // cada fala fica 2 s a mais na tela; "Seu poder..." fica a metade do tempo das outras
        float ZK { get { return 1f + 1.3f * zoomE; } }      // amplia ate 2,3x
        float ZOx { get { return zfx + (W / 2f - zfx) * zoomE; } }      // onde o ponto do Link cai na tela
        float ZOy { get { return zfy + (H / 2f - zfy) * zoomE; } }
        static float Ease(float t) { t = Math.Max(0f, Math.Min(1f, t)); return t * t * (3f - 2f * t); }

        void StartScene(int kind = 1)
        {
            sceneKind = kind; wizPoint = 0f; if (kind == 2) { wizHasStaff = true; acids.Clear(); pools.Clear(); sparks.Clear(); starPh = 0; starTarget = null; }
            ResetBoss(); CloseDeath(); if (dead) { dead = false; hp = MaxHP; }
            foreach (Enemy en in enemies) { puffs.Add(new Puff { Kind = 0, X = en.X, Y = en.Y - 4 * S }); puffs.Add(new Puff { Kind = 1, X = en.X, Y = en.Y - 8 * S }); }
            enemies.Clear(); rocks.Clear(); arrows.Clear(); items.Clear();
            scene = 1; sceneT = 0; dlgIdx = 0; zoomE = 0; fadeA = 0; allyOn = false; staffPh = 0;
            zfx = hx; zfy = hy - 8 * S; hface = 1; walking = false; attacking = false; bowing = false; speechT = 0; inv = 0; kvx = kvy = 0;
        }
        void StartSceneTest() { if (!playMode) SetPlayMode(true); sceneTest = true; StartScene(); }      // atalho: so a 1a conversa do Feiticeiro
        // ATALHOS DE TECLADO (escondidos): Ctrl+Shift+F1 = 1a conversa do Feiticeiro | Ctrl+Shift+F2 = 2a conversa. Tambem: --cena e --cena2 ao abrir o jogo.
        void CheckSceneHotkeys()
        {
            bool cs = IsDown(0x11) && IsDown(0x10), k1 = cs && IsDown(0x70), k2 = cs && IsDown(0x71);
            if (scene == 0 && !paused && !deathWait) { if (k1 && !prevHk1) StartSceneTest(); else if (k2 && !prevHk2) StartScene2Test(); }
            prevHk1 = k1; prevHk2 = k2;
        }
        void StartScene2Test() { if (!playMode) SetPlayMode(true); phase2 = true; BuildSprites(); sceneTest = true; StartScene(2); }      // atalho: so a segunda conversa (Feiticeiro pede a espada) e depois a Fase 3

        void UpdateScene(float dt)
        {
            sceneT += dt; UpdateSparks(dt);
            for (int i = puffs.Count - 1; i >= 0; i--) { puffs[i].T += dt; if (puffs[i].T > 0.45f) puffs.RemoveAt(i); }
            if (speechT > 0) speechT -= dt;
            walking = false; hface = (scene == 2 || scene == 3 || scene == 4 || scene == 8) ? 1 : hface;
            switch (scene)
            {
                case 1:
                    zoomE = Ease(sceneT / 3.4f);      // aproxima devagar
                    if (sceneT >= 3.9f) { scene = 2; sceneT = 0; allyOn = true; ax = hx + 200 * S; ay = hy; aface = -1; }
                    break;
                case 2:
                    {
                        float tx = hx + (sceneKind == 2 ? 44 : 26) * S; ax -= 62 * S * dt; ay = hy; aface = -1;      // na 2a conversa os dois ficam mais distantes
                        if (ax <= tx) { ax = tx; scene = 3; sceneT = 0; dlgIdx = 0; }
                        break;
                    }
                case 3:
                    {
                        bool act = CurDlg[dlgIdx][0] == "*"; float dur = CurDlgTime[dlgIdx];
                        wizPoint = act ? Math.Min(Ease(sceneT / 0.9f), Ease((dur - sceneT) / 1.5f)) : 0f;      // levanta o cajado (0,9 s), aponta para o Link e volta a posicao inicial em 1,5 s
                        wizGlow = act ? (sceneT < 4.5f ? 0f : (sceneT < 6f ? Ease((sceneT - 4.5f) / 1.5f) : (sceneT < 10.5f ? 1f : Ease((dur - sceneT) / (dur - 10.5f))))) : 0f;      // 4,5 s: comeca a brilhar aos poucos (1,5 s), brilha 4,5 s e para aos poucos (1,5 s)      // aos 3 s a ponta comeca a brilhar aos poucos (1 s), brilha 3 s (4 a 7 s) e para aos poucos (7 a 8 s)
                        if (sceneT >= dur) { dlgIdx++; sceneT = 0; wizPoint = 0f; wizGlow = 0f; if (dlgIdx >= CurDlg.Length) { scene = sceneKind == 2 ? 8 : 4; sceneT = 0; } }
                        break;
                    }
                case 8:      // 2a cena: o Feiticeiro vai embora (anda para a direita ate sair da tela)
                    aface = 1; ax += 70 * S * dt; ay = hy;
                    if (ax > hx + 230 * S) { allyOn = false; scene = 4; sceneT = 0; }
                    break;
                case 4:
                    zoomE = 1f - Ease(sceneT / 1.2f);
                    if (sceneT >= 1.4f) { zoomE = 0; scene = 5; sceneT = 0; }
                    break;
                case 5:
                    fadeA = Math.Min(1f, sceneT / 1.0f);
                    if (sceneT >= 1.2f)
                    {
                        if (sceneTest) { sceneTest = false; allyOn = false; scene = 6; sceneT = 0; fadeA = 1f; break; }      // atalho de teste: acabou a conversa, volta ao jogo normal
                        if (sceneKind == 2) { allyOn = false; StartPhase3(); }      // 2a conversa: depois dela comeca a Fase 3
                        else StartInterlude();      // monta o lugar escuro (fase intermediaria) enquanto a tela esta preta
                        scene = 6; sceneT = 0; fadeA = 1f;
                    }
                    break;
                case 7:      // depois do cajado: escurece e comeca a Fase 2
                    fadeA = Math.Min(1f, sceneT / 1.0f);
                    if (sceneT >= 1.2f) { RestoreWorld(); StartPhase2(); fadeA = 1f; scene = 6; sceneT = 0; p2MsgT = 0.001f; }
                    break;
                case 6:
                    fadeA = sceneT < 0.6f ? 1f : Math.Max(0f, 1f - (sceneT - 0.6f) / 1.2f);
                    if (sceneT >= 1.8f) { fadeA = 0; scene = 0; }
                    break;
            }
        }

        // Feiticeiro aliado: segue o Link de perto (nao ajuda a lutar)
        void UpdateAlly(float dt)
        {
            if (!allyOn || scene != 0 || staffPh >= 2) return;
            float tx = hx - hface * 22 * S, ty = hy + 2 * S, dx = tx - ax, dy = ty - ay, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > 3 * S)
            {
                float sp = (d > 40 * S ? 78 : 46) * S * dt, st = Math.Min(d, sp);
                ax += dx / d * st; ay += dy / d * st;
                if (Math.Abs(dx) > 2) aface = dx > 0 ? 1 : -1;
            }
            else aface = hface;
        }
        void DrawAlly(Graphics g)
        {
            float bob = (float)Math.Sin(time * 3f) * 1.2f * S, shx = 0, shy = 0;
            if (wizPoint > 0.6f)      // enquanto aponta o cajado ele fica parado, tremendo
            { shx = ((zTick & 1) == 0 ? 1 : -1) * 0.22f * S * wizPoint; shy = (((zTick >> 1) & 1) == 0 ? 1 : -1) * 0.12f * S * wizPoint; bob = 0; }      // treme a cada quadro (o mais rapido possivel) e se move menos de um lado para o outro
            Shadow(g, ax, ay, 11);
            Spr(g, wizSpr, ax + shx, ay - bob + shy, aface > 0);
            if (wizPoint > 0f && staffSpr != null) DrawPointingStaff(g, ax + shx, ay - bob + shy); else DrawHeldStaff(g, ax, ay - bob, 0.58f, aface);      // cajado um pouco menor
        }
        void DrawPointingStaff(Graphics g, float x, float feetY)      // cajado levantado e apontado para frente (para o Link), com a esfera brilhando
        {
            float k = 0.58f; int w = (int)(staffSpr.W * k), h = (int)(staffSpr.H * k);
            float px = x + aface * 7 * S, py = feetY - 7 * S, ang = (aface > 0 ? 90f : -90f) * Ease(wizPoint);
            GraphicsState st = g.Save(); g.TranslateTransform(px, py); g.RotateTransform(ang);
            g.DrawImage(staffSpr.R, (int)(-w / 2f), (int)(-h + 2 * S * k), w, h);
            g.Restore(st);
            if (wizGlow <= 0.01f) return;      // so brilha na carga
            double rad = ang * Math.PI / 180.0; float tl = h - 4 * S * k, tipX = px + (float)Math.Sin(rad) * tl, tipY = py - (float)Math.Cos(rad) * tl, gl = wizGlow, pulse = 1f + 0.15f * (float)Math.Sin(time * (10f + 8f * gl));
            // brilho transparente de verdade: vai para a camada de transparencia (gradiente), nao pontilhado
            float gk = zoomE > 0.0001f ? ZK : 1f, gx = zoomE > 0.0001f ? ZOx + (tipX - zfx) * gk : tipX, gy = zoomE > 0.0001f ? ZOy + (tipY - zfy) * gk : tipY;
            shadowList.Add(new float[] { gx, gy, (2.8f + 3.6f * gl) * S * gk * pulse, 150f * gl, 0f, 0f, 0f });
            using (SolidBrush b2 = new SolidBrush(Color.FromArgb(245, 230, 255)))
            {
                float c = (0.5f + 0.7f * gl) * S; g.FillRectangle(b2, tipX - c / 2f, tipY - c / 2f, c, c);      // pontinho claro no centro
                for (int q = 0; q < 4; q++) { double an = time * (6 + 6 * gl) + q * Math.PI / 2, rr = (2.2f + 1.6f * gl) * S; g.FillRectangle(b2, tipX + (float)Math.Cos(an) * (float)rr - S / 2f, tipY + (float)Math.Sin(an) * (float)rr - S / 2f, S, S); }
            }
        }

        // ---- fase intermediaria: lugar escuro (entre a Fase 1 e a 2) ----
        void StartInterlude()
        {
            interlude = true;
            enemies.Clear(); rocks.Clear(); arrows.Clear(); items.Clear(); puffs.Clear();
            hp = MaxHP; dead = false; deathWait = false; inv = 0f;
            hx = W / 2; hy = H / 2; spawnTimer = 1.5f; killsAtMax = 0;
            SetupDarkWorld();
        }
        void SetupDarkWorld()
        {
            if (savedTrees == null) savedTrees = new List<Tree>(trees);
            trees.Clear(); DisposePuddles();
            for (int n = 0; n < 12; n++)      // pedras espalhadas pelo cenario todo (nao so no canto), em 3 modelos
            {
                float x = W / 2, y = H / 2;
                for (int t = 0; t < 60; t++)
                {
                    x = W * (0.05f + 0.90f * (float)rnd.NextDouble()); y = H * (0.20f + 0.76f * (float)rnd.NextDouble());
                    bool ok = Math.Abs(x - W / 2f) + Math.Abs(y - H / 2f) > 90 * S;
                    foreach (Tree o in trees) if ((o.X - x) * (o.X - x) + (o.Y - y) * (o.Y - y) < (40 * S) * (40 * S)) ok = false;
                    if (ok) break;
                }
                Tree st = new Tree(); st.X = x; st.Y = y; st.Stone = true; st.Variant = n % 3;
                for (int i = 0; i < 5; i++) st.Regrow[i] = 1e9f;      // pedra nao da maca
                trees.Add(st);
            }
            int[,] ps = { { 30, 11 }, { 18, 15 }, { 34, 14 }, { 32, 20 } };      // 4 pocas, 4 formatos: oval larga, redonda, duas lobos, comprida inclinada
            for (int n = 0; n < 4; n++)
            {
                Puddle pd = new Puddle(); pd.Type = n; pd.W = ps[n, 0] * S; pd.H = ps[n, 1] * S;
                for (int t = 0; t < 60; t++)
                {
                    pd.X = W * (0.10f + 0.80f * (float)rnd.NextDouble()); pd.Y = H * (0.25f + 0.68f * (float)rnd.NextDouble());
                    bool ok = Math.Abs(pd.X - W / 2f) + Math.Abs(pd.Y - H / 2f) > 80 * S;
                    foreach (Puddle q in puddles) if ((q.X - pd.X) * (q.X - pd.X) + (q.Y - pd.Y) * (q.Y - pd.Y) < (60 * S) * (60 * S)) ok = false;
                    foreach (Tree o in trees) if ((o.X - pd.X) * (o.X - pd.X) + (o.Y - pd.Y) * (o.Y - pd.Y) < (34 * S) * (34 * S)) ok = false;      // poca nao fica por baixo de pedra
                    foreach (Tree o in trees) if ((o.X - pd.X) * (o.X - pd.X) + (o.Y - pd.Y) * (o.Y - pd.Y) < (34 * S) * (34 * S)) ok = false;      // poca longe das pedras
                    if (ok) break;
                }
                pd.Bmp = BuildPuddle(pd); puddles.Add(pd);
            }
            if (darkOverlay == null || darkOverlay.Width != (int)W || darkOverlay.Height != (int)H) { if (darkOverlay != null) darkOverlay.Dispose(); darkOverlay = BuildDarkOverlay((int)W, (int)H); }
            darkOn = true; allyOn = true; batKills = 0; staffPh = 0; drops.Clear(); wasInPuddle = false;
            ax = hx - 22 * S; ay = hy + 2 * S; aface = 1;
        }
        void DisposePuddles() { foreach (Puddle p in puddles) if (p.Bmp != null) p.Bmp.Dispose(); puddles.Clear(); }
        static bool PuddleIn(int type, float nx, float ny)      // nx, ny entre -1 e 1 dentro da caixa da poca
        {
            if (type == 0) return nx * nx + ny * ny <= 1f;
            if (type == 1) { float a = nx * 0.95f, b = ny * 0.95f; return a * a + b * b <= 0.9f; }
            if (type == 2) { float a = (nx + 0.42f) / 0.62f, b = ny / 0.85f, c = (nx - 0.4f) / 0.66f, d = (ny - 0.12f) / 0.7f; return a * a + b * b <= 1f || c * c + d * d <= 1f; }
            float u = nx * 0.9f + ny * 0.45f, v = -nx * 0.45f + ny * 0.9f; return u * u + v * v / 0.2f <= 1f;
        }
        Bitmap BuildPuddle(Puddle p) { return BuildPuddleColored(p.W, p.H, p.Type, Color.FromArgb(30, 58, 92), Color.FromArgb(52, 92, 138), Color.FromArgb(150, 190, 225)); }
        Bitmap BuildPuddleColored(int pw, int ph, int type, Color rimC, Color waterC, Color shineC)      // poca em quadradinhos (borda escura, liquido, brilho)
        {
            int cw = Math.Max(3, pw / S), ch = Math.Max(3, ph / S); bool[,] ins = new bool[ch, cw];
            for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++) ins[y, x] = PuddleIn(type, ((x + 0.5f) / cw) * 2f - 1f, ((y + 0.5f) / ch) * 2f - 1f);
            Bitmap bm = new Bitmap(cw * S, ch * S, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bm))
            using (SolidBrush rim = new SolidBrush(rimC)) using (SolidBrush water = new SolidBrush(waterC)) using (SolidBrush shine = new SolidBrush(shineC))
            {
                g.SmoothingMode = SmoothingMode.None;
                for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++)
                {
                    if (!ins[y, x]) continue;
                    bool edge = x == 0 || y == 0 || x == cw - 1 || y == ch - 1 || !ins[y - 1, x] || !ins[y + 1, x] || !ins[y, x - 1] || !ins[y, x + 1];
                    g.FillRectangle(edge ? rim : water, x * S, y * S, S, S);
                }
                for (int k = 0; k < 2; k++) { int sx = cw / 3 + k * 3, sy = ch / 3 + k; if (sx < cw && sy < ch && ins[sy, sx]) g.FillRectangle(shine, sx * S, sy * S, 2 * S, S); }
            }
            return bm;
        }
        void RestoreWorld()
        {
            if (savedTrees != null) { trees.Clear(); trees.AddRange(savedTrees); savedTrees = null; }
            DisposePuddles(); drops.Clear(); sparks.Clear(); staffPending = false; p2MsgT = 0; if (darkOverlay != null) { darkOverlay.Dispose(); darkOverlay = null; } darkOn = false; interlude = false; allyOn = false; scene = 0; zoomE = 0; fadeA = 0; staffPh = 0; batKills = 0; msgT = 0;
        }
        Bitmap BuildDarkOverlay(int w, int h)      // filtro cinza levemente transparente + degrade de cinza para preto nas bordas
        {
            Bitmap bm = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.Clear(Color.FromArgb(80, 72, 78, 90));
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(-w * 0.25f, -h * 0.25f, w * 1.5f, h * 1.5f);
                    using (PathGradientBrush pg = new PathGradientBrush(gp))
                    {
                        pg.CenterColor = Color.FromArgb(0, 0, 0, 0);
                        pg.SurroundColors = new Color[] { Color.FromArgb(245, 6, 6, 8) };
                        pg.FocusScales = new PointF(0.5f, 0.5f);
                        g.FillRectangle(pg, 0, 0, w, h);
                    }
                }
            }
            return bm;
        }
        void PushOutOfStones()      // o heroi nao passa por cima das pedras: para ao encostar
        {
            foreach (Tree st in trees)
            {
                if (!st.Stone) continue;
                float sk = Math.Max(S + 1, S * 3 / 2) / (float)S, cx = st.X, cy = st.Y - 4 * S, r = (st.Variant == 1 ? 7 : (st.Variant == 2 ? 10 : 8)) * S * sk, dx = hx - cx, dy = hy - cy, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d >= r) continue;
                if (d < 0.01f) { dx = 1; dy = 0; d = 1; }
                hx = cx + dx / d * r; hy = cy + dy / d * r;
            }
        }
        void DrawPuddles(Graphics g)
        {
            if (!darkOn) return;
            foreach (Puddle p in puddles) g.DrawImage(p.Bmp, (int)(p.X - p.Bmp.Width / 2f), (int)(p.Y - p.Bmp.Height / 2f), p.Bmp.Width, p.Bmp.Height);
        }
        bool InPuddle(float x, float y)
        {
            foreach (Puddle p in puddles)
            {
                float nx = (x - p.X) / (p.Bmp.Width / 2f), ny = (y - p.Y) / (p.Bmp.Height / 2f);
                if (nx >= -1f && nx <= 1f && ny >= -1f && ny <= 1f && PuddleIn(p.Type, nx, ny)) return true;
            }
            return false;
        }
        void UpdateDrops(float dt)      // espirra gotas quando o heroi anda dentro de uma poca
        {
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                Drop d = drops[i]; d.T += dt; d.VY += 260 * S * dt; d.X += d.VX * dt; d.Y += d.VY * dt;
                if (d.T > 0.55f) drops.RemoveAt(i);
            }
            if (!interlude || dead || scene != 0) { wasInPuddle = false; return; }
            bool inP = InPuddle(hx, hy);
            if (inP && (!wasInPuddle || (walking && (dropT -= dt) <= 0)))
            {
                dropT = 0.11f; int n = wasInPuddle ? 3 : 7;
                for (int k = 0; k < n; k++) drops.Add(new Drop { X = hx + ((float)rnd.NextDouble() - 0.5f) * 10 * S, Y = hy - 2 * S, VX = ((float)rnd.NextDouble() - 0.5f) * 70 * S, VY = -(40 + 50 * (float)rnd.NextDouble()) * S, T = 0 });
            }
            wasInPuddle = inP;
        }
        void DrawDrops(Graphics g)
        {
            using (SolidBrush a = new SolidBrush(Color.FromArgb(190, 225, 250))) using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 255, 255)))
                foreach (Drop d in drops) { g.FillRectangle(a, d.X - S, d.Y - S, 2 * S, 2 * S); g.FillRectangle(b, d.X - S, d.Y - S, S, S); }
        }
        // CONJUNTO DOCE: o Link age como uma FADA: flutua (pairando e balancando no ar), tem asinhas que batem e deixa um rastro de brilhinhos
        bool FairyOn { get { return eqTunic == 10 && eqHat == 10 && !dead && powerT <= 0 && !previewMode; } }
        float FairyLift { get { return 4.5f * S + (float)Math.Sin(time * 3.4f) * 0.9f * S; } }
        float fairyFxT;
        void DrawHeroFx(Graphics g)
        {
            if (!FairyOn) { DrawHeroMaybeWading(g); return; }
            bool calm = calmState == 2 && phase >= 2;
            GraphicsState st = g.Save(); g.TranslateTransform(0, calm ? 0 : -FairyLift);
            if (calm) { if (phase < 5) DrawFairyWings(g, hx, hy - 12 * S, 1, true); }      // dormindo/descansando: de frente, asas dos dois lados, sem flutuar
            else DrawFairyWings(g, hx, hy - 12 * S, hface, false);
            DrawHeroMaybeWading(g);
            g.Restore(st);
        }
        List<float[]> spinSparks = new List<float[]>();      // brilhinhos amarelos do giro do Link-fada: x, y, tempo
        void DrawHeroFxSparks(Graphics g)
        {
            DrawFairySparks(g);
            for (int i = spinSparks.Count - 1; i >= 0; i--)
            {
                float[] p = spinSparks[i]; p[2] += 0.016f;
                if (p[2] > 0.55f) { spinSparks.RemoveAt(i); continue; }
                float q = 1f - p[2] / 0.55f;
                shadowList.Add(new float[] { p[0], p[1] - p[2] * 8 * S, 2.2f * S, 150f * q, 0f, 0f, 0f, 0f });      // quadradinho amarelo meio transparente (camada com transparencia real)
            }
        }
        List<float[]> linkFairySparks = new List<float[]>();      // brilhinhos do Link-fada: x, y, tempo
        void UpdateFairyFx(float dt)      // rastro de brilhinhos pequenos atras do Link-fada
        {
            for (int i = linkFairySparks.Count - 1; i >= 0; i--) { linkFairySparks[i][2] += dt; linkFairySparks[i][1] += 6 * S * dt; if (linkFairySparks[i][2] > 0.8f) linkFairySparks.RemoveAt(i); }
            if (!FairyOn || scene != 0) { return; }
            fairyFxT -= dt; if (fairyFxT > 0) return;
            fairyFxT = walking ? 0.07f : 0.16f;
            linkFairySparks.Add(new float[] { hx + ((float)rnd.NextDouble() - 0.5f) * 12 * S - hface * 3 * S, hy - FairyLift - (float)rnd.NextDouble() * 15 * S, 0f });
        }
        void DrawFairySparks(Graphics g)
        {
            using (SolidBrush a = new SolidBrush(Color.FromArgb(255, 205, 232))) using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 255, 255)))
                foreach (float[] p in linkFairySparks)
                {
                    float k = Math.Max(0f, 1f - p[2] / 0.8f); int n = k > 0.55f ? 2 : 1;      // estrelinha em cruz que encolhe
                    g.FillRectangle(a, p[0] - n * S / 2f, p[1] - S / 2f, n * S, S); g.FillRectangle(a, p[0] - S / 2f, p[1] - n * S / 2f, S, n * S); if (n == 2) g.FillRectangle(b, p[0] - S / 2f, p[1] - S / 2f, S, S);
                }
        }
        void DrawFairyWings(Graphics g, float x, float y, int face, bool front)      // asinhas de fada: brancas e azul bebe, batendo; de lado uma grande atras e uma pequena embaixo; de frente uma de cada lado
        {
            string[] w1 = { "..KK..", ".KWWK.", "KWBWWK", "KWWBWK", "KWWWWK", ".KWWK.", "..KK.." };
            string[] w2 = { ".KK.", "KWWK", "KWBK", ".KK." };
            bool flap = ((int)(time * 9f)) % 2 == 0; int rowsN = flap ? 7 : 6;
            Color k = Color.FromArgb(150, 190, 236), wc = Color.FromArgb(252, 252, 255), bc = Color.FromArgb(188, 224, 255);
            SmoothingMode sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                for (int side = 0; side < (front ? 2 : 1); side++)
                {
                    float dir = front ? (side == 0 ? -1f : 1f) : -face;      // de lado fica atras do herói
                    float ox = x + dir * (front ? 4 : 4) * S, oy = y;      // asas nas costas (altura do tronco), nao na cabeca
                    for (int r = 0; r < rowsN; r++) for (int c = 0; c < 6; c++)
                    {
                        char ch = w1[r][c]; if (ch == '.') continue; b.Color = ch == 'K' ? k : (ch == 'W' ? wc : bc);
                        float cx = dir > 0 ? c : 5 - c; g.FillRectangle(b, ox + (dir > 0 ? 0 : -6 * S) + cx * S, oy + r * S, S, S);
                    }
                    for (int r = 0; r < 4; r++) for (int c = 0; c < 4; c++)      // asa pequena embaixo
                    {
                        char ch = w2[r][c]; if (ch == '.') continue; b.Color = ch == 'K' ? k : (ch == 'W' ? wc : bc);
                        float cx = dir > 0 ? c : 3 - c; g.FillRectangle(b, ox + (dir > 0 ? S : -5 * S) + cx * S, oy + (rowsN + 0) * S + r * S - S, S, S);
                    }
                }
            }
            g.SmoothingMode = sm;
        }
        void DrawHeroMaybeWading(Graphics g)      // dentro da poca o pe fica um pouquinho submerso
        {
            if (!interlude || dead || !InPuddle(hx, hy)) { DrawHero(g); return; }
            GraphicsState st = g.Save();
            g.SetClip(new RectangleF(hx - 12 * S, hy - 2 * S, 24 * S, 2 * S), CombineMode.Exclude);      // so a faixa dos pes (2 quadradinhos) fica escondida
            DrawHero(g);
            g.Restore(st);
            using (SolidBrush w1 = new SolidBrush(Color.FromArgb(52, 92, 138))) using (SolidBrush w2 = new SolidBrush(Color.FromArgb(150, 190, 225)))
            {
                g.FillRectangle(w1, hx - 7 * S, hy - 2 * S, 14 * S, 2 * S);
                g.FillRectangle(w2, hx - 8 * S, hy - 2 * S, 3 * S, S); g.FillRectangle(w2, hx + 5 * S, hy - 2 * S, 3 * S, S);
            }
        }
        void DrawHeldStaff(Graphics g, float x, float feetY, float k, int face)      // cajado na mao do Feiticeiro (depois de pegar o cajado, sempre que ele aparece)
        {
            if (!wizHasStaff || staffSpr == null) return;
            int w = (int)(staffSpr.W * k), h = (int)(staffSpr.H * k);
            g.DrawImage(staffSpr.R, (int)(x + face * 9 * S * k - w / 2f), (int)(feetY - h + 2 * S * k), w, h);
        }

        // ---- cajado ----
        void StartStaffEvent()
        {
            foreach (Enemy en in enemies) { puffs.Add(new Puff { Kind = 0, X = en.X, Y = en.Y - 4 * S }); }
            enemies.Clear(); rocks.Clear(); arrows.Clear();
            staffPh = 1; staffT = 0; staffX = W / 2f; staffGround = H / 2f; Say("Um cajado!");
        }
        void UpdateStaff(float dt)
        {
            if (staffPh == 0) return;
            staffT += dt;
            if (staffPh == 1) { if (staffT >= 1.3f) { staffPh = 2; staffT = 0; } }
            else if (staffPh == 2)
            {
                float tx = staffX - 9 * S, ty = staffGround + 2 * S, dx = tx - ax, dy = ty - ay, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > 3 * S) { float st = Math.Min(d, 80 * S * dt); ax += dx / d * st; ay += dy / d * st; aface = dx > 0 ? 1 : -1; }
                else
                {
                    for (int k = 0; k < 14; k++) puffs.Add(new Puff { Kind = k % 2, X = staffX + ((float)rnd.NextDouble() - 0.5f) * 30 * S, Y = staffGround - (float)rnd.NextDouble() * 26 * S });
                    SparkBurst(ax, ay - 8 * S); allyOn = false; wizHasStaff = true; staffPh = 3; staffT = 0;      // some numa explosao de estrelinhas roxas
                }
            }
            else if (staffPh == 3)
            {
                if (staffT >= 1.4f) { staffPh = 0; scene = 7; sceneT = 0; }      // o Feiticeiro sumiu: a tela escurece e comeca a Fase 2
            }
        }
        void DrawStaff(Graphics g)
        {
            if (staffPh != 1 && staffPh != 2) return;
            float y = staffPh == 1 ? -36 * S + (staffGround + 36 * S) * Ease(staffT / 1.2f) : staffGround - (float)Math.Sin(time * 3f) * 1.5f * S;
            Shadow(g, staffX, staffGround, 8);
            Spr(g, staffSpr, staffX, y, true);
            using (SolidBrush hi = new SolidBrush(Color.FromArgb(235, 215, 255))) using (SolidBrush ye = new SolidBrush(Color.FromArgb(255, 240, 90)))
            {
                float oy = y - staffSpr.H + 4 * S;      // altura da esfera
                bool t = ((int)(time * 6)) % 2 == 0;
                g.FillRectangle(t ? hi : ye, staffX + 8 * S, oy - 4 * S, S, S); g.FillRectangle(t ? ye : hi, staffX - 9 * S, oy + 2 * S, S, S);
                g.FillRectangle(t ? ye : hi, staffX - 8 * S, oy - 5 * S, S, S); g.FillRectangle(t ? hi : ye, staffX + 9 * S, oy + 3 * S, S, S);
            }
        }

        // ---- caixa de conversa (por cima de tudo, sem ampliar) ----
        void DrawDialog(Graphics g)
        {
            if (scene != 3 || dlgIdx >= CurDlg.Length || CurDlg[dlgIdx][0] == "*") return;      // "*" = acao do Feiticeiro (sem fala)
            if (dlgFont == null) dlgFont = new Font("Consolas", S * 6, FontStyle.Bold, GraphicsUnit.Pixel);
            string who = CurDlg[dlgIdx][0], raw = CurDlg[dlgIdx][1], txt = raw.Replace("|", "");
            float maxW = Math.Min(W * 0.62f, 420f * S), pad = 6 * S;
            List<string> lines = new List<string>(); string cur = "";
            foreach (string w in txt.Split(' '))
            {
                string t = cur.Length == 0 ? w : cur + " " + w;
                if (cur.Length > 0 && g.MeasureString(t, dlgFont).Width > maxW) { lines.Add(cur); cur = w; } else cur = t;
            }
            if (cur.Length > 0) lines.Add(cur);
            float lh = g.MeasureString("Ag", dlgFont).Height, bw = 0;
            foreach (string l in lines) bw = Math.Max(bw, g.MeasureString(l, dlgFont).Width);
            bw = Math.Max(bw, g.MeasureString(who, dlgFont).Width + 2 * S);
            float bh = lh * (lines.Count + 1) + 2 * pad, x = W / 2f - bw / 2f, y = H - bh - 36 * S;
            using (SolidBrush k = new SolidBrush(Color.FromArgb(28, 22, 36))) using (SolidBrush bg = new SolidBrush(Color.FromArgb(46, 40, 62))) using (SolidBrush gold = new SolidBrush(Color.FromArgb(247, 190, 70)))
            {
                g.FillRectangle(k, x - pad - S, y - S, bw + 2 * pad + 2 * S, bh + 2 * S);
                g.FillRectangle(bg, x - pad, y, bw + 2 * pad, bh);
                g.FillRectangle(gold, x - pad, y, bw + 2 * pad, S);
            }
            DrawTextF(g, who, dlgFont, x, y + pad, who == "Link" ? Color.FromArgb(120, 225, 110) : Color.FromArgb(255, 220, 90));
            int shown = 0; { float acc = 0f; foreach (char ch in raw) { if (ch == '|') { acc += 0.7f; continue; } acc += 0.05f; if (acc > sceneT) break; shown++; } }      // as letras vao aparecendo uma por uma (20 por segundo; '|' = pausa de 0,7 s); a caixa ja nasce do tamanho final
            for (int i = 0; i < lines.Count && shown > 0; i++)
            {
                string part = lines[i].Length <= shown ? lines[i] : lines[i].Substring(0, shown);
                DrawTextF(g, part, dlgFont, x, y + pad + lh * (i + 1), Color.White);
                shown -= lines[i].Length + 1;
            }
        }
        static Color DarkTint(Color c) { return Color.FromArgb(c.A, Math.Min(255, (int)(c.R * 0.55f + 30)), Math.Min(255, (int)(c.G * 0.38f + 12)), Math.Min(255, (int)(c.B * 0.62f + 58))); }
        void StartPhase2()
        {
            if (interlude || savedTrees != null) RestoreWorld();      // vindo da fase intermediaria (ou do atalho): volta ao mundo normal
            phase2 = true; ResetBoss(); BuildSprites();
            enemies.Clear(); rocks.Clear(); arrows.Clear(); items.Clear(); puffs.Clear();
            rupees = 0; lastStep = 0; SetMaxHp(Math.Max(MaxHP, 8)); hp = MaxHP; dead = false; deathWait = false; inv = 2f;
            apples = 0; hx = W / 2; hy = H / 2; spawnTimer = 2f; killsAtMax = 0;
            Say("Fase 2!");
        }
        void StartPhase3()
        {
            if (interlude || savedTrees != null) RestoreWorld();
            phase2 = true; phase3 = true; ResetBoss(); BuildSprites();
            enemies.Clear(); rocks.Clear(); arrows.Clear(); items.Clear(); puffs.Clear(); acids.Clear(); pools.Clear(); sparks.Clear(); starPh = 0; starTarget = null;
            rupees = 0; lastStep = 0; SetMaxHp(Math.Max(MaxHP, 8)); hp = MaxHP; dead = false; deathWait = false; inv = 2f;
            apples = 0; hx = W / 2; hy = H / 2; spawnTimer = 2f; killsAtMax = 0; p3EndT = 0; msgT = 0;
            Say("Fase 3!");
        }
        void StartPhase3Test() { if (!playMode) SetPlayMode(true); StartPhase3(); }      // atalho: direto na FASE 3 (nivel 1)
        void StartPhase2Test()      // atalho: liga o modo jogo direto na FASE 2 (nivel 1, vida cheia)
        {
            if (!playMode) SetPlayMode(true);
            StartPhase2();
        }
        // ================= CHEFAO DA FASE 2: OCTOROK GIGANTE =================
        // Uma estrela roxa voa ate um octorok da tela, que fica enorme. Ele cospe bolas de acido na direcao do Link; se o Link desvia, a bola cai no chao onde ele estava
        // e faz uma poca de acido que dura 5 s (machuca quem fica em cima). Vida: 150% da do dragao (5000 -> 7500).
        const int OctoHP = 7500, FireBossHP = 11250;      // Fase 3: Gelatina de Fogo Gigante com 150% da vida do Octorok Gigante
        int BossTargetType { get { return phase3 ? 2 : 0; } }      // a estrela atinge um octorok (fase 2) ou uma gelatina de fogo (fase 3)
        Sprite[] slimeBoss = new Sprite[2];
        List<Acid> acids = new List<Acid>(); List<float[]> pools = new List<float[]>();      // poca: x, y, tempo
        Sprite[] octoBoss = new Sprite[2]; Bitmap acidPoolBmp;
        int starPh; float starX, starY, starWait; Enemy starTarget;      // starWait: tempo esperando o octorok chegar perto do Link
        List<Spark> sparks = new List<Spark>();
        void SparkBurst(float x, float y)      // explosao de estrelinhas roxas que somem no ar
        {
            for (int i = 0; i < 32; i++)
            {
                double an = rnd.NextDouble() * Math.PI * 2; float sp = (35 + 85 * (float)rnd.NextDouble()) * S;
                sparks.Add(new Spark { X = x + ((float)rnd.NextDouble() - 0.5f) * 6 * S, Y = y + ((float)rnd.NextDouble() - 0.5f) * 10 * S, VX = (float)Math.Cos(an) * sp, VY = (float)Math.Sin(an) * sp - 14 * S, T = 0, Life = 0.8f + 0.8f * (float)rnd.NextDouble(), Col = rnd.Next(3) });
            }
        }
        void UpdateSparks(float dt)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark p = sparks[i]; p.T += dt; float dm = Math.Min(1f, 2.2f * dt); p.VX -= p.VX * dm; p.VY -= p.VY * dm; p.VY -= 10 * S * dt; p.X += p.VX * dt; p.Y += p.VY * dt;
                if (p.T >= p.Life) sparks.RemoveAt(i);
            }
        }
        void DrawSparks(Graphics g)      // estrelinhas em cruz que encolhem ate sumir
        {
            Color[] cs = { Color.FromArgb(170, 100, 235), Color.FromArgb(225, 190, 255), Color.FromArgb(125, 62, 195) };
            foreach (Spark p in sparks)
            {
                float L = Math.Max(S, (1f - p.T / p.Life) * 3.5f * S);
                using (SolidBrush b = new SolidBrush(cs[p.Col])) using (SolidBrush c = new SolidBrush(Color.FromArgb(250, 240, 255)))
                { g.FillRectangle(b, p.X - L, p.Y - S / 2f, 2 * L, S); g.FillRectangle(b, p.X - S / 2f, p.Y - L, S, 2 * L); g.FillRectangle(c, p.X - S / 2f, p.Y - S / 2f, S, S); }
            }
        }

        void SpawnOctoBoss()      // chamado quando chega a hora do chefao: a estrela vai buscar um octorok
        {
            bossSpawned = true;
            Enemy t = null; float bd = float.MaxValue;
            foreach (Enemy en in enemies) if (en.Type == BossTargetType) { float d = (en.X - hx) * (en.X - hx) + (en.Y - hy) * (en.Y - hy); if (d < bd) { bd = d; t = en; } }      // o octorok mais perto do Link
            if (t == null) t = NewStarTarget(Math.Max(20 * S, Math.Min(W - 20 * S, hx >= W / 2f ? hx - 150 * S : hx + 150 * S)), hy);
            t.NoAtk = true; MonsterSmoke(t);      // a fumaca sai DO MONSTRINHO e ele nao ataca antes de ser atingido pela estrela
            starWait = 0; starTarget = t; starPh = 1; starX = W * (0.15f + 0.7f * (float)rnd.NextDouble()); starY = -10 * S;
        }
        void MonsterSmoke(Enemy t)
        {
            for (int k = 0; k < 18; k++) smokes.Add(new Smoke { X = t.X + ((float)rnd.NextDouble() - 0.5f) * 14 * S, Y = t.Y - (float)rnd.NextDouble() * 6 * S, T = -(float)rnd.NextDouble() * 0.7f, Sz = 3 + (float)rnd.NextDouble() * 3 });
        }
        Enemy NewStarTarget(float x, float y)      // nao ha octorok na tela: nasce um (com fumaca) para a estrela atingir
        {
            Enemy t = Spawn(BossTargetType); t.X = x; t.Y = y;
            for (int k = 0; k < 6; k++) puffs.Add(new Puff { Kind = 2, X = x + ((float)rnd.NextDouble() - 0.5f) * 14 * S, Y = y - (float)rnd.NextDouble() * 10 * S });
            return t;
        }
        void UpdateStar(float dt)
        {
            if (starPh == 0) return;
            if (starTarget == null || starTarget.Type != BossTargetType || !enemies.Contains(starTarget))      // o octorok foi derrotado: acha outro (ou cria)
            {
                starTarget = null; foreach (Enemy en in enemies) if (en.Type == BossTargetType) { starTarget = en; break; }
                if (starTarget == null) starTarget = NewStarTarget(W / 2f, H * 0.4f);
                starTarget.NoAtk = true; MonsterSmoke(starTarget);
            }
            starWait += dt;
            float lx = starTarget.X - hx, ly = starTarget.Y - hy;
            bool near = lx * lx + ly * ly < (38 * S) * (38 * S) || starWait > 10f;      // so acerta quando o octorok chega perto do Link (ou depois de 10 s)
            if (starPh == 1)      // ainda nao apareceu: a estrela so surge quando o octorok chega perto do Link
            {
                if (!near) return;
                starPh = 2; starX = Math.Max(10 * S, Math.Min(W - 10 * S, starTarget.X + ((float)rnd.NextDouble() - 0.5f) * 80 * S)); starY = -10 * S;
            }
            float tx = starTarget.X, ty = starTarget.Y - 5 * S, dx = tx - starX, dy = ty - starY, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 7 * S) { TransformOcto(starTarget); starPh = 0; return; }
            float st = Math.Min(d, 150 * S * dt); starX += dx / d * st; starY += dy / d * st;
            if (rnd.NextDouble() < 0.7) puffs.Add(new Puff { Kind = 1, X = starX + ((float)rnd.NextDouble() - 0.5f) * 4 * S, Y = starY + ((float)rnd.NextDouble() - 0.5f) * 4 * S });      // rastro
        }
        void TransformOcto(Enemy t)      // a estrela acerta: o octorok vira o Octorok Gigante
        {
            t.NoAtk = false; t.Type = 7; t.HP = t.MaxHP = phase3 ? FireBossHP : OctoHP; t.SlamCd = 4f; t.SlamPh = 0; t.Lift = 0; t.T = 0; t.Scale = 0.25f; t.Timer = 2.6f; t.Mode = 0; t.KX = t.KY = 0; t.Fairy = false; t.Wave = false; t.Face = hx >= t.X ? 1 : -1;
            boss = t;
            for (int k = 0; k < 14; k++) puffs.Add(new Puff { Kind = k % 3 == 0 ? 2 : 1, X = t.X + ((float)rnd.NextDouble() - 0.5f) * 20 * S, Y = t.Y - (float)rnd.NextDouble() * 14 * S });
        }
        void UpdateOctoBoss(Enemy e, float dt)
        {
            if (e.T < 1.5f) { e.Scale = 0.25f + 0.75f * Ease(e.T / 1.5f); return; }      // crescendo
            e.Scale = 1f;
            bool rage = e.HP * 2 <= e.MaxHP;
            if (phase3)      // Gelatina de Fogo Gigante: alem de cuspir fogo como o octorok, SALTA em cima do Link
            {
                if (e.SlamPh != 0) { UpdateSlam(e, dt, rage); return; }
                if (!dead) { e.SlamCd -= dt; if (e.SlamCd <= 0) { StartSlam(e); return; } }
            }
            e.SparkT -= dt;
            if (e.SparkT <= 0 || ((e.TX - e.X) * (e.TX - e.X) + (e.TY - e.Y) * (e.TY - e.Y)) < (3 * S) * (3 * S))      // anda devagar para perto do heroi (sem encostar)
            {
                double a = rnd.NextDouble() * Math.PI * 2; float dist = (95 + (float)rnd.NextDouble() * 40) * S;
                e.TX = Math.Max(28 * S, Math.Min(W - 28 * S, hx + (float)Math.Cos(a) * dist)); e.TY = Math.Max(48 * S, Math.Min(H - 4 * S, hy + (float)Math.Sin(a) * dist * 0.7f)); e.SparkT = 3f + (float)rnd.NextDouble() * 1.5f;
            }
            float mx = e.TX - e.X, my = e.TY - e.Y, md = (float)Math.Sqrt(mx * mx + my * my);
            if (md > 1) { float v = (rage ? 17f : 12f) * S * dt; e.X += mx / md * Math.Min(md, v); e.Y += my / md * Math.Min(md, v); e.Moving = true; } else e.Moving = false;
            e.X = Math.Max(26 * S, Math.Min(W - 26 * S, e.X)); e.Y = Math.Max(44 * S, Math.Min(H - 2 * S, e.Y));      // o corpo grande nao sai da tela
            e.Face = hx >= e.X ? 1 : -1;
            e.Timer -= dt;
            if (e.Timer > 0) return;
            SpitAcid(e, rage);
            e.Timer = (rage ? 1.4f : 2.3f) + (float)rnd.NextDouble() * 0.6f;
        }
        // ---- ataque novo da Gelatina de Fogo Gigante: SALTO ESMAGADOR ----
        // Marca no chao onde o Link esta, salta em arco (1,3 s) e cai ali: quem estiver na marca perde 2 coracoes e a queda solta bolinhas de fogo em roda (explodem em 3 s, como a pedra do octorok).
        void StartSlam(Enemy e)
        {
            e.SlamPh = 1; e.SlamT = 0; e.SlamX0 = e.X; e.SlamY0 = e.Y; e.Moving = false;
            e.SlamX = Math.Max(30 * S, Math.Min(W - 30 * S, hx)); e.SlamY = Math.Max(48 * S, Math.Min(H - 4 * S, hy + 4 * S));
        }
        void UpdateSlam(Enemy e, float dt, bool rage)
        {
            e.SlamT += dt; float u = Math.Min(1f, e.SlamT / 1.3f), k = Ease(u);
            e.X = e.SlamX0 + (e.SlamX - e.SlamX0) * k; e.Y = e.SlamY0 + (e.SlamY - e.SlamY0) * k;
            e.Lift = (float)Math.Sin(u * Math.PI) * 78 * S; e.Face = hx >= e.X ? 1 : -1;
            if (u < 1f) return;
            e.SlamPh = 0; e.Lift = 0; e.X = e.SlamX; e.Y = e.SlamY; e.SlamCd = (rage ? 4f : 6.5f) + (float)rnd.NextDouble() * 1.5f; e.Timer = Math.Max(e.Timer, 0.8f); e.SparkT = 1.5f;
            for (int q = 0; q < 16; q++) puffs.Add(new Puff { Kind = q % 3 == 0 ? 2 : (q % 2), X = e.X + ((float)rnd.NextDouble() - 0.5f) * 50 * S, Y = e.Y - (float)rnd.NextDouble() * 12 * S });
            float ex = (hx - e.X) / (26 * S), ey = (hy - e.Y) / (13 * S);
            if (!dead && ex * ex + ey * ey < 1f) HurtHero(e.X, e.Y, 2f);      // a queda tira dois coracoes
            int n = rage ? 10 : 8; double a0 = rnd.NextDouble() * Math.PI;
            for (int q = 0; q < n; q++) { double an = a0 + q * 2 * Math.PI / n; Rock em = new Rock(); em.Ember = true; em.X = e.X; em.Y = e.Y - 6 * S; em.VX = (float)Math.Cos(an) * 60 * S; em.VY = (float)Math.Sin(an) * 50 * S; rocks.Add(em); }
        }
        void DrawSlamTarget(Graphics g, Enemy e)      // marca no chao onde o salto vai cair (pisca mais rapido perto da queda)
        {
            float u = Math.Min(1f, e.SlamT / 1.3f); if (((int)(e.SlamT * (6 + 10 * u))) % 2 == 0) return;
            using (Pen p = new Pen(Color.FromArgb(255, 96, 28), Math.Max(1f, S))) using (Pen p2 = new Pen(Color.FromArgb(255, 220, 120), Math.Max(1f, S * 0.5f)))
            { g.DrawEllipse(p, e.SlamX - 26 * S, e.SlamY - 13 * S, 52 * S, 26 * S); g.DrawEllipse(p2, e.SlamX - 13 * S, e.SlamY - 6.5f * S, 26 * S, 13 * S); }
        }
        void SpitAcid(Enemy e, bool rage)      // 1 bola na direcao do heroi (3 em leque com furia)
        {
            float mx = e.X + e.Face * 16 * S, my = e.Y - 16 * S;
            int n = rage ? 3 : 1;
            for (int i = 0; i < n; i++)
            {
                float off = (i - (n - 1) / 2f) * 30 * S;
                Acid a = new Acid(); a.SX = mx; a.SY = my; a.T = 0; a.Dur = 0.95f;
                a.TX = Math.Max(6 * S, Math.Min(W - 6 * S, hx + off)); a.TY = Math.Max(20 * S, Math.Min(H - 2 * S, hy + (i == 1 ? 6 * S : 0)));
                acids.Add(a);
            }
        }
        void UpdateAcid(float dt)
        {
            for (int i = acids.Count - 1; i >= 0; i--)
            {
                Acid a = acids[i]; a.T += dt;
                if (a.T < a.Dur) continue;
                acids.RemoveAt(i);      // a bola cai: faz a poca de acido (5 s)
                if (pools.Count < 14) pools.Add(new float[] { a.TX, a.TY, 0f });
                for (int k = 0; k < 6; k++) puffs.Add(new Puff { Kind = 1, X = a.TX + ((float)rnd.NextDouble() - 0.5f) * 10 * S, Y = a.TY - (float)rnd.NextDouble() * 6 * S });
                float ex = hx - a.TX, ey = hy - a.TY;
                if (!dead && ex * ex + ey * ey < (9 * S) * (9 * S)) HurtHero(a.TX, a.TY, 1f);      // acertou o heroi em cheio
            }
            for (int i = pools.Count - 1; i >= 0; i--)
            {
                float[] p = pools[i]; p[2] += dt;
                if (p[2] >= 5f) { pools.RemoveAt(i); continue; }
                float ex = (hx - p[0]) / (14.3f * S), ey = (hy - p[1]) / (7.15f * S);      // poca 30% maior (11 x 5,5 -> 14,3 x 7,15)
                if (!dead && ex * ex + ey * ey < 1f) HurtHero(p[0], p[1], 2f);      // ficar em cima da poca de acido tira DOIS coracoes (de novo a cada 1,2 s de invulnerabilidade)
            }
        }
        void DrawAcidPools(Graphics g)
        {
            if (acidPoolBmp == null) return;
            foreach (float[] p in pools)
            {
                if (p[2] > 4f && ((int)(p[2] * 10)) % 2 == 0) continue;      // pisca no ultimo segundo
                g.DrawImage(acidPoolBmp, (int)(p[0] - acidPoolBmp.Width / 2f), (int)(p[1] - acidPoolBmp.Height / 2f), acidPoolBmp.Width, acidPoolBmp.Height);
            }
        }
        void DrawAcidBalls(Graphics g)
        {
            using (SolidBrush rim = new SolidBrush(phase3 ? Color.FromArgb(150, 50, 10) : Color.FromArgb(60, 110, 15))) using (SolidBrush body = new SolidBrush(phase3 ? Color.FromArgb(255, 130, 30) : Color.FromArgb(140, 225, 40))) using (SolidBrush hi = new SolidBrush(phase3 ? Color.FromArgb(255, 235, 150) : Color.FromArgb(230, 255, 150)))      // fase 3: bolas e pocas de fogo
                foreach (Acid a in acids)
                {
                    float u = Math.Min(1f, a.T / a.Dur);
                    if (((int)(a.T * 8)) % 2 == 0) for (int k = 0; k < 8; k++)      // aviso: marca onde vai cair
                    { double an = k * Math.PI / 4; g.FillRectangle(rim, a.TX + (float)Math.Cos(an) * 9 * S - S, a.TY + (float)Math.Sin(an) * 4.5f * S - S / 2f, 2 * S, S); }
                    float x = a.SX + (a.TX - a.SX) * u, y = a.SY + (a.TY - a.SY) * u - (float)Math.Sin(u * Math.PI) * 34 * S;
                    g.FillRectangle(rim, x - 4 * S, y - 4 * S, 8 * S, 8 * S); g.FillRectangle(body, x - 3 * S, y - 3 * S, 6 * S, 6 * S); g.FillRectangle(hi, x - 2 * S, y - 2 * S, 2 * S, 2 * S);
                }
        }
        void DrawStar(Graphics g)      // estrela roxa que voa ate o octorok
        {
            if (starPh != 2) return;
            PointF[] pt = new PointF[10]; double rot = time * 6;
            for (int i = 0; i < 10; i++) { double an = rot + i * Math.PI / 5 - Math.PI / 2; float r = (i % 2 == 0 ? 8 : 3.4f) * S; pt[i] = new PointF(starX + (float)Math.Cos(an) * r, starY + (float)Math.Sin(an) * r); }
            using (SolidBrush fb = new SolidBrush(Color.FromArgb(150, 80, 205))) using (Pen ol = new Pen(Color.FromArgb(28, 22, 36), Math.Max(1f, S))) using (SolidBrush core = new SolidBrush(Color.FromArgb(240, 220, 255)))
            { g.FillPolygon(fb, pt); g.DrawPolygon(ol, pt); g.FillRectangle(core, starX - 2 * S, starY - 2 * S, 4 * S, 4 * S); }
        }
        void DrawOctoBoss(Graphics g, Enemy e)
        {
            Shadow(g, e.X, e.Y, 40 * e.Scale);
            if (phase3 && e.SlamPh != 0) DrawSlamTarget(g, e);
            Sprite sp = (phase3 ? slimeBoss : octoBoss)[e.Moving && e.SlamPh == 0 ? ((int)(e.T * 4)) % 2 : 0];
            int w = (int)(sp.W * e.Scale), h = (int)(sp.H * e.Scale);
            g.DrawImage(e.Face > 0 ? sp.R : sp.L, (int)(e.X - w / 2f), (int)(e.Y - h - e.Lift), w, h);
            if (e.T >= 1.5f && e.Timer < 0.45f)      // carregando o acido na boca antes de cuspir
                using (SolidBrush ob = new SolidBrush(phase3 ? Color.FromArgb(255, 130, 30) : Color.FromArgb(140, 225, 40))) using (SolidBrush ib = new SolidBrush(phase3 ? Color.FromArgb(255, 235, 150) : Color.FromArgb(230, 255, 150)))
                { float ox = e.X + e.Face * 16 * S, oy = e.Y - 16 * S; g.FillRectangle(ob, ox - 3 * S, oy - 3 * S, 6 * S, 6 * S); g.FillRectangle(ib, ox - S, oy - S, 2 * S, 2 * S); }
        }

        void BossDefeated(Enemy e)        {
            shocks.Clear(); bossPhase = 5; bossT = 0; endStarted = false; rewardSpawned = false; msgT = 0; rewardWaitT = 0;
            bossDeadX = e.X; bossDeadY = e.Y; bossDeadFace = e.Face; bossDeadMage = e.Type == 7;
            rocks.Clear(); acids.Clear();      // as pocas de acido que ja estao no chao somem sozinhas
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
            if (bossDeadMage) { Sprite bd = (phase3 ? slimeBoss : octoBoss)[0]; Shadow(g, bossDeadX, bossDeadY, 40); g.DrawImage(bd.R, (int)(bossDeadX - bd.W / 2f), (int)(bossDeadY - bd.H), bd.W, bd.H); return; }      // Octorok Gigante piscando antes de explodir
            Sprite sp = dragonSpr[0];
            Shadow(g, bossDeadX, bossDeadY, 34 * DK);
            g.DrawImage(bossDeadFace > 0 ? sp.R : sp.L, (int)(bossDeadX - sp.W / 2f), (int)(bossDeadY - sp.H), sp.W, sp.H);
        }

        Font msgFont;
        void DrawBossMsg(Graphics g)
        {
            if (p2MsgT > 0) { DrawBanner(g, "VOCÊ PASSOU PARA A FASE 2!", H * 0.28f); return; }      // mais alto, para nao cobrir o Link no centro      // aviso depois da cena do Feiticeiro
            if (p3EndT > 0) { DrawBanner(g, "ESPERE POR MAIS ATUALIZAÇÕES"); return; }
            if (msgT <= 0) return;
            DrawBanner(g, phase3 ? "ESPERE POR MAIS ATUALIZAÇÕES" : (phase2 ? "VOCÊ PASSOU PARA A FASE 3!" : "VOCÊ PASSOU PARA A FASE 2!"));      // depois do Octorok Gigante: Fase 3
        }
        void DrawBanner(Graphics g, string a, float cy = -1f)
        {
            if (msgFont == null) msgFont = new Font("Consolas", S * 8, FontStyle.Bold, GraphicsUnit.Pixel);
            SizeF sz = g.MeasureString(a, msgFont);
            float pad = 6 * S, x = W / 2f - sz.Width / 2, y = (cy < 0 ? H / 2f : cy) - sz.Height / 2;
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

        // ONDA FINAL (ultimo nivel antes do dragao): 2 gelatinas e 5 bokoblins a cavalo com arco, nunca todos juntos (no maximo 3 vivos ao mesmo tempo).
        // O dragao so aparece depois que os 7 foram derrotados (e das 5 mortes do nivel maximo).
        static readonly int[] WaveSeq = { 5, 2, 5, 5, 2, 5, 5 };
        int waveSpawned; float waveT = 2f;
        int WaveAlive() { int n = 0; foreach (Enemy e in enemies) if (e.Wave) n++; return n; }
        bool WaveDone() { return waveSpawned >= WaveSeq.Length && WaveAlive() == 0; }
        void UpdateWave(float dt)
        {
            if (RupeeStep() < 6 || dead || errandPh != 0 || waveSpawned >= WaveSeq.Length) return;
            waveT -= dt;
            if (waveT <= 0 && WaveAlive() < 3) { Enemy e = Spawn(WaveSeq[waveSpawned]); e.Wave = true; e.Fairy = false; waveSpawned++; waveT = 4f + (float)rnd.NextDouble() * 3f; }
        }

        void UpdateBoss(float dt)
        {
            if (!playMode) return;
            if (bossPhase == 0) { if (!interlude) UpdateWave(dt); if (killsAtMax >= 5 && WaveDone() && !interlude) StartFinalPhase(); return; }      // fase 2 e 3 terminam com o chefao (octorok / gelatina de fogo); na intermediaria o fim e o cajado      // na fase intermediaria o fim e o cajado (30 morcegos), nao o chefe
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
                if (!bossSpawned && bossT >= 0.9f) { if (phase2) SpawnOctoBoss(); else SpawnDragon(); }      // fase 2: a estrela roxa transforma um octorok no chefao
                if (bossT >= 3.2f && (!phase2 || boss != null || bossT >= 9f)) bossPhase = 4;
            }
            else if (bossPhase == 5)
            {
                // 0-1,3 s: o dragao volta ao normal (sem vermelho, tamanho normal) e pisca; depois EXPLODE e solta 1000 rupias
                if (bossT >= 1.3f && !rewardSpawned) { rewardSpawned = true; SpawnDragonReward(); }
                if (rewardSpawned && bossT < 3.3f && ((int)(bossT * 10)) != ((int)((bossT - dt) * 10)))
                    puffs.Add(new Puff { Kind = rnd.Next(2), X = bossDeadX + ((float)rnd.NextDouble() - 0.5f) * 44 * S, Y = bossDeadY - (float)rnd.NextDouble() * 34 * S });
                // depois de vencer o dragao: "Espere por mais atualizacoes" e entao os creditos
                if (bossT >= 3.6f && msgT <= 0 && !endStarted) { if (phase2) msgT = 0.001f; else endStarted = true; }      // o aviso da Fase 2 agora vem depois da cena do Feiticeiro
                if (msgT > 0 && !endStarted)
                {
                    msgT += dt;
                    if (msgT >= 4.5f)
                    {
                        endStarted = true; msgT = 0;
                        endShow = null;      // sem creditos: depois da mensagem o jogo segue direto (Fase 2, ou volta ao automatico depois do Feiticeiro)
                    }
                }
                if (endStarted && (endShow == null || endShow.IsDisposed || endShow.Done))
                {
                    if (!phase2)
                    {
                        // a Fase 2 so comeca depois de pegar as rupias do dragao (e mais um instante); no maximo espera 60 s
                        rewardWaitT += dt;
                        bool sobrou = items.Exists(delegate (Item it) { return it.Reward; });
                        if ((!sobrou && rewardWaitT >= 2f) || rewardWaitT >= 60f) StartScene();      // cena do Feiticeiro, e so depois a Fase 2
                    }
                    else if (!phase3)      // depois do Octorok Gigante: a Fase 3 so comeca depois de pegar as rupias (e mais um instante); no maximo espera 60 s
                    {
                        rewardWaitT += dt;
                        bool sobrou3 = items.Exists(delegate (Item it) { return it.Reward; });
                        if ((!sobrou3 && rewardWaitT >= 2f) || rewardWaitT >= 60f) StartScene(2);      // antes da Fase 3: o Feiticeiro conversa de novo com o Link
                    }
                    else SetPlayMode(false);
                }
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
            string name = boss != null && boss.Type == 7 ? (phase3 ? "Gelatina de Fogo Gigante" : "Octorok Gigante") : "Dragão";
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
            if (interlude) return r < 0.88 ? 1 : (r < 0.94 ? 0 : 2);      // fase intermediaria (lugar escuro): quase so morcegos
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

        Enemy Spawn(int type)
        {
            Enemy e = new Enemy();
            e.Type = type;
            int edge = rnd.Next(4);
            float m = 6 * S;
            if (edge == 0) { e.X = m; e.Y = m * 3 + (float)rnd.NextDouble() * (H - m * 6); }
            else if (edge == 1) { e.X = W - m; e.Y = m * 3 + (float)rnd.NextDouble() * (H - m * 6); }
            else if (edge == 2) { e.X = m * 3 + (float)rnd.NextDouble() * (W - m * 6); e.Y = 16 * S; }
            else { e.X = m * 3 + (float)rnd.NextDouble() * (W - m * 6); e.Y = H - m; }
            e.HP = type == 1 ? 50 : (type == 2 ? 50 : (type == 0 ? 200 : (type == 5 ? 250 : 100)));
            // (fase 2: mesma vida; so a quantidade e a frequencia sobem, como na fase 1)      // morcego 50, gelatina 50, octorok (vermelho) 200, bokoblin a cavalo 250, bokoblin comum 100
            e.Timer = 0.8f + (float)rnd.NextDouble();
            puffs.Add(new Puff { Kind = 2, X = e.X, Y = e.Y });
            if (fairyState == 1 && !offFairy && type != 3 && rnd.NextDouble() < 0.20 * FairyRate) e.Fairy = true;      // a fadinha derrota 20% dos monstros que aparecem (40% com a fadinha de estrela)
            enemies.Add(e);
            return e;
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
            if (!BowUse) return;
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
                FireArrows(sx, sy);
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
            if (FairyOn && FairyAutoControl(dt)) return;      // Link-fada: mata como a fadinha

            // octoroks e slimes: luta de espada (aproxima e golpeia), mas so se nao houver item para pegar primeiro
            if (near != null && (near.Type != 1 || !BowUse) && SwordUse && nd < 110 * S)
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
                bool melee = SwordUse && atkCool <= 0 && nd < 15 * S && ((crowd <= 1 && !lowHp) || (cornered && nd < 13 * S));
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

        bool fairyStrike; Enemy fhTarget; int fhPh; float fhOrbit, fhRest;
        // LINK-FADA no automatico: voa ate o monstro mais perto (menos chefoes), da voltas rapidas em torno da cabeca dele e o derrota, igual a fadinha
        bool FairyAutoControl(float dt)
        {
            fairyStrike = false;
            if (fhTarget != null && !enemies.Contains(fhTarget)) { fhTarget = null; fhPh = 0; }
            if (fhTarget == null)
            {
                fhRest -= dt;
                if (fhRest <= 0)
                {
                    Enemy best = null; float bd = float.MaxValue;
                    foreach (Enemy e in enemies) { if (e.Type == 3 || e.Type == 7) continue; float d = (e.X - hx) * (e.X - hx) + (e.Y - hy) * (e.Y - hy); if (d < bd) { bd = d; best = e; } }
                    if (best != null) { fhTarget = best; fhPh = 1; fhOrbit = 0; }
                }
                if (fhTarget == null)
                {
                    if (fhRest > 0) { Item it = NearestItem(); if (it != null) { MoveHero(it.X, it.Y, 56 * S, dt); bowT = 0; return true; } }
                    return false;
                }
            }
            Enemy tg = fhTarget; fairyStrike = true; bowT = 0;
            if (fhPh == 1)
            {
                float dx = tg.X - hx, dy = tg.Y - hy, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < 8 * S) { fhPh = 2; fhOrbit = 0; } else MoveHero(tg.X, tg.Y, 56 * S, dt);      // velocidade padrao
            }
            else
            {
                fhOrbit += dt; float a = fhOrbit * 18f;
                hx = tg.X + (float)Math.Cos(a) * 8 * S; hy = tg.Y + (float)Math.Sin(a) * 4 * S; hface = tg.X >= hx ? 1 : -1; walking = true;
                if (fhOrbit >= 0.7f)
                {
                    for (int k = 0; k < 6; k++) puffs.Add(new Puff { Kind = 1, X = tg.X + ((float)rnd.NextDouble() - 0.5f) * 12 * S, Y = tg.Y - (float)rnd.NextDouble() * 10 * S });
                    fhTarget = null; fhPh = 0; fhRest = 1f; fairyStrike = false; DamageEnemy(tg, 9999);
                }
            }
            return true;
        }

        bool SwordUse { get { return FairyOn || !offSword; } }      // espada/arco guardados (desequipados) nao atacam: sem uma, usa a outra
        bool BowUse { get { return FairyOn || !offBow; } }
        void StartAttack(bool practice)
        {
            if (!practice && !SwordUse) return;
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
                    if (phaseT > 0.1f && phaseT < 0.2f && speechT <= 0) Say("Oi!");      // diz oi uma vez so, enquanto acena
                    if (phaseT >= 2.2f) { phase = 4; phaseT = 0; }
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
                if (!hitDone && atkT >= SwHit) { hitDone = true; DoSwing(); }
                if (atkT >= SwEnd) { attacking = false; atkCool = SwCool; }
            }
            else if (errandPh != 0) { ErrandControl(dt); }      // colhendo macas a pedido (inventario): o heroi anda sozinho ate a arvore
            else if (playMode) { PlayerControl(dt); }
            else
            {
                AutoControl(dt);
            }
            if (walking) walkT += dt;
            if (interlude) PushOutOfStones();      // pedras: o heroi para ao encostar
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
            float cx = FairyOn ? hx : hx + hface * 8 * S, R = FairyOn ? 36 * S : (eqSword == 2 ? 27 : (eqSword == 4 ? 23 : 18)) * S;      // marreta e cogumelo-maca: area maior
            if (eqSword == 4) for (int k = 0; k < 6; k++) puffs.Add(new Puff { Kind = 1, X = cx + ((float)rnd.NextDouble() - 0.5f) * 26 * S, Y = hy - (float)rnd.NextDouble() * 10 * S });      // esporos
            if (eqSword == 2) for (int k = 0; k < 5; k++) puffs.Add(new Puff { Kind = k % 2, X = cx + ((float)rnd.NextDouble() - 0.5f) * 22 * S, Y = hy + ((float)rnd.NextDouble() - 0.3f) * 6 * S });      // impacto
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                float dx = e.X - cx, dy = e.Y - hy;
                { float rad = e.Type == 3 ? R + 14 * S * DK * e.Scale : (e.Type == 7 ? R + 20 * S * e.Scale : R); if (dx * dx + dy * dy < rad * rad) DamageEnemy(e, FairyOn && e.Type != 3 && e.Type != 7 ? 999999 : SwordDamage()); }      // 150 comum | 300 Espada Mestre | 600 marreta | 450 espada de chamas
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
            if ((e.Type == 3 || e.Type == 7) && e.T < 1.5f) return;          // o chefe ainda esta saindo da fumaca
            e.HP -= dmg; e.Flash = e.Type == 3 ? 0.12f : 0.25f;
            float dx = e.X - hx, dy = e.Y - hy, d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 1) { dx = hface; dy = 0; d = 1; }
            if (e.Type != 3 && e.Type != 7) { e.KX = dx / d * 110 * S; e.KY = dy / d * 110 * S; }      // chefoes nao sao empurrados
            puffs.Add(new Puff { Kind = 1, X = e.X, Y = e.Y - 5 * S });
            if (e.HP <= 0)
            {
                puffs.Add(new Puff { Kind = 0, X = e.X, Y = e.Y - 4 * S });
                enemies.Remove(e); killCount++;
                if (interlude && e.Type == 1 && staffPh == 0 && scene == 0 && ++batKills >= BatsForStaff) staffPending = true;      // so comeca no proximo Step (limpar as listas aqui quebraria o loop que chamou)      // muitos morcegos mortos: o cajado cai
                if (e.Type == 3 || e.Type == 7) { BossDefeated(e); return; }
                if (!playMode) CountKill();                                     // evolucao: conta todo monstro derrotado no automatico
                if (!playMode) CountFairyKill();      // depois da roupa de rei: mais 50 monstros dao a fadinha
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
            if ((inv > 0 && !ignoreInv) || dead || powerT > 0 || (fairyStrike && FairyOn && !playMode)) return;
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

        void UpdateEnemies(float dtBase)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy e = enemies[i];
                float dt = (phase2 && e.Type != 3 && e.Type != 7) ? dtBase * 1.10f : dtBase;      // fase 2: os monstros comuns sao 10% mais rapidos (andar, pular, atacar e atirar)
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
                else if (e.Type == 7) UpdateOctoBoss(e, dt);      // Octorok Gigante (vilao da fase 2)
                else if (powerT > 0) // poder do heroi: todos vao direto nele, com o dobro da velocidade
                {
                    float sp = (e.Type == 0 ? 16 : (e.Type == 1 ? 30 : (e.Type == 2 ? 28 : 26))) * 2 * S;
                    e.Mode = 0; e.VX = ux * sp; e.VY = uy * sp;
                    if (d > 3 * S) { e.X += e.VX * dt; e.Y += e.VY * dt; }
                }
                else if (e.Type == 5) // bokoblin a cavalo com arco: se aproxima um pouco e depois rodeia o heroi atirando flechas
                {
                    float rv = 0.95f * 56 * S;      // 95% da velocidade do heroi (56)
                    if (e.Mode == 0)
                    {
                        if (e.SparkT <= 0) { e.SparkT = (62 + (float)rnd.NextDouble() * 14) * S; e.TY = rnd.Next(2) == 0 ? 1f : -1f; e.Timer = 0.8f + (float)rnd.NextDouble(); }      // SparkT: raio da volta, TY: sentido, Timer: tempo ate a proxima flecha
                        if (d > e.SparkT + 6 * S) { e.X += ux * rv * dt; e.Y += uy * rv * dt; }
                        else { e.Mode = 1; e.TX = (float)Math.Atan2(e.Y - hy, e.X - hx); }      // TX: angulo atual em volta do heroi
                    }
                    else
                    {
                        e.TX += e.TY * (rv / e.SparkT) * dt;
                        float tx = hx + (float)Math.Cos(e.TX) * e.SparkT, ty = hy + (float)Math.Sin(e.TX) * e.SparkT * 0.75f;
                        tx = Math.Max(8 * S, Math.Min(W - 8 * S, tx)); ty = Math.Max(20 * S, Math.Min(H - 2 * S, ty));
                        float mx2 = tx - e.X, my2 = ty - e.Y, md2 = (float)Math.Sqrt(mx2 * mx2 + my2 * my2);
                        if (md2 > 1) { float st2 = Math.Min(md2, rv * dt); e.X += mx2 / md2 * st2; e.Y += my2 / md2 * st2; e.VX = mx2 / md2; e.VY = my2 / md2; }
                        if (d > e.SparkT + 40 * S) e.Mode = 0;      // ficou longe demais: volta a se aproximar
                        e.Timer -= dt;
                        if (e.Timer <= 0 && !dead)
                        {
                            // mira perto do heroi, mas sem precisao: o erro e sorteado para ~50% das flechas errarem; depois de solta ela nao muda de rumo
                            float sx2 = e.X + e.Face * 8 * S, sy2 = e.Y - 12 * S;
                            float aimX = hx - sx2, aimY = (hy - 6 * S) - sy2, al = (float)Math.Sqrt(aimX * aimX + aimY * aimY);
                            if (al < 1) al = 1;
                            float err = ((float)rnd.NextDouble() * 2f - 1f) * (14f * S / al);      // acerta se |erro| * distancia < 7 S (raio do heroi): ~50%
                            float ang2 = (float)Math.Atan2(aimY, aimX) + err;
                            Rock r = new Rock(); r.Arrow = true; r.X = sx2; r.Y = sy2; r.VX = (float)Math.Cos(ang2) * 90 * S; r.VY = (float)Math.Sin(ang2) * 90 * S;
                            rocks.Add(r);
                            e.Timer = 1.7f + (float)rnd.NextDouble() * 1.3f;
                        }
                    }
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
                        if (!e.NoAtk) rocks.Add(r);      // o monstrinho da estrela nao atira
                        e.Mode = 0; e.Timer = 1.5f + (float)rnd.NextDouble() * 1.5f; e.VX = e.VY = 0;
                    }
                }
                else if (e.Type == 1) // morcego: voa ondulando em direcao ao heroi
                {
                    float wob = (float)Math.Sin(e.T * 5f) * (phase2 ? 12 : 26) * S;      // fase 2: voa mais rapido e mais reto em direcao ao heroi
                    float bsp = phase2 ? 48 : 30;
                    e.VX = ux * bsp * S - uy * wob * 0.6f;
                    e.VY = uy * bsp * S + ux * wob * 0.6f;
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
                            if (d < 20 * S) HurtHero(e.X, e.Y, 1f);      // o golpe do garrote tira exatamente 1 coracao
                            puffs.Add(new Puff { Kind = 1, X = e.X + e.Face * 12 * S, Y = e.Y - 3 * S });
                            e.Mode = 2; e.Timer = 0.8f;
                        }
                    }
                    else if (e.Timer <= 0) { e.Mode = 0; e.Timer = 0.2f; }
                }
                else // slime: pula em ciclos
                {
                    if (phase3 && !dead)      // GELATINA DE FOGO: mesma frequencia do octorok (1,5-3 s de andar + 0,6 s de mira = a cada ~2,1 a 3,6 s) e a bolinha explode em 3 s como a pedra dele
                    {
                        if (e.SparkT == 0f) e.SparkT = 1.2f + (float)rnd.NextDouble() * 1.8f;
                        e.SparkT -= dt;
                        if (e.SparkT <= 0)
                        {
                            Rock em = new Rock(); em.Ember = true; em.X = e.X + e.Face * 3 * S; em.Y = e.Y - 5 * S; em.VX = ux * 80 * S; em.VY = uy * 80 * S; if (!e.NoAtk) rocks.Add(em);
                            e.SparkT = 2.1f + (float)rnd.NextDouble() * 1.5f;
                        }
                    }
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
                        else if (e.Type == 7)      // Octorok Gigante: elipse no chao (corpo grande), so depois de crescer
                        {
                            float rx = 20 * S, ry = 8 * S;
                            toca = e.T > 1.5f && e.SlamPh == 0 && (cx * cx) / (rx * rx) + (cy * cy) / (ry * ry) < 1f;
                        }
                        else toca = cx * cx + cy * cy < (9 * S) * (9 * S);
                        if (toca && !e.NoAtk) HurtHero(e.X, e.Y, 0.5f);      // encostar em monstro: meio coracao
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
                shadowList.Add(new float[] { s.X - r, s.Y - r * 0.7f, 2 * r, 1.4f * r, S });      // onda amarela transparente: vai para a camada com transparencia real (a janela do heroi so liga/desliga a cor)
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
                if (!r.Fire && !r.Arrow && !r.Homing)      // pedra de octorok: explode depois de 3 s (area de 14 S; o escudo nao protege da explosao)
                {
                    r.Life += dt;
                    if (r.Life >= 3f)
                    {
                        for (int k = 0; k < 7; k++)
                            puffs.Add(new Puff { Kind = k % 2, X = r.X + ((float)rnd.NextDouble() - 0.5f) * 12 * S, Y = r.Y + ((float)rnd.NextDouble() - 0.5f) * 12 * S });
                        float ex = hx - r.X, ey = (hy - 6 * S) - r.Y;
                        if (!dead && ex * ex + ey * ey < (14 * S) * (14 * S)) HurtHero(r.X, r.Y, 1f);
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
                    bool blocked = !r.Fire && sp > 0 && (FairyOn || (eqSword != 2 && (r.VX * hface) / sp < -0.35f));      // Link-fada: as asas protegem de todos os lados
                    if (false) ;   // pedra vem de frente: escudo! (bola de fogo nao e bloqueada)
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
                if (powerT > 0 && !it.Reward) { float dd = (float)Math.Sqrt(dx * dx + dy * dy); if (dd > 0.5f) { float st = Math.Min(dd, 220f * S * dt); it.X -= dx / dd * st; it.Y -= dy / dd * st; dx = it.X - hx; dy = it.Y - hy; } }      // durante o poder as moedas voam direto para o Link
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
            if (zoomE > 0.0001f) { float k = ZK, ox = ZOx, oy = ZOy; shadowList.Add(new float[] { ox + (x - wTiles * S / 2f - zfx) * k, oy + (y - S - zfy) * k, wTiles * S * k, 3 * S * k }); return; }      // cena ampliada: a sombra acompanha
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
            if (tintLayer != null)      // filtro cinza + bordas pretas (Fase 2) e o preto de escurecer a tela
            {
                int fa = (int)(fadeA * 255f); bool want = darkOn || fa > 0;
                if (want || tintWasOn)
                {
                    if (fa != lastFa || darkOn != lastDark) { tintLayer.RenderTint(darkOn ? darkOverlay : null, fa); lastFa = fa; lastDark = darkOn; }      // so quando muda: redesenhar a tela inteira a cada quadro piscava
                    tintWasOn = want;
                }
            }
        }

        void PaintWorld(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Key);
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            GraphicsState zst = null;
            if (zoomE > 0.0001f) { zst = g.Save(); g.TranslateTransform(ZOx, ZOy); g.ScaleTransform(ZK, ZK); g.TranslateTransform(-zfx, -zfy); }      // cena: amplia em volta do Link
            DrawPuddles(g);
            DrawAcidPools(g);

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
            if (allyOn) ents.Add(allyMark);
            ents.Add(this);
            ents.Sort(delegate (object a, object b) { return YOf(a).CompareTo(YOf(b)); });
            foreach (object o in ents)
            {
                if (o == this) DrawHeroFx(g);
                else if (o == allyMark) DrawAlly(g);
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
                    else if (r.Arrow) DrawArrow(g, r.X, r.Y, r.VX, r.VY, 1f);
                    else if (r.Orb) using (SolidBrush ob = new SolidBrush(Color.FromArgb(150, 70, 230))) using (SolidBrush ib = new SolidBrush(Color.FromArgb(235, 210, 255))) using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36))) { g.FillRectangle(kb, r.X - 4 * S, r.Y - 4 * S, 8 * S, 8 * S); g.FillRectangle(ob, r.X - 3 * S, r.Y - 3 * S, 6 * S, 6 * S); g.FillRectangle(ib, r.X - S, r.Y - S, 2 * S, 2 * S); }
                    else if (r.Fire) Spr(g, fireSpr[((int)(time * 12)) % 2], r.X, r.Y + fireSpr[0].H / 2f, true);
                    else if (r.Ember)      // bolinha de fogo da gelatina: pisca mais forte perto de explodir
                    {
                        Sprite fs = fireSpr[((int)(time * 12)) % 2]; float ek = 0.8f + (r.Life > 2f ? 0.16f * (float)Math.Sin(r.Life * 34) : 0f);
                        int fw = (int)(fs.W * ek), fh = (int)(fs.H * ek);
                        g.DrawImage(fs.R, (int)(r.X - fw / 2f), (int)(r.Y - fh / 2f), fw, fh);
                    }
                    else Spr(g, rockSpr, r.X, r.Y + 2 * S, true);
                }

            foreach (Arrow a in arrows)
            {
                if (a.Glow)
                {
                    shadowList.Add(new float[] { a.X, a.Y, 5 * S, 190f, 1f, 0f, 0f, 0f });
                    using (SolidBrush yb = new SolidBrush(Color.FromArgb(255, 235, 70))) g.FillRectangle(yb, a.X - 1.5f * S, a.Y - 1.5f * S, 3 * S, 3 * S);
                    using (SolidBrush wb = new SolidBrush(Color.FromArgb(255, 255, 220))) g.FillRectangle(wb, a.X - S / 2f, a.Y - S / 2f, S, S);
                    continue;
                }
                if (a.Power) DrawPowerArrow(g, a); else DrawArrow(g, a.X, a.Y, a.VX, a.VY, 1f);
                if (a.Crystal) using (SolidBrush cb = new SolidBrush(Color.FromArgb(120, 235, 245))) g.FillRectangle(cb, a.X - S, a.Y - S, 2 * S, 2 * S);      // ponta de cristal
            }

            foreach (FlyApple fa in flyApples)
            {
                float u = Math.Min(1f, fa.T / 0.6f);
                float tx = hx + hface * 6 * S, ty = hy - 20 * S;
                float ax = fa.X0 + (tx - fa.X0) * u, ay = fa.Y0 + (ty - fa.Y0) * u - (float)Math.Sin(u * Math.PI) * 6 * S;
                Spr(g, appleSpr, ax, ay + appleSpr.H / 2f, true);
            }

            DrawFairy(g);
            DrawCookApples(g);
            DrawSmokes(g);
            foreach (Puff p in puffs) DrawPuff(g, p);

            DrawDrops(g);
            DrawAcidBalls(g);
            DrawStar(g);
            DrawSparks(g);
            DrawFairySparks(g);
            DrawStaff(g);
            if (zst != null) g.Restore(zst);      // HUD e mensagens nao ampliam
            DrawChestPrompt(g);
            DrawHud(g);
            DrawBossMsg(g);
            DrawDialog(g);
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
            if (o == allyMark) return ay;
            if (o is Fire) return fireY;
            return ((Enemy)o).Y;
        }

        void DrawTree(Graphics g, Tree t)
        {
            if (t.Stone) { Shadow(g, t.X, t.Y, 16); Spr(g, stoneSpr[t.Variant % 3], t.X, t.Y, true); return; }      // Fase 2: pedra no lugar da macieira
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
            float cx = hx + f * 8 * S, cy = hy - bob - 8 * S;
            float dx = aimX, dy = aimY, px = -dy, py = dx;
            float pull = Math.Min(1f, bowT / 0.4f);
            PixelBow(g, cx, cy, dx, dy, 5.6f * S, 2.4f * S, S, eqBow);      // arco 20% menor em proporcao ao Link
            float nx = cx - dx * pull * 3 * S, ny = cy - dy * pull * 3 * S;      // a corda vai ate a mao, sem entrar no rosto
            using (Pen st = new Pen(Color.FromArgb(235, 235, 235), 1))
            {
                g.DrawLine(st, cx + px * 5.6f * S, cy + py * 5.6f * S, nx, ny);
                g.DrawLine(st, nx, ny, cx - px * 5.6f * S, cy - py * 5.6f * S);
            }
            using (SolidBrush sk = new SolidBrush(Color.FromArgb(250, 208, 165))) using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
            { g.FillRectangle(kb, cx - 2 * S, cy - 2 * S, 3 * S, 3 * S); g.FillRectangle(sk, cx - S, cy - S, S, S); }      // a mao no meio do arco
            if (pull > 0.05f) DrawArrow(g, nx + dx * 10 * S, ny + dy * 10 * S, dx, dy, 1f);
        }
        // braco esticado para colher a maca
        void DrawReach(Graphics g, float f, float bob)
        {
            if ((calmState != 2 || phase != 1) && playPickT <= 0) return;
            float reach = (float)Math.Sin(Math.Min(1f, pickT / 1.0f) * Math.PI * 0.5);
            float x = hx + f * 4 * S, y0 = hy - bob - 10 * S, y1 = y0 - (5 + 5 * reach) * S;
            using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
            using (SolidBrush gb = new SolidBrush(TunicColor()))
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
            int fr = walking && !FairyOn ? ((int)(walkT * 9)) % 2 : 0;      // Link-fada voa: nao anda (sem passinhos)
            float bob = (walking && fr == 1) ? S : 0;
            if (FairyOn)      // Link-fada: sem arma nem escudo (as asas protegem); no ataque gira uma vez em torno de si
            {
                float fprog = attacking ? Math.Min(1f, atkT / SwEnd) : 0f;
                int qd = attacking ? Math.Min(7, (int)(fprog * 8f)) % 4 : 0;      // DUAS voltas de 360 graus em torno de si
                if (attacking && fprog > 0.05f && fprog < 0.97f)      // estrelinhas azuis meio transparentes fazendo o arco da espada
                {
                    float fang = -110f + 170f * (float)(1 - Math.Pow(1 - fprog, 2));
                    for (float a = -110f; a < fang; a += 3f)
                        for (int m = 0; m < 2; m++)
                        {
                            float h1 = (float)(Math.Abs(Math.Sin(a * 12.9898 + m * 78.233) * 43758.5453) % 1.0), h2 = (float)(Math.Abs(Math.Sin(a * 39.346 + m * 11.135) * 24634.6345) % 1.0);      // espalhados: raio e angulo sorteados (fixos por estrela)
                            double r2 = (a + (h2 - 0.5f) * 14f) * Math.PI / 180.0; float q = (a + 110f) / Math.Max(1f, fang + 110f), rr = (10f + h1 * 18f) * S;
                            shadowList.Add(new float[] { hx + hface * 3 * S + hface * (float)Math.Cos(r2) * rr, hy - 6 * S - FairyLift + (float)Math.Sin(r2) * rr, (1.6f + h2 * 1.6f) * S, 60f + 120f * q, 2f, 0f, 0f, 0f });
                        }
                }
                if (qd == 0 || qd == 2) Spr(g, heroWalk[fr], hx, hy - bob, qd == 0 ? right : !right); else Spr(g, heroFront, hx, hy - bob, true);
                if (bowing)      // 2: carrega a bolinha amarela na mao
                {
                    float gl = 0.5f + Math.Min(1f, bowT / 0.4f) * 0.5f;
                    shadowList.Add(new float[] { hx + hface * 7 * S, hy - 8 * S - FairyLift, 4 * S * gl, 170f * gl, 1f, 0f, 0f, 0f });
                    using (SolidBrush yb = new SolidBrush(Color.FromArgb(255, 235, 90))) g.FillRectangle(yb, hx + hface * 7 * S - S, hy - 9 * S, 2 * S * gl, 2 * S * gl);
                }
                return;
            }
            Spr(g, heroWalk[fr], hx, hy - bob, right);
            if (previewBare)      // aba Roupas: sem armas nem escudo; o braco do outro lado (esquerda da tela) e igual ao da direita: a mao na mesma altura
            {
                using (SolidBrush skin = new SolidBrush(Color.FromArgb(250, 208, 165)))
                    g.FillRectangle(skin, (int)(hx - 7 * S) + 3 * S, (int)(hy - bob - 16 * S) + 12 * S, S, S);
                return;
            }

            // escudo
            float f = hface;
            float sx = hx - f * 3 * S - 3 * S, sy = hy - bob - 11 * S;
            if (eqSword != 2 && !offShield) DrawShieldBody(g, sx, sy);      // a marreta usa as duas maos: sem escudo


            if (cookState == 6) DrawEatApple(g, f, bob); else if (playEatT > 0) DrawPlayEat(g, f, bob);
            if (calmState == 2 || cookState != 0 || playPickT > 0 || playEatT > 0) { DrawReach(g, f, bob); return; }   // espada guardada
            if (bowing) { if (!offBow) DrawBow(g, f, bob); return; }
            if (offSword) return;      // espada desequipada

            // espada
            float handX = hx + f * 5 * S, handY = hy - bob - 4 * S;
            float ang;
            float prog = attacking ? Math.Min(1f, atkT / SwEnd) : 0;
            if (attacking) ang = -110f + 170f * (float)(1 - Math.Pow(1 - prog, 2));
            else ang = walking ? -70f : -80f;
            double rad = ang * Math.PI / 180.0;
            float dx = f * (float)Math.Cos(rad), dy = (float)Math.Sin(rad);
            float px = -dy, py = dx;
            float len = 11 * S, w = 1.0f * S;      // espada comum menor em proporcao ao Link (antes 14)

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
            if (DrawSpecialSword(g, handX, handY, dx, dy)) return;      // marreta / espada de chamas
            DrawPixelatedSword(g, handX, handY, dx, dy, S, 11f);      // espada comum / Mestre: o mesmo desenho de sempre, agora pixelado
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
            if (eqSword != 2 && !FairyOn && !offShield) DrawShieldBody(g, sx, sy);


            if ((FairyOn || offSword) && !waving) return;      // Link-fada nao usa armas
            if (!waving)
            {
                // espada na mao direita: da posicao de combate para a ponta no chao
                float handX = hx + 6 * S, handY = hy - 5 * S;
                double rad = (-80f + 175f * q) * Math.PI / 180.0;
                float dx = (float)Math.Cos(rad), dy = (float)Math.Sin(rad), px = -dy, py = dx;
                float len = 9.5f * S, w = 1.0f * S;
                if (DrawSpecialSword(g, handX, handY, dx, dy)) goto swordDone;      // marreta / espada de chamas
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
                swordDone: ;
            }
            else
            {
                // espada guardada nas costas (so o cabo aparece sobre o ombro) e a mao direita acena
                float sw = (float)Math.Sin(time * 10) * S;      // a mao balanca de um lado para o outro
                using (SolidBrush kb = new SolidBrush(Color.FromArgb(28, 22, 36)))
                using (SolidBrush gb = new SolidBrush(TunicColor()))
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(250, 208, 165)))
                using (SolidBrush gold = new SolidBrush(Color.FromArgb(215, 160, 40)))
                using (SolidBrush br = new SolidBrush(Color.FromArgb(135, 85, 45)))
                {
                    if (!FairyOn) { g.FillRectangle(br, hx - 7 * S, hy - 11 * S, 2 * S, 3 * S);
                    g.FillRectangle(gold, hx - 8 * S, hy - 9 * S, 4 * S, S); }
                    // braco levantado, curto e proporcional (6 S de ponta a ponta, perto do tamanho do braco abaixado)
                    g.FillRectangle(kb, hx + 4 * S, hy - 10 * S, 4 * S, 4 * S);       // manga, saindo do ombro
                    g.FillRectangle(gb, hx + 5 * S, hy - 9 * S, 2 * S, 3 * S);
                    g.FillRectangle(kb, hx + 4 * S + sw, hy - 12 * S, 4 * S, 3 * S);  // mao
                    g.FillRectangle(sb, hx + 5 * S + sw, hy - 11 * S, 2 * S, 2 * S);
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
            if (e.Type == 7) { DrawOctoBoss(g, e); return; }
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
            else if (e.Type == 5) DrawRider(g, e);
            else if (e.Type == 4)
            {
                Shadow(g, e.X, e.Y, 12);
                if (e.Mode == 1 || e.Mode == 2) { Spr(g, boko[3], e.X, e.Y, right); DrawBokoClub(g, e); return; }      // ataque: levanta o garrote de madeira e bate
                int bf = (e.Mode == 0 && ((hx - e.X) * (hx - e.X) + (hy - e.Y) * (hy - e.Y)) > (12 * S) * (12 * S) ?((int)(e.T * 6)) % 2 : 0);
                Spr(g, boko[bf], e.X, e.Y, right);
            }
            else
            {
                Shadow(g, e.X, e.Y, 11);
                float hop = e.Mode == 1 ? (float)Math.Sin((1 - e.Timer / 0.45f) * Math.PI) * 5 * S : 0;
                Spr(g, slime[e.Mode == 1 ? 0 : 1], e.X, e.Y - hop, right);
            }
        }

        // garrote de madeira do bokoblin, IGUAL ao que ele segura parado (porrete fino de 2 cores, 2 de largura): parado fica na vertical; no ataque levanta um pouco para tras (aviso de 0,55 s),
        // desce batendo (0,2 s), fica baixo e volta para a vertical
        void DrawBokoClub(Graphics g, Enemy e)
        {
            int f = e.Face > 0 ? 1 : -1; float a;
            if (e.Mode == 1) { float t = 1f - e.Timer / 0.55f; a = -90f - 30f * t; }
            else { float el = 0.8f - e.Timer; a = el < 0.2f ? -120f + 160f * (el / 0.2f) : (el < 0.55f ? 40f : 40f - 130f * Math.Min(1f, (el - 0.55f) / 0.25f)); }
            double r = a * Math.PI / 180.0; float dx = f * (float)Math.Cos(r), dy = (float)Math.Sin(r), px = -dy, py = dx;
            float bx = e.X + f * 5.5f * S, by = e.Y - 6 * S, len = 8 * S;      // base do garrote na mao
            using (Pen pd = new Pen(Color.FromArgb(95, 60, 30), S)) using (Pen pl = new Pen(Color.FromArgb(135, 85, 45), S))
            {
                pd.StartCap = pd.EndCap = LineCap.Flat; pl.StartCap = pl.EndCap = LineCap.Flat;
                g.DrawLine(pd, bx - px * S / 2f, by - py * S / 2f, bx + dx * len - px * S / 2f, by + dy * len - py * S / 2f);
                g.DrawLine(pl, bx + px * S / 2f, by + py * S / 2f, bx + dx * len + px * S / 2f, by + dy * len + py * S / 2f);
            }
        }
        // bokoblin montado num cavalo, com arco (desenhado por retangulos; x vai para frente, y para cima, em unidades de S)
        void HRect(Graphics g, Color c, float ox, float oy, int f, float x, float y, float w, float h)
        {
            using (SolidBrush b = new SolidBrush(c))
                g.FillRectangle(b, f > 0 ? ox + x * S : ox - (x + w) * S, oy - (y + h) * S, w * S, h * S);
        }
        void DrawLeg(Graphics g, float hipX, float hipY, float deg, Color k, Color d)      // perna de 3x5 que gira em torno do quadril (topo)
        {
            GraphicsState st = g.Save();
            g.TranslateTransform(hipX, hipY); g.RotateTransform(deg);
            using (SolidBrush bk = new SolidBrush(k)) g.FillRectangle(bk, -1.5f * S, 0, 3 * S, 5 * S);
            using (SolidBrush bd = new SolidBrush(d)) g.FillRectangle(bd, -0.5f * S, 0, 1 * S, 4 * S);
            g.Restore(st);
        }
        void DrawRider(Graphics g, Enemy e)
        {
            Shadow(g, e.X, e.Y, 13);
            int f = e.Face > 0 ? 1 : -1;
            bool run = e.Mode == 0 || e.VX != 0 || e.VY != 0;
            // galope: as patas INCLINAM (giram no quadril): as da frente para frente, as de tras para tras, e voltam
            float ang = run ? 24f * (1f - (float)Math.Cos(e.T * 13.0)) / 2f : 0f;
            Color K = Color.FromArgb(28, 22, 36), body = Color.FromArgb(165, 105, 58), dark = Color.FromArgb(105, 62, 32);
            // cavaleiro primeiro: as pernas dele ficam escondidas atras do corpo do cavalo
            Spr(g, boko[0], e.X, e.Y - 6 * S, f > 0);
            // patas: 2 de tras e 2 da frente
            DrawLeg(g, e.X + f * -5.5f * S, e.Y - 5 * S, f * ang, K, dark);      // de tras: para tras
            DrawLeg(g, e.X + f * 5.5f * S, e.Y - 5 * S, -f * ang, K, dark);      // da frente: para frente
            // corpo, pescoco, cabeca e rabo
            HRect(g, K, e.X, e.Y, f, -8, 3, 16, 8); HRect(g, body, e.X, e.Y, f, -7, 4, 14, 6);
            HRect(g, K, e.X, e.Y, f, 5, 8, 5, 6); HRect(g, body, e.X, e.Y, f, 6, 9, 3, 4);
            HRect(g, K, e.X, e.Y, f, 8, 10, 6, 4); HRect(g, body, e.X, e.Y, f, 9, 11, 4, 2);
            HRect(g, dark, e.X, e.Y, f, -11, 6, 3, 3);
            // arco na frente do cavaleiro
            HRect(g, Color.FromArgb(135, 85, 45), e.X, e.Y, f, 8, 17, 1, 1); HRect(g, Color.FromArgb(135, 85, 45), e.X, e.Y, f, 9, 15, 1, 2);
            HRect(g, Color.FromArgb(135, 85, 45), e.X, e.Y, f, 10, 12, 1, 3); HRect(g, Color.FromArgb(135, 85, 45), e.X, e.Y, f, 9, 10, 1, 2); HRect(g, Color.FromArgb(135, 85, 45), e.X, e.Y, f, 8, 9, 1, 1);
            HRect(g, Color.White, e.X, e.Y, f, 8, 10, 1, 7);
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
            if (playMode) DrawText(g, "NV " + ((phase3 ? 15 : (phase2 ? 8 : 1)) + RupeeStep()), axx + appleHud.W + 2 * S + 16 * S, y0, Color.FromArgb(255, 220, 90));
            DrawBossBar(g);
            if (!playMode)
            {
                string gs = powerT > 0 ? "PODER! " + (int)Math.Ceiling(powerT) + "s" : "GELATINAS " + slimeKills + "/" + SlimesForPower;
                SizeF gz = g.MeasureString(gs, font);
                DrawText(g, gs, W - gz.Width - 6 * S, 4 * S, powerT > 0 ? Color.FromArgb(120, 230, 255) : Color.FromArgb(150, 200, 255));
            }
            if (playMode) DrawControlsHelp(g);
            DrawNewItemNotice(g);
            if (paused) DrawPauseBanner(g);
        }
    }
}


