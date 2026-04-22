# UI POLISH TASK - ACT TRANSITION FLOW

Scope: polish 2 screens only
Target resolution: 1920x1080
Direction: Option A - journal / chapter-page
Important: this is a visual polish pass, not an architecture pass

## GOAL

Polish the 2-screen act transition flow so both screens:
- feel like the same UI family
- look intentional and aesthetically beautiful
- match the game's caravan / travel-journal tone
- remove prototype feel
- reduce redundant information
- improve readability and hierarchy

## FLOW TO KEEP

Do not merge screens.
Keep this exact 2-screen flow:
1. Act Intermission / Story Page
2. Boon Selection Page

## FONT RULES

Test Rimouski on these 2 screens, but do NOT use it everywhere.

USE RIMOUSKI ONLY FOR:
- main screen titles
- section headers / eyebrow labels
- boon card titles
- button labels
- resource chips
- family tags
- keyword chips / short keyword labels

DO NOT USE RIMOUSKI FOR:
- narrative paragraph text
- story body text
- inspect panel body copy
- glossary explanation text
- long helper text
- long descriptions

FOR ALL BODY TEXT:
Keep the current readable body font already in the project.
Body text must remain clean, readable, and neutral.

## VISUAL DIRECTION

Both screens must share one visual language:
- journal / chapter page
- warm paper surface
- dark ink text
- muted brass accent
- restrained, elegant, readable
- not ornate
- not a generic dark modal
- not a fantasy parchment cliche
- not app-dashboard UI

## SHARED COLOR PALETTE

Main page background:
- warm paper tone
- use one of these values as base:
  - #E3DCCF
  - #DDD5C6
- optional slightly darker variation:
  - #D3CAB8

Primary text:
- dark ink / charcoal
- use:
  - #2A2925
  - #2F312E

Secondary text:
- muted dark neutral
- use:
  - #5B554A
  - #645E53

Accent:
- aged brass / muted gold
- use:
  - #A88952
  - #9B7C46

Dividers / borders:
- muted paper-shadow line
- use:
  - #B9AE99
  - #B2A58E

Overlay behind modal/page:
- dark desaturated dim
- use roughly:
  - rgba(10, 12, 16, 0.72)

## RESOURCE CATEGORY TINTS

Food:
- muted brown-gold
Morale:
- muted dusty wine / muted rose
Gold:
- muted ochre / brass

Do not use bright saturated colors.

## SHARED SHAPE RULES

- page corner radius: 12 px
- card corner radius: 8 px
- button corner radius: 8 px
- chips: pill shape
- no heavy ornament
- no glossy styling

## SHARED SPACING RULES

- outer page padding: 32 px
- spacing between major sections: 24 px
- spacing between smaller elements: 12-16 px
- internal padding in cards/panels: 16 px

## SHARED TYPOGRAPHY SIZES

Eyebrow / small section label:
- 12 px
Rimouski section subhead:
- 20-24 px
Rimouski main title:
- 36 px
Body text:
- 16-18 px
Small metadata:
- 12-14 px
Buttons:
- 16-18 px

## GLOBAL RULES

- Hide or strongly suppress the gameplay HUD during both screens
- The map remains visible behind a dark overlay
- Do not display visible "placeholder" text inside art blocks
- If art is missing, use a quiet framed placeholder area with no literal placeholder label

==================================================
SCREEN 1 - ACT INTERMISSION / STORY PAGE
==================================================

## PURPOSE

A chapter break between acts.
This screen should feel reflective, clean, and narrative-first.

## LAYOUT

Use one centered page panel.

Panel size:
- width: 800 px
- height: content-driven, around 620-700 px

## STRUCTURE

From top to bottom:
1. Eyebrow label
2. Main title
3. Thin divider
4. Short story text
5. Image block
6. Travel provisions summary
7. Primary button

## HEADER

Eyebrow:
- format like: ROAD INTERMISSION - ACT 1 TO ACT 2
- Rimouski
- 12 px
- muted tone

Main title:
- format like: Act 1 Complete
- Rimouski
- 36 px

## BODY STORY TEXT

Keep only 1 short narrative paragraph.
Target:
- 2 to 4 lines max
- clean, readable, no excess lore

Example structure:
"The caravan reaches the first goal. A harder road opens ahead."

## IMAGE BLOCK

Keep the image area.
Do not remove it.
Do not show the word "placeholder".

Image block metrics:
- full content width
- height: 210 px
- soft framed inset look
- subtle border
- mild tonal separation from page

## TRAVEL PROVISIONS SUMMARY

Show only:
- Carry-over
- Fresh supplies / Between-act grant

Use this structure:
- section label: Travel provisions
- row 1: Carry-over
- row 2: Fresh supplies

Each row uses compact resource chips.

Resource chip metrics:
- height: 30 px
- horizontal padding: 14 px
- text size: 13-14 px

Do not use large stat boxes.
Do not create dashboard-looking panels.

## PRIMARY BUTTON

Label:
- Choose a Boon

Metrics:
- width: 170 px
- height: 44 px
- centered at bottom of page

## BUTTON STYLE

- dark ink background
- warm light text
- subtle hover lift
- no oversized styling

## SCREEN 1 TEXT CLEANUP

Remove unnecessary duplication.
Do not repeat act context more than needed.
Do not let the top HUD compete with this page.

==================================================
SCREEN 2 - BOON SELECTION PAGE
==================================================

## PURPOSE

This is the second page of the same chapter break.
It must look like part of the same journal system.

## IMPORTANT

Do NOT keep the current generic dark modal look.
Convert this screen to the same warm journal/page visual language as Screen 1.

## PANEL SIZE

Use one centered wide page panel.

Panel metrics:
- width: 1040 px
- height: content-driven, around 700 px

## TOP AREA

Left side:
- main title
- one short support line

Right side:
- compact carry-over strip

## HEADER TEXT

Main title:
- "Choose one boon for Act 2"
- Rimouski
- 30-34 px

Support line:
- "This choice determines the family for Act 3."
- body font
- 16 px

Do not repeat this same information elsewhere unless strictly necessary.

## CARRY-OVER STRIP

Show Food / Morale / Gold in a compact strip.
It must be visually light and secondary.
Do not use a heavy framed box.

Use:
- small chips or mini stat pills
- aligned to top-right
- same category color logic as Screen 1

## BOON CARD AREA

Keep 3 cards in one row.

Card metrics:
- width: 235 px
- height: 350 px
- gap between cards: 16 px

## CARD STRUCTURE

From top to bottom:
1. Boon title
2. Image block
3. Short keyword summary or keyword chips
4. Family tag

Do NOT put long effect text on the card face.

## CARD CONTENT RULES

Allowed on card:
- boon title
- 1 short keyword summary OR 2 small keyword chips
- family tag

Not allowed on card:
- full rule paragraph
- lore paragraph
- long explanation text

## CARD TITLE

- Rimouski
- 20-24 px
- allow wrapping to 2 lines max

## CARD IMAGE BLOCK

- rectangular block
- no visible placeholder label
- quiet framed surface

## KEYWORD SUMMARY

Use one of these two options:
A. one short text line, such as:
   "Detour + Recharge"
B. small keyword chips:
   "Detour"  "Recharge"

Use Rimouski only if keywords stay short and readable.
Otherwise use body font for the chips.

## FAMILY TAG

One compact bottom tag only:
- example: Hunter Family
- use muted brass accent
- small and clean
- do not make it dominant

## CARD STATES

Hover:
- subtle elevation
- slight brightness increase
- slight border emphasis

Selected:
- stronger border emphasis using brass accent
- clear visual focus
- slightly stronger shadow or tonal lift

Do not use exaggerated animation.

==================================================
INSPECT PANEL - HIGH PRIORITY REDESIGN
==================================================

## PROBLEM TO FIX

The current explanation area is too messy and paragraph-heavy.
The keyword explanations need stronger structure.

## NEW INSPECT PANEL RULE

Below the cards, keep one detail panel.
It must be clean, editorial, and highly readable.

## INSPECT PANEL STRUCTURE

In this exact order:
1. Small label
2. Selected boon title
3. Keyword row
4. One plain-language summary sentence
5. Keyword glossary rows
6. Optional flavor line
7. Small family / consequence note

## INSPECT PANEL METRICS

- width: full available content width
- internal padding: 20 px
- spacing between sections: 12-14 px

## CONTENT RULES

1. Small label
- text: "Selected boon" or "Inspect"
- small, muted

2. Boon title
- larger
- Rimouski
- 28 px

3. Keyword row
- example: Keywords: Detour, Recharge
- can be inline text or small chips
- keep compact

4. Plain-language summary sentence
- one clean summary only
- example:
  "Ignore the penalty from one obstacle tile, then restore the boon at the next pitstop."

5. Keyword glossary rows
This is the key change.
Replace the current paragraph/italic block with short labeled rows.

Use this format:
- Detour - Ignore the penalty from one obstacle tile.
- Recharge - Restore the boon at the next pitstop.

Design them as stacked short rows.
Do not format them as a dense paragraph.
Do not use a long italic explanation block.

6. Optional flavor line
If used:
- one short line only
- visually secondary
- separated from the rules
- do not let it dominate

7. Family / consequence note
Show this only once in the inspect panel if needed.
Example:
- Family: Hunter
- This choice determines the Act 3 family.

Keep it small and quiet.

## DEFAULT EMPTY STATE

Before hover/selection:
- show a simple default message:
  "Select a boon to inspect it."

## BEGIN BUTTON

Keep one primary action button below the inspect panel:
- label: Begin Act 2
- width: 170 px
- height: 44 px
- centered

==================================================
REDUNDANCY REMOVAL
==================================================

REMOVE OR REDUCE THESE:
- repeated family information in multiple places
- repeated "this locks Act 3" notes in several locations
- repeated long descriptions
- repeated rule text both on the card and in the inspect panel

The player should be able to answer only 3 questions:
1. What am I choosing?
2. What does it do?
3. What consequence does it have later?

Anything outside those 3 should be reduced.

==================================================
SUCCESS CRITERIA
==================================================

This pass is successful if:
1. Screen 1 and Screen 2 clearly look like the same journal/chapter-page UI family.
2. Screen 1 feels cleaner, warmer, and less sterile.
3. Screen 2 no longer looks like a generic dark modal.
4. Rimouski improves titles and UI accents without hurting readability.
5. Rimouski is NOT used for long body text.
6. The boon cards are easy to compare at a glance.
7. The inspect panel is clean and structured.
8. Keyword explanations are shown as short glossary rows, not messy paragraphs.
9. Redundant text is removed.
10. The whole flow feels like a beautiful caravan chapter break.

==================================================
DO NOT DO
==================================================

- Do not merge the 2 screens
- Do not revert to a one-screen layout
- Do not use Rimouski for paragraphs or long descriptions
- Do not keep Screen 2 as a generic dark modal
- Do not show visible placeholder text
- Do not add heavy fantasy ornament
- Do not add unnecessary nested boxes
- Do not put full rule text back inside the cards
- Do not let the HUD visually compete with these screens
