#!/usr/bin/env python3
import os
import time
from typing import List, Tuple, Generator, Optional

import gradio as gr

# --- OpenAI (required) ---
try:
    from openai import OpenAI
except Exception:
    OpenAI = None

# --- Replicate (optional) ---
try:
    import replicate
except Exception:
    replicate = None


SYSTEM_PROMPT = (
    "You are a helpful, concise assistant. Avoid fluff. "
    "When code is requested, produce minimal, correct snippets."
)

# Build available model choices based on env/packages
MODEL_CHOICES = []
if OpenAI is not None and os.getenv("OPENAI_API_KEY"):
    MODEL_CHOICES.append("OpenAI · gpt-4o-mini")  # default
    # You can add more OpenAI models if you like:
    # MODEL_CHOICES.append("OpenAI · gpt-4o")
if replicate is not None and os.getenv("REPLICATE_API_TOKEN"):
    MODEL_CHOICES.append("Replicate · meta/llama-3.1-405b-instruct")

if not MODEL_CHOICES:
    raise SystemExit("No backends available. Set OPENAI_API_KEY (and optionally REPLICATE_API_TOKEN).")


# --------------- Backends ---------------

def stream_openai_reply(
    history: List[Tuple[str, str]],
    temperature: float,
    max_tokens: int,
) -> Generator[List[Tuple[str, str]], None, None]:
    """Stream a reply from OpenAI Chat Completions."""
    client = OpenAI()
    # Convert history to OpenAI messages
    messages = [{"role": "system", "content": SYSTEM_PROMPT}]
    for user_msg, assistant_msg in history[:-1]:
        messages.append({"role": "user", "content": user_msg})
        if assistant_msg:
            messages.append({"role": "assistant", "content": assistant_msg})
    # last user turn
    messages.append({"role": "user", "content": history[-1][0]})

    # Stream chunks
    stream = client.chat.completions.create(
        model="gpt-4o-mini",
        temperature=float(temperature),
        max_tokens=int(max_tokens) if max_tokens > 0 else None,
        stream=True,
        messages=messages,
    )

    partial = ""
    for chunk in stream:
        delta = chunk.choices[0].delta.content or ""
        if delta:
            partial += delta
            # yield updated history (update last assistant message)
            new_hist = history[:-1] + [(history[-1][0], partial)]
            yield new_hist
    # final yield to ensure completion shows
    yield history[:-1] + [(history[-1][0], partial)]


def stream_replicate_reply(
    history: List[Tuple[str, str]],
    temperature: float,
    max_tokens: int,
) -> Generator[List[Tuple[str, str]], None, None]:
    """Stream-ish reply from Replicate (Llama-3). Falls back to chunked appends."""
    token = os.getenv("REPLICATE_API_TOKEN")
    if not token:
        # produce an immediate error message in the chat
        err = "Replicate token missing. Set REPLICATE_API_TOKEN."
        yield history[:-1] + [(history[-1][0], err)]
        return

    client = replicate.Client(api_token=token)

    # Build a simple chat-style prompt from history
    convo = [f"System: {SYSTEM_PROMPT}"]
    for u, a in history[:-1]:
        convo.append(f"User: {u}")
        if a:
            convo.append(f"Assistant: {a}")
    convo.append(f"User: {history[-1][0]}")
    prompt = "\n".join(convo) + "\nAssistant:"

    # Many Replicate text models accept "prompt" and return an iterator of tokens
    # meta/llama-3-70b-instruct
    model = "meta/meta-llama-3.1-405b-instruct"
    inputs = {
        "prompt": prompt,
        "temperature": float(temperature),
        "max_tokens": int(max_tokens) if max_tokens > 0 else 512,
    }

    partial = ""
    try:
        # replicate.run usually yields chunks (strings). We append as they arrive.
        for chunk in client.run(model, input=inputs):
            if not isinstance(chunk, str):
                continue
            partial += chunk
            yield history[:-1] + [(history[-1][0], partial)]
            # tiny sleep avoids flooding the UI
            time.sleep(0.02)
    except Exception as e:
        yield history[:-1] + [(history[-1][0], f"[Replicate error] {e}")]
        return

    yield history[:-1] + [(history[-1][0], partial)]


# --------------- Gradio wiring ---------------

def user_submit(message: str, chat_history: List[Tuple[str, str]]):
    # Add user turn; assistant placeholder None
    chat_history = (chat_history or []) + [(message, None)]
    return "", chat_history

def bot_reply(
    chat_history: List[Tuple[str, str]],
    model_choice: str,
    temperature: float,
    max_tokens: int,
):
    if not chat_history:
        return chat_history
    try:
        if model_choice.startswith("OpenAI"):
            yield from stream_openai_reply(chat_history, temperature, max_tokens)
        elif model_choice.startswith("Replicate"):
            yield from stream_replicate_reply(chat_history, temperature, max_tokens)
        else:
            # Fallback: just echo an error msg
            yield chat_history[:-1] + [(chat_history[-1][0], f"Unknown model: {model_choice}")]
    except Exception as e:
        yield chat_history[:-1] + [(chat_history[-1][0], f"[Error] {e}")]


with gr.Blocks(title="Multi-LLM Chat") as demo:
    gr.Markdown(
        "### Multi-LLM Chat\n"
        "Pick a backend, then chat. Supports **OpenAI** and (if configured) **Replicate/Llama-3**."
    )
    with gr.Row():
        model_dd = gr.Dropdown(
            choices=MODEL_CHOICES,
            value=MODEL_CHOICES[0],
            label="Model",
            interactive=True,
        )
        temp = gr.Slider(0.0, 1.5, value=0.3, step=0.1, label="Temperature")
        max_tok = gr.Slider(0, 4096, value=512, step=64, label="Max tokens (0 = model default)")
    chatbot = gr.Chatbot(height=480, label="Conversation")
    with gr.Row():
        msg = gr.Textbox(placeholder="Type your message…", scale=5)
        send = gr.Button("Send", variant="primary")
    clear = gr.Button("Clear chat")

    # Events
    send.click(user_submit, [msg, chatbot], [msg, chatbot], queue=False) \
        .then(bot_reply, [chatbot, model_dd, temp, max_tok], chatbot)
    msg.submit(user_submit, [msg, chatbot], [msg, chatbot], queue=False) \
        .then(bot_reply, [chatbot, model_dd, temp, max_tok], chatbot)
    clear.click(lambda: None, None, chatbot, queue=False)

if __name__ == "__main__":
    # Launch on localhost; set share=True if you want a public link
    demo.launch()
