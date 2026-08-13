using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
        Localizer.Apply(SettingsStore.Load().LanguageCode);

        if (args.Length >= 2 && args[0].Equals("--set-remark", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length >= 5 && args[2].Equals("--original-name", StringComparison.OrdinalIgnoreCase))
            {
                var showOriginalName = args.Length >= 7 &&
                    args[5].Equals("--show-original-name", StringComparison.OrdinalIgnoreCase) &&
                    bool.TryParse(args[6], out var parsedShowOriginalName) &&
                    parsedShowOriginalName;
                RemarkEditor.ApplyRemark(args[1], args[3], args[4], showOriginalName, null);
            }
            else
            {
                RemarkEditor.EditFolder(args[1], null);
            }
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

internal static class Localizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new()
        {
            ["AppName"] = "Folder Remark Name",
            ["SettingsMenu"] = "Settings...",
            ["HelpMenu"] = "Help",
            ["ExitMenu"] = "Exit",
            ["HelpText"] = "Select a folder in Windows File Explorer, then press {0} to set its remark name.\r\n\r\nYou can also right-click a folder and choose \"Set remark name\".",
            ["HotkeyRegisterFailed"] = "Shortcut {0} registration failed. It may already be used by another app.\r\n\r\nPlease choose another shortcut in settings, such as Ctrl + Alt + F2.",
            ["SettingsSaved"] = "Settings saved.",
            ["SelectFolderFirst"] = "Please select a folder in File Explorer first.",
            ["InvalidFolder"] = "Please select a valid folder.",
            ["RemarkSaved"] = "Remark name saved.",
            ["WriteRemarkFailedTitle"] = "Failed to write remark",
            ["UnauthorizedHint"] = "Possible causes: desktop.ini or the target folder has read-only/system attributes, the file is in use, or this folder requires higher permissions.",
            ["RetryAsAdmin"] = "Retry as administrator",
            ["AdminRetryFailed"] = "Failed to start administrator retry.",
            ["RemarkDialogTitle"] = "Set Folder Remark Name",
            ["PreviewDisplayName"] = "Display name preview",
            ["OriginalName"] = "Original name",
            ["RemarkName"] = "Remark name",
            ["ShowOriginalName"] = "Show original name in parentheses after remark name",
            ["Save"] = "Save",
            ["Cancel"] = "Cancel",
            ["OriginalNameRequired"] = "Original name cannot be empty.",
            ["SettingsTitle"] = "Folder Remark Name Settings",
            ["SettingsSubtitle"] = "Shortcut, context menu, startup, and language",
            ["Hotkey"] = "Shortcut:",
            ["Language"] = "Language:",
            ["Options"] = "Options",
            ["ContextMenuOption"] = "Add folder context menu in File Explorer",
            ["StartupOption"] = "Start with Windows",
            ["Hint"] = "Tip: Context menu changes are written to the current user registry immediately.",
            ["InvalidHotkey"] = "Please set a valid shortcut first.",
            ["About"] = "About this app",
            ["OpenGitHub"] = "Open GitHub",
            ["ContextSetRemark"] = "Set remark name",
            ["LanguageAuto"] = "Auto (System)",
            ["LanguageZhHans"] = "Chinese (Simplified)",
            ["LanguageEn"] = "English",
            ["LanguageJa"] = "Japanese",
            ["LanguageKo"] = "Korean",
            ["RenameParentFailed"] = "Cannot get parent folder.",
            ["InvalidFileName"] = "Original name contains characters that Windows does not allow.",
            ["NameExists"] = "A folder or file with this name already exists in the same directory.",
        },
        ["zh-Hans"] = new()
        {
            ["AppName"] = "文件夹备注名",
            ["SettingsMenu"] = "设置...",
            ["HelpMenu"] = "使用说明",
            ["ExitMenu"] = "退出",
            ["HelpText"] = "在 Windows 资源管理器中选中一个文件夹，按 {0} 设置备注名。\r\n\r\n也可以右键文件夹，选择“设置备注名”。",
            ["HotkeyRegisterFailed"] = "快捷键 {0} 注册失败，可能已被其他程序占用。\r\n\r\n请在设置里换一个组合键，例如 Ctrl + Alt + F2。",
            ["SettingsSaved"] = "设置已保存。",
            ["SelectFolderFirst"] = "请先在资源管理器中选中一个文件夹。",
            ["InvalidFolder"] = "请选择一个有效文件夹。",
            ["RemarkSaved"] = "备注名已保存。",
            ["WriteRemarkFailedTitle"] = "写入备注失败",
            ["UnauthorizedHint"] = "可能原因：desktop.ini 或目标文件夹带只读/系统属性、文件被占用，或该目录需要更高权限。",
            ["RetryAsAdmin"] = "以管理员权限重试",
            ["AdminRetryFailed"] = "启动管理员重试失败。",
            ["RemarkDialogTitle"] = "设置文件夹备注名",
            ["PreviewDisplayName"] = "预览显示名",
            ["OriginalName"] = "原名",
            ["RemarkName"] = "备注名",
            ["ShowOriginalName"] = "备注名后面用括号显示原名",
            ["Save"] = "保存",
            ["Cancel"] = "取消",
            ["OriginalNameRequired"] = "原名不能为空。",
            ["SettingsTitle"] = "文件夹备注名设置",
            ["SettingsSubtitle"] = "快捷键、右键菜单、启动项和语言",
            ["Hotkey"] = "快捷键：",
            ["Language"] = "语言：",
            ["Options"] = "选项",
            ["ContextMenuOption"] = "加入资源管理器文件夹右键菜单",
            ["StartupOption"] = "开机自动启动",
            ["Hint"] = "提示：右键菜单会立即写入当前用户注册表。",
            ["InvalidHotkey"] = "请先设置一个有效快捷键。",
            ["About"] = "关于本软件",
            ["OpenGitHub"] = "打开 GitHub",
            ["ContextSetRemark"] = "设置备注名",
            ["LanguageAuto"] = "跟随系统",
            ["LanguageZhHans"] = "简体中文",
            ["LanguageEn"] = "English",
            ["LanguageJa"] = "日本語",
            ["LanguageKo"] = "한국어",
            ["RenameParentFailed"] = "无法获取父目录。",
            ["InvalidFileName"] = "原名包含 Windows 不允许的字符。",
            ["NameExists"] = "同一目录下已经存在这个名称。",
        },
        ["zh-Hant"] = new()
        {
            ["AppName"] = "資料夾備註名",
            ["SettingsMenu"] = "設定...",
            ["HelpMenu"] = "使用說明",
            ["ExitMenu"] = "結束",
            ["HelpText"] = "在 Windows 檔案總管中選取一個資料夾，按 {0} 設定備註名。\r\n\r\n也可以右鍵資料夾，選擇「設定備註名」。",
            ["HotkeyRegisterFailed"] = "快捷鍵 {0} 註冊失敗，可能已被其他程式占用。\r\n\r\n請在設定裡換一個組合鍵，例如 Ctrl + Alt + F2。",
            ["SettingsSaved"] = "設定已儲存。",
            ["SelectFolderFirst"] = "請先在檔案總管中選取一個資料夾。",
            ["InvalidFolder"] = "請選擇一個有效資料夾。",
            ["RemarkSaved"] = "備註名已儲存。",
            ["WriteRemarkFailedTitle"] = "寫入備註失敗",
            ["UnauthorizedHint"] = "可能原因：desktop.ini 或目標資料夾帶唯讀/系統屬性、檔案被占用，或該目錄需要更高權限。",
            ["RetryAsAdmin"] = "以系統管理員權限重試",
            ["AdminRetryFailed"] = "啟動系統管理員重試失敗。",
            ["RemarkDialogTitle"] = "設定資料夾備註名",
            ["PreviewDisplayName"] = "預覽顯示名",
            ["OriginalName"] = "原名",
            ["RemarkName"] = "備註名",
            ["ShowOriginalName"] = "備註名後面用括號顯示原名",
            ["Save"] = "儲存",
            ["Cancel"] = "取消",
            ["OriginalNameRequired"] = "原名不能為空。",
            ["SettingsTitle"] = "資料夾備註名設定",
            ["SettingsSubtitle"] = "快捷鍵、右鍵選單、啟動項和語言",
            ["Hotkey"] = "快捷鍵：",
            ["Language"] = "語言：",
            ["Options"] = "選項",
            ["ContextMenuOption"] = "加入檔案總管資料夾右鍵選單",
            ["StartupOption"] = "開機自動啟動",
            ["Hint"] = "提示：右鍵選單會立即寫入目前使用者登錄檔。",
            ["InvalidHotkey"] = "請先設定一個有效快捷鍵。",
            ["About"] = "關於本軟體",
            ["OpenGitHub"] = "開啟 GitHub",
            ["ContextSetRemark"] = "設定備註名",
            ["LanguageAuto"] = "跟隨系統",
            ["LanguageZhHans"] = "简体中文",
            ["LanguageZhHant"] = "繁體中文",
            ["LanguageEn"] = "English",
            ["LanguageJa"] = "日本語",
            ["LanguageKo"] = "한국어",
            ["RenameParentFailed"] = "無法取得父目錄。",
            ["InvalidFileName"] = "原名包含 Windows 不允許的字元。",
            ["NameExists"] = "同一目錄下已經存在這個名稱。",
        },
        ["ja"] = new()
        {
            ["AppName"] = "フォルダー備考名",
            ["SettingsMenu"] = "設定...",
            ["HelpMenu"] = "使い方",
            ["ExitMenu"] = "終了",
            ["HelpText"] = "Windows エクスプローラーでフォルダーを選択し、{0} を押して備考名を設定します。\r\n\r\nフォルダーを右クリックして「備考名を設定」を選ぶこともできます。",
            ["HotkeyRegisterFailed"] = "ショートカット {0} の登録に失敗しました。他のアプリで使用されている可能性があります。\r\n\r\n設定で Ctrl + Alt + F2 など別のショートカットに変更してください。",
            ["SettingsSaved"] = "設定を保存しました。",
            ["SelectFolderFirst"] = "先にエクスプローラーでフォルダーを選択してください。",
            ["InvalidFolder"] = "有効なフォルダーを選択してください。",
            ["RemarkSaved"] = "備考名を保存しました。",
            ["WriteRemarkFailedTitle"] = "備考名の書き込みに失敗しました",
            ["UnauthorizedHint"] = "考えられる原因: desktop.ini または対象フォルダーが読み取り専用/システム属性、ファイルが使用中、または高い権限が必要です。",
            ["RetryAsAdmin"] = "管理者として再試行",
            ["AdminRetryFailed"] = "管理者としての再試行を開始できませんでした。",
            ["RemarkDialogTitle"] = "フォルダー備考名を設定",
            ["PreviewDisplayName"] = "表示名プレビュー",
            ["OriginalName"] = "元の名前",
            ["RemarkName"] = "備考名",
            ["ShowOriginalName"] = "備考名の後ろに元の名前を括弧で表示",
            ["Save"] = "保存",
            ["Cancel"] = "キャンセル",
            ["OriginalNameRequired"] = "元の名前は空にできません。",
            ["SettingsTitle"] = "フォルダー備考名設定",
            ["SettingsSubtitle"] = "ショートカット、右クリックメニュー、起動、言語",
            ["Hotkey"] = "ショートカット:",
            ["Language"] = "言語:",
            ["Options"] = "オプション",
            ["ContextMenuOption"] = "エクスプローラーのフォルダー右クリックメニューに追加",
            ["StartupOption"] = "Windows 起動時に開始",
            ["Hint"] = "ヒント: 右クリックメニューの変更は現在のユーザーのレジストリにすぐ書き込まれます。",
            ["InvalidHotkey"] = "有効なショートカットを設定してください。",
            ["About"] = "このアプリについて",
            ["OpenGitHub"] = "GitHub を開く",
            ["ContextSetRemark"] = "備考名を設定",
            ["LanguageAuto"] = "自動 (システム)",
            ["LanguageZhHans"] = "簡体字中国語",
            ["LanguageEn"] = "English",
            ["LanguageJa"] = "日本語",
            ["LanguageKo"] = "한국어",
            ["RenameParentFailed"] = "親フォルダーを取得できません。",
            ["InvalidFileName"] = "元の名前に Windows で使用できない文字が含まれています。",
            ["NameExists"] = "同じ場所に同名のフォルダーまたはファイルが既に存在します。",
        },
        ["ko"] = new()
        {
            ["AppName"] = "폴더 표시 이름",
            ["SettingsMenu"] = "설정...",
            ["HelpMenu"] = "사용법",
            ["ExitMenu"] = "종료",
            ["HelpText"] = "Windows 파일 탐색기에서 폴더를 선택한 뒤 {0} 키로 표시 이름을 설정합니다.\r\n\r\n폴더를 마우스 오른쪽 버튼으로 클릭하고 \"표시 이름 설정\"을 선택할 수도 있습니다.",
            ["HotkeyRegisterFailed"] = "바로 가기 키 {0} 등록에 실패했습니다. 다른 앱에서 사용 중일 수 있습니다.\r\n\r\n설정에서 Ctrl + Alt + F2 같은 다른 조합으로 변경하세요.",
            ["SettingsSaved"] = "설정이 저장되었습니다.",
            ["SelectFolderFirst"] = "먼저 파일 탐색기에서 폴더를 선택하세요.",
            ["InvalidFolder"] = "올바른 폴더를 선택하세요.",
            ["RemarkSaved"] = "표시 이름이 저장되었습니다.",
            ["WriteRemarkFailedTitle"] = "표시 이름 쓰기 실패",
            ["UnauthorizedHint"] = "가능한 원인: desktop.ini 또는 대상 폴더의 읽기 전용/시스템 속성, 파일 사용 중, 또는 더 높은 권한 필요.",
            ["RetryAsAdmin"] = "관리자 권한으로 다시 시도",
            ["AdminRetryFailed"] = "관리자 권한 다시 시도를 시작하지 못했습니다.",
            ["RemarkDialogTitle"] = "폴더 표시 이름 설정",
            ["PreviewDisplayName"] = "표시 이름 미리보기",
            ["OriginalName"] = "원래 이름",
            ["RemarkName"] = "표시 이름",
            ["ShowOriginalName"] = "표시 이름 뒤에 원래 이름을 괄호로 표시",
            ["Save"] = "저장",
            ["Cancel"] = "취소",
            ["OriginalNameRequired"] = "원래 이름은 비워 둘 수 없습니다.",
            ["SettingsTitle"] = "폴더 표시 이름 설정",
            ["SettingsSubtitle"] = "바로 가기, 컨텍스트 메뉴, 시작, 언어",
            ["Hotkey"] = "바로 가기:",
            ["Language"] = "언어:",
            ["Options"] = "옵션",
            ["ContextMenuOption"] = "파일 탐색기 폴더 컨텍스트 메뉴에 추가",
            ["StartupOption"] = "Windows 시작 시 실행",
            ["Hint"] = "팁: 컨텍스트 메뉴 변경은 현재 사용자 레지스트리에 즉시 기록됩니다.",
            ["InvalidHotkey"] = "올바른 바로 가기 키를 먼저 설정하세요.",
            ["About"] = "이 앱 정보",
            ["OpenGitHub"] = "GitHub 열기",
            ["ContextSetRemark"] = "표시 이름 설정",
            ["LanguageAuto"] = "자동 (시스템)",
            ["LanguageZhHans"] = "중국어 간체",
            ["LanguageEn"] = "English",
            ["LanguageJa"] = "日本語",
            ["LanguageKo"] = "한국어",
            ["RenameParentFailed"] = "상위 폴더를 가져올 수 없습니다.",
            ["InvalidFileName"] = "원래 이름에 Windows에서 허용하지 않는 문자가 포함되어 있습니다.",
            ["NameExists"] = "같은 위치에 같은 이름의 폴더 또는 파일이 이미 있습니다.",
        },
    };

    public static readonly IReadOnlyList<LanguageOption> Languages = new[]
    {
        new LanguageOption("auto", "LanguageAuto"),
        new LanguageOption("zh-Hans", "LanguageZhHans"),
        new LanguageOption("zh-Hant", "LanguageZhHant"),
        new LanguageOption("en", "LanguageEn"),
        new LanguageOption("ja", "LanguageJa"),
        new LanguageOption("ko", "LanguageKo"),
    };

    public static string ActiveLanguageCode { get; private set; } = ResolveLanguageCode("auto");

    public static void Apply(string languageCode)
    {
        ActiveLanguageCode = ResolveLanguageCode(languageCode);
    }

    public static string T(string key)
    {
        if (Texts.TryGetValue(ActiveLanguageCode, out var current) && current.TryGetValue(key, out var text))
            return text;

        return Texts["en"].TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, T(key), args);

    public static string GetLanguageDisplayName(string code)
    {
        var option = Languages.FirstOrDefault(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) ?? Languages[0];
        return T(option.DisplayNameKey);
    }

    private static string ResolveLanguageCode(string languageCode)
    {
        if (!string.IsNullOrWhiteSpace(languageCode) && !languageCode.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return Texts.ContainsKey(languageCode) ? languageCode : "en";

        var culture = CultureInfo.CurrentUICulture.Name;
        if (culture.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
            culture.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
            culture.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase) ||
            culture.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase))
            return "zh-Hant";
        if (culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return "zh-Hans";
        if (culture.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
            return "ja";
        if (culture.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            return "ko";
        return "en";
    }
}

internal sealed record LanguageOption(string Code, string DisplayNameKey);

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
        Localizer.Apply(settings.LanguageCode);
        ContextMenuInstaller.Apply(settings.ContextMenuEnabled);
        StartupManager.Apply(settings.StartWithWindows);
        invoker = new Control();
        _ = invoker.Handle;

        notifyIcon = new NotifyIcon
        {
            Icon = AppIcon.CreateIcon(),
            Text = Localizer.T("AppName"),
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };

        hotkeyWindow = new HotkeyWindow(OnHotkey);
        explorerHotkeyHook = new ExplorerHotkeyHook(settings.Hotkey, OnHotkey);
        RegisterConfiguredHotkey();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            Font = UiStyle.MenuFont,
            RenderMode = ToolStripRenderMode.System,
            ShowImageMargin = false,
            Padding = new Padding(2, 4, 2, 4),
        };
        menu.Items.Add(Localizer.T("SettingsMenu"), null, (_, _) => ShowSettings());
        menu.Items.Add(Localizer.T("HelpMenu"), null, (_, _) => MessageBox.Show(
            Localizer.Format("HelpText", settings.Hotkey.DisplayText),
            Localizer.T("AppName"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Localizer.T("ExitMenu"), null, (_, _) => ExitThread());
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
                Localizer.Format("HotkeyRegisterFailed", settings.Hotkey.DisplayText),
                Localizer.T("AppName"),
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
        Localizer.Apply(settings.LanguageCode);
        SettingsStore.Save(settings);
        ContextMenuInstaller.Apply(settings.ContextMenuEnabled);
        StartupManager.Apply(settings.StartWithWindows);
        RegisterConfiguredHotkey();
        notifyIcon.Text = Localizer.T("AppName");
        notifyIcon.ContextMenuStrip = BuildMenu();
        notifyIcon.ShowBalloonTip(1500, Localizer.T("AppName"), Localizer.T("SettingsSaved"), ToolTipIcon.Info);
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
            notifyIcon.ShowBalloonTip(1500, Localizer.T("AppName"), Localizer.T("SelectFolderFirst"), ToolTipIcon.Info);
            return;
        }

        RemarkEditor.EditFolder(folderPath, notifyIcon);
        }
        catch (Exception ex)
        {
            CrashLog.Write(ex);
            MessageBox.Show(ex.Message, Localizer.T("AppName"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(Localizer.T("InvalidFolder"), Localizer.T("AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = SettingsStore.Load();
        Localizer.Apply(settings.LanguageCode);
        using var dialog = new RemarkDialog(folderPath, DesktopIniRemarkStore.GetRemark(folderPath), settings.ShowOriginalNameInDisplayName);
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        ApplyRemark(folderPath, dialog.OriginalName, dialog.Remark, dialog.ShowOriginalNameInDisplayName, notifyIcon);
    }

    public static void ApplyRemark(string folderPath, string originalName, string remark, bool showOriginalNameInDisplayName, NotifyIcon? notifyIcon)
    {
        try
        {
            var settings = SettingsStore.Load();
            Localizer.Apply(settings.LanguageCode);
            settings.ShowOriginalNameInDisplayName = showOriginalNameInDisplayName;
            SettingsStore.Save(settings);

            var originalPath = folderPath;
            var finalPath = FolderRenameService.RenameIfNeeded(folderPath, originalName);
            DesktopIniRemarkStore.SetRemark(finalPath, remark, showOriginalNameInDisplayName);
            ShellRefresh.RefreshFolderChange(originalPath, finalPath);
            ExplorerRefreshService.RefreshOpenExplorerWindows(originalPath, finalPath);
            notifyIcon?.ShowBalloonTip(1200, Localizer.T("AppName"), Localizer.T("RemarkSaved"), ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            HandleWriteFailure(ex, folderPath, originalName, remark, showOriginalNameInDisplayName);
        }
    }

    private static void HandleWriteFailure(Exception ex, string folderPath, string originalName, string remark, bool showOriginalNameInDisplayName)
    {
        var message = ex is UnauthorizedAccessException
            ? $"{ex.Message}\r\n\r\n{Localizer.T("UnauthorizedHint")}"
            : ex.Message;

        if (ex is UnauthorizedAccessException)
        {
            var result = MessageBox.Show(
                $"{message}\r\n\r\n{Localizer.T("RetryAsAdmin")}",
                Localizer.T("WriteRemarkFailedTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.Yes)
                RetryAsAdministrator(folderPath, originalName, remark, showOriginalNameInDisplayName);
            return;
        }

        MessageBox.Show(message, Localizer.T("WriteRemarkFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void RetryAsAdministrator(string folderPath, string originalName, string remark, bool showOriginalNameInDisplayName)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Application.ExecutablePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = BuildSetRemarkArguments(folderPath, originalName, remark, showOriginalNameInDisplayName),
            });
        }
        catch (Exception retryException)
        {
            CrashLog.Write(retryException);
            MessageBox.Show(Localizer.T("AdminRetryFailed"), Localizer.T("WriteRemarkFailedTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string BuildSetRemarkArguments(string folderPath, string originalName, string remark, bool showOriginalNameInDisplayName)
    {
        return string.Join(
            " ",
            QuoteArgument("--set-remark"),
            QuoteArgument(folderPath),
            QuoteArgument("--original-name"),
            QuoteArgument(originalName),
            QuoteArgument(remark),
            QuoteArgument("--show-original-name"),
            QuoteArgument(showOriginalNameInDisplayName.ToString()));
    }

    private static string QuoteArgument(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
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
        Text = Localizer.T("RemarkDialogTitle");
        UiStyle.StyleForm(this);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 320);

        var headerPanel = new Panel
        {
            BackColor = SystemColors.Window,
            Location = new Point(0, 0),
            Size = new Size(560, 82),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        previewTitleLabel = new Label
        {
            Text = Path.GetFileName(folderPath),
            ForeColor = UiStyle.TextColor,
            Font = UiStyle.HeaderFont,
            AutoEllipsis = true,
            Location = new Point(32, 20),
            Size = new Size(496, 28),
        };

        var previewHintLabel = new Label
        {
            Text = Localizer.T("PreviewDisplayName"),
            ForeColor = UiStyle.MutedTextColor,
            Location = new Point(32, 50),
            Size = new Size(496, 20),
        };
        headerPanel.Controls.AddRange(new Control[] { previewTitleLabel, previewHintLabel });

        var originalNameLabel = new Label
        {
            Text = Localizer.T("OriginalName"),
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 112),
            Size = new Size(84, 26),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        originalNameBox = new TextBox
        {
            Text = Path.GetFileName(folderPath),
            Location = new Point(126, 108),
            Size = new Size(402, 30),
        };
        UiStyle.StyleTextBox(originalNameBox);

        var remarkLabel = new Label
        {
            Text = Localizer.T("RemarkName"),
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 160),
            Size = new Size(84, 26),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        remarkBox = new TextBox
        {
            Text = currentRemark,
            Location = new Point(126, 156),
            Size = new Size(402, 30),
        };
        UiStyle.StyleTextBox(remarkBox);

        showOriginalNameCheckBox = new CheckBox
        {
            Text = Localizer.T("ShowOriginalName"),
            Checked = showOriginalNameInDisplayName,
            ForeColor = UiStyle.TextColor,
            Location = new Point(126, 206),
            Size = new Size(360, 28),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true,
        };

        var okButton = new Button
        {
            Text = Localizer.T("Save"),
            DialogResult = DialogResult.OK,
            Location = new Point(346, 262),
            Size = new Size(86, 34),
        };
        UiStyle.StylePrimaryButton(okButton);

        var cancelButton = new Button
        {
            Text = Localizer.T("Cancel"),
            DialogResult = DialogResult.Cancel,
            Location = new Point(442, 262),
            Size = new Size(86, 34),
        };
        UiStyle.StyleSecondaryButton(cancelButton);

        Controls.AddRange(new Control[] { headerPanel, originalNameLabel, originalNameBox, remarkLabel, remarkBox, showOriginalNameCheckBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;

        originalNameBox.TextChanged += (_, _) => UpdateDisplayPreview();
        remarkBox.TextChanged += (_, _) => UpdateDisplayPreview();
        showOriginalNameCheckBox.CheckedChanged += (_, _) => UpdateDisplayPreview();

        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(OriginalName))
            {
                MessageBox.Show(Localizer.T("OriginalNameRequired"), Localizer.T("AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
    private readonly ComboBox languageBox;
    private readonly CheckBox contextMenuCheckBox;
    private readonly CheckBox startupCheckBox;
    private readonly CheckBox showOriginalNameCheckBox;

    public AppSettings Settings { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Settings = current.Clone();
        Icon = AppIcon.CreateIcon();
        Text = Localizer.T("SettingsTitle");
        UiStyle.StyleForm(this);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(540, 428);

        var headerPanel = new Panel
        {
            BackColor = SystemColors.Window,
            Location = new Point(0, 0),
            Size = new Size(540, 74),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var titleLabel = new Label
        {
            Text = Localizer.T("AppName"),
            ForeColor = UiStyle.TextColor,
            Font = UiStyle.HeaderFont,
            Location = new Point(32, 18),
            Size = new Size(456, 26),
        };

        var subtitleLabel = new Label
        {
            Text = Localizer.T("SettingsSubtitle"),
            ForeColor = UiStyle.MutedTextColor,
            Location = new Point(32, 46),
            Size = new Size(456, 20),
        };
        headerPanel.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });

        var hotkeyLabel = new Label
        {
            Text = Localizer.T("Hotkey"),
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 102),
            Size = new Size(90, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        hotkeyBox = new HotkeyCaptureBox
        {
            Hotkey = Settings.Hotkey,
            Location = new Point(136, 98),
            Size = new Size(372, 30),
        };

        var languageLabel = new Label
        {
            Text = Localizer.T("Language"),
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 140),
            Size = new Size(90, 24),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        languageBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(136, 136),
            Size = new Size(372, 30),
        };
        foreach (var language in Localizer.Languages)
            languageBox.Items.Add(new LanguageComboItem(language.Code, Localizer.GetLanguageDisplayName(language.Code)));
        languageBox.SelectedItem = languageBox.Items
            .OfType<LanguageComboItem>()
            .FirstOrDefault(item => item.Code.Equals(Settings.LanguageCode, StringComparison.OrdinalIgnoreCase))
            ?? languageBox.Items[0];

        var optionsGroup = new GroupBox
        {
            Text = Localizer.T("Options"),
            ForeColor = UiStyle.TextColor,
            Location = new Point(32, 190),
            Size = new Size(476, 116),
        };

        contextMenuCheckBox = new CheckBox
        {
            Text = Localizer.T("ContextMenuOption"),
            Checked = Settings.ContextMenuEnabled,
            ForeColor = UiStyle.TextColor,
            Location = new Point(18, 28),
            Size = new Size(440, 24),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true,
        };

        startupCheckBox = new CheckBox
        {
            Text = Localizer.T("StartupOption"),
            Checked = Settings.StartWithWindows,
            ForeColor = UiStyle.TextColor,
            Location = new Point(18, 56),
            Size = new Size(440, 24),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true,
        };

        showOriginalNameCheckBox = new CheckBox
        {
            Text = Localizer.T("ShowOriginalName"),
            Checked = Settings.ShowOriginalNameInDisplayName,
            ForeColor = UiStyle.TextColor,
            Location = new Point(18, 84),
            Size = new Size(440, 24),
            FlatStyle = FlatStyle.System,
            UseVisualStyleBackColor = true,
        };
        optionsGroup.Controls.AddRange(new Control[] { contextMenuCheckBox, startupCheckBox, showOriginalNameCheckBox });

        var hintLabel = new Label
        {
            Text = Localizer.T("Hint"),
            ForeColor = UiStyle.MutedTextColor,
            Location = new Point(32, 322),
            Size = new Size(476, 20),
        };

        var aboutButton = new Button
        {
            Text = Localizer.T("About"),
            Location = new Point(32, 374),
            Size = new Size(112, 32),
        };
        UiStyle.StyleSecondaryButton(aboutButton);
        aboutButton.Click += (_, _) => OpenProjectPage();

        var okButton = new Button
        {
            Text = Localizer.T("Save"),
            DialogResult = DialogResult.OK,
            Location = new Point(338, 374),
            Size = new Size(78, 32),
        };
        UiStyle.StylePrimaryButton(okButton);

        var cancelButton = new Button
        {
            Text = Localizer.T("Cancel"),
            DialogResult = DialogResult.Cancel,
            Location = new Point(430, 374),
            Size = new Size(78, 32),
        };
        UiStyle.StyleSecondaryButton(cancelButton);

        okButton.Click += (_, _) =>
        {
            if (hotkeyBox.Hotkey.KeyCode == Keys.None)
            {
                MessageBox.Show(Localizer.T("InvalidHotkey"), Localizer.T("AppName"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            Settings.Hotkey = hotkeyBox.Hotkey;
            Settings.LanguageCode = (languageBox.SelectedItem as LanguageComboItem)?.Code ?? "auto";
            Settings.ContextMenuEnabled = contextMenuCheckBox.Checked;
            Settings.StartWithWindows = startupCheckBox.Checked;
            Settings.ShowOriginalNameInDisplayName = showOriginalNameCheckBox.Checked;
        };

        Controls.AddRange(new Control[] { headerPanel, hotkeyLabel, hotkeyBox, languageLabel, languageBox, optionsGroup, hintLabel, aboutButton, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private static void OpenProjectPage()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/wantosure/FolderRemarkNameTool",
            UseShellExecute = true,
        });
    }
}

internal sealed record LanguageComboItem(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
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
    public string LanguageCode { get; set; } = "auto";

    public AppSettings Clone() => new()
    {
        Hotkey = Hotkey.Clone(),
        ContextMenuEnabled = ContextMenuEnabled,
        StartWithWindows = StartWithWindows,
        ShowOriginalNameInDisplayName = ShowOriginalNameInDisplayName,
        LanguageCode = LanguageCode,
    };
}

internal sealed class HotkeyGesture
{
    public static HotkeyGesture Default => new() { KeyCode = Keys.F2 };

    public Keys KeyCode { get; set; } = Keys.F2;
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
    public static readonly Font AppFont = new("Microsoft YaHei UI", 9.25f);
    public static readonly Font MenuFont = new("Microsoft YaHei UI", 9f);
    public static readonly Font TitleFont = new("Microsoft YaHei UI", 10.5f, FontStyle.Bold);
    public static readonly Font HeaderFont = new("Microsoft YaHei UI", 13.5f, FontStyle.Bold);
    public static readonly Color WindowBackColor = SystemColors.Control;
    public static readonly Color TextColor = SystemColors.ControlText;
    public static readonly Color MutedTextColor = SystemColors.GrayText;
    public static void StyleForm(Form form)
    {
        form.Font = AppFont;
        form.BackColor = WindowBackColor;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = true;
        button.ForeColor = Color.White;
        button.Font = AppFont;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = true;
        button.ForeColor = TextColor;
        button.Font = AppFont;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.Fixed3D;
        textBox.BackColor = SystemColors.Window;
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
        key?.SetValue("", Localizer.T("ContextSetRemark"));
        key?.SetValue("Icon", iconPath);
        key?.DeleteValue("MUIVerb", false);
        key?.DeleteValue("SubCommands", false);

        using var setRemarkCommandKey = Registry.CurrentUser.CreateSubKey(menuKey + @"\command");
        setRemarkCommandKey?.SetValue("", $"\"{exePath}\" --set-remark \"{folderArgument}\"");
        CrashLog.WriteMessage($"安装设置备注菜单：{menuKey}");
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
            ?? throw new InvalidOperationException(Localizer.T("RenameParentFailed"));

        var currentName = Path.GetFileName(folderPath);
        if (string.Equals(currentName, requestedName, StringComparison.Ordinal))
            return folderPath;

        if (requestedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException(Localizer.T("InvalidFileName"));

        var targetPath = Path.Combine(parentPath, requestedName);
        if (Directory.Exists(targetPath) || File.Exists(targetPath))
            throw new InvalidOperationException(Localizer.T("NameExists"));

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
        var originalFolderAttributes = File.GetAttributes(folderPath);

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

        File.SetAttributes(
            desktopIniPath,
            (originalIniAttributes | FileAttributes.Hidden | FileAttributes.System) & ~FileAttributes.ReadOnly);

        EnsureShellFolderAttributes(folderPath, originalFolderAttributes);
    }

    private static void EnsureShellFolderAttributes(string folderPath, FileAttributes originalFolderAttributes)
    {
        var requiredAttributes = FileAttributes.ReadOnly | FileAttributes.System;
        if ((originalFolderAttributes & requiredAttributes) != 0)
            return;

        try
        {
            File.SetAttributes(folderPath, originalFolderAttributes | FileAttributes.ReadOnly);
        }
        catch (UnauthorizedAccessException)
        {
            File.SetAttributes(folderPath, originalFolderAttributes | FileAttributes.System);
        }
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
