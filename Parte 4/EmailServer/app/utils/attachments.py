# app/utils/attachments.py
import io
from typing import Tuple, Optional
import httpx
from urllib.parse import unquote
from app.config import settings

class PDFFetchError(RuntimeError):
    pass

def _extract_filename_from_content_disposition(cd: Optional[str]) -> Optional[str]:
    """
    Extrae filename o filename* desde un header Content-Disposition simple.
    Soporta:
      - Content-Disposition: attachment; filename="miarchivo.pdf"
      - Content-Disposition: attachment; filename=miarchivo.pdf
      - Content-Disposition: attachment; filename*=utf-8''mi%20archivo.pdf
    Devuelve None si no se encuentra.
    """
    if not cd:
        return None
    # dividir por ; y buscar parámetros
    parts = [p.strip() for p in cd.split(';')]
    # iterar parámetros (omitimos la parte principal como 'attachment')
    for part in parts[1:]:
        if '=' not in part:
            continue
        k, v = part.split('=', 1)
        k = k.strip().lower()
        v = v.strip()
        if k == "filename*":
            # filename* puede venir como: utf-8''file%20name.pdf
            # quitamos posible prefijo de codificación y hacemos unquote
            if "''" in v:
                try:
                    # remover comillas si existen
                    if v.startswith('"') and v.endswith('"'):
                        v = v[1:-1]
                    enc_and_value = v.split("''", 1)[1]
                    return unquote(enc_and_value)
                except Exception:
                    # fallback simple
                    return unquote(v.strip('"'))
            else:
                return unquote(v.strip('"'))
        if k == "filename":
            # remover comillas si las tiene
            if v.startswith('"') and v.endswith('"'):
                v = v[1:-1]
            return v
    return None

async def fetch_pdf_from_storage(correlation_id: str, timeout: int = 30) -> Tuple[bytes, str]:
    """
    Hace una sola petición GET en streaming a STORAGE_BASE_URL/{correlation_id},
    valida status, checa cabecera PDF '%PDF-' y tamaño máximo.
    Devuelve (bytes_del_pdf, filename_guess).
    Lanza PDFFetchError en cualquier reventada.
    """
    url = f"{settings.STORAGE_BASE_URL}/{correlation_id}"
    max_bytes = settings.PDF_MAX_BYTES

    async with httpx.AsyncClient(timeout=timeout) as client:
        try:
            async with client.stream("GET", url, timeout=timeout) as resp:
                if resp.status_code != 200:
                    raise PDFFetchError(f"Storage returned status {resp.status_code}")

                # Intentar extraer filename desde content-disposition
                cd = resp.headers.get("content-disposition")
                filename = _extract_filename_from_content_disposition(cd)

                buf = io.BytesIO()
                total = 0
                header_checked = False

                async for chunk in resp.aiter_bytes():
                    if not chunk:
                        continue
                    buf.write(chunk)
                    total += len(chunk)

                    # si se tiene porlomenos 5 bytes comprobamos la cabecera PDF
                    if (not header_checked) and buf.getbuffer().nbytes >= 5:
                        start = buf.getvalue()[:5]
                        if not start.startswith(b"%PDF-"):
                            raise PDFFetchError("Contenido recuperado no parece un PDF (cabecera inválida)")
                        header_checked = True

                    if total > max_bytes:
                        raise PDFFetchError(f"Archivo excede tamaño máximo permitido ({max_bytes} bytes)")

                data = buf.getvalue()

                # si no se puede chepiar la cabecera
                if not header_checked:
                    if len(data) < 5 or not data.startswith(b"%PDF-"):
                        raise PDFFetchError("Contenido recuperado no parece un PDF (archivo demasiado pequeño o cabecera faltante)")

                return data, filename or f"{correlation_id}.pdf"

        except httpx.ReadTimeout:
            raise PDFFetchError("Timeout al conectar con storage")
        except PDFFetchError:
            raise
        except Exception as e:
            raise PDFFetchError(f"Error al recuperar PDF: {e}")