# Better Growth Vat Learning

A small mod that adds an expensive reseach that increases the learning rate of children being grown in growth pods to be nearly as good as growing in the real world.

### Notes/Compatibility
I had to use a destructive Harmony prefix on the Growth Pod's PodLearning property to prevent the old PodLearning Hediff form being re-added and removed each time its referenced. This will cause issues with any other mods that rely on this property, but if they do then the other mod is probably trying to to a similar thing.