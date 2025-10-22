# app/schemas/email.py
from pydantic import BaseModel, EmailStr, Field, validator
from typing import List, Optional, Dict
from uuid import UUID
from datetime import datetime

class Attachment(BaseModel):
    filename: str
    content_base64: Optional[str] = None
    url: Optional[str] = None
    content_type: Optional[str] = None
    size: Optional[int] = None

    @validator("url", always=True)
    def one_of_content(cls, v, values):
        if not (v or values.get("content_base64")):
            raise ValueError("Attachment must have either content_base64 or url")
        return v

class EmailSendRequest(BaseModel):
    correlation_id: UUID
    to: EmailStr
    subject: str
    body: str
    body_type: Optional[str] = Field("text")
    from_: Optional[EmailStr] = Field(None, alias="from")
    cc: Optional[List[EmailStr]] = None
    bcc: Optional[List[EmailStr]] = None
    reply_to: Optional[EmailStr] = None
    attachments: Optional[List[Attachment]] = None
    send_at: Optional[datetime] = None
    priority: Optional[str] = Field("normal")
    metadata: Optional[Dict[str, object]] = None
    tags: Optional[List[str]] = None

    # configuración compatible con Pydantic v2
    model_config = {
        "populate_by_name": True,   # permite llenar usando el nombre del campo ("from")
    }
