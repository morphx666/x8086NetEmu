# 02-supporting-tools: Upgrade supporting tooling and regression utilities

Upgrade `GenOpCodes/GenOpCodes.vbproj`, `RunTests/RunTests.vbproj`, and `RunTests2/RunTests2.csproj` as a low-risk tooling group after the core library is modernized. This keeps project conversion, target framework updates, and regression harness alignment together without entangling them with the UI migration path.

**Done when**: The tooling and regression projects target modern .NET, any required SDK-style conversions are complete, and the retained build/test workflow runs against the upgraded core library.
