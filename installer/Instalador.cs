// Instalador do "Heroi do Desktop"
// - instala para o usuario atual (sem pedir administrador) em %LOCALAPPDATA%\Programs\Heroi do Desktop
// - cria atalhos, entrada em "Aplicativos instalados" e um desinstalador
// Uso:  Instalador.exe            (abre o assistente)
//       Instalador.exe /S         (instalacao silenciosa)
//       Desinstalar.exe /uninstall [/S]
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Instalador do Herói do Desktop")]
[assembly: AssemblyProduct("Herói do Desktop")]
[assembly: AssemblyVersion("2.0.0.0")]

namespace HeroiInstalador
{
    static class Info
    {
        public const string Nome = "Herói do Desktop";
        public const string Versao = "2.0.0";
        public const string Exe = "HeroiPet.exe";
        public const string UninstKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\HeroiDoDesktop";
        public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static string PastaPadrao
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Heroi do Desktop"); }
        }
        public static string MenuIniciar
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), Nome); }
        }
        public static string AtalhoDesktop
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Nome + ".lnk"); }
        }

        public static void FecharPrograma()
        {
            foreach (Process p in Process.GetProcessesByName("HeroiPet"))
            {
                try { p.Kill(); p.WaitForExit(3000); } catch { }
            }
        }

        public static void CriarAtalho(string lnk, string alvo, string pasta, string descricao, string icone, string args)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object sh = Activator.CreateInstance(t);
            object s = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
            Type st = s.GetType();
            st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, s, new object[] { alvo });
            if (args != null) st.InvokeMember("Arguments", BindingFlags.SetProperty, null, s, new object[] { args });
            st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, s, new object[] { pasta });
            st.InvokeMember("Description", BindingFlags.SetProperty, null, s, new object[] { descricao });
            st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, s, new object[] { icone + ",0" });
            st.InvokeMember("Save", BindingFlags.InvokeMethod, null, s, null);
        }

        public static void Instalar(string pasta, bool desktop, bool iniciarComWindows)
        {
            FecharPrograma();
            Directory.CreateDirectory(pasta);
            string exe = Path.Combine(pasta, Exe);
            using (Stream rs = Assembly.GetExecutingAssembly().GetManifestResourceStream(Exe))
            using (FileStream fs = new FileStream(exe, FileMode.Create, FileAccess.Write))
            {
                byte[] buf = new byte[81920]; int n;
                while ((n = rs.Read(buf, 0, buf.Length)) > 0) fs.Write(buf, 0, n);
            }
            string desinst = Path.Combine(pasta, "Desinstalar.exe");
            File.Copy(Assembly.GetExecutingAssembly().Location, desinst, true);

            Directory.CreateDirectory(MenuIniciar);
            CriarAtalho(Path.Combine(MenuIniciar, Nome + ".lnk"), exe, pasta, "Um herói que vive na sua área de trabalho", exe, null);
            CriarAtalho(Path.Combine(MenuIniciar, "Desinstalar " + Nome + ".lnk"), desinst, pasta, "Remove o " + Nome, exe, "/uninstall");
            if (desktop) CriarAtalho(AtalhoDesktop, exe, pasta, "Um herói que vive na sua área de trabalho", exe, null);

            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (iniciarComWindows) k.SetValue("HeroiPet", "\"" + exe + "\"");
                else k.DeleteValue("HeroiPet", false);
            }
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstKey))
            {
                k.SetValue("DisplayName", Nome);
                k.SetValue("DisplayVersion", Versao);
                k.SetValue("Publisher", "Alexandre e Bia Krepel");
                k.SetValue("InstallLocation", pasta);
                k.SetValue("DisplayIcon", exe);
                k.SetValue("UninstallString", "\"" + desinst + "\" /uninstall");
                k.SetValue("QuietUninstallString", "\"" + desinst + "\" /uninstall /S");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                k.SetValue("EstimatedSize", (int)((new FileInfo(exe).Length + new FileInfo(desinst).Length) / 1024), RegistryValueKind.DWord);
            }
        }

        public static void Desinstalar()
        {
            FecharPrograma();
            string pasta = null;
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(UninstKey))
                if (k != null) pasta = k.GetValue("InstallLocation") as string;
            if (string.IsNullOrEmpty(pasta)) pasta = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try { if (File.Exists(AtalhoDesktop)) File.Delete(AtalhoDesktop); } catch { }
            try { if (Directory.Exists(MenuIniciar)) Directory.Delete(MenuIniciar, true); } catch { }
            try { using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true)) if (k != null) k.DeleteValue("HeroiPet", false); } catch { }
            try { Registry.CurrentUser.DeleteSubKey(UninstKey, false); } catch { }
            try
            {
                string cfg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HeroiPet");
                if (Directory.Exists(cfg)) Directory.Delete(cfg, true);
            }
            catch { }
            // o desinstalador esta dentro da pasta: apaga a pasta depois que ele terminar
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"" + pasta + "\"");
            psi.CreateNoWindow = true; psi.UseShellExecute = false; psi.WindowStyle = ProcessWindowStyle.Hidden;
            try { Process.Start(psi); } catch { }
        }
    }

    class Assistente : Form
    {
        TextBox caminho;
        CheckBox cbDesktop, cbRun, cbAbrir;
        Button btnInstalar, btnFechar;
        Panel pagina1, pagina2;
        Label lblFim;
        string pastaFinal;

        public Assistente()
        {
            Text = "Instalador - " + Info.Nome;
            ClientSize = new Size(560, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); } catch { }

            Panel topo = new Panel(); topo.Dock = DockStyle.Top; topo.Height = 92; topo.BackColor = Color.FromArgb(34, 30, 48);
            PictureBox pic = new PictureBox(); pic.Size = new Size(64, 64); pic.Location = new Point(20, 14); pic.SizeMode = PictureBoxSizeMode.Zoom;
            try { pic.Image = new Icon(Icon, 48, 48).ToBitmap(); } catch { }
            Label t1 = new Label(); t1.Text = Info.Nome; t1.ForeColor = Color.White; t1.Font = new Font("Segoe UI", 18f, FontStyle.Bold); t1.AutoSize = true; t1.Location = new Point(96, 16);
            Label t2 = new Label(); t2.Text = "Versão " + Info.Versao + "  •  vive na sua área de trabalho"; t2.ForeColor = Color.FromArgb(210, 210, 225); t2.AutoSize = true; t2.Location = new Point(98, 56);
            topo.Controls.Add(pic); topo.Controls.Add(t1); topo.Controls.Add(t2);
            Controls.Add(topo);

            pagina1 = new Panel(); pagina1.Location = new Point(0, 92); pagina1.Size = new Size(560, 308);
            Label l1 = new Label(); l1.Text = "Pasta de instalação"; l1.Location = new Point(24, 18); l1.AutoSize = true; l1.Font = new Font(Font, FontStyle.Bold);
            caminho = new TextBox(); caminho.Location = new Point(24, 42); caminho.Width = 420; caminho.Text = Info.PastaPadrao;
            Button procurar = new Button(); procurar.Text = "Procurar..."; procurar.Location = new Point(452, 40); procurar.Size = new Size(84, 27);
            procurar.Click += delegate
            {
                using (FolderBrowserDialog d = new FolderBrowserDialog())
                {
                    d.Description = "Escolha a pasta onde instalar";
                    if (d.ShowDialog(this) == DialogResult.OK) caminho.Text = Path.Combine(d.SelectedPath, "Heroi do Desktop");
                }
            };
            Label l2 = new Label(); l2.Text = "Instala só para a sua conta. Não precisa de senha de administrador."; l2.Location = new Point(24, 72); l2.AutoSize = true; l2.ForeColor = Color.Gray;
            cbDesktop = new CheckBox(); cbDesktop.Text = "Criar atalho na área de trabalho"; cbDesktop.Location = new Point(24, 112); cbDesktop.AutoSize = true; cbDesktop.Checked = true;
            cbRun = new CheckBox(); cbRun.Text = "Iniciar junto com o Windows"; cbRun.Location = new Point(24, 142); cbRun.AutoSize = true;
            cbAbrir = new CheckBox(); cbAbrir.Text = "Abrir o " + Info.Nome + " ao terminar"; cbAbrir.Location = new Point(24, 172); cbAbrir.AutoSize = true; cbAbrir.Checked = true;
            Label l3 = new Label(); l3.Text = "Dica: depois de instalado, clique com o botão direito no ícone do herói, perto do relógio, para escolher entre jogar e deixar rodando."; l3.Location = new Point(24, 214); l3.Size = new Size(510, 40); l3.ForeColor = Color.FromArgb(90, 90, 110);
            btnInstalar = new Button(); btnInstalar.Text = "Instalar"; btnInstalar.Location = new Point(344, 262); btnInstalar.Size = new Size(94, 32); btnInstalar.BackColor = Color.FromArgb(240, 168, 60); btnInstalar.FlatStyle = FlatStyle.Flat; btnInstalar.Font = new Font(Font, FontStyle.Bold);
            Button cancelar = new Button(); cancelar.Text = "Cancelar"; cancelar.Location = new Point(446, 262); cancelar.Size = new Size(90, 32);
            cancelar.Click += delegate { Close(); };
            btnInstalar.Click += Instalar;
            pagina1.Controls.AddRange(new Control[] { l1, caminho, procurar, l2, cbDesktop, cbRun, cbAbrir, l3, btnInstalar, cancelar });
            Controls.Add(pagina1);

            pagina2 = new Panel(); pagina2.Location = new Point(0, 92); pagina2.Size = new Size(560, 308); pagina2.Visible = false;
            lblFim = new Label(); lblFim.Location = new Point(24, 30); lblFim.Size = new Size(510, 150); lblFim.Font = new Font("Segoe UI", 10.5f);
            btnFechar = new Button(); btnFechar.Text = "Concluir"; btnFechar.Location = new Point(442, 262); btnFechar.Size = new Size(94, 32);
            btnFechar.Click += delegate
            {
                if (cbAbrir.Checked && pastaFinal != null)
                    try { Process.Start(new ProcessStartInfo(Path.Combine(pastaFinal, Info.Exe)) { WorkingDirectory = pastaFinal, UseShellExecute = true }); } catch { }
                Close();
            };
            pagina2.Controls.Add(lblFim); pagina2.Controls.Add(btnFechar);
            Controls.Add(pagina2);
            pagina1.BringToFront();
        }

        void Instalar(object s, EventArgs e)
        {
            string pasta = caminho.Text.Trim();
            if (pasta.Length == 0) { MessageBox.Show(this, "Escolha uma pasta.", Text); return; }
            try
            {
                btnInstalar.Enabled = false; btnInstalar.Text = "Instalando...";
                Application.DoEvents();
                Info.Instalar(pasta, cbDesktop.Checked, cbRun.Checked);
                pastaFinal = pasta;
                lblFim.Text = "Instalação concluída!\r\n\r\nO " + Info.Nome + " foi instalado em:\r\n" + pasta +
                    "\r\n\r\nVocê o encontra no Menu Iniciar" + (cbDesktop.Checked ? " e na área de trabalho" : "") +
                    ".\r\nPara remover: Configurações > Aplicativos instalados, ou o atalho \"Desinstalar\" no Menu Iniciar.";
                pagina1.Visible = false; pagina2.Visible = true; pagina2.BringToFront();
            }
            catch (Exception ex)
            {
                btnInstalar.Enabled = true; btnInstalar.Text = "Instalar";
                MessageBox.Show(this, "Não foi possível instalar:\r\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    static class Programa
    {
        [STAThread]
        static int Main(string[] args)
        {
            bool silencioso = false, desinstalar = false; string pasta = null;
            foreach (string a in args)
            {
                string l = a.ToLowerInvariant();
                if (l == "/s" || l == "/silent" || l == "-s") silencioso = true;
                else if (l == "/uninstall" || l == "-uninstall") desinstalar = true;
                else if (l.StartsWith("/dir=")) pasta = a.Substring(5).Trim('"');
            }
            if (pasta == null) pasta = Info.PastaPadrao;
            Application.EnableVisualStyles();

            if (desinstalar)
            {
                if (!silencioso && MessageBox.Show("Remover o " + Info.Nome + " do seu computador?", "Desinstalar " + Info.Nome,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return 1;
                Info.Desinstalar();
                if (!silencioso) MessageBox.Show(Info.Nome + " foi removido.", "Desinstalar " + Info.Nome, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }
            if (silencioso)
            {
                try { Info.Instalar(pasta, true, false); return 0; }
                catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 2; }
            }
            Application.Run(new Assistente());
            return 0;
        }
    }
}
