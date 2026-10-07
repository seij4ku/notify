"""Run once while online to install and cache Pix2Text's model assets."""
from pix2text import Pix2Text

Pix2Text.from_config()
print("Pix2Text models are ready for offline use.")
