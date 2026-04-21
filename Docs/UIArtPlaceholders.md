# UI Art Placeholders

This document tracks the current UI locations that are using placeholder art panels instead of final illustrations.

## 1. Act Transition Screen 1 - Intermission Journal Image

- Location:
  - `HexRunStateModalPresenter` intermission screen
  - `HexActTransitionStepDefinition.intermissionIllustration`
- Purpose:
  - one landscape illustration for the act-complete / road-intermission story page
- Current live slot:
  - outer framed block: approximately `676 x 236`
  - visible image area inside the frame: approximately `632 x 192`
- Orientation:
  - landscape / banner
- Recommended source export:
  - at least `1896 x 576` to keep a clean 3x source for the current slot
- Notes:
  - the UI now shows a `placeholder` panel when no art is assigned
  - this is the main authored image for screen 1 and should carry the mood of the transition

## 2. Act Transition Screen 2 - Boon Card Illustration Area

- Location:
  - `HexRunStateModalPresenter` boon selection cards
  - currently a presenter-level placeholder panel inside each boon card
- Purpose:
  - one illustration per boon card
- Current live slot per card:
  - visible image area inside the card: approximately `180 x 140`
- Orientation:
  - compact landscape panel inside a portrait card
- Recommended source export:
  - at least `540 x 420` to keep a clean 3x source for the current slot
- Notes:
  - there are 3 simultaneous card art slots on screen
  - the UI now shows `placeholder` instead of reusing the boon icon in the art area
  - if final boon art should be data-driven per boon, add a dedicated illustration field to `HexBoonDefinition` instead of reusing small icon assets

## Current Scope

These are the current non-icon UI art slots found in the act transition flow.
