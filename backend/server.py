from fastapi import FastAPI, APIRouter, HTTPException
from dotenv import load_dotenv
from starlette.middleware.cors import CORSMiddleware
from motor.motor_asyncio import AsyncIOMotorClient
import os
import logging
import uuid
from pathlib import Path
from pydantic import BaseModel
from typing import Optional

from emergentintegrations.llm.chat import LlmChat, UserMessage


ROOT_DIR = Path(__file__).parent
load_dotenv(ROOT_DIR / '.env')

# MongoDB connection
mongo_url = os.environ['MONGO_URL']
client = AsyncIOMotorClient(mongo_url)
db = client[os.environ['DB_NAME']]

# Emergent LLM key for Gemini Nano Banana image generation
EMERGENT_LLM_KEY = os.environ.get("EMERGENT_LLM_KEY")

# Create the main app without a prefix
app = FastAPI()

# Create a router with the /api prefix
api_router = APIRouter(prefix="/api")


# ---------- Plinko asset generation ----------
ASSET_PROMPTS = {
    "ball-clock": (
        "Bright cartoon flat art glowing sphere with a pocket-watch clock face inside, "
        "white glow, dark blue rim light, ticking minute and hour hands at 10:10, "
        "centered on a fully transparent background, no shadow on ground, "
        "high contrast playful arcade style, single icon, PNG with transparent background"
    ),
    "badge-day-clear": (
        "Bright cartoon flat art golden medal badge with a big check-mark and rays of light, "
        "blue and gold colors, glossy plastic look, transparent background PNG, single icon, "
        "playful arcade casino style"
    ),
    "badge-on-fire": (
        "Bright cartoon flat art flaming fire badge with the number 3 stylised in the middle, "
        "orange and red flames with glow, transparent background PNG, single icon, "
        "playful arcade casino style"
    ),
    "badge-week-champion": (
        "Bright cartoon flat art golden trophy cup badge with a star on top and small laurels, "
        "yellow gold and blue ribbon, transparent background PNG, single icon, "
        "playful arcade casino style"
    ),
    "card-steady-day": (
        "Bright cartoon flat art illustration of a calm blue mountain with a clock at 12, "
        "balanced and steady mood, transparent background PNG, square illustration, "
        "playful arcade casino style"
    ),
    "card-quick-win": (
        "Bright cartoon flat art illustration of green lightning bolt with small check marks, "
        "fast energetic mood, transparent background PNG, square illustration, "
        "playful arcade casino style"
    ),
    "card-deep-work": (
        "Bright cartoon flat art illustration of yellow brain with concentric focus rings and "
        "a small clock showing 90 minutes, calm but focused mood, transparent background PNG, "
        "square illustration, playful arcade casino style"
    ),
    "card-full-sprint": (
        "Bright cartoon flat art illustration of a red running figure with a flaming clock "
        "reading 6:00, urgent and dramatic mood, transparent background PNG, square "
        "illustration, playful arcade casino style"
    ),
}


class AssetResponse(BaseModel):
    name: str
    mime_type: str
    data: str  # base64-encoded PNG


@api_router.get("/")
async def root():
    return {"message": "Task Drop Day Plinko API"}


@api_router.get("/assets/list")
async def list_assets():
    return {"assets": list(ASSET_PROMPTS.keys())}


@api_router.post("/assets/generate", response_model=AssetResponse)
async def generate_asset(payload: dict):
    name = (payload or {}).get("name")
    if not name or name not in ASSET_PROMPTS:
        raise HTTPException(status_code=400, detail=f"Unknown asset name: {name}")

    if not EMERGENT_LLM_KEY:
        raise HTTPException(status_code=500, detail="EMERGENT_LLM_KEY not configured")

    prompt = ASSET_PROMPTS[name]
    session_id = f"plinko-asset-{name}-{uuid.uuid4()}"
    chat = (
        LlmChat(
            api_key=EMERGENT_LLM_KEY,
            session_id=session_id,
            system_message="You generate transparent PNG illustrations for a mobile game.",
        )
        .with_model("gemini", "gemini-3.1-flash-image-preview")
        .with_params(modalities=["image", "text"])
    )

    try:
        _, images = await chat.send_message_multimodal_response(UserMessage(text=prompt))
    except Exception as e:
        logger.exception("asset generation failed")
        raise HTTPException(status_code=502, detail=f"image generation error: {e}")

    if not images:
        raise HTTPException(status_code=502, detail="no image returned")

    img = images[0]
    return AssetResponse(name=name, mime_type=img.get("mime_type", "image/png"), data=img["data"])


# Include the router in the main app
app.include_router(api_router)

app.add_middleware(
    CORSMiddleware,
    allow_credentials=True,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
)
logger = logging.getLogger(__name__)


@app.on_event("shutdown")
async def shutdown_db_client():
    client.close()
