PICO builds in this multi-target project use Unity’s merged main Android manifest (no Assets/Plugins/Android/AndroidManifest.xml).

ApplyXrTargetEditor removes Plugins/Android when you select Pico and sets Player Settings → use custom main manifest = Off.

Do not add AndroidManifest.xml here unless you need PICO-specific overrides; the Pico OpenXR package normally supplies the right merged fragments.
