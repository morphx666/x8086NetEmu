# 02.03-runtests2: Retarget the RunTests2 harness

# 02.03-runtests2: Retarget the RunTests2 harness

## Objective
Retarget `RunTests2/RunTests2.csproj` from `.NET Framework 4.8.1` to modern .NET and verify that it continues to build against the upgraded `x8086NetEmu` core project.

## Scope
- `RunTests2/RunTests2.csproj`
- Runtime or path-handling code only if build errors appear after retargeting

## Done when
- `RunTests2.csproj` targets the planned modern .NET framework
- The project restores and builds cleanly with the upgraded core library
- Existing compatible package references remain valid

