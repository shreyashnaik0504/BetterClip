```markdown
# Better Clip

A modern, high-performance, and lightweight clipboard manager designed natively for Windows 11. Built with **C#**, **.NET 10**, and **WPF**, Better Clip combines native system integration with an ultra-clean, modern UI to elevate your daily productivity.

---

## ⚡ Overview

Windows' built-in clipboard history is functional, but lacks advanced categorization, instant visual previews, and flexible background utility management. **Better Clip** runs silently in your system tray, listens for clipboard changes with zero perceptible overhead, and appears instantly via a global hotkey to paste items straight into your active application.

---

## ✨ Features

- **Global Hotkey Summon:** Press `Ctrl + Space` from any app or window to instantly open or hide your clipboard deck.
- **Direct Paste Injection:** Clicking any clipboard item hides Better Clip and immediately types or pastes the content via synthetic keystrokes into your target window.
- **Loop-Shielded Monitoring:** Built with internal write-guard flags to intercept Win32 `WM_CLIPBOARDUPDATE` messages cleanly, preventing self-duplication or recursive clip looping.
- **Multi-Type Categorization & Smart Badges:**
  - 📝 **Plain Text:** Formatted cleanly with item length truncation and age counters.
  - `</>` **Code Snippets:** Automatic detection of programming syntax, rendered in monospace block wrappers.
  - 🖼️ **Images & Screenshots:** Snipping Tool (`Win + Shift + S`) integration with local SHA-256 caching and in-card thumbnail rendering.
  - 🔗 **Links & URLs:** Direct web link badges showing destination domain names.
  - 📂 **Files & Documents:** Distinct extension badges (e.g., PDF, DOCX, ZIP) with file sizes.
  - 🎬 **Video Media:** Video clip tags and path tracking.
- **Pinning & History Pruning:** Star/pin important snippets so they stay at the top and avoid automatic cleanup.
- **System Tray Life-Cycle:**
  - Runs in the background without cluttering your taskbar.
  - Right-click tray menu to toggle display, toggle Windows autostart (`Run` registry integration), view About info, or cleanly exit.
- **Keyboard-First Controls:**
  - `Ctrl + F`: Jump straight into search.
  - `Enter`: Paste the first matching result.
  - `Esc`: Clear search filter or dismiss the window.

---

## 🛠️ Tech Stack & Architecture

- **Language:** C# 13 / .NET 10
- **Framework:** Windows Presentation Foundation (WPF)
- **APIs & Interop:**
  - `user32.dll`: `AddClipboardFormatListener`, `RegisterHotKey`, `keybd_event`, `SetForegroundWindow`, `TrackPopupMenu`
  - `shell32.dll`: `Shell_NotifyIcon` for background tray lifecycle
  - `dwmapi.dll`: `DwmSetWindowAttribute`, `DwmExtendFrameIntoClientArea`
  - `gdi32.dll`: Native bitmap and icon conversion

---

## 🚀 Getting Started

### Prerequisites

- **OS:** Windows 10 (1809+) or Windows 11 (x64)
- **SDK:** [.NET 10.0 SDK](https://dotnet.microsoft.com/)

### Installation & Local Run

1. **Clone the repository:**
   ```powershell
   git clone [https://github.com/shreyashnaik0504/BetterClip.git](https://github.com/shreyashnaik0504/BetterClip.git)
   cd BetterClip

```

2. **Restore dependencies & build:**
```powershell
dotnet build src\BetterClip.UI\BetterClip.UI.csproj

```


3. **Run the application:**
```powershell
dotnet run --project src\BetterClip.UI\BetterClip.UI.csproj

```



---

## 🎮 How to Use

| Shortcut / Action | Function |
| --- | --- |
| **`Ctrl + Space`** | Toggle Better Clip open/closed from anywhere. |
| **`Ctrl + F`** | Focus and select the search bar to filter clips. |
| **Left Click Card** | Copy payload and paste directly into your active window. |
| **`★` (Star Icon)** | Pin the item permanently to the top of your deck. |
| **`•••` (More Menu)** | Open the context menu to copy, pin, or delete individual items. |
| **`🗑` (Trash Icon)** | Bulk-delete all unpinned items. |
| **`Esc`** | Clear search field if typed; dismiss window if empty. |
| **Right-Click Tray Icon** | Access autostart settings, view About info, or Quit. |

---

## 📦 Directory Structure

```text
BetterClip/
├── Assets/
│   └── app_logo.png
├── src/
│   └── BetterClip.UI/
│       ├── Assets/
│       │   └── app_logo.png
│       ├── MainWindow.cs
│       ├── Program.cs
│       └── BetterClip.UI.csproj
├── LICENSE
└── README.md

```

---

## 👨‍💻 Author

**Better Clip** was created and developed by **Shreyash Naik**.

* **GitHub:** [@shreyashnaik0504](https://www.google.com/search?q=https://github.com/shreyashnaik0504)
* **Project:** [Better Clip Repository](https://www.google.com/search?q=https://github.com/shreyashnaik0504/BetterClip)

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](https://www.google.com/search?q=LICENSE) file for complete details.

```

```
