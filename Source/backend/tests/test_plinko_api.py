"""Backend API tests for Task Drop Day Plinko."""
import os
import base64
import pytest
import requests

BASE_URL = os.environ.get("EXPO_PUBLIC_BACKEND_URL", "https://daily-plinko.preview.emergentagent.com").rstrip("/")

EXPECTED_ASSETS = {
    "ball-clock",
    "badge-day-clear",
    "badge-on-fire",
    "badge-week-champion",
    "card-steady-day",
    "card-quick-win",
    "card-deep-work",
    "card-full-sprint",
}


@pytest.fixture(scope="module")
def api_client():
    s = requests.Session()
    s.headers.update({"Content-Type": "application/json"})
    return s


# ---------- root ----------
def test_root(api_client):
    r = api_client.get(f"{BASE_URL}/api/", timeout=15)
    assert r.status_code == 200, r.text
    assert r.json() == {"message": "Task Drop Day Plinko API"}


# ---------- list assets ----------
def test_list_assets(api_client):
    r = api_client.get(f"{BASE_URL}/api/assets/list", timeout=15)
    assert r.status_code == 200, r.text
    data = r.json()
    assert "assets" in data
    names = set(data["assets"])
    assert names == EXPECTED_ASSETS
    assert len(data["assets"]) == 8


# ---------- generate asset (valid) ----------
def test_generate_ball_clock(api_client):
    r = api_client.post(
        f"{BASE_URL}/api/assets/generate",
        json={"name": "ball-clock"},
        timeout=90,
    )
    assert r.status_code == 200, r.text
    j = r.json()
    assert j["name"] == "ball-clock"
    assert j["mime_type"].startswith("image/")
    assert isinstance(j["data"], str)
    # base64 long content
    assert len(j["data"]) > 500
    # validate base64
    try:
        base64.b64decode(j["data"][:1000] + "==")
    except Exception as e:
        pytest.fail(f"data is not valid base64: {e}")


# ---------- generate asset (invalid) ----------
def test_generate_invalid_asset(api_client):
    r = api_client.post(
        f"{BASE_URL}/api/assets/generate",
        json={"name": "invalid"},
        timeout=15,
    )
    assert r.status_code == 400, r.text
    j = r.json()
    assert "detail" in j


def test_generate_missing_name(api_client):
    r = api_client.post(
        f"{BASE_URL}/api/assets/generate",
        json={},
        timeout=15,
    )
    assert r.status_code == 400, r.text
