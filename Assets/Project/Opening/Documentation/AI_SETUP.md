# Standalone spoken guide

This prototype runs speech recognition, question routing and speech synthesis locally. No API key, PC server, or microphone upload is used. Internet is needed on the development machine to provision packages/models; the finished app is intended to work offline.

## Use

After the opening instructions finish, hold **B on the right Quest controller**, ask a question, and release B. Recording ends after 12 seconds. In desktop preview, hold **V**. All spoken and visual guidance teaches Quest controls, including during keyboard testing. Allow the microphone permission on first use. The panel shows listening/thinking/reply status. Ask about movement, turning, where to go, the elevator, or your progress.

The small local model classifies the question. Replies use authored controller facts and measured scene progress; generated controller instructions are deliberately not spoken. Automatic tutorial narration uses exact authored text through offline TTS. If initialization or narration fails, existing prerecorded clips still run. Questions require all local models to be ready.

## Reproduce on a new checkout

1. Close Unity and run `python3 Tools/setup_offline_guide.py` from this repository. Python 3.10+ and curl are required. All downloaded model/speech archives have pinned SHA-256 checks. Models and native dependencies are ignored by Git; the small Sherpa C# binding is tracked so a fresh clone compiles before provisioning.
2. Open with Unity 6000.6.0f1. LLMUnity downloads its own LlamaLib 2.0.5 native archive on first import (about 1.8 GB across platforms). Wait for import to complete.
3. Run **Tools > Opening Scene > Configure Standalone AI**. This registers the model, sets native importers and wires the scene without duplicating the guide.
4. Use the opening scene. For an Android build install Unity Android Build Support, SDK/NDK and OpenJDK through Unity Hub. Then choose **Tools > Opening Scene > Build Quest APK** to build the opening and lower-floor scenes to `Builds/OpeningQuest.apk`. The project uses IL2CPP/ARM64. The custom Android manifest requests microphone access. Sherpa's build hook generates its StreamingAssets file manifest; LLMUnity bundles the model with download-on-start disabled.

The provisioning script supports Apple Silicon editor and Quest ARM64. For a Windows editor install matching Sherpa 1.13.0 Windows x64 native libraries through the package's installer before using voice in that editor. Do not enable Android native libraries in the editor.

LLMUnity installs platform runtimes under the generated `Assets/StreamingAssets/LlamaLib-v2.0.5` directory. Let its first-import setup finish before running the scene. Do not commit generated runtime binaries.

## Dependencies and attribution

- [LLMUnity 3.0.3](https://github.com/undreamai/LLMUnity), MIT; Git dependency pinned by commit.
- [Qwen2.5-0.5B-Instruct GGUF](https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF), Q4_K_M, Apache 2.0. Approximately 491 MB. This model was insufficiently reliable for free-form control advice, hence bounded answer selection.
- [Unity Sherpa ONNX](https://github.com/Ponyu-dev/Unity-Sherpa-ONNX) with [sherpa-onnx 1.13.0](https://github.com/k2-fsa/sherpa-onnx), pinned package commit and native binaries.
- Whisper tiny.en ONNX for English speech recognition. [Model releases](https://github.com/k2-fsa/sherpa-onnx/releases/tag/asr-models).
- Piper en_US-lessac-low for English speech output. Keep the downloaded `MODEL_CARD` with the voice; it links the source voice/dataset terms. [Voice releases](https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models).
- UniTask pinned to a Unity 6.6-compatible commit.

## Validation boundary

Editor tests can verify model loading, synthetic speech transcription, routing, synthesized responses, and scene progression. They do not establish Quest microphone accuracy, controller B behavior, headset performance, comfort or Android packaging. On-device acceptance remains required: install APK, disable Wi-Fi, deny/allow microphone, ask all supported questions, test silence/12-second recording/pause-resume, and measure frame rate and response latency during locomotion.

No Android Build Support module is installed on this Mac at the time of integration, so no APK has been built or tested here.
