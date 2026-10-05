import QtQuick
import Quickshell
import Quickshell.Io

// Control Center tile for the general AI workspace: glue between omni-shell and
// sherlock, belonging to neither.
//
// omni-shell knows nothing about sherlock, and sherlock knows nothing about the
// desktop. This file is where this machine's setup joins them, and it lives in
// the dotfiles for that reason.
//
// omni-shell discovers it in ~/.config/omni-shell/extensions/. Delete it and the
// tile disappears; nothing else changes.
QtObject {
    id: root

    property string label: "AI"
    property string subtitle: "General workspace"

    // cod-copilot. Referenced by codepoint: Nerd Font codepoints are not
    // guessable from the glyph name.
    property string glyph: String.fromCodePoint(0xEC1E)

    // N B V D E are the built-in tiles, L S M R P the power row, A/F/S the
    // expanded Wi-Fi and Bluetooth sections, T the theme extension.
    property string mnemonic: "C"

    // kitty takes keyboard focus the moment it maps, and the Control Center
    // holds an exclusive Wayland keyboard grab while open. Without this the
    // terminal would appear but swallow nothing — every keystroke would still
    // go to the panel.
    property bool closesPanel: true

    // Opening a workspace is an action, not a state that can be "on".
    property bool active: false

    // The workspace is a symlink into the shell-dev repo, created by
    // linux/init-symlinks.sh. On a machine where that has not run yet the tile
    // hides itself rather than offering a terminal that would open in a
    // directory that does not exist.
    readonly property string workspace: (Quickshell.env("XDG_DATA_HOME") || (Quickshell.env("HOME") + "/.local/share")) + "/ai"
    property bool available: false

    function activate() {
        // execDetached, not a Process: the terminal has to outlive the shell
        // that spawned it, and a Process child dies with omni-shell.
        //
        // --directory is what makes this work at all. sherlock resolves its
        // project root by walking up from the working directory, and it execs
        // the agent without changing directory, so the workspace has to be the
        // cwd before sherlock starts.
        Quickshell.execDetached(["kitty", "--directory", root.workspace, "--class", "ai-workspace", "sherlock", "copilot"]);
    }

    // sherlock.toml is the file that marks the workspace as a project root, so
    // its presence is exactly the condition for the tile being useful.
    property FileView marker: FileView {
        path: root.workspace + "/sherlock.toml"
        preload: true
        blockLoading: true
        watchChanges: true
        printErrors: false
        onFileChanged: reload()
        onLoaded: root.available = true
        onLoadFailed: root.available = false
    }
}
