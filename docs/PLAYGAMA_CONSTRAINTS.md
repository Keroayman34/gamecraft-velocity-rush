# Playgama Platform Constraints

**Purpose:** Working checklist for platform-facing design and implementation.  
**Status:** Baseline notes; re-check current official Playgama documentation before submission and integration.

## Known platform requirements to account for
- Integrate the current Playgama Bridge SDK and complete the required integration steps.
- Do not force external account registration or authentication at game launch.
- Use Playgama Bridge Analytics; do not add external analytics such as Google Analytics unless current platform policy explicitly changes.
- Required game resources should be packaged with the game; avoid relying on external asset hosting.
- Respect the platform's external-link permission mechanism before opening third-party links.
- Do not restrict game operation based on URL.
- Keep the archive within the current platform size limit (previously documented as 300 MB; verify before release).
- Keep startup memory below the platform's current limit (previously documented as 2 GB; verify before release).
- Ensure root archive structure and entry file meet current submission requirements.
- Support responsive layouts and the required desktop input scheme.
- If mobile is selected, test touch controls, orientation behavior, fullscreen expectations, and mobile browser behavior.
- Do not use browser-reserved controls in a way that prevents platform navigation.
- Test ad lifecycle behavior: pause gameplay/audio appropriately and resume safely.
- Rewarded-ad rewards must only be granted after confirmed successful completion.

## Product implications
- Design for short loading times and predictable memory use.
- Avoid unnecessary large textures, video, and duplicated assets.
- Do not assume mobile support until tested on representative devices.
- Never show ads during active driving.
- Handle unavailable ads gracefully.

## Verification checklist
Before implementation:
- Read current official Bridge documentation.
- Confirm current archive size and memory limits.
- Confirm current SDK version and integration requirements.
- Confirm current browser/device matrix.
- Confirm ad callbacks and save APIs.

Before release:
- Run platform QA.
- Test startup, loading, pause/resume, browser history, orientation changes, and ad transitions.
- Test all game content for completion blockers.
- Record the exact documentation date/version used.

**Important:** This file is a project checklist, not a substitute for the current official Playgama documentation.
