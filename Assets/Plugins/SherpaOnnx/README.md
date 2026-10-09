# Sherpa ONNX managed binding

`sherpa-onnx.dll` is the unmodified .NET Standard 2.0 binding from `org.k2fsa.sherpa.onnx` 1.13.0 (NuGet), Copyright 2019-2023 Xiaomi Corporation, Apache-2.0; see LICENSE.txt.

Source: https://github.com/k2-fsa/sherpa-onnx/tree/b0592803468fa98777e328aa38136cf6ec015a05

The 50 KB binding is tracked because the Unity Sherpa package needs it at compile time. Native runtimes and voice models are provisioned by `Tools/setup_offline_guide.py` using SHA-256 verified archives.

Binding SHA-256: `097012908e4ea8ee3ce82aacdb55eda30c05ec10f50d858697f71a10e3d8f737`
