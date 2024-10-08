# Space Engineers: Gas Production Fix

A [Torch][torch] server plugin for [Space Engineers][space-engineers],
to fix gas production calculations.
Because of rounding, players can place a very small amount of input each tick
to get an outsized amount overall.
The fix is to refuse to process the input until a large enough amount is
present.

Source code: on [github][github].

Releases: on the [Torch plugin page][plugin].

[torch]: https://torchapi.com/
[space-engineers]: https://www.spaceengineersgame.com/
[github]: https://github.com/StalkR/Space-Engineers-Gas-Production-Fix
[plugin]: https://torchapi.com/plugins/view/5f0ce44b-7caa-4cb5-a940-c5c647d68721

## Bugs, comments, questions

Create a [new issue][new-issue].

[new-issue]: https://github.com/StalkR/Space-Engineers-Gas-Production-Fix/issues/new
