# get_gmail_credentials.py
import os
from google_auth_oauthlib.flow import InstalledAppFlow
from app.config import settings
import json

def main():
    creds_path = settings.GMAIL_CREDENTIALS_FILE
    token_path = settings.GMAIL_TOKEN_FILE
    scopes = settings.GMAIL_SCOPES

    if not os.path.exists(creds_path):
        raise SystemExit(f"No encuentro {creds_path}. Descarga tu credentials.json desde Google Cloud y colócalo ahí.")

    flow = InstalledAppFlow.from_client_secrets_file(creds_path, scopes=scopes)
    creds = flow.run_local_server(port=0)

    # guardar token (authorized user) para uso posterior
    data = {
        "token": creds.token,
        "refresh_token": creds.refresh_token,
        "token_uri": creds.token_uri,
        "client_id": creds.client_id,
        "client_secret": creds.client_secret,
        "scopes": creds.scopes,
    }
    with open(token_path, "w") as f:
        json.dump(data, f)
    print("Credenciales guardadas en", token_path)

if __name__ == "__main__":
    main()