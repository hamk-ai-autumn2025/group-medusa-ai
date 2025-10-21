#!/usr/bin/env python3
import argparse
import os
import sys
import time
from pathlib import Path
import tempfile

import numpy as np
import sounddevice as sd
import soundfile as sf
import simpleaudio as sa
from openai import OpenAI

# To use:
# pip install --upgrade openai sounddevice soundfile simpleaudio numpy

def record_wav(path: Path, duration_s: float, samplerate: int = 16000):
    """Record mono audio for duration_s seconds to a WAV file."""
    channels = 1
    print(f"[rec] Recording {duration_s:.2f}s @ {samplerate} Hz…", file=sys.stderr)
    audio = sd.rec(int(duration_s * samplerate), samplerate=samplerate, channels=channels, dtype="float32")
    sd.wait()
    # Scale to int16 for compact WAV + compatibility
    audio_i16 = np.int16(np.clip(audio, -1.0, 1.0) * 32767)
    sf.write(str(path), audio_i16, samplerate, subtype="PCM_16")
    print(f"[rec] Saved: {path}", file=sys.stderr)


def transcribe_whisper(client: OpenAI, wav_path: Path) -> str:
    """OpenAI Whisper-1 transcription -> text string."""
    with wav_path.open("rb") as f:
        resp = client.audio.transcriptions.create(
            model="whisper-1",
            file=f,
            response_format="text",
            # language can be auto; you can set e.g. "en" to force
        )
    return resp.strip()


def translate_text(client: OpenAI, text: str, target_lang: str) -> str:
    """Translate using GPT-4o-mini with tight instructions."""
    sys_msg = (
        "You translate text. Output only the translation with no extra words. "
        f"Target language: {target_lang}."
    )
    resp = client.chat.completions.create(
        model="gpt-4o-mini",
        temperature=0.0,
        messages=[
            {"role": "system", "content": sys_msg},
            {"role": "user", "content": text},
        ],
    )
    return resp.choices[0].message.content.strip()


def tts_speak_wav(client, text: str, out_path, voice: str = "alloy"):
    """
    Generate speech as WAV and save to disk, then play it.
    Uses the streaming helper (no .read()).
    """
    import simpleaudio as sa
    import time

    # Write audio directly to file
    with client.audio.speech.with_streaming_response.create(
        model="tts-1",            # or "gpt-4o-mini-tts" if you prefer
        voice=voice,
        input=text,
        response_format="wav",    # <-- key change (was 'format')
    ) as resp:
        resp.stream_to_file(str(out_path))

    # Play and time playback
    wave_obj = sa.WaveObject.from_wave_file(str(out_path))
    play_obj = wave_obj.play()
    t0 = time.perf_counter()
    play_obj.wait_done()
    return time.perf_counter() - t0


def main():
    parser = argparse.ArgumentParser(description="Voice → STT → Translate → TTS interpreter (OpenAI).")
    parser.add_argument("--duration", type=float, default=5.0, help="Recording duration in seconds (default: 5).")
    parser.add_argument("--to", default="fr", help="Target language (e.g., fr, de, es, fi, ja). Default: fr.")
    parser.add_argument("--voice", default="alloy", help="OpenAI TTS voice (e.g., alloy, verse, coral…).")
    parser.add_argument("--keep", action="store_true", help="Keep temp files (input.wav, spoken.wav).")
    args = parser.parse_args()

    if not os.getenv("OPENAI_API_KEY"):
        print("ERROR: OPENAI_API_KEY not set.", file=sys.stderr)
        sys.exit(2)

    client = OpenAI()
    tmpdir = Path(tempfile.mkdtemp(prefix="interp_"))
    in_wav = tmpdir / "input.wav"
    out_wav = tmpdir / "spoken.wav"

    # --- Record ---
    record_wav(in_wav, args.duration)

    # --- STT ---
    t0 = time.perf_counter()
    transcript = transcribe_whisper(client, in_wav)
    t_stt = time.perf_counter() - t0
    print(f"[transcript] {transcript}")

    # --- Translate ---
    t1 = time.perf_counter()
    translated = translate_text(client, transcript, args.to)
    t_translate = time.perf_counter() - t1
    print(f"[translated→{args.to}] {translated}")

    # --- TTS & playback ---
    t2 = time.perf_counter()
    speak_time = tts_speak_wav(client, translated, out_wav, voice=args.voice)
    t_tts_api = time.perf_counter() - t2  # API+IO+playback wait measured separately below

    # --- Latency report ---
    # Two views:
    # 1) Processing latency (excluding recording time): STT + Translate + TTS request time (not including playback)
    # 2) End-to-end (record stop → playback start) is approximately STT + Translate + TTS request setup;
    #    here we report measured components so you can reason about it.
    print("\n[latency]")
    print(f"  STT (Whisper-1):   {t_stt:.2f}s")
    print(f"  Translate (GPT):   {t_translate:.2f}s")
    print(f"  TTS gen+IO:        {t_tts_api:.2f}s (excludes playback time)")
    print(f"  Playback duration: {speak_time:.2f}s")
    total_processing = t_stt + t_translate + t_tts_api
    print(f"  ≈ Pipeline delay (excl. recording & playback): {total_processing:.2f}s")

    if args.keep:
        print(f"\n[files] kept at: {tmpdir}")
        print(f"  input wav:  {in_wav}")
        print(f"  spoken wav: {out_wav}")
    else:
        try:
            in_wav.unlink(missing_ok=True)
            out_wav.unlink(missing_ok=True)
            tmpdir.rmdir()
        except Exception:
            pass


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("Interrupted.", file=sys.stderr)
        sys.exit(130)
