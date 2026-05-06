# Player Manual

## Premise

You guide a caravan fleeing tyranny toward a promised land. Each act is a new stretch of hostile road: supplies run down, morale falters, hazards appear from the fog, and by the final act the Hunter begins to close in.

## Objective

Reach the goal hex in all three acts. Reaching the goal in Act 1 or Act 2 advances the run to the next act. Reaching the goal in Act 3 wins the game.

## Starting Resources

A new run begins with:

- Food: 30
- Morale: 5
- Gold: 5

Food and Morale are survival resources. If either reaches 0, the caravan is defeated unless that move has already completed the act. Gold can fall to 0 without ending the run.

## Turn And Movement Rules

The caravan moves one adjacent hex per turn.

To move:

1. Click the caravan's current hex.
2. Click an adjacent destination to preview it.
3. Click that same destination again to confirm the move.

Moving costs Food based on the destination terrain. Goal hexes currently cost 1 Food to enter. You may confirm a move that spends your last Food; if it does not complete the act, the caravan loses after the move resolves.

Clicking other hexes inspects them in the right-side panel.

## Terrain

Terrain affects Food cost:

| Terrain | Travel Cost |
|---|---:|
| Grass | 1 |
| Forest | 2 |
| Mountain | 3 |
| Water | 3 |
| Desert | 5 |

Water is passable. Act 2 uses a desert-biased map profile, so expect more expensive routes.

## Fog Of War

The caravan sees nearby hexes around its current position. Unseen terrain is hidden; discovered terrain remains remembered after the caravan moves away. The goal and pitstop locations are treated as known route anchors.

## Resources

Food is spent by movement and can also be lost to hazards or events.

Morale represents the caravan's will to continue and is affected by hazards or events.

Gold is used in events and can be stolen by bandits. It is useful, but running out of Gold is not an immediate defeat.

## Pitstops

Pitstops are roadside points of interest. Reaching one applies its base reward on first visit, then may open a choice event.

| Pitstop | Base Reward |
|---|---:|
| Mill | +3 Food |
| Wall Tower | +1 Morale |
| Mansion | +2 Gold |

Pitstop choices can add or spend Food, Morale, and Gold. Choices you cannot afford are disabled.

## Obstacles

Obstacles can appear as you reveal new terrain. They are visible only while in sight and disappear when they leave current visibility.

Current obstacles:

| Obstacle | Effect |
|---|---|
| Wolves | -1 Food |
| Bandits | -1 Gold, or -1 Morale if you have no Gold |
| Ruined Caravan | -1 Morale |

Normally, up to 2 obstacles can be active. Obstacles do not spawn on water, pitstops, the start, the goal, the caravan, the Hunter, or another obstacle.

## Acts And Rewards

The run has three acts.

After Act 1:

- Resources carry forward.
- The caravan gains +6 Food, +1 Morale, and +2 Gold.
- You choose one reward for the next act.

After Act 2:

- Resources carry forward.
- The caravan gains +8 Food, +1 Morale, and +3 Gold.
- You choose one final reward for Act 3.
- The Hunter becomes active in Act 3.

## The Hunter

In Act 3, the Hunter pursues the caravan.

The Hunter starts away from the caravan and advances after caravan moves. If the Hunter reaches the caravan, or if the caravan moves onto the Hunter's hex, the run ends in defeat.

## Winning

Reach the Act 3 goal. The victory screen appears when the final goal is reached.

## Losing

The run ends in Game Over if:

- Food reaches 0.
- Morale reaches 0.
- The Hunter reaches the caravan.
- The caravan moves onto the Hunter's hex.

The Game Over screen offers Retry and Return to Title.

## Controls And UI

| Action | Control |
|---|---|
| Inspect/select hex | Left click |
| Confirm move | Click the previewed destination again |
| Pan camera | WASD or Arrow Keys |
| Rotate camera | Q / E |
| Zoom | Mouse wheel |
| Reset camera | Space |

The top HUD shows act status, resources, reward information, movement status, and travel time. The right inspector shows details for the selected hex, including terrain, travel cost, pitstops, hazards, and Hunter context.

## Current Content Notes

This is a playable prototype focused on a three-act caravan journey, procedural maps, terrain costs, fog of war, pitstops, obstacles, act rewards, victory, defeat, and the Hunter.
