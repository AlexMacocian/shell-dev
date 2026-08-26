import QtQuick
import Quickshell
import Quickshell.Io

// Control Center tile for switching themes: glue between omni-shell and
// rainbeau, belonging to neither.
//
// Both are standalone projects — omni-shell knows nothing about rainbeau, and
// rainbeau treats omni-shell as just another config target. This file is where
// this machine's setup joins them, and it lives in the dotfiles for that reason.
//
// omni-shell discovers it in ~/.config/omni-shell/extensions/. Delete it and the
// tile disappears; nothing else changes.
QtObject {
    id: root

    property string label: "Theme"

    // md-palette, the glyph the old wofi power menu used for "Change Theme".
    // Referenced by codepoint: Nerd Font codepoints are not guessable from the
    // glyph name.
    property string glyph: String.fromCodePoint(0xF03D8)

    property string mnemonic: "T"

    // `rainbeau select` is a fullscreen overlay that reads the keyboard, and the
    // Control Center holds an exclusive keyboard grab while open. Without this
    // the picker would appear but never receive a keystroke.
    property bool closesPanel: true

    // A theme switch is an action, not a state that can be "on".
    property bool active: false

    property string themeName: ""
    property string subtitle: themeName !== "" ? themeName : "Not themed"

    function activate() {
        pickerProc.running = true;
    }

    function _apply(text) {
        if (!text || text.trim().length === 0) {
            themeName = "";
            return;
        }
        try {
            const config = JSON.parse(text);
            themeName = (typeof config.themeName === "string") ? config.themeName : "";
        } catch (error) {
            // Half-written during a theme apply; keep the previous name rather
            // than blanking the tile for a frame.
        }
    }

    // rainbeau writes themeName into the shell config it generates, so the name
    // costs no extra state and updates the moment a theme is applied.
    property FileView configFile: FileView {
        path: (Quickshell.env("XDG_CONFIG_HOME") || (Quickshell.env("HOME") + "/.config")) + "/omni-shell/config.json"
        preload: true
        blockLoading: true
        watchChanges: true
        printErrors: false
        onFileChanged: reload()
        onLoaded: root._apply(text())
        onLoadFailed: root._apply("")
    }

    // No --themes-dir: rainbeau defaults to ~/.config/rainbeau/themes, which is
    // where init-symlinks.sh links this repo's themes.
    property Process pickerProc: Process {
        command: ["rainbeau", "select"]
        running: false
    }
}
