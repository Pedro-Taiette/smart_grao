import io

import numpy as np
import pytest
from PIL import Image

from smartgrao_analytics import vision


def model_available():
    try:
        vision.load()
        return True
    except (FileNotFoundError, OSError):
        return False


pytestmark = pytest.mark.skipif(not model_available(), reason="sem modelo ONNX em analytics/models")


def leaf_png():
    rng = np.random.default_rng(0)
    pixels = np.clip(rng.normal([60, 140, 50], 20, (300, 300, 3)), 0, 255).astype(np.uint8)
    buffer = io.BytesIO()
    Image.fromarray(pixels).save(buffer, format="PNG")
    return buffer.getvalue()


def test_probabilities_form_a_distribution():
    result = vision.classify(leaf_png(), with_heatmap=False)
    assert set(result["probabilities"]) == {"cercosporiose", "saudavel", "outras_manchas"}
    assert abs(sum(result["probabilities"].values()) - 1) < 1e-4
    assert result["label"] == max(result["probabilities"], key=result["probabilities"].get)


def test_heatmap_is_a_png():
    result = vision.classify(leaf_png())
    import base64
    assert base64.b64decode(result["heatmap_png"])[:8] == b"\x89PNG\r\n\x1a\n"


def test_exif_rotation_is_applied():
    rotated = Image.open(io.BytesIO(leaf_png()))
    exif = rotated.getexif()
    exif[0x0112] = 6
    buffer = io.BytesIO()
    rotated.convert("RGB").save(buffer, format="JPEG", exif=exif)
    assert vision.classify(buffer.getvalue(), with_heatmap=False)["label"] in {"cercosporiose", "saudavel", "outras_manchas"}


def test_model_card_brings_the_out_of_domain_test():
    card = vision.model_card()
    assert card["out_of_domain"]["holdout"] == "craze"
    assert card["out_of_domain"]["test"]["n"] > 0
