# Folder Remark Name Tool

中文 | [English](#english)

一个 Windows 托盘小工具，用来给资源管理器里的文件夹设置“备注名”。真实文件夹名不变，备注显示名通过 `desktop.ini` 保存。

它特别适合这类场景：很多软件、游戏或开发工具要求安装目录必须是纯英文、不能包含中文或特殊符号，但你又希望在资源管理器里看到一个好记的中文名。这个工具可以让真实路径继续保持 `Absolum` 这样的英文目录，同时在资源管理器里显示为 `绝对魔权（Absolum）`。备注名写入文件夹后长期有效，即使本软件关闭，也能继续显示。

![文件夹备注名使用说明](docs/images/usage-guide.png)

## 下载安装

普通用户不需要编译源码。请到 [Releases](https://github.com/wantosure/FolderRemarkNameTool/releases) 下载最新的 `FolderRemarkNameTool-*-win-x64.zip`，解压后运行 `FolderRemarkNameTool.exe`。

如果 Windows 提示未知发布者，这是因为当前版本没有代码签名证书。确认来源是本仓库 Release 后再运行。

## 功能

- 在 Windows 资源管理器中选中文件夹后，用快捷键打开备注窗口
- 默认快捷键为 `F4`，也支持在设置中自定义快捷键
- 可同时修改真实文件夹名和备注名
- 可选择显示为 `备注名（原名）`
- 保持真实路径为英文/无特殊符号，同时在资源管理器显示中文备注名
- 备注写入后长期有效，软件退出后仍可显示
- 支持多语言界面，默认跟随系统，可在设置中切换
- 默认加入资源管理器文件夹右键菜单，也支持在设置中移除
- 支持开机自动启动
- 备注写入目标文件夹内的 `desktop.ini`

## 使用说明

1. 启动 `FolderRemarkNameTool.exe`。
2. 在 Windows 资源管理器中选中一个文件夹。
3. 按快捷键，输入备注名并保存。
4. 也可以右键文件夹，选择“设置文件夹备注名”。

## 设置

托盘图标右键选择“设置”，可以修改：

- 快捷键
- 界面语言
- 是否加入资源管理器文件夹右键菜单
- 是否开机自动启动
- 是否在备注名后面用括号显示原名
- 关于本软件：点击后打开 GitHub 项目主页

![设置界面](docs/images/settings.png)

## 实现说明

Windows 资源管理器没有开放“显示名”和“真实文件夹名”完全分离的普通插件接口。本工具使用 Windows Shell 支持的 `desktop.ini` 机制写入显示名。

因为显示名保存在目标文件夹自己的 `desktop.ini` 中，所以它不是临时覆盖层，也不依赖本工具持续运行。设置成功后，即使退出本工具，资源管理器仍会按照 Windows Shell 机制显示备注名。

单独的 `F4` 默认会被资源管理器用于地址栏/导航栏，因此本工具对 `F4` 使用轻量键盘钩子，只在资源管理器前台时处理。带修饰键的组合快捷键使用 `RegisterHotKey`。

## 限制

- 仅支持 Windows。
- 仅处理文件夹，不处理普通文件。
- 某些受保护目录可能需要更高权限才能写入 `desktop.ini` 或设置文件夹属性；遇到权限失败时可以选择以管理员权限重试。
- Windows 可能缓存图标或显示名，必要时需要刷新或重启 Explorer。

## 构建

需要 .NET 6 Windows Desktop SDK。

```powershell
dotnet build .\FolderRemarkNameTool.csproj -c Release -p:Platform=x64
```

发布示例：

```powershell
dotnet publish .\FolderRemarkNameTool.csproj -c Release -p:Platform=x64
```

## 开源许可

本项目为个人开源项目，允许个人学习、研究、修改和非商业使用，禁止任何商业用途。详见 [LICENSE](LICENSE)。

---

## English

[中文](#folder-remark-name-tool) | English

A small Windows tray utility for assigning a remark display name to folders in File Explorer. The real folder name stays unchanged, while the display name is stored through `desktop.ini`.

It is useful when software, games, or development tools require install paths to be plain English without Chinese characters or special symbols, but you still want a readable Chinese display name in File Explorer. For example, the real folder can stay as `Absolum`, while File Explorer shows `绝对魔权（Absolum）`. The display name is persistent after it is written, and it still works when this tool is closed.

![Folder Remark Name Tool usage guide](docs/images/usage-guide.png)

## Download

End users do not need to build from source. Download the latest `FolderRemarkNameTool-*-win-x64.zip` from [Releases](https://github.com/wantosure/FolderRemarkNameTool/releases), extract it, and run `FolderRemarkNameTool.exe`.

Windows may show an unknown publisher warning because this project is not code-signed yet. Only run builds downloaded from this repository's Releases page.

## Features

- Open the remark dialog from File Explorer with a keyboard shortcut
- Default shortcut is `F4`; custom shortcuts are supported in settings
- Edit both the real folder name and the remark name
- Optional display format: `Remark name (Original name)`
- Keep real paths plain English / symbol-safe while showing a readable remark name in File Explorer
- Persistent display after writing; the tool does not need to keep running
- Multi-language UI, follows the system language by default and can be changed in settings
- Explorer folder context menu enabled by default, removable from settings
- Optional startup on Windows login
- Stores remarks in the target folder's `desktop.ini`

## Usage

1. Start `FolderRemarkNameTool.exe`.
2. Select a folder in Windows File Explorer.
3. Press the configured shortcut, enter a remark name, and save.
4. You can also right-click a folder and choose "Set folder remark name".

## Settings

Right-click the tray icon and choose "Settings" to configure:

- Keyboard shortcut
- UI language
- Explorer folder context menu
- Start with Windows
- Show original name in parentheses after the remark name
- About this app: opens the GitHub project page

![Settings](docs/images/settings.png)

## How It Works

Windows File Explorer does not provide a normal plugin API that fully separates a folder's display name from its real filesystem name. This tool uses the Windows Shell `desktop.ini` mechanism to write the display name.

Because the display name is stored in the target folder's own `desktop.ini`, it is not a temporary overlay and does not depend on this tool staying open. After the remark is saved, File Explorer can continue showing it through the Windows Shell mechanism even when this tool is closed.

Plain `F4` is commonly used by File Explorer for the address/navigation bar, so this tool handles it with a lightweight keyboard hook only when File Explorer is in the foreground. Shortcuts with modifier keys use `RegisterHotKey`.

## Limitations

- Windows only.
- Folders only; regular files are not supported.
- Protected folders may require elevated permissions to write `desktop.ini` or set folder attributes; when permission fails, you can retry as administrator.
- Windows may cache icons or display names, so Explorer refresh or restart may be required in some cases.

## Build

.NET 6 Windows Desktop SDK is required.

```powershell
dotnet build .\FolderRemarkNameTool.csproj -c Release -p:Platform=x64
```

Publish example:

```powershell
dotnet publish .\FolderRemarkNameTool.csproj -c Release -p:Platform=x64
```

## License

This is a personal open-source project. Personal learning, research, modification, and non-commercial use are allowed. Commercial use is prohibited. See [LICENSE](LICENSE).
