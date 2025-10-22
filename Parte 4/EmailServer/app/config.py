from typing import List
from pydantic_settings import BaseSettings
from pydantic import AnyHttpUrl, EmailStr

class Settings(BaseSettings):
    STORAGE_BASE_URL: AnyHttpUrl = "http://127.0.0.1:8000/api/storage/file"
    PDF_MAX_BYTES: int = 10 * 1024 * 1024  # 10 MB


    GMAIL_CREDENTIALS_FILE: str = "credentials.json"   # descargar desde Google Cloud
    GMAIL_TOKEN_FILE: str = "token.json"               # generado por get_gmail_credentials.py
    GMAIL_SCOPES: List[str] = ["https://www.googleapis.com/auth/gmail.send"]
    GMAIL_FROM: EmailStr = "pablo18jru@gmail.com"       # cuenta remitente permitida

    model_config = {
        "env_file": ".env",   # pydantic-settings usa model_config para opciones
    }

settings = Settings()