# Meta Quest Candidate-Build Checklist

## Build record

- Date:
- Tester:
- Git branch and commit:
- APK filename or SHA-256:
- Headset model and OS version:
- Unity version:

## Install and startup

- [ ] APK installs without an ADB error.
- [ ] App launches without returning to the Quest home screen.
- [ ] Passthrough appears.
- [ ] Head movement updates the view correctly.
- [ ] Controllers or tracked hands appear and remain responsive.
- [ ] No repeating error is visible in the device log.

## Formula workflow

- [ ] The formula field accepts keyboard input.
- [ ] `x^2+y^2` generates a bowl-shaped surface.
- [ ] `x^2-y^2` generates a saddle-shaped surface.
- [ ] `sin(x)cos(y)` generates an oscillating surface.
- [ ] Invalid input displays a useful error and does not crash.
- [ ] Division by zero or an invalid domain leaves gaps instead of producing extreme geometry.

## Controls and presentation

- [ ] Range controls visibly change the graph domain.
- [ ] Rotation controls visibly rotate the graph.
- [ ] Reset restores the expected starting state.
- [ ] Text and buttons are readable at the intended viewing distance.
- [ ] UI controls can be selected reliably with the intended input method.
- [ ] The graph does not obscure essential UI.

## Stability and performance

- [ ] Interaction remains responsive at the default resolution.
- [ ] Maximum supported resolution does not crash or freeze the app.
- [ ] Ten formula changes in succession complete without a crash.
- [ ] The app remains stable for at least 10 minutes.
- [ ] Any visible frame-rate drop is recorded with the formula and resolution that caused it.

## Result

- Overall: Pass / Fail / Blocked
- Failed step(s):
- Notes:
- Screenshot or video location:
- Device log location:
