# Enhanced Growth Vats
A small mod that adds a feature to increase the learning rate of children being grown in growth pods to be nearly as good as growing in the real (Rim)world!

Vanilla/unenhanced growth vats only provide a piddly 2 growth points per day. This is also not adjusted for the extra growth speed in the vat, meaning vat grown kiddos have really low growth scores. Additionally growth pod occupants receive a small 8000xp skill boost in a random skill, once every 3 days. Meh!

The 'Enhanced Growth Vat Learning' Research project allows players to toggle an optional 'Enhanced Learning' mode that greatly increases these values to be more comparable of what you would expect from a naturally-grown pawn, at a cost of high power consumption and reduced growth speed (requiring more food & power for the vat overall). Extra researches also allows players to broadly focus the skills that are trained by the software, including combat, labor and leadership skill packages.

Also makes the growth gizmo visible for pawns in vats so you can see how they are doing, enhanced mode or not.

### Notes/Compatibility
I had to use a destructive Harmony prefix on the Growth Pod's PodLearning property to prevent some funky stuff. This will cause issues with any other mods that rely on this property, but if they do then the other mod is probably trying to to a similar thing and you only want one of them.