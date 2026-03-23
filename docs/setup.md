# Setup

## Windows

```powershell
# Install dependencies (via winget/choco)
.\windows\init-deps.ps1

# Set up git and SSH keys
.\windows\init-git.ps1

# Create symlinks for configs
.\windows\init-symlinks.ps1
```

## Linux

```bash
# Install packages (pacman + AUR + dotnet tools)
bash linux/init-deps.sh

# Symlinks, SDDM autologin
bash linux/init-symlinks.sh

# Firefox: chrome symlinks, theme extension, native messaging host
bash linux/init-firefox.sh

# Apply a theme
bash linux/apply-theme.sh "Scarlet Rot" --restart
```

### Machine-specific

After running `init-symlinks.sh`, edit `~/.config/hypr/monitors.conf` for your
display layout. This file is gitignored.

### Theming

Themes live in `themes/*.json`. Apply with:

```bash
# By name
bash linux/apply-theme.sh "Scarlet Rot" --restart

# List available
bash linux/list-themes.sh

# Or from the desktop: SUPER + X → Change Theme
```

See [theme-engine.md](theme-engine.md) and [theme-json.md](theme-json.md) for details.
