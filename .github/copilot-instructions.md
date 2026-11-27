# Copilot Instructions

## Project Guidelines
- For the active .NET upgrade scenario, discard the WinForms version of the project and use the Eto.Forms versions going forward.
- In the active .NET upgrade workflow, do not merge or rebase the working branch into the source branch yet; run tests first and open the PR to merge manually when ready.
- Project outputs should go directly to the Release folder rather than framework-specific subfolders like Release/net10.0 or Release/net10.0-windows.
- The repository should also track the contents of the Release/misc folder in addition to the specific allowed Release ROM and disk files.

## Emulator Requirements
- For the x8086NetEmu upgrade, required emulator ROMs are expected to be available under `Release\roms\` and runtime validation should use that location.
- The emulator menu must not be attached to the Eto canvas/right-click behavior. It should only be displayed via the Shift+Alt+Home keyboard shortcut so canvas context input remains available to the emulator.
- Eto mouse behavior should be hover-based: while over the emulator surface, hide the host cursor and forward all mouse events to the emulator; outside the emulator window, do nothing. Mouse event forwarding must continue working while capture logic is active. Once the Eto emulator captures mouse input, it must remain captured until the user presses the host shortcut Shift+Alt+Home; moving outside the emulator must not release capture.
- Eto double-click input must be translated so the emulator receives two complete mouse click cycles, matching WinForms behavior.