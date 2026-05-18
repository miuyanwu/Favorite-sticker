#!/usr/bin/env bash
# Claude Code status line script
# Displays: model name | context usage percentage | input/output tokens

input=$(cat)

# Model name
model=$(echo "$input" | jq -r '.model.display_name // empty')

# Context window percentages
used_pct=$(echo "$input" | jq -r '.context_window.used_percentage // empty')
remaining_pct=$(echo "$input" | jq -r '.context_window.remaining_percentage // empty')

# Token values (format as K for readability)
total_in=$(echo "$input" | jq -r '.context_window.total_input_tokens // empty')
total_out=$(echo "$input" | jq -r '.context_window.total_output_tokens // empty')

# Format token numbers
format_tokens() {
    local val=$1
    if [ -z "$val" ] || [ "$val" = "null" ]; then
        echo ""
        return
    fi
    if [ "$val" -ge 1000000 ]; then
        echo "$(echo "scale=1; $val / 1000000" | bc)M"
    elif [ "$val" -ge 1000 ]; then
        echo "$(echo "scale=1; $val / 1000" | bc)K"
    else
        echo "$val"
    fi
}

in_fmt=$(format_tokens "$total_in")
out_fmt=$(format_tokens "$total_out")

# Build output parts
parts=""

# Model name
if [ -n "$model" ]; then
    parts="$model"
fi

# Context percentage
if [ -n "$used_pct" ] && [ "$used_pct" != "null" ]; then
    if [ -n "$parts" ]; then
        parts="$parts | "
    fi
    parts="$parts${used_pct}%"
elif [ -n "$remaining_pct" ] && [ "$remaining_pct" != "null" ]; then
    if [ -n "$parts" ]; then
        parts="$parts | "
    fi
    parts="$parts${remaining_pct}% remaining"
fi

# Token details
if [ -n "$in_fmt" ] || [ -n "$out_fmt" ]; then
    if [ -n "$parts" ]; then
        parts="$parts | "
    fi
    parts="${parts}In: ${in_fmt}"
    if [ -n "$out_fmt" ]; then
        parts="${parts} Out: ${out_fmt}"
    fi
fi

echo "$parts"
