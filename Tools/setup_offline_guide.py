#!/usr/bin/env python3
"""Provision pinned offline guide models and Sherpa libraries before opening Unity.
Requires Python 3.10+ and curl. Downloads are local; no account or API key.
LLMUnity provisions its own LlamaLib runtime when Unity first imports the package.
"""
import hashlib
import pathlib
import shutil
import subprocess
import tarfile
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
CACHE = ROOT / '.offline-guide-cache'
STREAM = ROOT / 'Assets/StreamingAssets'
NATIVE = ROOT / 'Assets/Plugins/SherpaOnnx'
FILES = {
    'qwen2.5-0.5b-instruct-q4_k_m.gguf': (
        'https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/qwen2.5-0.5b-instruct-q4_k_m.gguf',
        '74a4da8c9fdbcd15bd1f6d01d621410d31c6fc00986f5eb687824e7b93d7a9db'),
    'asr.tar.bz2': (
        'https://github.com/k2-fsa/sherpa-onnx/releases/download/asr-models/sherpa-onnx-whisper-tiny.en.tar.bz2',
        '2bd6cf965c8bb3e068ef9fa2191387ee63a9dfa2a4e37582a8109641c20005dd'),
    'tts.tar.bz2': (
        'https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/vits-piper-en_US-lessac-low.tar.bz2',
        '8fb427b8637334072ee5723d72fa418c45bfdd4b7deebeacdf2938662618c1cb'),
    'managed.zip': (
        'https://api.nuget.org/v3-flatcontainer/org.k2fsa.sherpa.onnx/1.13.0/org.k2fsa.sherpa.onnx.1.13.0.nupkg',
        '26329013154699556498cb345eb959d3d4de04d5cf0dd14d4423a78db3be9501'),
    'mac.zip': (
        'https://api.nuget.org/v3-flatcontainer/org.k2fsa.sherpa.onnx.runtime.osx-arm64/1.13.0/org.k2fsa.sherpa.onnx.runtime.osx-arm64.1.13.0.nupkg',
        'f9b18d3938f7ef2e0610b532deeef272886e7325621c17f17d594acd4a045389'),
    'android.tar.bz2': (
        'https://github.com/k2-fsa/sherpa-onnx/releases/download/v1.13.0/sherpa-onnx-v1.13.0-android.tar.bz2',
        'd2268fd5baf4cce691d0008a3dd8ed988b4375e3435e3af9eb2be372dffd9321'),
}


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def download(name, url, sha):
    path = CACHE / name
    if path.exists() and digest(path) == sha:
        return path
    part = path.with_name(path.name + '.partial')
    print('Downloading', name, flush=True)
    subprocess.run(['curl', '--fail', '--location', '--retry', '3', url, '--output', str(part)], check=True)
    if digest(part) != sha:
        raise RuntimeError(f'Checksum mismatch: {name}. Nothing installed from this file.')
    part.replace(path)
    return path


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)


def extract_model(archive, destination):
    # Extract regular files only. Reject path traversal and omit unused test/quantized files.
    with tarfile.open(archive) as tar:
        for entry in tar.getmembers():
            if not entry.isfile():
                continue
            relative = pathlib.PurePosixPath(entry.name)
            if relative.is_absolute() or '..' in relative.parts:
                raise RuntimeError('Unsafe archive member: ' + entry.name)
            if 'test_wavs' in relative.parts or '.int8.' in entry.name:
                continue
            write(destination / relative, tar.extractfile(entry).read())


def main():
    CACHE.mkdir(exist_ok=True)
    paths = {name: download(name, *details) for name, details in FILES.items()}
    model = 'qwen2.5-0.5b-instruct-q4_k_m.gguf'
    STREAM.mkdir(parents=True, exist_ok=True)
    if not (STREAM / model).exists() or digest(STREAM / model) != FILES[model][1]:
        shutil.copyfile(paths[model], STREAM / model)
    extract_model(paths['asr.tar.bz2'], STREAM / 'SherpaOnnx/asr-models')
    extract_model(paths['tts.tar.bz2'], STREAM / 'SherpaOnnx/tts-models')
    with zipfile.ZipFile(paths['managed.zip']) as archive:
        write(NATIVE / 'sherpa-onnx.dll', archive.read('lib/netstandard2.0/sherpa-onnx.dll'))
    with zipfile.ZipFile(paths['mac.zip']) as archive:
        for name in archive.namelist():
            if name.endswith('.dylib') and name.startswith('runtimes/osx-arm64/native/'):
                write(NATIVE / 'osx-arm64' / pathlib.PurePosixPath(name).name, archive.read(name))
    with tarfile.open(paths['android.tar.bz2']) as archive:
        for entry in archive.getmembers():
            if entry.isfile() and '/arm64-v8a/' in entry.name and entry.name.endswith('.so'):
                write(NATIVE / 'arm64-v8a' / pathlib.PurePosixPath(entry.name).name, archive.extractfile(entry).read())
    print('Models and Sherpa libraries verified and installed for Quest ARM64 and Apple Silicon editor.')
    print('Open Unity; allow LLMUnity to finish its native-runtime setup. Then run Tools > Opening Scene > Configure Standalone AI.')


if __name__ == '__main__':
    main()
