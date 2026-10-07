"""Version 1: PNG bytes on stdin, Markdown on stdout, diagnostics on stderr."""
import io
import sys

try:
    from PIL import Image
    from pix2text import Pix2Text

    image = Image.open(io.BytesIO(sys.stdin.buffer.read())).convert("RGB")
    result = Pix2Text.from_config().recognize(
        image, file_type="text_formula", return_text=True, save_debug_res=None
    )
    print(result if isinstance(result, str) else str(result))
except Exception as exc:
    print(f"OCR setup or recognition failed: {exc}. Run: python -m pip install pix2text", file=sys.stderr)
    raise SystemExit(1)
