# Folder Remark Name Tool

一个 Windows 托盘小工具，用来给资源管理器里的文件夹设置“备注名”。真实文件夹名不变，备注显示名通过 `desktop.ini` 保存。

## 功能

- 在资源管理器中选中文件夹后，用快捷键打开备注窗口
- 默认快捷键为 `F4`
- 可编辑真实文件夹名和备注名
- 可选择显示为 `备注名（原名）`
- 支持资源管理器右键菜单
- 支持开机自启动
- 备注写入目标文件夹内的 `desktop.ini`

## 使用

1. 启动 `FolderRemarkNameTool.exe`。
2. 在 Windows 资源管理器中选中一个文件夹。
3. 按快捷键，输入备注名并保存。
4. 如启用右键菜单，也可以右键文件夹选择“设置文件夹备注名”。

## 设置

托盘图标右键选择“设置”，可以修改：

- 快捷键
- 是否加入资源管理器文件夹右键菜单
- 是否开机自动启动
- 是否在备注名后用括号显示原名

## 实现说明

Windows 资源管理器没有开放“显示名”和“真实文件夹名”完全分离的普通插件接口。本工具使用 Windows Shell 支持的 `desktop.ini` 机制写入显示名。

单独的 `F4` 默认会被资源管理器用于地址栏/导航栏，因此本工具对 `F4` 使用轻量键盘钩子，只在资源管理器前台时拦截。带修饰键的组合键使用 `RegisterHotKey`。

## 限制

- 仅支持 Windows。
- 仅处理文件夹，不处理普通文件。
- 因为备注名是 Shell 显示名，资源管理器中的 `F2` 行为可能仍受 Windows Explorer 机制影响。
- 某些受保护目录可能需要更高权限才能写入 `desktop.ini`。
- Windows 可能缓存图标或显示名，必要时需要刷新或重启 Explorer。

## 构建

需要 .NET 6 Windows Desktop SDK。

```powershell
dotnet build .\FolderRemarkNameTool.csproj -c Release -p:Platform=x64
```

构建输出：

```text
bin\x64\Release\net6.0-windows\FolderRemarkNameTool.exe
```

## 开源许可

MIT License
