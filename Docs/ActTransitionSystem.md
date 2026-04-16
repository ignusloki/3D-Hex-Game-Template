# Act Transition System

Last updated: 2026-04-16

## Purpose

This document defines the first real multi-act run flow for the game.

It covers:

- when acts end
- what the end-of-act transition screen must do
- how boon selection works across acts
- how the Act 3 nemesis is locked
- how act-specific map generation profiles should behave
- what run state must persist between maps

This is the design source of truth for the upcoming act-transition implementation.

## Implementation Slice Status

### Slice 1: Multi-act skeleton

Status: done

Delivered:

- persistent run session across scene reloads
- transition modal between acts
- resource carryover
- editor-configurable between-act grants
- Act 2 nemesis suppression
- final victory still ending the run in Act 3

### Slice 2: Act-specific map generation profiles

Status: next

Target:

- act-profile authoring support
- desert-biased Act 2 generation
- general Act 1 / Act 3 generation profiles

### Slice 3: Transition boon selection and stacking

Status: pending

Target:

- real boon choice in both transitions
- family lock from the first choice
- stacked boon runtime by Act 3

### Slice 4: Act 3 nemesis family lock

Status: pending

Target:

- Act 3 nemesis archetype driven by the family locked after Act 1
- removal of the current temporary dependence on scene nemesis setup

## High-Level Run Structure

The intended production run is now:

1. Play Act 1.
2. Reach the goal.
3. Show an act transition screen.
4. Choose a boon for Act 2.
5. Generate and play Act 2.
6. Reach the goal.
7. Show another act transition screen.
8. Choose a boon for Act 3.
9. Generate and play Act 3.
10. Reach the goal.
11. End the run.

For now, the implementation can stay in the same gameplay scene and regenerate the board in place.

This feature is about multi-act runtime flow, not scene-cinematic polish.

## Act Rules

### Act 1

- Uses the current general map profile.
- No special biome theme is required.
- The run begins here.
- Reaching the goal does not end the whole game anymore once the multi-act system is implemented.

### Transition: Act 1 -> Act 2

The player should see a transition screen that includes:

- story / vision / omen text
- boon choice UI
- temporary presentation is acceptable for the first implementation

This first boon choice does two things:

- grants the boon that will modify Act 2
- locks the boon family that will define the Act 3 nemesis archetype

That family lock must also restrict which boon options become available at the next transition.

### Act 2

- No nemesis is active in Act 2.
- The map is still procedurally generated.
- The map uses a desert-biased generation profile.
- Any accumulated boon effects must apply to Act 2 generation and runtime.

#### Act 2 map profile

The Act 2 map should be desert-dominant.

Target biome direction:

- desert is the main biome
- forest is nearly absent: `0` to `2` tiles total is acceptable
- water is rare, but not as rare as forest
- mountains remain allowed
- exact tuning should remain editable in the Unity editor

The system should support these values through data and settings, not hardcoded constants embedded across gameplay code.

### Transition: Act 2 -> Act 3

The second transition screen follows the same structure:

- story text
- boon choice UI
- temporary presentation is acceptable at first

This second boon choice does not change the locked family.

It does:

- add the Act 3 boon from the already locked family
- finalize the player's stacked boon loadout for the last act

### Act 3

- Uses a general procedural map profile again
- no special biome theme is required by default
- accumulated boon effects still apply
- the Act 3 nemesis is enabled
- the nemesis archetype must match the family locked by the first boon choice

Act 3 is the final act.

Reaching the goal in Act 3 ends the game.

## Boon Rules Across Acts

### Core rules

- Boons stack across acts.
- The system must support more than one active boon at runtime.
- Boons are authored per archetype family.
- The first choice determines the family for the rest of the run.
- The second choice must come from that same family.

### Content constraints

- The runtime system must support Hunter, Echo, and Corruptor boon families.
- Hunter-only content is acceptable for early testing, but the architecture must not assume Hunter forever.
- Boons are designed so stacked combinations should not conflict by design.

### Act timing

- The boon chosen after Act 1 modifies Act 2.
- The boon chosen after Act 2 modifies Act 3.
- By Act 3, both selected boons are active unless a specific boon is explicitly one-act-only in future content.

## Nemesis Rules Across Acts

- There is no nemesis in Act 2.
- The Act 3 nemesis family is locked by the first transition choice.
- The second transition does not override that family lock.
- The current existing nemesis archetypes remain the supported families:
  - Hunter
  - Echo
  - Corruptor

## Resource Carryover

Resources carry between acts.

That includes:

- Food
- Morale
- Gold

In addition, the caravan receives a between-act resource grant before the next act starts.

This grant must stay editor-configurable so balancing can change later without rewriting code.

The first implementation should treat between-act resource grants as data, not fixed hardcoded bonuses.

## Quest / Map-Object Interaction

- Quest markers only need to affect act generation if a boon or related content requests them.
- The act-transition system does not require a full quest system to ship its first slice.
- It should, however, preserve compatibility with boon-driven map-object requests and future quest-state output.

## Required Runtime State

The act-transition system needs a persistent run-state model that can survive map regeneration between acts.

At minimum it should track:

- current act number
- accumulated selected boons
- locked boon family / future nemesis family
- whether Act 3 nemesis should be enabled
- caravan resources carried between acts
- act-specific map profile to generate next
- any configured between-act resource grant

## Required Authoring Direction

The first implementation should remain data-driven where practical.

At minimum, authoring should support:

- act generation profiles
- transition-specific boon pools
- between-act resource grants
- family restriction for the second boon selection

This does not require final cinematic UI or final narrative writing yet.

## First Implementation Boundaries

The first implementation does need:

- multi-act run progression
- transition screens between acts
- boon selection at both transitions
- resource carryover plus configurable between-act grants
- Act 2 desert-biased generation
- Act 3 general generation
- Act 3 nemesis enablement based on the family locked after Act 1

The first implementation does not need:

- final cinematic presentation
- full quest system
- final boon content for every family on day one
- final narrative text polish

## Relationship To Existing Docs

- The broader map-generation modifier and placement infrastructure is documented in `Docs/MapGenerationRefactor.md`.
- The overall current prototype and game overview is documented in `Docs/HexGame.md`.
- The active implementation backlog should reference this document from `Docs/ProjectTasks.md`.
