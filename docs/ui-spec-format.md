# UI spec format

This document defines the format for UI specification documents that describe the console layout using a screenshot and a line-by-line reference.

## Purpose

UI specs document what appears on each line of the console so that implementers, testers, and agents can refer to exact row numbers and know what each row represents. Screenshots come from the [screen-dump](../adr/0046-screen-dump-ascii-screenshot.md) feature (ASCII screenshot).

## Layout and alignment

Layout follows [ADR-0050](../adr/0050-ui-alignment-blocks-label-format.md):

- **Left alignment:** UI is left-aligned; padding is on the right. Content starts at column 0 (or at the start of its region).
- **8-character blocks:** Components use space in 8-character (column) blocks. Label components default to 8 columns for the label and 8 for the value unless specified otherwise in the spec.
- **Label format:** Labels are formatted as `Label:value` (colon immediately after the label, no space before the value).

## Format structure

1. **Title and context** (optional): Short title and when this layout applies (e.g. main view, preset mode).
2. **Screenshot**: The raw ASCII screenshot in a fenced code block, exactly as captured (no line numbers in the block).
3. **Line reference**: After the screenshot, a section "Line reference" (or "Line-by-line reference") where **every line** of the screenshot is listed by **line number** with a **description**.

## Screenshot block

- Use a fenced code block (e.g. ```text or ```).
- Paste the screen-dump content as-is; do not add line numbers inside the block.
- Preserve leading/trailing spaces if they are meaningful for layout.

## Line reference section

- List every line from the screenshot, starting at line 1.
- Use the format: **`NN`** — description.
- Keep descriptions concise; one line per screen line.
- If a line is blank or padding, say so (e.g. "Blank line" or "Padding").

## Example structure (skeleton)

```markdown
# UI spec: [Name]

[Optional context.]

## Screenshot

\`\`\`text
... paste screen-dump content here ...
\`\`\`

## Line reference

- **1** — Description of line 1.
- **2** — Description of line 2.
...
```

## Where to use this format

- New UI layout specs in `docs/` (e.g. `docs/ui-spec-main-view.md`).
- When documenting a specific screen or modal for which a screen dump exists.
- When updating an existing UI spec, regenerate the screenshot and line reference from a fresh dump so they stay in sync.

## Generating a UI spec from a screen dump

1. Run the app (or use `--dump-after N` and `--dump-path` to capture automatically).
2. Trigger a screen dump (Ctrl+Shift+E or Print Screen per ADR-0046) to get a `.txt` file in `screen-dumps/`.
3. Paste the file contents into the "Screenshot" code block.
4. Add a "Line reference" section and describe each line (1 to N) based on the actual content and the [UI components](ui-components.md) (title bar, header, toolbar, visualizer area, etc.).
