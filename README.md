# Favorite Sticker

轻量级 Windows 剪贴板管理器，基于 C# WPF (.NET 8) 构建。

## 功能

| 功能 | 说明 |
|------|------|
| 剪贴板监听 | 自动捕获文本和图片，历史上限 200 条，连续重复自动合并 |
| 收藏夹 + 子文件夹 | 多级文件夹分类，收藏内容永久保留、不受上限约束 |
| 星标置顶 | 一键星标，星标内容全局置顶，支持「只看星标」视图 |
| 实时搜索 | 搜索文本内容、标签名、文件夹名 |
| 窗口置顶 | 图钉按钮切换窗口始终最前 |
| 悬停预览 | 鼠标悬停显示 Tooltip（文本前 200 字符 / 图片缩略图） |
| 智能清理 | 5 条自动清理规则：时间、条数、去重、大小过滤、分类批量 |
| 定时提醒 | 内容到期提醒 + 定时自动粘贴，Windows Toast 通知 |
| 开机自启动 | HKCU 注册表，无需管理员权限，可开关 |
| 全局热键 | 默认 `Alt+V`，支持自定义组合键 |
| 跟随系统主题 | 自动适配 Windows 深色/浅色模式 |
| 窗口可调整大小 | 拖拽边框自由调整 |

## 安装

### 环境要求

- Windows 10 19041+ 或 Windows 11
- .NET 8.0 Desktop Runtime（[下载](https://dotnet.microsoft.com/download/dotnet/8.0)）

### 构建 & 运行

```bash
# 克隆或进入项目目录
cd Favorite-sticker

# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行
dotnet run --project src/FavoriteSticker
```

### 发布为单文件

```bash
dotnet publish src/FavoriteSticker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

发布后在 `./publish/FavoriteSticker.exe` 获得单文件可执行程序。

## 使用指南

### 基础操作

| 操作 | 方式 |
|------|------|
| 呼出窗口 | 按 `Alt+V`（可在设置中自定义） |
| 隐藏窗口 | 点击窗口右上角 ✕，或按 `Alt+V` 切换 |
| 复制内容 | 点击列表中的条目，内容自动写入剪贴板，然后 `Ctrl+V` 粘贴 |
| 预览内容 | 鼠标悬停在条目上，显示 Tooltip 预览 |
| 搜索 | 在顶部搜索框输入关键词，实时过滤 |

### 收藏夹

- **左侧边栏** 显示收藏夹文件夹树
- 点击 `+ Folder` 创建文件夹
- 将条目**拖入文件夹**即可收藏
- 收藏内容**不受 200 条上限约束**，永久保留

### 星标

- 点击条目左侧的 ☆ 按钮即可星标
- 星标内容在列表中**全局置顶**
- 点击搜索框旁的 ☆ 按钮**只看星标**

### 窗口置顶

- 点击标题栏的 📌 图钉按钮切换窗口置顶

### 托盘图标

- 应用启动后自动缩小到**系统托盘**
- 右键托盘图标：显示窗口 / 设置 / 退出
- 双击托盘图标：显示窗口

### 设置

点击标题栏 ⚙ 按钮进入设置：

| 设置项 | 说明 |
|--------|------|
| Hotkey | 自定义全局热键，点击 Change 后按下组合键录制 |
| Max History | 剪贴板历史上限（默认 200） |
| Max Age | 超过 N 天自动清理（默认 30） |
| Max Text Size | 超过 N 字符的文本自动跳过 |
| Max Image Size | 超过 N MB 的图片自动跳过 |
| Duplicate Merging | 连续相同内容合并为一条，显示复制次数 |
| Auto Start | 开机自启动开关 |
| Data Path | 数据存储路径（默认 `%LocalAppData%\FavoriteSticker`） |

### 定时提醒

通过 ReminderService 创建两类提醒：
- **内容到期提醒**：设定时间，到点弹 Windows Toast 通知
- **定时粘贴**：设定时间自动将指定内容复制到剪贴板并通知

## 项目结构

```
src/FavoriteSticker/
├── Models/           # 数据模型
│   ├── ClipboardItem.cs
│   ├── Folder.cs
│   ├── Reminder.cs
│   └── AppConfig.cs
├── Services/         # 业务逻辑（10 个模块）
│   ├── DataStore.cs          # SQLite + JSON 持久化
│   ├── ClipboardMonitor.cs   # Win32 剪贴板监听
│   ├── CleanupEngine.cs      # 5 条清理规则引擎
│   ├── HotkeyManager.cs      # RegisterHotKey 全局热键
│   ├── AutoStartService.cs   # HKCU 注册表自启动
│   ├── ReminderService.cs    # 定时提醒 + Toast 通知
│   ├── FavoriteManager.cs    # 收藏夹文件夹树
│   ├── StarManager.cs        # 星标操作
│   └── SearchService.cs      # 实时搜索
├── ViewModels/       # MVVM ViewModel
│   └── MainViewModel.cs
├── Views/            # WPF XAML 视图
│   ├── MainWindow.xaml/.cs
│   ├── SettingsWindow.xaml/.cs
│   ├── InputDialog.xaml/.cs
│   └── Themes.xaml
└── Helpers/          # Win32 P/Invoke 封装
    ├── Win32.cs
    └── TrayIcon.cs

tests/FavoriteSticker.Tests/  # 36 项单元测试
```

## 技术栈

| 组件 | 用途 |
|------|------|
| .NET 8 + WPF | 桌面 UI 框架 |
| Dapper | 轻量级 SQLite ORM |
| Microsoft.Data.Sqlite | SQLite 数据库 |
| CommunityToolkit.Mvvm | MVVM 工具包 |
| System.Drawing.Common | 托盘图标生成 |
| xUnit + Moq | 单元测试框架 |

## 数据存储

- **SQLite 数据库**：`%LocalAppData%\FavoriteSticker\favorite-sticker.db`
  - `clipboard_items` — 剪贴板条目
  - `folders` — 收藏夹文件夹树
  - `reminders` — 定时提醒
- **JSON 配置**：`%LocalAppData%\FavoriteSticker\config.json`
- **图片文件**：`%LocalAppData%\FavoriteSticker\images\`
- **缩略图**：`%LocalAppData%\FavoriteSticker\thumbnails\`

可在设置中自定义数据存储路径。

## License

MIT
