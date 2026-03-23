#!/usr/bin/env bash
# Outputs JSON for waybar custom/recording module.
# Signal 9 (RTMIN+9) triggers a refresh.

if [ -f /tmp/wf-recorder-running ]; then
    echo '{"text": "\uf111 REC", "tooltip": "Screen recording in progress", "class": "on"}'
else
    echo '{"text": "", "tooltip": "", "class": "off"}'
fi
