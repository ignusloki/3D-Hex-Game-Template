# UI Art Slots

Last updated: 2026-05-02

## Purpose

This document tracks current UI art slots that should be authored or replaced
through data/assets rather than hardcoded presenter changes.

## Act Complete Hero Images

Location:

- `HexActTransitionStepDefinition.intermissionIllustration`

Purpose:

- landscape hero image for the act-complete intermission screen

Current behavior:

- the image is shown inside the Act Complete modal
- the UI crops/scales the image to the configured frame
- missing art should show a quiet framed surface with no visible placeholder text

Current placeholder guidance:

- Act 1 -> Act 2 may use `Placeholder01.png` / `Placeholder02.png`
- Act 2 -> Act 3 may use `Placeholder03.png` / `Placeholder04.png`

## Boon Choice Images

Location:

- boon ScriptableObject image field on `HexBoonDefinition`

Purpose:

- the selectable boon image shown in the boon-selection screen

Current behavior:

- each boon image comes from boon data
- images are not hardcoded in presenter code
- the selection UI uses the image as the primary choice object
- hover updates inspect content
- click selects the boon

## Boon Family Icons

Location:

- boon ScriptableObject family icon field on `HexBoonDefinition`

Purpose:

- small family identity icon used with boon/family display

Current placeholder icon assets:

- `Assets/Art/Image/Placeholder/tile_0110.png`
- `Assets/Art/Image/Placeholder/tile_0121.png`
- `Assets/Art/Image/Placeholder/tile_0122.png`

## Victory Hero Image

Location:

- Victory overlay UI

Current asset:

- `S_2.png`

Purpose:

- hero image inside the Victory page

## Main Menu Logo

Location:

- main menu UI

Current asset:

- `Logo.png`

Purpose:

- title/logo graphic on the parchment main menu panel

## Reference Images

- `Docs/Example.png` is retained as the visual reference for the final map screen
  UI check.
