# app/routers/email.py
from fastapi import APIRouter, status, HTTPException
from app.schemas.email import EmailSendRequest
from app.services.sender import EmailSender, EmailSendError

router = APIRouter(prefix="/api/email")

@router.post("/send", status_code=status.HTTP_202_ACCEPTED)
async def send_email(payload: EmailSendRequest):
    """
    Endpoint:
    Recupera PDF del storage usando correlation_id
    Valida el PDF
    Envía el correo con el PDF adjunto
    Respuestas:
    202 Accepted si todo ok (email enviado)
    400/500 si algo reviennta
    """
    try:
        result = await EmailSender.send_with_pdf_from_storage(payload)


    except EmailSendError as e:
        # errores (recuperacion o envio)
        raise HTTPException(status_code=500, detail=str(e))
    return {"status": "sent", "correlation_id": str(payload.correlation_id)}