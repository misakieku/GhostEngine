# Synchronization

You never write a barrier in GhostEngine, and you never choose a load action or a queue. Those are conclusions the graph draws from the declarations you make on the pass builder. This page explains what it concludes, so you can tell when a declaration is going to cost you something.

## Barriers, From Your Declarations

Each `UseTexture`, `UseBuffer`, `SetColorAttachment` and `SetDepthAttachment` says two things: that this pass touches this resource, and in what role. From that, across the whole frame, the graph works out every hazard — read-after-write, write-after-read, write-after-write — and inserts the transitions needed to make them safe.

The role matters as much as the direction. A texture read by a compute pass and then drawn into by a raster pass needs two transitions, and the graph derives both. A buffer declared `Read` that has `BufferUsage.IndirectArgument` is understood as an indirect-argument buffer rather than a shader resource, and gets the state that implies.

> **Note:** The graph can only reason about what you declared. An access you left out gets no barrier before it, no lifetime extension, and no protection from being deleted as dead work. Every declaration mistake looks like a rendering artifact somewhere else in the frame.

Declarations also merge sensibly. If you both `UseTexture(t, ReadWrite)` and `SetColorAttachment(t, 0, Write)`, the explicit attachment declaration wins, because it says more. That is also why a resource can hold only one role per pass: declaring the same texture as an attachment *and* a UAV in one pass is rejected rather than guessed at.

## Why Most Barriers Disappear

A resource that stays in the same role across consecutive passes needs no transition, and the graph emits none. Inside a merged render pass this is the normal case — eight passes drawing into the same GBuffer produce one transition, not eight.

One hazard is invisible to that logic and gets special treatment: two compute passes both writing the same UAV. Both want the resource in the identical state, so state comparison says nothing is needed, yet the writes must still be ordered. The graph detects it and forces a barrier anyway. If you ever see a transition to the state a resource is already in, this is why — it is ordering, not a state change.

## Load and Store Actions

For a texture bound as an attachment, whether the hardware loads its previous contents, clears it, or ignores it is inferred from where the resource sits in the frame:

- **First use in the frame**, with `clearAtFirstUse` on the descriptor — clear to the descriptor's clear value. Without it — don't care, since nothing needs the old contents.
- **Later use, declared `Discard` or `WriteAll`** — don't care. The pass is overwriting everything.
- **Later use, declared `Read`** — load. Contents must survive.
- **Last use in the frame**, with `discardAtLastUse` — don't care, so tile memory is dropped without writing back to video memory. Otherwise store.

The practical consequence is that these two descriptor flags are per-resource decisions about the whole frame, not about one pass. If a texture needs to be cleared in one place and preserved in another, it wants to be two textures.

Imported and extracted resources are protected from the discard case: their last use always stores, because something outside the graph still needs those pixels.

## Render Pass Merging

Consecutive raster passes sharing the same attachments are merged into one hardware render pass. Inside it, attachment contents stay in tile memory instead of round-tripping to video memory between passes, and clears become load actions. On a tiled GPU this is the difference between a frame that fits in budget and one that does not.

A merge needs all of:

- Both passes raster, with the same color slots and the same depth texture.
- Nothing in either pass touched through a UAV.
- **Every resource either pass touches is one of its attachments.**
- No transition needed between them.

The third condition is the one that bites, and it is not a limitation of the graph — reading a separate texture partway through a render pass would need a transition the hardware does not allow. So a raster pass that samples a texture *and* writes attachments cannot merge with anything.

Two habits keep merging alive: prefer attachments over UAV writes for anything a raster pass produces, and keep each raster pass's resource touches to its own attachments. If a pass genuinely needs both, it is two passes, and that is fine — say so explicitly rather than hoping the graph finds a way.

## Async Compute

`EnableAsyncCompute(true)` marks a compute pass as willing to run on the compute queue. The graph then looks for a window: some independent graphics work between this pass and the first pass that reads its results. If it finds one, the pass moves to a compute command buffer and the synchronization is inserted at the boundaries. If it does not, the pass stays on the graphics queue.

That makes the hint cheap to leave on, and it means **adding independent raster work is what unlocks async compute**, not the flag alone. A compute pass at the very end of a frame, or one whose result is consumed immediately, has nowhere to overlap and will never move.

Eligibility is narrow on purpose. The pass must be a compute pass with no attachments and no side effects — a compute pass that writes a render target is not a candidate, and neither is one that writes an imported resource. Unsafe passes cannot be async at all.

When several eligible compute passes happen to run before the same consumer, they are grouped into one compute region so they share a single pair of queue transitions. Regions can also chain, so a frame with culling early on and post-processing later can overlap both.

To find out what actually happened, read `QueueDecision` from a dump. Nothing warns when the graph declines, and the frame is correct either way. See [Debugging and Validation](debugging.md).

## Crossing Queues

A resource moving between the graphics and compute queues needs a release/acquire pair rather than a single barrier, because the two queues record into separate command lists. The graph emits both, and only for resources that actually cross with a real dependency between them.

The cost is that a resource ping-ponging between queues is slower than the same work kept on one queue. If a dump shows many handoff pairs for one texture, that texture's producers and consumers are being split across queues when they could run together.

## The One Rule

Everything on this page follows from it: **declare exactly what you touch, and nothing more.** Under-declaring loses barriers and lifetimes. Over-declaring — a `Read` the shader never performs, a UAV where an attachment would do — costs transitions and breaks merging. The graph is faithful to your declarations, so the accuracy of the frame is the accuracy of the declarations.

## Next

- [Debugging and Validation](debugging.md) — seeing what the graph concluded, and fixing it when the conclusion is wrong.
