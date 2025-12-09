# AbxrLib for Unity - Training Demo

A comprehensive XR training application built with Unity that demonstrates the capabilities of the AbxrLib SDK. This demo showcases interactive AR/VR experiences with object manipulation, spatial interactions, and immersive training scenarios.

## Description

This Unity project serves as a training demonstration for the AbxrLib SDK, featuring:
- Interactive 3D object manipulation
- Spatial AR/VR interactions
- Training scenarios with grabbable objects
- Particle effects and visual feedback
- Cross-platform XR support (Android, WebGL, and more)

The project includes various prefabs, scenes, and scripts that demonstrate best practices for XR development using the AbxrLib framework.

## How to Run

### Prerequisites
- Unity 2022.3 LTS or later
- AbxrLib SDK installed
- XR-compatible device (for testing)

### Setup Instructions
1. **Install Unity**: Download and install Unity 2022.3 LTS from [unity.com](https://unity.com)
2. **Install AbxrLib SDK**: 
   ```bash
   git clone https://github.com/ArborXR/abxrlib-for-unity.git
   ```
3. **Open the Project**: Launch Unity and open this project folder
4. **Configure XR Settings**: Ensure your XR settings are properly configured in Project Settings

### Running the Application
1. **In Unity Editor**:
   - Open the `TrainingDemo.unity` scene from the Scenes folder
   - Press the Play button to test in the Unity Editor
   - Use XR simulation tools if available

2. **Build and Deploy**:
   - Go to File → Build Settings
   - Select your target platform (Android, WebGL, etc.)
   - Click "Build and Run" to deploy to your device

3. **Android Deployment**:
   ```bash
   # Build APK for Android
   # Use Unity's Build Settings to create an APK
   # Deploy to Android device via ADB or direct installation
   ```

## How to Test

### Unity Editor Testing
1. **Scene Testing**:
   - Open `Assets/Scenes/TrainingDemo.unity`
   - Test object interactions and XR functionality
   - Verify particle effects and audio feedback

2. **Script Testing**:
   - Run the project in Play mode
   - Test grabbable objects and interaction systems
   - Verify UI elements and feedback systems

### Automated Testing
1. **Unit Tests** (if available):
   ```bash
   # Run Unity Test Runner
   # Window → General → Test Runner
   # Execute Play Mode and Edit Mode tests
   ```

2. **Performance Testing**:
   - Use Unity Profiler to monitor performance
   - Test on target devices for frame rate analysis
   - Verify memory usage and optimization

### Device Testing
1. **XR Device Testing**:
   - Deploy to actual XR hardware
   - Test spatial tracking and hand interactions
   - Verify cross-platform compatibility

2. **Platform Testing**:
   - Test on Android devices
   - Test WebGL builds in browsers
   - Verify platform-specific features

### Manual Testing Checklist
- [ ] Object grabbing and manipulation works correctly
- [ ] Particle effects trigger appropriately
- [ ] Audio feedback plays on interactions
- [ ] UI elements respond to user input
- [ ] Scene transitions work smoothly
- [ ] Performance is acceptable on target devices

## Troubleshooting

For common issues and solutions, refer to the `TROUBLESHOOTING.md` file in the project root.

## Dependencies

- **AbxrLib SDK**: Core XR functionality
- **Unity XR Interaction Toolkit**: XR interaction systems
- **Unity TextMeshPro**: UI text rendering
- **Unity Input System**: Input handling