# AR Function Lab Test Plan

## Purpose

Verify that formulas are parsed and evaluated correctly, generated meshes represent the requested mathematical surfaces, and the main interaction flow works on Meta Quest 3S.

## Result labels

- **Pass**: observed result matches the stated expectation.
- **Fail**: implemented behavior does not match the stated expectation.
- **Not tested**: the feature exists, but has not yet been verified.
- **Not implemented**: the expected feature is not present in the current build. This is a scope or development status, not a test failure.
- **Blocked**: the test cannot run because a required device, build, permission, or dependency is unavailable.

## Automated EditMode coverage

The suite in `Assets/_Project/Tests/EditMode` currently covers:

- known-value evaluation for constant, paraboloid, saddle, and trigonometric formulas;
- known-value and invalid-domain cases for every function advertised by `FunctionCatalog`;
- implicit multiplication (`sin(x)cos(y)`);
- parameter substitution;
- division-by-zero and invalid-domain errors;
- rejection of malformed formulas and invalid parameter names;
- rejection of duplicate parameters;
- mesh sample, vertex, and triangle counts;
- graph-to-Unity coordinate mapping;
- independent verification of every generated paraboloid vertex.

Evaluation uses a tolerance of `1e-12` for double-precision formula results. Generated mesh positions use `1e-5` because Unity stores mesh vertices as single-precision floats.

Run the suite from the Unity Test Runner, or from the command line:

```text
/Applications/Unity/Hub/Editor/6000.6.6f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath "/path/to/group7_ar_2026_project" \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/arfunctionlab-editmode-results.xml \
  -logFile /tmp/arfunctionlab-editmode-tests.log
```

Do not add `-quit`; the test runner exits Unity when the run finishes.

## Next automated coverage

1. Test height clipping and undefined samples in `SurfaceGenerator`.
2. Add parser precedence, associativity, and unary-operator cases.
3. Add PlayMode tests for formula submission, reset, and range controls.
4. Add a repeatable performance test at low, medium, and maximum mesh resolutions.

## Device coverage

Use `QuestChecklist.md` for every candidate APK. Record the APK name, commit, headset model, tester, date, and outcome in the release notes or a copied checklist.
