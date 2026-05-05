# Run State Architecture Plan

Last updated: 2026-05-05

## Purpose

This document defines task 5 from the production-readiness audit: introduce a
real run/game state architecture.

The current game loop is playable, but state ownership is still spread across
`PlayerController`, `HexActTransitionService`, modal presenters, bootstrap
logic, and several runtime helpers. That makes regressions hard to reason about
because movement, act transitions, defeat, UI modal state, boon state, and scene
reload state are controlled by different objects.

The goal is not to rewrite the game. The goal is to make the runtime state
explicit enough that a human or AI can verify one turn, one act transition, and
one run end without reconstructing hidden static state.

## Current Problems

- `PlayerController` owns too many gameplay concerns: input gating, current
  tile, selected tile, resources, move resolution, pitstop arrival, obstacle
  turns, nemesis turns, act completion, run end, and UI updates.
- `HexActTransitionService` stores the active run session in static state.
  This works across scene reloads, but it hides ownership and makes tests/order
  of operations fragile.
- Input gating is scattered across UI modal checks instead of one game-flow
  state.
- Movement and turn resolution are tightly coupled to UI presentation.
- Debug preview tools mutate real act-transition state.
- Run data is not represented as one serializable model, which blocks clean
  save/load later.

## Target Architecture

### `HexRunState`

Create a serializable model that owns the current run facts:

- current act number
- resources
- selected boons
- locked boon/nemesis family
- current caravan coordinates
- goal coordinates
- run phase
- run outcome if finished
- pending modal context when needed

This should be plain data first. It should not depend on `MonoBehaviour`.

### `HexGameFlowController`

Create one flow owner for high-level runtime phase:

- `Booting`
- `MainMenu`
- `PreparingGameplay`
- `AwaitingPlayerInput`
- `ResolvingMove`
- `PitstopChoice`
- `ActTransition`
- `Victory`
- `Defeat`

This controller should answer questions such as "can gameplay input run now?"
instead of each controller checking every modal.

### `HexRunSessionController`

Own the active `HexRunState`, bridge scene reloads, and replace the current
static session storage in `HexActTransitionService`.

This can start simple:

- one active in-memory run state
- survives current scene reload flow
- exposes explicit methods for start run, advance act, end run, reset run

Save/load can use this later, but save/load is not part of this task.

### `HexTurnResolver`

Move turn resolution out of `PlayerController`:

- spend movement food
- update caravan coordinates
- refresh fog
- resolve obstacle contact
- process nemesis turn
- process pitstop arrival/recharge
- detect defeat/victory

The resolver should return a result object. UI presenters should consume that
result rather than the resolver directly updating HUD text.

### `PlayerController` After Refactor

`PlayerController` should become an input and visual bridge:

- click/select tile
- request move from flow/session controller
- attach caravan/goal visuals
- refresh highlights
- forward tile-inspector requests

It should not own act-transition session state or decide run lifecycle outcomes.

## Proposed Slices

### Slice 1: Introduce State Types Without Behavior Changes

Add:

- `HexRunPhase`
- `HexRunOutcome`
- `HexRunState`
- `HexRunStateSnapshot` if useful for immutable UI/test reads

Wire `PlayerController` to populate the state, but keep existing behavior.

Test checkpoint:

- new game starts
- resources display correctly
- movement still works
- act 1 to act 2 transition still works
- defeat/victory modal still opens

### Slice 2: Move Act Session Ownership Out Of Static Service

Create `HexRunSessionController`.

Refactor `HexActTransitionService` so it computes transition data and validates
boons, but does not own the current run session.

Keep compatibility wrappers only temporarily if needed.

Test checkpoint:

- act 1 complete shows the intermission
- boon selection persists to act 2/3
- act 3 nemesis family lock still works
- returning to main menu resets run state

### Slice 3: Centralize Flow/Input Gating

Create `HexGameFlowController` or fold phase control into
`HexRunSessionController` if a separate class is too much.

Replace scattered checks in `PlayerController.CanHandleGameplayInput`,
`HexCameraController.IsModalBlockingCamera`, and modal entry/exit paths with
one phase query.

Test checkpoint:

- map input blocked while pitstop modal is open
- map input blocked while act transition modal is open
- camera input blocked while run-end modal is open
- input returns after modal close

### Slice 4: Extract Turn Resolution

Create `HexTurnResolver` and result types:

- `HexTurnResolutionRequest`
- `HexTurnResolutionResult`
- optional sub-results for obstacles, pitstops, nemesis, resources

Move current `PlayerController.CommitMove` decision tree into resolver/service
form.

Test checkpoint:

- normal move
- move that reaches pitstop
- move that triggers obstacle contact
- move that causes resource defeat
- move that reaches goal and starts act transition

### Slice 5: Separate UI Reporting From Gameplay Decisions

Create one adapter that translates turn/run results into HUD/modal actions.

Keep UI Toolkit presenters as they are, but stop gameplay services from making
presentation decisions directly.

Test checkpoint:

- HUD status text still matches each outcome
- pitstop resolution modal still works
- act transition modal still works
- victory/game over overlays still work

### Slice 6: Regression Tests

Add focused EditMode or PlayMode coverage around the extracted state and
resolver logic.

Minimum useful coverage:

- starting run state initializes correctly
- movement spends food and updates coordinates
- movement is allowed even if it causes resource defeat
- goal move starts act transition before final victory when more acts remain
- selected boon advances the run state correctly
- modal/flow phase blocks gameplay input

## Non-Goals For This Task

- Do not replace the scene reload approach unless the state refactor makes it
  easy and low risk.
- Do not implement save/load yet.
- Do not migrate all dependencies to explicit composition yet. That is task 7.
- Do not add assembly definitions yet. That is task 6.
- Do not rebalance content during this task.

## Risk Areas

- Static state in `HexActTransitionService` currently hides scene reload
  complexity. Moving it too early without a session owner can break act
  transitions.
- `PlayerController.CommitMove` has many subtle ordering rules. Extract it in
  small steps and test between each step.
- Pitstop choice flow has deferred nemesis destruction behavior. Preserve this
  explicitly in result state.
- Resource defeat after movement must remain allowed. The caravan should move,
  then the run can end.
- UI modals currently act as implicit flow state. Replacing this with explicit
  phases needs careful modal open/close handling.

## Recommended Starting Point For A New Chat

Ask the new session to:

1. Read this document.
2. Inspect `PlayerController`, `PlayerController.MoveResolution`,
   `PlayerController.RunLifecycle`, `HexActTransitionService`,
   `HexGameBootstrap`, `HexCameraController`, and modal presenters.
3. Implement Slice 1 only.
4. Stop after Slice 1 and ask for manual regression testing.

## Done Criteria

Task 5 is complete when:

- current run state has one explicit owner
- act session state is not hidden in a static service
- game phase controls gameplay input and modal blocking
- movement resolution can be tested without driving UI directly
- `PlayerController` is no longer the run lifecycle owner
- full manual loop still works from main menu through act 3 victory or defeat
