using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FolderRemarkNameTool;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.ThreadException += (_, e) => CrashLog.Write(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                CrashLog.Write(exception);
        };

        ApplicationConfiguration.Initialize();
        CrashLog.WriteMessage("启动程序");

        if (args.Length >= 2 && args[0].Equals("--set-remark", StringComparison.OrdinalIgnoreCase))
        {
            RemarkEditor.EditFolder(args[1], null);
            return;
        }

        if (args.Length >= 1 && args[0].Equals("--restart-app", StringComparison.OrdinalIgnoreCase))
        {
            AppRestarter.RestartCurrentExecutable();
            return;
        }

        if (args.Length >= 1 && args[0].Equals("--install-context-menu", StringComparison.OrdinalIgnoreCase))
        {
            ContextMenuInstaller.Apply(true);
            return;
        }

        if (args.Length >= 1 && args[0].Equals("--remove-context-menu", StringComparison.OrdinalIgnoreCase))
        {
            ContextMenuInstaller.Apply(false);
            return;
        }

        Application.Run(new TrayContext());
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon notifyIcon;
    private readonly HotkeyWindow hotkeyWindow;
    private readonly ExplorerHotkeyHook explorerHotkeyHook;
    private readonly Control invoker;
    private AppSettings settings;
    private int hotkeyInProgress;

    public TrayContext()
    {
        settings = SettingsStore.Load();
        ContextMenuInstaller.Apply(settings.ContextMenuEnabled);
        StartupManager.Apply(settings.StartWithWindows);
        invoker = new Control();
        _ = invoker.Handle;

        notifyIcon = new NotifyIcon
        {
            Icon = AppIcon.CreateIcon(),
            Text = "文件夹备注名",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };

        hotkeyWindow = new HotkeyWindow(OnHotkey);
        explorerHotkeyHook = new ExplorerHotkeyHook(settings.Hotkey, OnHotkey);
        RegisterConfiguredHotkey();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("设置", null, (_, _) => ShowSettings());
        menu.Items.Add("说明", null, (_, _) => MessageBox.Show(
            $"在 Windows 资源管理器中选中一个文件夹，按 {settings.Hotkey.DisplayText} 设置备注名。\r\n\r\n右键菜单可直接设置备注，也可以重启工具。",
            "文件夹备注名",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information));
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private void RegisterConfiguredHotkey()
    {
        try
        {
            hotkeyWindow.Unregister();
            explorerHotkeyHook.Stop();

            if (settings.Hotkey.IsPlainFunctionKey)
            {
                explorerHotkeyHook.Start(settings.Hotkey);
                CrashLog.WriteMessage($"使用 Explorer 钩子快捷键：{settings.Hotkey.DisplayText}");
            }
            else
            {
                hotkeyWindow.Register(settings.Hotkey);
                CrashLog.WriteMessage($"使用系统注册快捷键：{settings.Hotkey.DisplayText}");
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            MessageBox.Show(
                $"快捷键 {settings.Hotkey.DisplayText} 注册失败，可能已被其他程序占用。\r\n\r\n请在设置里换一个组合键，例如 Ctrl + Alt + F4。",
                "文件夹备注名",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(settings);
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        settings = dialog.Settings;
        SettingsStore.Save(settings);
        ContextMenuInstaller.Apply(settings.ContextMenuEnabled);
        StartupManager.Apply(settings.StartWithWindows);
        RegisterConfiguredHotkey();
        notifyIcon.ContextMenuStrip = BuildMenu();
        notifyIcon.ShowBalloonTip(1500, "文件夹备注名", "设置已保存。", ToolTipIcon.Info);
    }

    private void OnHotkey()
    {
        if (!ExplorerSelection.IsForegroundExplorer())
            return;

        if (Interlocked.Exchange(ref hotkeyInProgress, 1) == 1)
            return;

        invoker.BeginInvoke((Action)HandleExplorerHotkey);
    }

    private void HandleExplorerHotkey()
    {
        try
        {
        if (!ExplorerSelection.TryGetSingleSelectedFolder(out var folderPath))
        {
            notifyIcon.ShowBalloonTip(1500, "文件夹备注名", "请先在资源管理器中选中一个文件夹。", ToolTipIcon.Info);
            return;
        }

        RemarkEditor.EditFolder(folderPath, notifyIcon);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            MessageBox.Show(ex.Message, "文件夹备注名", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Interlocked.Exchange(ref hotkeyInProgress, 0);
        }
    }

    protected override void ExitThreadCore()
    {
        hotkeyWindow.Dispose();
        explorerHotkeyHook.Dispose();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        invoker.Dispose();
        base.ExitThreadCore();
    }
}

internal static class RemarkEditor
{
    public static void EditFolder(string folderPath, NotifyIcon? notifyIcon)
    {
        if (!Directory.Exists(folderPath))
        {
            MessageBox.Show("请选择一个有效文件夹。", "文件夹备注名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = SettingsStore.Load();
        using var dialog = new RemarkDialog(folderPath, DesktopIniRemarkStore.GetRemark(folderPath), settings.ShowOriginalNameInDisplayName);
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            settings.ShowOriginalNameInDisplayName = dialog.ShowOriginalNameInDisplayName;
            SettingsStore.Save(settings);

            var originalPath = folderPath;
            var finalPath = FolderRenameService.RenameIfNeeded(folderPath, dialog.OriginalName);
            DesktopIniRemarkStore.SetRemark(finalPath, dialog.Remark, settings.ShowOriginalNameInDisplayName);
            ShellRefresh.RefreshFolderChange(originalPath, finalPath);
            ExplorerRefreshService.RefreshOpenExplorerWindows(originalPath, finalPath);
            notifyIcon?.ShowBalloonTip(1200, "文件夹备注名", "备注名已保存。", ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            var message = ex is UnauthorizedAccessException
                ? $"{ex.Message}\r\n\r\n可能原因：desktop.ini 或目标文件夹带只读/系统属性、文件被占用，或该目录需要更高权限。"
                : ex.Message;
            MessageBox.Show(message, "写入备注失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class RemarkDialog : Form
{
    private readonly Label previewTitleLabel;
    private readonly TextBox originalNameBox;
    private readonly TextBox remarkBox;
    private readonly CheckBox showOriginalNameCheckBox;

    public string OriginalName => originalNameBox.Text.Trim();
    public string Remark => remarkBox.Text.Trim();
    public bool ShowOriginalNameInDisplayName => showOriginalNameCheckBox.Checked;

    public RemarkDialog(string folderPath, string currentRemark, bool showOriginalNameInDisplayName)
    {
        Icon = AppIcon.CreateIcon();
        Text = "设置文件夹备注名";
        Font = UiStyle.AppFont;
        BackColor = UiStyle.WindowBackColor;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 286);

        previewTitleLabel = new Label
        {
            Text = Path.GetFileName(folderPath),
            ForeColor = UiStyle.TextColor,
            Font = UiStyle.HeaderFont,
            AutoEllipsis = true,
            Location = new Point(32, 28),
            Size = new Size(496, 30),
        };

        var originalNameLabel = new Label
        {
            Text = "原名",
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 84),
            Size = new Size(84, 26),
        };

        originalNameBox = new TextBox
        {
            Text = Path.GetFileName(folderPath),
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(126, 80),
            Size = new Size(402, 30),
        };
        UiStyle.StyleTextBox(originalNameBox);

        var remarkLabel = new Label
        {
            Text = "备注名",
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 132),
            Size = new Size(84, 26),
        };

        remarkBox = new TextBox
        {
            Text = currentRemark,
            BorderStyle = BorderStyle.FixedSingle,
            Location = new Point(126, 128),
            Size = new Size(402, 30),
        };
        UiStyle.StyleTextBox(remarkBox);

        showOriginalNameCheckBox = new CheckBox
        {
            Text = "备注名后面用括号显示原名",
            Checked = showOriginalNameInDisplayName,
            ForeColor = UiStyle.TextColor,
            Location = new Point(126, 178),
            Size = new Size(360, 28),
            FlatStyle = FlatStyle.System,
        };

        var okButton = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.OK,
            Location = new Point(358, 224),
            Size = new Size(80, 36),
        };
        UiStyle.StylePrimaryButton(okButton);

        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(448, 224),
            Size = new Size(80, 36),
        };
        UiStyle.StyleSecondaryButton(cancelButton);

        Controls.AddRange(new Control[] { previewTitleLabel, originalNameLabel, originalNameBox, remarkLabel, remarkBox, showOriginalNameCheckBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;

        originalNameBox.TextChanged += (_, _) => UpdateDisplayPreview();
        remarkBox.TextChanged += (_, _) => UpdateDisplayPreview();
        showOriginalNameCheckBox.CheckedChanged += (_, _) => UpdateDisplayPreview();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(OriginalName))
            {
                MessageBox.Show("原名不能为空。", "文件夹备注名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };

        Shown += (_, _) =>
        {
            UpdateDisplayPreview();
            remarkBox.Focus();
            remarkBox.SelectAll();
        };
    }

    private void UpdateDisplayPreview()
    {
        var remark = remarkBox.Text.Trim();
        var originalName = originalNameBox.Text.Trim();

        if (!showOriginalNameCheckBox.Checked)
        {
            previewTitleLabel.Text = string.IsNullOrWhiteSpace(remark) ? originalName : remark;
            return;
        }

        previewTitleLabel.Text = string.IsNullOrWhiteSpace(remark)
            ? originalName
            : $"{remark}（{originalName}）";
    }
}

internal sealed class SettingsForm : Form
{
    private readonly HotkeyCaptureBox hotkeyBox;
    private readonly CheckBox contextMenuCheckBox;
    private readonly CheckBox startupCheckBox;
    private readonly CheckBox showOriginalNameCheckBox;

    public AppSettings Settings { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Settings = current.Clone();
        Icon = AppIcon.CreateIcon();
        Text = "文件夹备注名设置";
        Font = UiStyle.AppFont;
        BackColor = UiStyle.WindowBackColor;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(500, 336);

        var hotkeyLabel = new Label
        {
            Text = "快捷键：",
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 40),
            Size = new Size(82, 24),
        };

        hotkeyBox = new HotkeyCaptureBox
        {
            Hotkey = Settings.Hotkey,
            Location = new Point(126, 35),
            Size = new Size(330, 32),
        };

        contextMenuCheckBox = new CheckBox
        {
            Text = "加入资源管理器文件夹右键菜单",
            Checked = Settings.ContextMenuEnabled,
            ForeColor = UiStyle.TextColor,
            Location = new Point(126, 92),
            Size = new Size(330, 26),
            FlatStyle = FlatStyle.System,
        };

        startupCheckBox = new CheckBox
        {
            Text = "开机自动启动",
            Checked = Settings.StartWithWindows,
            ForeColor = UiStyle.TextColor,
            Location = new Point(126, 128),
            Size = new Size(330, 26),
            FlatStyle = FlatStyle.System,
        };

        showOriginalNameCheckBox = new CheckBox
        {
            Text = "备注名后面用括号显示原名",
            Checked = Settings.ShowOriginalNameInDisplayName,
            ForeColor = UiStyle.TextColor,
            Location = new Point(126, 164),
            Size = new Size(330, 26),
            FlatStyle = FlatStyle.System,
        };

        var hintLabel = new Label
        {
            Text = "提示：右键菜单会立即写入当前用户注册表，通常不需要重启 Windows。",
            ForeColor = UiStyle.MutedTextColor,
            Location = new Point(126, 212),
            Size = new Size(330, 38),
        };

        var okButton = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.OK,
            Location = new Point(304, 280),
            Size = new Size(72, 34),
        };
        UiStyle.StylePrimaryButton(okButton);

        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(384, 280),
            Size = new Size(72, 34),
        };
        UiStyle.StyleSecondaryButton(cancelButton);

        okButton.Click += (_, _) =>
        {
            if (hotkeyBox.Hotkey.KeyCode == Keys.None)
            {
                MessageBox.Show("请先设置一个有效快捷键。", "文件夹备注名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            Settings.Hotkey = hotkeyBox.Hotkey;
            Settings.ContextMenuEnabled = contextMenuCheckBox.Checked;
            Settings.StartWithWindows = startupCheckBox.Checked;
            Settings.ShowOriginalNameInDisplayName = showOriginalNameCheckBox.Checked;
        };

        Controls.AddRange(new Control[] { hotkeyLabel, hotkeyBox, contextMenuCheckBox, startupCheckBox, showOriginalNameCheckBox, hintLabel, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}

internal sealed class HotkeyCaptureBox : TextBox
{
    private HotkeyGesture hotkey = HotkeyGesture.Default;

    public HotkeyGesture Hotkey
    {
        get => hotkey;
        set
        {
            hotkey = value;
            Text = hotkey.DisplayText;
        }
    }

    public HotkeyCaptureBox()
    {
        ReadOnly = true;
        TabStop = true;
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = Color.White;
        ForeColor = UiStyle.TextColor;
        Font = UiStyle.AppFont;
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return true;
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        e.IsInputKey = true;
        base.OnPreviewKeyDown(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        CaptureHotkey(keyData);
        return true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        CaptureHotkey(e.KeyData);
        e.SuppressKeyPress = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        CaptureHotkey(e.KeyData);
        e.SuppressKeyPress = true;
        base.OnKeyUp(e);
    }

    private void CaptureHotkey(Keys keyData)
    {
        var keyCode = keyData & Keys.KeyCode;
        if (keyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu)
            return;

        Hotkey = new HotkeyGesture
        {
            KeyCode = keyCode,
            Control = keyData.HasFlag(Keys.Control),
            Alt = keyData.HasFlag(Keys.Alt),
            Shift = keyData.HasFlag(Keys.Shift),
        };
    }
}

internal sealed class AppSettings
{
    public HotkeyGesture Hotkey { get; set; } = HotkeyGesture.Default;
    public bool ContextMenuEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool ShowOriginalNameInDisplayName { get; set; }

    public AppSettings Clone() => new()
    {
        Hotkey = Hotkey.Clone(),
        ContextMenuEnabled = ContextMenuEnabled,
        StartWithWindows = StartWithWindows,
        ShowOriginalNameInDisplayName = ShowOriginalNameInDisplayName,
    };
}

internal sealed class HotkeyGesture
{
    public static HotkeyGesture Default => new() { KeyCode = Keys.F4 };

    public Keys KeyCode { get; set; } = Keys.F4;
    public bool Control { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }

    public string DisplayText
    {
        get
        {
            var parts = new List<string>();
            if (Control) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            parts.Add(KeyCode.ToString());
            return string.Join(" + ", parts);
        }
    }

    public uint Modifiers
    {
        get
        {
            uint modifiers = 0;
            if (Alt) modifiers |= NativeMethods.MOD_ALT;
            if (Control) modifiers |= NativeMethods.MOD_CONTROL;
            if (Shift) modifiers |= NativeMethods.MOD_SHIFT;
            return modifiers;
        }
    }

    public bool IsPlainFunctionKey =>
        !Control &&
        !Alt &&
        !Shift &&
        KeyCode >= Keys.F1 &&
        KeyCode <= Keys.F24;

    public bool Matches(Keys key)
    {
        return key == KeyCode
            && Control == NativeMethods.IsKeyDown(Keys.ControlKey)
            && Alt == NativeMethods.IsKeyDown(Keys.Menu)
            && Shift == NativeMethods.IsKeyDown(Keys.ShiftKey);
    }

    public HotkeyGesture Clone() => new()
    {
        KeyCode = KeyCode,
        Control = Control,
        Alt = Alt,
        Shift = Shift,
    };
}

internal static class SettingsStore
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FolderRemarkNameTool");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath, Encoding.UTF8)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json, Encoding.UTF8);
    }
}

internal static class CrashLog
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FolderRemarkNameTool");

    private static readonly string FilePath = Path.Combine(DirectoryPath, "crash.log");

    public static void Write(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.AppendAllText(
                FilePath,
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}]\r\n{exception}\r\n\r\n",
                Encoding.UTF8);
        }
        catch
        {
            // 日志写入失败时不能再次打断主流程。
        }
    }

    public static void WriteMessage(string message)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            File.AppendAllText(
                FilePath,
                $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}] {message}\r\n",
                Encoding.UTF8);
        }
        catch
        {
        }
    }
}

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "FolderRemarkNameTool";

    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
                key?.SetValue(ValueName, $"\"{Application.ExecutablePath}\"");
            else
                key?.DeleteValue(ValueName, false);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }
    }
}

internal static class AppRestarter
{
    public static void RestartCurrentExecutable()
    {
        var exePath = Application.ExecutablePath;
        var currentId = Environment.ProcessId;

        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exePath)))
        {
            try
            {
                if (process.Id == currentId)
                    continue;

                if (!string.Equals(process.MainModule?.FileName, exePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                process.CloseMainWindow();
                if (!process.WaitForExit(1500))
                    process.Kill();
            }
            catch
            {
                // 单个进程无法结束时继续尝试启动新实例。
            }
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
        });
    }
}

internal static class UiStyle
{
    public static readonly Font AppFont = new("Microsoft YaHei UI", 9.5f);
    public static readonly Font TitleFont = new("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
    public static readonly Font HeaderFont = new("Microsoft YaHei UI", 13f, FontStyle.Bold);
    public static readonly Color WindowBackColor = Color.FromArgb(245, 247, 250);
    public static readonly Color TextColor = Color.FromArgb(0, 0, 0);
    public static readonly Color MutedTextColor = Color.FromArgb(89, 89, 89);
    private static readonly Color PrimaryColor = Color.FromArgb(22, 119, 255);
    private static readonly Color BorderColor = Color.FromArgb(217, 217, 217);

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 150, 255);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(9, 88, 217);
        button.BackColor = PrimaryColor;
        button.ForeColor = Color.White;
        button.Font = AppFont;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(250, 250, 250);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(240, 240, 240);
        button.BackColor = Color.White;
        button.ForeColor = TextColor;
        button.Font = AppFont;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Color.White;
        textBox.ForeColor = TextColor;
        textBox.Font = AppFont;
    }
}

internal static class ContextMenuInstaller
{
    private const string FolderMenuKey = @"Software\Classes\Directory\shell\FolderRemarkNameTool";
    private const string FolderRestartMenuKey = @"Software\Classes\Directory\shell\FolderRemarkNameToolRestart";
    private const string FolderDirectMenuKey = @"Software\Classes\Directory\shell\FolderRemarkNameToolSetDirect";
    private const string FolderDirectRestartMenuKey = @"Software\Classes\Directory\shell\FolderRemarkNameToolRestartDirect";
    private const string BackgroundMenuKey = @"Software\Classes\Directory\Background\shell\FolderRemarkNameTool";
    private const string BackgroundRestartMenuKey = @"Software\Classes\Directory\Background\shell\FolderRemarkNameToolRestart";
    private const string BackgroundDirectMenuKey = @"Software\Classes\Directory\Background\shell\FolderRemarkNameToolSetDirect";
    private const string BackgroundDirectRestartMenuKey = @"Software\Classes\Directory\Background\shell\FolderRemarkNameToolRestartDirect";

    public static void Apply(bool enabled)
    {
        CrashLog.WriteMessage($"应用右键菜单设置：{enabled}");
        Uninstall();

        if (enabled)
        {
            TryInstall(() => InstallSetRemarkKey(FolderDirectMenuKey, "%1"));
            TryInstall(() => InstallSetRemarkKey(BackgroundDirectMenuKey, "%V"));
            TryInstall(() => InstallRestartKey(FolderDirectRestartMenuKey));
            TryInstall(() => InstallRestartKey(BackgroundDirectRestartMenuKey));
        }
    }

    private static void TryInstall(Action install)
    {
        try
        {
            install();
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }
    }

    private static void InstallSetRemarkKey(string menuKey, string folderArgument)
    {
        DeleteRegistryTree(menuKey + @"\shell");
        DeleteRegistryTree(menuKey + @"\command");

        var exePath = Application.ExecutablePath;
        var iconPath = AppIcon.GetIconFilePath();
        using var key = Registry.CurrentUser.CreateSubKey(menuKey);
        key?.SetValue("", "设置文件夹备注名");
        key?.SetValue("Icon", iconPath);
        key?.DeleteValue("MUIVerb", false);
        key?.DeleteValue("SubCommands", false);

        using var setRemarkCommandKey = Registry.CurrentUser.CreateSubKey(menuKey + @"\command");
        setRemarkCommandKey?.SetValue("", $"\"{exePath}\" --set-remark \"{folderArgument}\"");
        CrashLog.WriteMessage($"安装设置备注菜单：{menuKey}");
    }

    private static void InstallRestartKey(string menuKey)
    {
        DeleteRegistryTree(menuKey + @"\shell");
        DeleteRegistryTree(menuKey + @"\command");

        var exePath = Application.ExecutablePath;
        var iconPath = AppIcon.GetIconFilePath();
        using var restartKey = Registry.CurrentUser.CreateSubKey(menuKey);
        restartKey?.SetValue("", "重启文件夹备注名工具");
        restartKey?.SetValue("Icon", iconPath);
        restartKey?.DeleteValue("MUIVerb", false);
        restartKey?.DeleteValue("SubCommands", false);

        using var restartCommandKey = Registry.CurrentUser.CreateSubKey(menuKey + @"\command");
        restartCommandKey?.SetValue("", $"\"{exePath}\" --restart-app");
        CrashLog.WriteMessage($"安装重启菜单：{menuKey}");
    }

    private static void Uninstall()
    {
        DeleteRegistryTree(FolderMenuKey);
        DeleteRegistryTree(FolderRestartMenuKey);
        DeleteRegistryTree(FolderDirectMenuKey);
        DeleteRegistryTree(FolderDirectRestartMenuKey);
        DeleteRegistryTree(BackgroundMenuKey);
        DeleteRegistryTree(BackgroundRestartMenuKey);
        DeleteRegistryTree(BackgroundDirectMenuKey);
        DeleteRegistryTree(BackgroundDirectRestartMenuKey);
    }

    private static void DeleteRegistryTree(string keyPath)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }
    }
}

internal static class ExplorerSelection
{
    public static bool IsForegroundExplorer()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        return foreground != IntPtr.Zero && IsExplorerWindow(foreground);
    }

    public static bool TryGetSingleSelectedFolder(out string folderPath)
    {
        folderPath = "";

        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero || !IsExplorerWindow(foreground))
            return false;

        var shellType = Type.GetTypeFromProgID("Shell.Application");
        if (shellType is null)
            return false;

        dynamic shell = Activator.CreateInstance(shellType)!;
        foreach (dynamic window in shell.Windows())
        {
            try
            {
                if ((IntPtr)(int)window.HWND != foreground)
                    continue;

                dynamic selectedItems = window.Document.SelectedItems();
                if ((int)selectedItems.Count != 1)
                    return false;

                string path = selectedItems.Item(0).Path;
                if (!Directory.Exists(path))
                    return false;

                folderPath = path;
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static bool IsExplorerWindow(IntPtr hwnd)
    {
        var className = new StringBuilder(256);
        _ = NativeMethods.GetClassName(hwnd, className, className.Capacity);
        return className.ToString() is "CabinetWClass" or "ExploreWClass";
    }
}

internal static class FolderRenameService
{
    public static string RenameIfNeeded(string folderPath, string requestedName)
    {
        var parentPath = Directory.GetParent(folderPath)?.FullName
            ?? throw new InvalidOperationException("无法获取父目录。");

        var currentName = Path.GetFileName(folderPath);
        if (string.Equals(currentName, requestedName, StringComparison.Ordinal))
            return folderPath;

        if (requestedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException("原名包含 Windows 不允许的字符。");

        var targetPath = Path.Combine(parentPath, requestedName);
        if (Directory.Exists(targetPath) || File.Exists(targetPath))
            throw new InvalidOperationException("同一目录下已经存在这个名称。");

        Directory.Move(folderPath, targetPath);
        return targetPath;
    }
}

internal static class DesktopIniRemarkStore
{
    private const string Section = ".ShellClassInfo";
    private const string DisplayNameKey = "LocalizedResourceName";
    private const string InfoTipKey = "InfoTip";
    private const string RawRemarkKey = "FolderRemarkNameToolRemark";

    public static string GetRemark(string folderPath)
    {
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        if (!File.Exists(desktopIniPath))
            return "";

        var lines = File.ReadAllLines(desktopIniPath, Encoding.Unicode);
        foreach (var line in lines)
        {
            if (line.StartsWith(RawRemarkKey + "=", StringComparison.OrdinalIgnoreCase))
                return line[(RawRemarkKey.Length + 1)..].Trim();
        }

        foreach (var line in lines)
        {
            if (line.StartsWith(DisplayNameKey + "=", StringComparison.OrdinalIgnoreCase))
                return line[(DisplayNameKey.Length + 1)..].Trim();
        }

        return "";
    }

    public static void SetRemark(string folderPath, string remark, bool showOriginalName)
    {
        var desktopIniPath = Path.Combine(folderPath, "desktop.ini");
        var originalIniAttributes = File.Exists(desktopIniPath)
            ? File.GetAttributes(desktopIniPath)
            : FileAttributes.Hidden | FileAttributes.System;

        if (File.Exists(desktopIniPath))
        {
            File.SetAttributes(
                desktopIniPath,
                originalIniAttributes & ~(FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System));
        }

        var lines = File.Exists(desktopIniPath)
            ? File.ReadAllLines(desktopIniPath, Encoding.Unicode).ToList()
            : new List<string>();

        var displayName = BuildDisplayName(folderPath, remark, showOriginalName);
        SetIniValue(lines, Section, RawRemarkKey, remark);
        SetIniValue(lines, Section, DisplayNameKey, displayName);
        SetIniValue(lines, Section, InfoTipKey, string.IsNullOrWhiteSpace(remark) ? "" : $"真实名称：{Path.GetFileName(folderPath)}");

        File.WriteAllLines(desktopIniPath, lines, Encoding.Unicode);

        var folderAttributes = File.GetAttributes(folderPath);
        File.SetAttributes(folderPath, folderAttributes | FileAttributes.ReadOnly);

        File.SetAttributes(
            desktopIniPath,
            (originalIniAttributes | FileAttributes.Hidden | FileAttributes.System) & ~FileAttributes.ReadOnly);
    }

    private static string BuildDisplayName(string folderPath, string remark, bool showOriginalName)
    {
        if (string.IsNullOrWhiteSpace(remark))
            return "";

        var originalName = Path.GetFileName(folderPath);
        return showOriginalName ? $"{remark}（{originalName}）" : remark;
    }

    private static void SetIniValue(List<string> lines, string section, string key, string value)
    {
        var sectionIndex = lines.FindIndex(line => line.Trim().Equals($"[{section}]", StringComparison.OrdinalIgnoreCase));
        if (sectionIndex < 0)
        {
            lines.Add($"[{section}]");
            lines.Add($"{key}={value}");
            return;
        }

        var insertIndex = lines.Count;
        for (var i = sectionIndex + 1; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                insertIndex = i;
                break;
            }

            if (trimmed.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(value))
                    lines.RemoveAt(i);
                else
                    lines[i] = $"{key}={value}";
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(value))
            lines.Insert(insertIndex, $"{key}={value}");
    }
}

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 0x524e;
    private readonly Action onHotkey;
    private bool registered;

    public HotkeyWindow(Action onHotkey)
    {
        this.onHotkey = onHotkey;
        CreateHandle(new CreateParams());
    }

    public void Register(HotkeyGesture hotkey)
    {
        Unregister();

        if (!NativeMethods.RegisterHotKey(Handle, HotkeyId, hotkey.Modifiers, (uint)hotkey.KeyCode))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "快捷键注册失败");

        registered = true;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
        {
            onHotkey();
            return;
        }

        base.WndProc(ref m);
    }

    public void Unregister()
    {
        if (!registered)
            return;

        NativeMethods.UnregisterHotKey(Handle, HotkeyId);
        registered = false;
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }
}

internal sealed class ExplorerHotkeyHook : IDisposable
{
    private HotkeyGesture hotkey;
    private readonly Action onHotkey;
    private readonly NativeMethods.LowLevelKeyboardProc proc;
    private IntPtr hookId;

    public ExplorerHotkeyHook(HotkeyGesture hotkey, Action onHotkey)
    {
        this.hotkey = hotkey;
        this.onHotkey = onHotkey;
        proc = HookCallback;
    }

    public void Start(HotkeyGesture newHotkey)
    {
        hotkey = newHotkey.Clone();
        Stop();

        hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            proc,
            IntPtr.Zero,
            0);

        if (hookId == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "快捷键钩子安装失败");
    }

    public void Stop()
    {
        if (hookId == IntPtr.Zero)
            return;

        NativeMethods.UnhookWindowsHookEx(hookId);
        hookId = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == NativeMethods.WM_KEYDOWN)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            if (hotkey.Matches((Keys)vkCode) && ExplorerSelection.IsForegroundExplorer())
            {
                onHotkey();
                return new IntPtr(1);
            }
        }

        return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        Stop();
    }
}

internal static class ShellRefresh
{
    public static void RefreshPath(string path)
    {
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_UPDATEITEM,
            NativeMethods.SHCNF_PATHW,
            path,
            IntPtr.Zero);
    }

    public static void RefreshFolderChange(string oldPath, string newPath)
    {
        var oldParent = Directory.GetParent(oldPath)?.FullName;
        var newParent = Directory.GetParent(newPath)?.FullName;

        RefreshPath(oldPath);
        RefreshPath(newPath);
        RefreshItem(Path.Combine(newPath, "desktop.ini"));
        RefreshDirectory(newPath);

        if (!string.IsNullOrWhiteSpace(oldParent))
            RefreshDirectory(oldParent);

        if (!string.IsNullOrWhiteSpace(newParent) &&
            !string.Equals(oldParent, newParent, StringComparison.OrdinalIgnoreCase))
            RefreshDirectory(newParent);

        NativeMethods.SHChangeNotifyPointer(
            NativeMethods.SHCNE_ASSOCCHANGED,
            NativeMethods.SHCNF_IDLIST,
            IntPtr.Zero,
            IntPtr.Zero);
    }

    private static void RefreshItem(string path)
    {
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_UPDATEITEM,
            NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSHNOWAIT,
            path,
            IntPtr.Zero);
    }

    private static void RefreshDirectory(string path)
    {
        NativeMethods.SHChangeNotify(
            NativeMethods.SHCNE_UPDATEDIR,
            NativeMethods.SHCNF_PATHW | NativeMethods.SHCNF_FLUSHNOWAIT,
            path,
            IntPtr.Zero);
    }
}

internal static class ExplorerRefreshService
{
    public static void RefreshOpenExplorerWindows(string oldPath, string newPath)
    {
        try
        {
            var oldParent = Directory.GetParent(oldPath)?.FullName;
            var newParent = Directory.GetParent(newPath)?.FullName;
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
                return;

            dynamic shell = Activator.CreateInstance(shellType)!;
            foreach (dynamic window in shell.Windows())
            {
                try
                {
                    string location = window.Document.Folder.Self.Path;
                    if (IsRelated(location, oldPath, newPath, oldParent, newParent))
                        window.Refresh();
                }
                catch
                {
                    // 跳过非资源管理器窗口。
                }
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
        }
    }

    private static bool IsRelated(string location, params string?[] paths)
    {
        return paths.Any(path =>
            !string.IsNullOrWhiteSpace(path) &&
            string.Equals(location, path, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class AppIcon
{
    public static string GetIconFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
    }

    public static Icon CreateIcon()
    {
        var iconPath = GetIconFilePath();
        if (File.Exists(iconPath))
        {
            try
            {
                return new Icon(iconPath);
            }
            catch (Exception ex)
            {
                CrashLog.Write(ex);
            }
        }

        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var folderBrush = new SolidBrush(Color.FromArgb(255, 22, 119, 255));
        using var folderLightBrush = new SolidBrush(Color.FromArgb(255, 64, 150, 255));
        using var noteBrush = new SolidBrush(Color.White);
        using var noteShadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
        using var folderPen = new Pen(Color.FromArgb(255, 9, 88, 217), 2);
        using var notePen = new Pen(Color.FromArgb(255, 22, 119, 255), 3);

        g.FillRoundedRectangle(folderLightBrush, new Rectangle(10, 12, 22, 12), 4);
        g.FillRoundedRectangle(folderBrush, new Rectangle(7, 19, 50, 36), 9);
        g.DrawRoundedRectangle(folderPen, new Rectangle(7, 19, 50, 36), 9);

        g.FillRoundedRectangle(noteShadowBrush, new Rectangle(28, 27, 23, 23), 5);
        g.FillRoundedRectangle(noteBrush, new Rectangle(25, 24, 23, 23), 5);
        g.DrawLine(notePen, 30, 31, 43, 31);
        g.DrawLine(notePen, 30, 38, 40, 38);

        return Icon.FromHandle(bitmap.GetHicon());
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectangle(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int WH_KEYBOARD_LL = 13;
    public static readonly IntPtr WM_KEYDOWN = new(0x0100);
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint SHCNE_UPDATEITEM = 0x00002000;
    public const uint SHCNE_UPDATEDIR = 0x00001000;
    public const uint SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0x0000;
    public const uint SHCNF_PATHW = 0x0005;
    public const uint SHCNF_FLUSHNOWAIT = 0x2000;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(Keys vKey);

    public static bool IsKeyDown(Keys key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern void SHChangeNotify(uint wEventId, uint uFlags, string dwItem1, IntPtr dwItem2);

    [DllImport("shell32.dll", EntryPoint = "SHChangeNotify")]
    public static extern void SHChangeNotifyPointer(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
