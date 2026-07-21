# Copilot Instructions

## Project Guidelines
- For the active .NET upgrade scenario, discard the WinForms version of the project and use the Eto.Forms versions going forward.
- In the active .NET upgrade workflow, do not merge or rebase the working branch into the source branch yet; run tests first and open the PR to merge manually when ready.

## Emulator Requirements
- For the x8086NetEmu upgrade, required emulator ROMs are expected to be available under `Release\roms\` and runtime validation should use that location.