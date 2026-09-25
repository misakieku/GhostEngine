# Render Graph Concepts

The render graph is how GhostEngine builds a frame. Instead of recording GPU commands directly, you describe what each pass reads and writes, and the graph works out when the passes run, what memory they share, and what synchronization they need.

This page defines the terms the rest of the set uses. To start writing passes, go to [Writing Render Passes](authoring-passes.md).

## What the Graph Does for You

Written by hand, a frame carries four kinds of bookkeeping that no single pass can see far enough ahead to get right. The graph takes all of them over:

- **Resource lifetime.** Intermediates are allocated once per frame and recycled automatically. You never decide when a texture is safe to free.
- **Memory.** Textures that are never alive at the same time share physical video memory, so a frame's footprint follows its peak concurrency rather than its total size.
- **Synchronization.** Every read-after-write, write-after-read and write-after-write hazard becomes a barrier, derived from your declarations. You never write a barrier.
- **Dead work.** A pass whose results nothing reads is removed before it reaches the GPU.
- **Ordering.** Passes are reordered to batch render state and shorten lifetimes, which in turn frees more memory.

The trade is that the frame you authored is not the frame that runs. Passes move, vanish, merge, and change queues. [Debugging and Validation](debugging.md) covers how to see what actually happened.

## Pass

A **pass** is one unit of rendering work: a compute dispatch, a set of draws, a copy. You add passes to a `RenderGraph`, and each one gets a name, the resources it touches, a small struct of data, and a function that records the GPU commands.

There are three kinds. **Raster** passes draw into color and depth attachments. **Compute** passes dispatch shaders over textures and buffers. **Unsafe** passes drop to the raw command buffer for explicit view clears, buffer mapping, and anything else the graph does not model — they exist because the abstraction cannot cover everything, and a pass that needs an escape hatch should be visibly marked as one.

## Resource

A **resource** is a texture or a buffer the graph knows about. Creating one describes it rather than allocating it: the descriptor is recorded immediately, and the GPU memory arrives at compile time, once the graph knows which resources can share.

Resources come in three kinds, and the difference is who owns them:

- A **transient** resource is created by the graph and lives only inside one frame. It is the only kind that shares memory with other resources.
- An **imported** resource already exists — the swap-chain back buffer, a persistent history texture — and you bring it into the graph so passes can use it.
- An **extracted** resource starts as a transient and is handed to you at the end of the frame, so the next frame can import it.

Identifiers are valid only for the frame that produced them. Keeping one across frames reads whatever the next frame happens to place at that index, so anything that must survive has to be imported or extracted. [Resources](resources.md) covers choosing between the three.

## Declaration

A **declaration** is you telling the graph that a pass reads or writes a resource: `UseTexture`, `UseBuffer`, `SetColorAttachment`, `SetDepthAttachment`. Declarations are the entire input to everything the graph automates. Dependencies, barriers, lifetimes, aliasing, culling and render pass merging are all derived from them and nothing else.

This is the rule that matters most: **a resource access you do not declare is invisible to the graph.** An undeclared read gets no barrier, no lifetime extension, and no protection from culling — the graph will happily reuse that memory underneath you.

## Native Render Pass

Several raster passes drawing into the same attachments are merged into one **native render pass** — a single hardware render pass on the GPU. Inside it, attachment contents stay in tile memory instead of being written to and re-read from video memory, and clears become load actions rather than separate work.

Merging is the largest performance prize in the graph, and it breaks in one specific way: a raster pass that touches anything besides its own attachments cannot merge, because reading a separate texture mid-render-pass needs a transition the hardware does not allow. Writing results through a UAV instead of an attachment forfeits the merge for the same reason.

## The Shape of a Frame

A frame is three steps, in order:

1. **Reset** the graph, discarding the previous frame's passes and resources.
2. **Add your passes**, declaring resources and access as you go. This is the only part you write.
3. **Compile and execute**, which resolves sizes, works out order and memory, records the GPU commands, and submits them.

The graph is reused across frames rather than rebuilt as an object, and the compilation result is cached: a frame whose structure matches the previous one skips compilation entirely. That is why descriptors built the same way every frame cost nothing after the first, and why frame-varying values belong in pass data rather than in resource descriptors.

Engine code drives this from `GhostRenderPipeline`, which keeps one graph per view and imports the back buffer before adding its passes. The graph never owns command buffers or fences — those belong to the frame scheduler, which the graph borrows for the duration of a frame.

## Next

- [Writing Render Passes](authoring-passes.md) — the tutorial: your first pass, compute dispatches, attachments, and data flow.
- [Resources](resources.md) — picking transient, imported, or extracted, and sizing for the viewport.
- [Synchronization](synchronization.md) — what the graph does with your declarations.
- [Debugging and Validation](debugging.md) — when a pass vanishes, moves, or misbehaves.

If you are changing the graph itself rather than writing passes for it, the compile pipeline, data structures and invariants are documented in [Render Graph — Architecture Reference](../../../developer-docs/render_graph_architecture.md).
