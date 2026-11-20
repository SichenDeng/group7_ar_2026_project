# Baseline Validation Results

- Date: 2026-11-20
- Unity: 6000.6.6f1
- Headset: Meta Quest 3S
- Android package: `com.arfunctionlab.weekone`

## Summary

| Area | Result | Evidence |
| --- | --- | --- |
| Project import and script compilation | Pass | Unity imported the project and compiled the runtime and test assemblies. |
| Desktop smoke test | Pass | Scene entered Play Mode with the formula UI and generated surface visible; Console reported no errors or warnings. |
| Quest APK build | Pass | `Builds/Quest/ARFunctionLab.apk` completed successfully. |
| Quest install and launch | Pass | APK installed and stayed running on a connected Quest 3S. |
| OpenXR, tracking, and passthrough startup | Pass | Device logs showed OpenXR initialization, active tracking, and passthrough creation. |
| Visible control response on Quest | Pass (smoke test) | Tester confirmed the visible controls responded. Exact behavior is not yet covered by a written case-by-case device test. |
| Quest formula and control spot-check | Pass | Tester confirmed the bowl, saddle, and oscillating examples, invalid-input handling, rotation, range, and reset appeared to work correctly. |
| EditMode automated suite | Pass | 58 of 58 tests passed. |
| Advertised mathematical-function catalog | Pass | Every function in `FunctionCatalog` has at least one known-value case; invalid-domain cases are also covered. |
| Parser and generator edge-case coverage | Partial | Representative errors and mesh cases pass, but exhaustive precedence, clipping, and discontinuity cases have not been added. |
| Long-session stability and performance | Not tested | Brief repeated use appeared fine, but the 10-minute soak and detailed performance checks were not completed. |

## Automated cases included in this baseline

- `0`
- `x^2+y^2`
- `x^2-y^2`
- `sin(x)cos(y)`
- parameterized `a*x+y`
- `sqrt(-1)` domain error
- `1/0` division-by-zero error
- malformed input and parameter-name validation
- known-value and invalid-domain checks for every advertised function
- coordinate conversion and settings boundary validation
- zero-plane mesh domain and topology
- paraboloid mesh vertex accuracy

## Current feature gaps

The following expected capabilities were not found in the current implementation and should be tracked as **Not implemented**, not as failed tests:

- cross-section visualization;
- runtime parameter sliders;
- user translation, scaling, or grabbing of the graph;
- plane-anchor placement.

## Observation to monitor

The Android build succeeded, but the editor logged a Meta OpenXR validation `NullReferenceException`. Because the APK built and launched successfully, this is currently a warning to reproduce and investigate rather than evidence of a device failure.
