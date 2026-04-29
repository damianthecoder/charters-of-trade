# Visual Layer Research - 2026-04-29

## Problem

The first Godot prototype proved that the systems load and tick, but it still read like a debug shell. The visual layer needs to communicate flow of value: routes, bottlenecks, price pressure, cashflow, and city supply state.

## Sources

- Godot custom 2D drawing documentation: https://docs.godotengine.org/en/stable/tutorials/2d/custom_drawing_in_2d.html
- Godot input event documentation: https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html
- Godot Control class documentation: https://docs.godotengine.org/en/stable/classes/class_control.html
- Godot Line2D class documentation: https://docs.godotengine.org/en/stable/classes/class_line2d.html
- Flow map layout research, Stanford Graphics: https://graphics.stanford.edu/papers/flow_map_layout/
- David Rumsey commerce map reference: https://www.davidrumsey.com/luna/servlet/detail/RUMSEY~8~1~3063~430022%3AWorld---commerce-
- Beinecke portolan chart reference: https://beinecke.library.yale.edu/collections/highlights/portolan-charts
- Rijksmuseum VOC collection reference: https://www.rijksmuseum.nl/en/collection/node/The-Dutch-East-India-Company-VOC--45d4a3daf93e41baae6017768c3d93df
- The National Archives East India Company charter reference: https://discovery.nationalarchives.gov.uk/details/r/f22038fd-c4be-42f3-a0fd-bdd343f8a82d

## Conclusion

The working art pillar is Ledger Cartography: historical cartography, contract ledgers, and modern flow-map readability. Territory should stay quiet. Routes, markets, margins, capacity, and supply pressure should be the main visual language.

## Decision

Start with a practical Godot runtime slice: selectable cities/routes, highlighted connected flows, animated route pulses, cashflow labels, a contextual inspector, supply rings, and priority signals. Keep all selection and drawing state in the Godot presentation layer. Do not add Godot dependencies, input state, colors, or scene concepts to core simulation projects.

## Risk

The biggest risks are visual clutter and presentation state leaking into simulation logic. For P0, custom drawing on a `Control` is acceptable because the map is small. Before scale increases, cache static terrain and route lookup data, and add interaction smoke tests that click the map and tick buttons.
