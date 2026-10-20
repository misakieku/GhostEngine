# Common Mistakes in GhostEngine

This document records recurring pitfalls, subtle bugs, and incorrect assumptions encountered during engine development. **Read this document before implementing low-level systems, unmanaged collections, or memory-sensitive pipelines.**

---

## 1. Unmanaged Collections (`Misaki.HighPerformance`)

### 1.1 `UnsafeList<T>.Resize` Only Changes Capacity, NOT Count

#### The Mistake
Calling `list.Resize(newSize)` expecting it to behave like C++ `std::vector::resize()` or to set `list.Count = newSize`:

```csharp
// BAD: Count remains 0!
list.Resize(offset + size);
MemoryUtility.MemCpy((byte*)list.GetUnsafePtr() + offset, pSource, (nuint)size);
// list.Count is STILL 0! Next append calculates offset = list.Count (0) and overwrites!
```

#### The Reality
In `Misaki.HighPerformance.LowLevel.Collections.UnsafeList<T>`, `Resize(capacity)` **only reallocates the underlying buffer capacity**. It does **not** modify `Count`.

#### The Consequence
Because `Count` was never incremented, subsequent code querying `list.Count` to find the append offset will read `0`. This silently overwrites previous data at offset 0, leading to mysterious data corruption and clobbered components.

#### The Correct Patterns

- **When appending managed/unmanaged elements**:
  ```csharp
  list.Add(value);
  // or
  list.AddRange(span);
  ```

- **When writing raw memory directly via pointers / `MemCpy`**:
  Always ensure capacity, set the count explicitly with `UnsafeSetCount`, and then copy:
  ```csharp
  var requiredCount = offset + size;
  if (list.Capacity < requiredCount)
  {
      list.Resize(requiredCount);
  }

  list.UnsafeSetCount(requiredCount);
  MemoryUtility.MemCpy((byte*)list.GetUnsafePtr() + offset, pSource, (nuint)size);
  ```

---

## 2. Memory Alignment in Heterogeneous Buffers

### 2.1 Appending Unmanaged Structs Without Respecting `alignof`

#### The Mistake
Packing unmanaged structs of varying sizes consecutively into a raw `byte` buffer without aligning the start offset:

```csharp
// BAD: Ignores struct alignment requirements
var offset = buffer.Count; // e.g. 2 after storing a 2-byte struct
// Storing HDRISettings (48 bytes, 4/8-byte aligned) at offset 2 is unaligned!
```

#### The Reality
Structs with fields like `float`, `float3`, `float4`, `quaternion`, or 64-bit pointers/handles require 4-byte or 8-byte alignment. If a 1-byte or 2-byte struct is added first, the next struct's address will be unaligned.

#### The Consequence
Casting unaligned pointers (`*(T*)ptr`) degrades CPU memory throughput, causes SIMD instruction traps/faults on certain hardware architectures, and produces undefined behavior.

#### The Correct Pattern
Always compute aligned offsets using `MemoryUtility.AlignOf<T>()` and `MemoryUtility.AlignUp`:

```csharp
var align = MemoryUtility.AlignOf<T>();
var alignedOffset = (int)MemoryUtility.AlignUp((nuint)buffer.Count, align);
var requiredCount = alignedOffset + sizeof(T);

if (buffer.Capacity < requiredCount)
{
    buffer.Resize(requiredCount);
}

buffer.UnsafeSetCount(requiredCount);
MemoryUtility.MemCpy((byte*)buffer.GetUnsafePtr() + alignedOffset, pValue, (nuint)sizeof(T));
```
Or encapsulate into a dedicated [`UnmanagedTypeMap`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Volumes/UnmanagedTypeMap.cs).
