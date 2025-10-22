# app/routers/test_fetch.py
from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse
import io
from app.utils.attachments import fetch_pdf_from_storage, PDFFetchError

router = APIRouter(prefix="/api/test")

@router.get("/fetch_pdf_info/{correlation_id}")
async def fetch_pdf_info(correlation_id: str):
    try:
        data, filename = await fetch_pdf_from_storage(correlation_id)
    except PDFFetchError as e:
        raise HTTPException(status_code=500, detail=str(e))
    return {"ok": True, "filename": filename, "size": len(data), "starts_with_pdf": data.startswith(b"%PDF-")}

@router.get("/fetch_pdf_stream/{correlation_id}")
async def fetch_pdf_stream(correlation_id: str):
    try:
        data, filename = await fetch_pdf_from_storage(correlation_id)
    except PDFFetchError as e:
        raise HTTPException(status_code=500, detail=str(e))
    bio = io.BytesIO(data)
    headers = {"Content-Disposition": f'attachment; filename="{filename}"'}
    return StreamingResponse(bio, media_type="application/pdf", headers=headers)