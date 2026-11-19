# AR Function Lab

## Unity Preview

1. Open Unity Hub and add the cloned project folder.
2. Wait for Unity to import the packages and automatically compile the scripts, then open `Assets/_Project/Scenes/SurfaceDemo.unity`.
3. Once compilation finishes without errors, click **Play** to start the project.

- **× 2 / ÷ 2**: change the x and y ranges together. Both start at `[-10, 10]`.
- **Rotate − / Rotate + / Reset view**: adjust the graph view.

## Quest 3

1. In Unity Hub, add **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK**.
2. Reopen the project and select **AR Function Lab -> Build Quest APK**. The APK is saved to `Builds/Quest/ARFunctionLab.apk`.
3. Connect a Quest 3 with developer mode enabled and allow USB debugging. 

With `adb` available, run the following command from this project folder:
```bash
adb install -r Builds/Quest/ARFunctionLab.apk
```

Then you can launch **AR Function Lab** on the headset. 
