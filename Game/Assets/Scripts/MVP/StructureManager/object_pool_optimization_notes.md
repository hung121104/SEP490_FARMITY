FifoObjectPool & StructurePool – Optimization Notes

1. Replace Position Queue with Single Object

Current design: Dictionary<Vector3Int, Queue> m_PositionPools

Problem: Each position only stores at most 1 object, so using Queue is
unnecessary and adds memory and CPU overhead.

Optimization: Use: Dictionary<Vector3Int, T>

Benefits: - Less memory usage - Simpler logic - Faster access - Less GC
pressure

2. Remove HashSet Duplicate Check (Optional Optimization)

Current: HashSet m_PooledObjects

Used to detect double release.

Problem: HashSet operations (Contains/Add/Remove) run every Get and
Release. If the pool is called frequently, this adds overhead.

Better approach: Let pooled objects track their own state.

Example interface:

interface IPoolable { bool IsInPool { get; set; } }

Benefits: - Removes HashSet lookup cost - Faster Get/Release

3. Remove or Limit Debug.Log in Runtime

Current code prints logs in several places.

Example: Debug.Log(“[FifoObjectPool] Get from position pool…”)

Problem: Debug.Log is expensive, especially when spawning/despawning
many objects.

Optimization: Wrap logs:

#if UNITY_EDITOR Debug.Log(…) #endif

or remove them entirely for production builds.

4. Cache Component References

Current code often calls:

GetComponentInChildren()

Problem: Component lookup is slow if called frequently.

Optimization: Cache references when object is created.

Example: Store SpriteRenderer once in a component or structure script.

5. Prewarm Object Pools

Currently objects are created only when needed:

if (queue empty) create new

Problem: Instantiation spikes when many objects spawn at once.

Optimization: Pre-create objects during initialization.

Example:

for (int i = 0; i < defaultCapacity; i++) { var obj = createFunc();
Release(obj); }

Benefits: - Smoother runtime - Avoid spawn spikes

6. ClearPositionPools May Cause Frame Spike

Current method iterates through every position and moves objects to the
general pool.

Problem: If many tiles unload simultaneously (for example chunk unload),
this can create a frame spike.

Optimization ideas: - Clear pools per chunk - Spread cleanup across
multiple frames - Or destroy directly if chunk unloading completely

7. Avoid Repeated Sprite Refresh Logic

Dynamic pool checks sprite each time:

if (sr != null && sr.sprite == null)

Problem: Unnecessary repeated checks.

Optimization: Assign sprite once during creation if possible, or cache
structure sprite data.

8. Instantiate Cost for Dynamic Structures

Dynamic structures use:

Instantiate(template)

If many structures spawn simultaneously this can still be expensive.

Possible improvements: - Prewarm pools - Group instantiate during
loading - Keep pool sizes large enough for typical gameplay

9. Minor Structural Improvements

Possible architectural simplification:

StructurePool ├── GlobalQueuePool └── PositionCache
(Dictionary<Vector3Int, T>)

Remove: - Queue per position - HashSet if safe

Benefits: - Less complexity - Better performance - Cleaner code

Summary of Most Important Optimizations

High Impact: 1. Replace position Queue with Dictionary<Vector3Int, T> 2.
Remove or limit Debug.Log 3. Prewarm object pools

Medium Impact: 4. Cache SpriteRenderer references 5. Reduce HashSet
usage

Situational: 6. Optimize ClearPositionPools 7. Improve sprite refresh
logic
