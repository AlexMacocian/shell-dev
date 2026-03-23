#!/usr/bin/env bash
# Toggle screen recording with wf-recorder.
# First invocation starts recording; second invocation stops it.
# Recordings are saved to ~/Documents/Recordings/.
# Uses slurp for region selection on start.

RECORDINGS_DIR="$HOME/Documents/Recordings"

if pgrep -x wf-recorder > /dev/null; then
    # Stop recording
    pkill -INT -x wf-recorder
    rm -f /tmp/wf-recorder-running
    pkill -RTMIN+9 waybar
    notify-send "Screen Recording" "Recording saved to $RECORDINGS_DIR"
else
    # Select region with slurp (exits if user presses Escape)
    GEOMETRY=$(slurp 2>/dev/null) || exit 0

    # Start recording
    mkdir -p "$RECORDINGS_DIR"
    FILENAME="$RECORDINGS_DIR/recording-$(date +%Y%m%d-%H%M%S).mp4"
    touch /tmp/wf-recorder-running
    pkill -RTMIN+9 waybar
    notify-send "Screen Recording" "Recording started..."
    setsid wf-recorder -g "$GEOMETRY" -f "$FILENAME" > /dev/null 2>&1 &
    disown
fi
