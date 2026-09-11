import os

# Directory containing model.joblib + metadata.json for the production artifact.
# Unset/empty means "no model configured" — /predict keeps returning 503.
MODEL_DIR = os.environ.get("MAPAN_ML_MODEL_DIR") or None
