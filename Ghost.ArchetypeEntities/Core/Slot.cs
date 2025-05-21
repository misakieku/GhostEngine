using System.Runtime.CompilerServices;

namespace Ghost.Entities.Core;

/// <summary>
/// The <see cref="Slot"/> struct references an <see cref="Entity"/> entry within an <see cref="Archetype"/> using a reference to its <see cref="Chunk"/> and its index.
/// </summary>
[SkipLocalsInit]
internal record struct Slot
{
    /// <summary>
    /// The index of the <see cref="Entity"/> in the <see cref="Chunk"/>.
    /// </summary>
    public int index;

    /// <summary>
    /// The index of the <see cref="Chunk"/> in which the <see cref="Entity"/> is located.
    /// </summary>
    public int chunkIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="Slot"/> struct.
    /// </summary>
    /// <param name="index">The index of the <see cref="Entity"/> in the <see cref="Chunk"/>.</param>
    /// <param name="chunkIndex">The index of the <see cref="Chunk"/> in which the <see cref="Entity"/> is located.</param>
    public Slot(int index, int chunkIndex)
    {
        this.index = index;
        this.chunkIndex = chunkIndex;
    }

    /// <summary>
    /// Adds a plus operator for easy calculation of new <see cref="Slot"/>. Adds the positions of both <see cref="Slot"/>s.
    /// </summary>
    /// <param name="first">The first <see cref="Slot"/>.</param>
    /// <param name="second">The second <see cref="Slot"/>.</param>
    /// <returns>The result <see cref="Slot"/>.</returns>
    public static Slot operator +(Slot first, Slot second)
    {
        return new Slot(first.index + second.index, first.chunkIndex + second.chunkIndex);
    }

    /// <summary>
    /// Adds a plus plus operator for easy calculation of new <see cref="Slot"/>. Increases the index by one.
    /// </summary>
    /// <param name="slot">The <see cref="Slot"/>.</param>
    /// <returns>The <see cref="Slot"/> with index increased by one..</returns>
    public static Slot operator ++(Slot slot)
    {
        slot.index++;
        return slot;
    }

    /// <summary>
    /// Validates the <see cref="Slot"/>, moves the <see cref="Slot"/> if it is outside a <see cref="Chunk.Capacity"/> to match it.
    /// </summary>
    /// <returns></returns>
    public void Wrap(int capacity)
    {
        // Result outside valid chunk, wrap into next one
        if (index < capacity)
        {
            return;
        }

        // Index outside of its chunk, so we calculate how many times a chunk fit into the index for adjusting the chunkindex to that position.
        // Floor since we do not need a rounded value since the index is within that chunk and not the next one.
        chunkIndex += (int)Math.Floor(index / (float)capacity);

        // After moving the chunk index we can simply take the rest and assign it as a index.
        index %= capacity;
    }

    /// <summary>
    /// Moves or shifts this <see cref="Slot"/> by one slot forward.
    /// Ensures that the slots chunkindex updated properly once the end was reached.
    /// </summary>
    /// <param name="source">The <see cref="Slot"/> to shift by one.</param>
    /// <param name="sourceCapacity">The capacity of the chunk the slot is in.</param>
    /// <returns></returns>
    public static Slot Shift(ref Slot source, int sourceCapacity)
    {
        source.index++;
        source.Wrap(sourceCapacity);
        return source;
    }

    /// <summary>
    /// Moves or shifts the source <see cref="Slot"/> based on the destination <see cref="Slot"/> and calculates its new position.
    /// Used for copy operations to predict where the source <see cref="Slot"/> will end up.
    /// </summary>
    /// <param name="source">The source <see cref="Slot"/>, from which we want to calculate where it lands..</param>
    /// <param name="destination">The destination <see cref="Slot"/>, a reference point at which the copy or shift operation starts.</param>
    /// <param name="sourceCapacity">The source <see cref="Chunk.Capacity"/>.</param>
    /// <param name="destinationCapacity">The destination <see cref="Chunk.Capacity"/></param>
    public static Slot Shift(in Slot source, int sourceCapacity, in Slot destination, int destinationCapacity)
    {
        var freeSpot = destination;
        var resultSlot = source + freeSpot;
        resultSlot.index += source.chunkIndex * (sourceCapacity - destinationCapacity);
        resultSlot.Wrap(destinationCapacity);

        return resultSlot;
    }
}