# app/services/sender.py
import asyncio
import base64
from datetime import datetime
import json
import logging
from email.message import EmailMessage
from typing import Optional

from google.auth.transport.requests import Request as GoogleRequest
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

from app.kafka_service import KafkaLogProducer
from app.config import settings
from app.schemas.email import EmailSendRequest
from app.utils.attachments import fetch_pdf_from_storage, PDFFetchError

logger = logging.getLogger(__name__)

KAFKA_BROKER = 'localhost:9092'
KAFKA_TOPIC = 'logs-email'
kafka_producer = KafkaLogProducer(KAFKA_BROKER, KAFKA_TOPIC)

class EmailSendError(RuntimeError):
    pass

def _load_credentials_from_token(token_path: str, scopes):
    """
    Carga credenciales desde token.json. Si están caducadas intenta refresh.
    Devuelve un objeto google.oauth2.credentials.Credentials
    """
    try:
        with open(token_path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except FileNotFoundError:
        raise EmailSendError(f"No existe {token_path}. Ejecuta get_gmail_credentials.py para generar el token.")

    creds = Credentials(
        token=data.get("token"),
        refresh_token=data.get("refresh_token"),
        token_uri=data.get("token_uri"),
        client_id=data.get("client_id"),
        client_secret=data.get("client_secret"),
        scopes=scopes,
    )

    # refrescar 
    if not creds.valid and creds.refresh_token:
        try:
            creds.refresh(GoogleRequest())
            # actualizar el token.json con nuevos token/refresh si cambió
            with open(token_path, "w", encoding="utf-8") as f:
                json.dump({
                    "token": creds.token,
                    "refresh_token": creds.refresh_token,
                    "token_uri": creds.token_uri,
                    "client_id": creds.client_id,
                    "client_secret": creds.client_secret,
                    "scopes": list(creds.scopes) if creds.scopes else [],
                }, f)
        except Exception as e:
            logger.exception("No se pudo refresh token: %s", e)
            raise EmailSendError(f"No se pudo refrescar credenciales: {e}")

    if not creds.valid:
        raise EmailSendError("Credenciales inválidas. Ejecuta get_gmail_credentials.py para re-autenticar.")

    return creds

def _build_raw_message_bytes(sender: str, to: str, subject: str, body: str, attachment_bytes: bytes, attachment_filename: str) -> str:
    """
    Construye un mensaje MIME con attachment y devuelve la cadena 'raw' (base64-urlsafe).
    """
    msg = EmailMessage()
    msg["Subject"] = subject
    msg["From"] = sender
    msg["To"] = to
    msg.set_content(body)

    # Adjuntar PDF
    msg.add_attachment(attachment_bytes, maintype="application", subtype="pdf", filename=attachment_filename)

    raw_bytes = base64.urlsafe_b64encode(msg.as_bytes())
    return raw_bytes.decode("utf-8")

def _send_via_gmail_sync(sender: str, to: str, subject: str, body: str, attachment_bytes: bytes, attachment_filename: str) -> dict:
    """
    Código que crea el servicio Gmail y envía el correo.
    Se ejecuta en un executor desde el async caller.
    """
    #  cargar credenciales y refrescar si hace falta
    creds = _load_credentials_from_token(settings.GMAIL_TOKEN_FILE, settings.GMAIL_SCOPES)

    # construir el servicio Gmail (bloqueante)
    service = build("gmail", "v1", credentials=creds)

    #  construir raw message
    raw = _build_raw_message_bytes(sender, to, subject, body, attachment_bytes, attachment_filename)

    # 4) enviar
    try:
        sent = service.users().messages().send(userId="me", body={"raw": raw}).execute()
        logger.info("Correo enviado via Gmail API: %s", sent.get("id"))
        return {"ok": True, "id": sent.get("id")}
    except Exception as e:
        logger.exception("Error enviando con Gmail API: %s", e)
        raise EmailSendError(f"Error enviando email via Gmail API: {e}")

class EmailSender:
    @staticmethod
    async def send_with_pdf_from_storage(payload: EmailSendRequest) -> dict:
        #recuperar pdf
        try:
            pdf_bytes, filename = await fetch_pdf_from_storage(str(payload.correlation_id))
        except PDFFetchError as e:
            raise EmailSendError(f"Error al recuperar PDF: {e}")

        log_message = {
                    "correlationId": str(payload.correlation_id),
                    "service": "Email Server",
                    "endpoint": f"/send/api/mail/{payload.correlation_id}",
                    "timestamp": datetime.now().isoformat() + "Z",
                    "description":"El pdf se extrajo exitosamente, listo para enviar email",
                    "payload": {
                        "fileName": filename
                    },
                    "success": True
                }
        kafka_producer.send_log(log_message)

        #elegir remitente
        sender = payload.from_ or settings.GMAIL_FROM 
        to = str(payload.to)

        # enviar via Gmail API en threadpool (porque google client es sincrónico)
        loop = asyncio.get_running_loop()
        try:
            result = await loop.run_in_executor(
                None,
                _send_via_gmail_sync,
                sender,
                to,
                payload.subject,
                payload.body,
                pdf_bytes,
                filename,
            )
        except EmailSendError:
            raise
        except Exception as e:

            log_message = {
                    "correlationId": str(payload.correlation_id),
                    "service": "Email Server",
                    "endpoint": f"/api/storage/file/{payload.correlation_id}",
                    "timestamp": datetime.now().isoformat() + "Z",
                    "payload": {
                        "fileName": filename
                    },
                    "success": False
                }
            kafka_producer.send_log(log_message)

            logger.exception("Error inesperado al enviar email: %s", e)
            raise EmailSendError(f"Error inesperado al enviar email: {e}")

        return result
