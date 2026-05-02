# Act Transition UI Reference

Last updated: 2026-05-02

## Purpose

This document records the current implemented UI direction for the act-end flow.
It is no longer a task prompt. Use it as a regression/reference note when polishing
the act-complete and boon-selection screens.

## Runtime Flow

The act-end flow remains two separate screens:

1. Act Complete intermission screen
2. Boon Selection screen

The screens are implemented in UI Toolkit inside the shared gameplay UI root modal
layer. They use the shared UI transition system rather than local one-off animation
loops.

## Shared Visual Direction

- Parchment / journal UI language.
- Dark subdued map/gameplay visible behind modal overlays.
- Rimouski is used for display titles and important button text only.
- Body copy remains in the readable project UI font.
- Layout should remain clean, minimal, and editorial rather than ornate.

## Act Complete Screen

Current content order:

1. Main title: `Act 1 Complete` or `Act 2 Complete`
2. Large hero image for the next biome/act
3. One short supporting sentence
4. Thin divider
5. Two-column resource summary
6. `Choose a Boon` CTA

Current key rules:

- No eyebrow text above the title.
- No `Travel provisions` label.
- No resource chips/pills on this screen.
- Hero image remains the primary visual anchor.
- Resource summary uses two columns:
  - `Current Resources`
  - `Act Reward`
- CTA remains centered at the bottom of the modal.

## Boon Selection Screen

Current content structure:

1. Header row with title and carry-over resource text
2. Three boon image choices
3. Fixed-height inspect panel
4. Centered `Begin Act 2` / `Begin Act 3` button

Current key rules:

- Boon cards were simplified into image-focused choices.
- Each boon image comes from the boon ScriptableObject so art can be changed in the
  Unity editor without code changes.
- Family icon also comes from the boon ScriptableObject.
- The inspect panel is fixed and should not resize while hovering/selecting.
- The proceed button must remain disabled until the player selects a boon.
- Hover updates inspect content; click sets the selected boon.
- Image spacing should remain clear enough for the three choices to read as separate
  selectable objects.

## Asset Authoring Notes

Relevant boon asset fields:

- boon portrait / card image
- family icon image

Current placeholder family icon assets:

- `Assets/Art/Image/Placeholder/tile_0110.png`
- `Assets/Art/Image/Placeholder/tile_0121.png`
- `Assets/Art/Image/Placeholder/tile_0122.png`

## Regression Checklist

- Act Complete screen starts directly with the main title.
- Act Complete hero image loads for both act transitions.
- Resource summary remains stat-style, not chip-style.
- Boon selection button starts disabled until a boon is selected.
- Boon images and family icons load from boon data assets.
- Inspect panel does not resize or jump on hover.
- Act-to-act map reload is hidden behind the shared global fade.
