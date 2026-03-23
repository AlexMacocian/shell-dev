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

    # Adjust geometry for fractional scaling.
    # wf-recorder fails with "Failed to copy frame" when dimensions × scale
    # produce non-integer buffer sizes. Round down to the nearest safe value.
    SCALE=$(hyprctl monitors -j | jq -r '.[] | select(.focused) | .scale')
    GEOMETRY=$(echo "$GEOMETRY" | awk -v s="${SCALE:-1}" '{
        # Parse "X,Y WxH"
        split($1, pos, ",")
        split($2, dim, "x")
        # Find denominator: smallest q where s*q is integer
        for (q = 1; q <= 120; q++) {
            v = s * q
            if (v - int(v) < 0.001 && int(v) + 0.001 > v) break
        }
        # Round W and H down to nearest multiple of q
        w = int(dim[1] / q) * q
        h = int(dim[2] / q) * q
        printf "%s,%s %dx%d", pos[1], pos[2], w, h
    }')

    # Start recording
    mkdir -p "$RECORDINGS_DIR"
    FILENAME="$RECORDINGS_DIR/recording-$(date +%Y%m%d-%H%M%S).mp4"
    touch /tmp/wf-recorder-running
    pkill -RTMIN+9 waybar
    notify-send "Screen Recording" "Recording started..."
    # Run wf-recorder in a subshell so cleanup runs automatically when it exits
    (setsid wf-recorder -g "$GEOMETRY" -f "$FILENAME" > /dev/null 2>&1;
     rm -f /tmp/wf-recorder-running; pkill -RTMIN+9 waybar) &
    disown
fi
