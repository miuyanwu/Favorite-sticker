# PRD: Favorite Sticker — Windows 轻量级剪贴板管理器

## Problem Statement

Windows 内置的剪贴板历史（Win+V）功能有限：不支持收藏夹分类管理、不支持星标置顶、不支持定时提醒、历史条数受限且无法自定义清理策略。用户需要一个轻量级但功能完备的剪贴板管理工具，能够高效组织、检索和复用日常工作中频繁复制的内容（文本、图片），并通过智能清理策略保持存储精简不膨胀。

## Solution

**Favorite Sticker** 是一款 Windows 原生剪贴板管理器，采用 WPF 构建，以系统托盘 + 热键弹窗形态运行。核心能力：后台监听剪贴板内容变化（文本 + 图片），自动记录历史（上限 200 条），用户可将有价值内容归档到多级收藏夹永久保存，通过星标实现全局置顶快速访问，支持智能清理规则自动去重和淘汰，提供内容到期提醒和定时粘贴提醒。

## User Stories

### 剪贴板监听与历史记录
1. As a user, I want the app to automatically capture text I copy (Ctrl+C), so that I don't need to manually save every piece of content.
2. As a user, I want the app to automatically capture images I copy (screenshots, browser image copy), so that image content is also included in my history.
3. As a user, I want the history to be capped at 200 items, with the oldest non-favorited items evicted first, so that storage doesn't grow indefinitely.
4. As a user, I want favorited items to be protected from automatic eviction, so that valuable content is never lost due to the 200-item limit.
5. As a user, I want consecutive duplicate copies to be merged into a single entry with a copy count indicator, so that my history list stays clean.
6. As a user, I want to see a thumbnail preview of image entries in the history list, so that I can visually identify images without opening them.
7. As a user, I want to hover over any history item to see a Tooltip preview (first 200 chars for text, enlarged thumbnail for images), so that I can verify content without clicking into it.

### 收藏夹与分类
8. As a user, I want to create a multi-level folder structure to organize my saved content, so that I can categorize items by project, topic, or usage scenario.
9. As a user, I want to drag an item from the history list into a folder in the favorites tree, so that I can quickly organize content.
10. As a user, I want to rename, delete, and reorder folders in the favorites tree, so that I can evolve my organization system over time.
11. As a user, I want favorited content to have no storage limit, so that I can build a long-term knowledge repository without worrying about caps.
12. As a user, I want to see the favorites tree in the left sidebar and the content area on the right, so that I have a clear spatial model of navigation and content.

### 星标置顶
13. As a user, I want to star any item (whether in history or favorites), so that frequently-used content is always at my fingertips.
14. As a user, I want all starred items to appear pinned at the top of the right-side content list, regardless of which folder is currently selected in the left sidebar, so that I never have to search for them.
15. As a user, I want a one-click "Starred Only" filter view that shows exclusively starred items, so that I can quickly narrow down to my most important content.
16. As a user, I want to un-star an item to remove it from the global pinned section, so that pins don't accumulate stale entries.

### 搜索
17. As a user, I want a real-time search box that filters the content list as I type, matching against text content, tag names, and folder names, so that I can instantly find what I'm looking for.
18. As a user, I want to combine search with the "Starred Only" filter, so that I can search within my pinned items.

### 粘贴操作
19. As a user, I want to click on an item to copy it to the clipboard, so that I can then manually paste (Ctrl+V) into my target application.
20. As a user, I want the paste operation to work reliably without the app attempting to simulate keystrokes, so that I am always in control of the paste action.

### 窗口控制
21. As a user, I want to press Alt+V (or a custom hotkey) to toggle the main popup window from anywhere in Windows, so that I can access my clipboard manager without using the mouse.
22. As a user, I want to configure the global hotkey to a different key combination, so that I can avoid conflicts with other applications.
23. As a user, I want a pushpin button on the popup window to toggle "always on top" mode, so that I can keep it visible while working in another application.
24. As a user, I want the popup window to be resizable, so that I can adjust it to fit my screen and workflow.
25. As a user, I want the app to minimize to the system tray (not the taskbar) when I close the window, so that it stays running without cluttering my workspace.
26. As a user, I want to right-click the tray icon to access a context menu with options like "Show", "Settings", and "Exit", so that I can control the app from the tray.
27. As a user, I want the app to automatically follow Windows light/dark theme, so that it integrates visually with my system.

### 清理策略
28. As a user, I want to configure a time-based auto-cleanup rule (e.g., delete history items older than 30 days), so that stale content is automatically removed.
29. As a user, I want to configure a size threshold for content (especially images), so that overly large items are automatically skipped and not stored.
30. As a user, I want to batch-delete items by selecting multiple entries and pressing Delete, so that I can manually prune my history.
31. As a user, I want a "Clear All Non-Favorited" one-click action, so that I can reset my history while keeping saved content intact.
32. As a user, I want to batch-delete items within a specific favorites folder, so that I can clean up entire categories at once.
33. As a user, I want the auto-cleanup rules (time threshold, size threshold, duplicate merging) to be configurable in the Settings window, so that I can tune the behavior to my needs.

### 定时提醒
34. As a user, I want to set a future time reminder on a specific favorited item, so that the app notifies me (via Windows toast) when it's time to use that content.
35. As a user, I want to schedule a "timed paste" — automatically copy a specified item to the clipboard at a future time and notify me, so that I don't forget to use a specific piece of content at the right moment.
36. As a user, I want to view and cancel pending reminders, so that I can manage what's scheduled.

### 自启动
37. As a user, I want the app to automatically start when I log into Windows (using HKCU registry, no admin required), so that I never forget to launch it.
38. As a user, I want to toggle auto-start on/off in the Settings window, so that I can disable the behavior if I no longer need it.
39. As a user, I want the app to start silently and minimize to the system tray, so that auto-start is non-intrusive.

### 配置与自定义
40. As a user, I want to choose the installation directory during setup, so that I can control where the app lives on my disk.
41. As a user, I want to configure the data storage path (where images and database are stored), so that I can point it to a location with sufficient disk space.

## Implementation Decisions

### Technology Stack
- **Framework**: C# WPF (.NET 8/9), targeting Windows 10/11
- **UI**: WPF with MVVM pattern (CommunityToolkit.Mvvm)
- **ORM**: Dapper for SQLite access (lightweight, fast)
- **Database**: SQLite via System.Data.SQLite or Microsoft.Data.Sqlite
- **Configuration**: JSON files (System.Text.Json)
- **Packaging**: MSIX or ClickOnce installer, allowing custom install path

### Module Architecture

Ten modules with clear responsibility boundaries. "Deep modules" are marked with (D) — these encapsulate significant complexity behind a simple interface and are the primary unit testing targets.

| # | Module | Responsibility | Deep? |
|---|--------|---------------|-------|
| 1 | **ClipboardMonitor** | Win32 clipboard API wrapping, text/image capture, thumbnail generation | D |
| 2 | **DataStore** | SQLite init/migration/CRUD, JSON config read/write, data path resolution | D |
| 3 | **FavoriteManager** | Multi-level folder tree CRUD, sort, move operations | D |
| 4 | **StarManager** | Star/unstar/toggle, global pinned list query | — |
| 5 | **CleanupEngine** | Rule scheduling, threshold evaluation, dedup merging, batch delete | D |
| 6 | **ReminderService** | Scheduled tasks, Windows Toast notifications, timed clipboard push | D |
| 7 | **SearchService** | Real-time text search across content/tags/folders, starred filter | — |
| 8 | **AutoStartService** | HKCU registry read/write, enable/disable toggle | D |
| 9 | **HotkeyManager** | Win32 RegisterHotKey wrapper, hotkey rebinding, conflict detection | D |
| 10 | **UIShell** | Tray icon, main popup window, settings window, pushpin toggle; thin composition layer over modules 1–9 | — |

### Dependency Graph

```
UIShell ──→ ClipboardMonitor, FavoriteManager, StarManager, SearchService,
             CleanupEngine, ReminderService, AutoStartService, HotkeyManager

ClipboardMonitor  → DataStore
FavoriteManager   → DataStore
StarManager       → DataStore
SearchService     → DataStore
CleanupEngine     → DataStore
ReminderService   → DataStore
AutoStartService  → (standalone, registry-only)
HotkeyManager     → (standalone, Win32-only)
```

All modules are wired via dependency injection. No module directly instantiates another — UIShell resolves and composes them.

### Data Schema (SQLite — key tables)

**clipboard_items**
- `id` (TEXT, PK, GUID)
- `content_type` (INTEGER, 0=text, 1=image)
- `text_content` (TEXT, nullable)
- `image_path` (TEXT, nullable — file system path)
- `thumbnail_path` (TEXT, nullable)
- `char_count` / `file_size` (INTEGER — for size threshold cleanup)
- `copy_count` (INTEGER, for dedup merging)
- `content_hash` (TEXT, for dedup detection)
- `is_favorite` (INTEGER, 0/1)
- `is_starred` (INTEGER, 0/1)
- `folder_id` (TEXT, nullable, FK → folders)
- `created_at` (TEXT, ISO 8601)
- `last_copied_at` (TEXT, ISO 8601)

**folders**
- `id` (TEXT, PK, GUID)
- `name` (TEXT)
- `parent_id` (TEXT, nullable, FK → folders, for tree structure)
- `sort_order` (INTEGER)

**reminders**
- `id` (TEXT, PK, GUID)
- `item_id` (TEXT, FK → clipboard_items)
- `remind_at` (TEXT, ISO 8601)
- `reminder_type` (INTEGER, 0=content reminder, 1=timed paste)
- `is_fired` (INTEGER, 0/1)

### Configuration (JSON)

```jsonc
{
  "hotkey": {
    "modifiers": "Alt",
    "key": "V"
  },
  "cleanup": {
    "max_history_count": 200,
    "max_age_days": 30,
    "max_text_chars": 10000,
    "max_image_size_bytes": 5242880,
    "dedup_enabled": true
  },
  "auto_start": {
    "enabled": true
  },
  "storage": {
    "data_path": "%AppData%\\FavoriteSticker"
  }
}
```

### Cleanup Engine Rules (priority order)
1. **Size gate**: On capture — skip items exceeding configured text char limit or image byte limit
2. **Dedup**: On capture — if content hash matches the most recent item, increment its `copy_count` instead of inserting a new row
3. **Count cap (FIFO)**: After each insert — if count > 200, delete oldest items where `is_favorite=0 AND is_starred=0`
4. **Age-based**: On scheduled timer — delete items where `created_at` older than `max_age_days` AND `is_favorite=0 AND is_starred=0`
5. **Manual batch**: Deletion by user selection or "Clear All Non-Favorited" command

### Hotkey & Window Lifecycle
- `Alt+V` registered via `RegisterHotKey` Win32 API on app start
- Hotkey press → toggle main popup window (show/hide)
- Window close button → hide to tray (not exit)
- Tray "Exit" → unregister hotkey, dispose resources, exit process
- Pushpin toggle → set `WS_EX_TOPMOST` on the popup window
- Auto-complete when window loses focus (configurable, default off — user asked for explicit click-to-copy)

### Thumbnail Generation
- On image capture, generate a 120x120 thumbnail saved alongside the original
- Use WPF built-in image resizing (BitmapDecoder + BitmapFrame)
- Thumbnail path stored in `thumbnail_path` column

### Reminder Mechanism
- In-process timer (System.Timers.Timer) polling the reminders table every 60 seconds
- On fire: if type=content_reminder → Windows Toast notification with item preview
- On fire: if type=timed_paste → set item content to clipboard + Windows Toast notification
- Fired reminders marked `is_fired=1`, retained for 7 days then cleaned

### Auto-start Implementation
- Registry path: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- Value name: `FavoriteSticker`
- Value data: path to the application executable
- No admin privileges required
- Toggle in Settings removes/re-adds the registry entry

## Testing Decisions

### Testing Philosophy
- Tests verify external behavior, not implementation details
- Deep modules are tested in isolation via unit tests with mocked dependencies
- Shallow modules (thin orchestrators) are covered by integration tests
- No tests that assert on internal state or private method calls

### Unit Test Targets (xUnit + Moq)

| Module | What's Tested |
|--------|--------------|
| **ClipboardMonitor** | Correct detection of text vs image clipboard formats; thumbnail generation produces expected dimensions; content hash computation is deterministic |
| **DataStore** | CRUD operations for items/folders/reminders; migration scripts run idempotently; JSON config round-trip (read→modify→write→read) |
| **CleanupEngine** | Each of the 5 rules triggers under correct conditions; FIFO respects is_favorite/is_starred protection; dedup correctly merges consecutive identical content; size gate rejects oversized items; age rule uses correct date comparison |
| **ReminderService** | Reminder fires at correct time; Windows Toast notification is triggered for both reminder types; timed paste sets clipboard content |
| **AutoStartService** | Enable writes correct registry entry; disable removes it; toggle is idempotent |
| **HotkeyManager** | RegisterHotKey succeeds with valid key combo; unregister cleans up; rebind updates internal state correctly; invalid combo is rejected |

### Integration Test Targets
- Full capture→store→retrieve→paste flow
- Cleanup engine + DataStore combined: real SQLite, verify rows deleted per rules
- Settings window: change config → save → restart → config persists
- AutoStartService: real registry read/write (run on dev machine only)

## Out of Scope

- Cloud sync / multi-device sync
- Clipboard sharing over network
- Rich text / HTML content (plain text only from rich text sources)
- File copy monitoring (copying files in Explorer)
- Password / sensitive content auto-detection and masking
- Plugin system or scripting
- Linux / macOS support
- Localization (i18n) — Chinese UI for now, English may follow
- Accessibility features beyond standard WPF accessibility

## Further Notes

- The app name "Favorite Sticker" is a working title and can be changed before release.
- The primary use case is Chinese-speaking Windows users who frequently copy and reuse text snippets, code blocks, account information, addresses, and screenshots in their daily workflow.
- Performance target: popup window should appear within 300ms of hotkey press; clipboard capture should not block the system clipboard (use clipboard viewer chain, not polling).
- The app should not interfere with Windows' own clipboard history (Win+V) — it runs independently.
